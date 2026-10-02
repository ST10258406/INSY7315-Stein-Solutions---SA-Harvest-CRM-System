namespace CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonorDocument;

using System.Text.Json;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Files;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.PublicDonors.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;

public class SubmitPublicDonorDocumentCommandHandler
    : IRequestHandler<SubmitPublicDonorDocumentCommand, SubmitPublicDonorDocumentResponseDto>
{
    private const string SuccessMessage = "Document uploaded successfully.";

    private readonly IDonorRepository _donors;
    private readonly IDonorDocumentRepository _documents;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<SubmitPublicDonorDocumentCommandHandler> _logger;

    public SubmitPublicDonorDocumentCommandHandler(
        IDonorRepository donors,
        IDonorDocumentRepository documents,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork,
        IBlobStorageService blobStorage,
        ILogger<SubmitPublicDonorDocumentCommandHandler> logger)
    {
        _donors = donors;
        _documents = documents;
        _auditLogs = auditLogs;
        _unitOfWork = unitOfWork;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task<SubmitPublicDonorDocumentResponseDto> Handle(
        SubmitPublicDonorDocumentCommand command, CancellationToken cancellationToken)
    {
        // Atomically claims the token (nulls it in the same DB statement that
        // reads it) and returns the donor Id, or null if no donor has that token
        // unexpired. A token that doesn't match, has expired, or was already
        // consumed by a prior successful call is indistinguishable here from an
        // outright invalid token — all collapse to the same generic 400, same
        // discipline as ResetPasswordCommandHandler's token check. See
        // IDonorRepository.ClaimBySubmissionTokenAsync's remarks for why this
        // must be a single atomic claim rather than a separate read-then-null.
        var donorId = await _donors.ClaimBySubmissionTokenAsync(command.SessionToken, cancellationToken);

        if (donorId is null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("SessionToken", "Session token expired or invalid.")
            });
        }

        // documentType is already constrained to "BBBEECertificate" by
        // SubmitPublicDonorDocumentCommandValidator — this endpoint accepts no
        // other value, so no Enum.TryParse fallback is needed here.
        const DocumentType documentType = DocumentType.BBBEECertificate;

        // The token is already claimed/burned at this point — it cannot be
        // un-burned if anything below fails. That's a deliberate trade-off: a
        // stranded donor (rare — a transient blob/DB failure) has to restart the
        // two-step flow with a fresh token from a new /submit call, which is far
        // preferable to leaving any window where the same token could be
        // replayed. Logged loudly here specifically so an operator can see when
        // that trade-off actually bit someone.
        try
        {
            // Upload before inserting the DonorDocument row — if the upload fails,
            // we don't want a DB record pointing at a blob that was never written.
            // An orphaned blob from a subsequent failed SaveChanges is an accepted,
            // cheap failure mode (mirrors UploadDonorDocumentCommandHandler).
            // Blob name and content type come from the sniffed file type, never from
            // client-supplied values (see UploadDonorDocumentCommandHandler).
            var sniffed = UploadedFileInspector.Sniff(command.FileStream)
                ?? throw new InvalidOperationException("Upload reached the handler without a recognised file signature.");
            var blobPath = $"donors/{donorId}/{documentType}/{Guid.NewGuid()}{sniffed.Extension}";
            await _blobStorage.UploadAsync(command.FileStream, blobPath, sniffed.MimeType);

            var document = new DonorDocument
            {
                Id = Guid.NewGuid(),
                DonorId = donorId.Value,
                DocumentType = documentType,
                FileName = UploadedFileInspector.SanitizeDisplayName(command.OriginalFileName),
                BlobStoragePath = blobPath,
                FileSizeBytes = UploadedFileInspector.RealLength(command.FileStream, command.FileSizeBytes),
                MimeType = sniffed.MimeType,
                // Null, not the system user — DonorDocument.UploadedByUserId is
                // nullable specifically for an anonymous uploader like this one.
                UploadedByUserId = null,
                IsActive = true
            };
            await _documents.AddAsync(document, cancellationToken);

            // Manual audit_logs write — this command deliberately does not
            // implement IAuditableCommand (see SubmitPublicDonorDocumentCommand's
            // remarks), so AuditBehaviour never runs for it. The donor id is
            // recorded here (server side only) but never appears anywhere in the
            // response sent back to the public client.
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = null,
                EntityType = nameof(DonorDocument),
                EntityId = document.Id,
                Action = AuditAction.Created,
                NewValues = JsonSerializer.Serialize(new
                {
                    document.Id,
                    DonorId = donorId.Value,
                    document.DocumentType,
                    document.FileName
                }),
                IpAddress = command.IpAddress,
                UserAgent = command.UserAgent,
                CreatedAt = DateTime.UtcNow
            };
            await _auditLogs.AddAsync(auditLog, cancellationToken);

            // One SaveChangesAsync call — the new document and the audit log
            // commit together or not at all. The token claim already committed
            // separately above (see the atomicity remarks on ClaimBySubmissionTokenAsync).
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Donor {DonorId}'s BBBEE certificate submission session token was claimed but the upload " +
                "failed partway through — the token cannot be reused; the donor must restart the two-step " +
                "submission flow to get a new one.", donorId);
            throw;
        }

        _logger.LogInformation(
            "BBBEE certificate uploaded for donor {DonorId} via public submission session.", donorId);

        return new SubmitPublicDonorDocumentResponseDto
        {
            Message = SuccessMessage
        };
    }
}
