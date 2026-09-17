namespace CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonorDocument;

using System.Text.Json;
using CRM.Application.Common.Exceptions;
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
        // Tracked lookup — we burn the token on success below. A donor whose
        // token doesn't match, has expired, or was already consumed (burned to
        // null by a prior successful call) is indistinguishable here from an
        // outright invalid token — all three collapse to the same generic 400,
        // same discipline as ResetPasswordCommandHandler's token check.
        var donor = await _donors.GetBySubmissionTokenAsync(command.SessionToken, cancellationToken);

        if (donor is null
            || donor.SubmissionTokenExpiresAt is null
            || donor.SubmissionTokenExpiresAt < DateTimeOffset.UtcNow)
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

        // Upload before inserting the DonorDocument row — if the upload fails,
        // we don't want a DB record pointing at a blob that was never written.
        // An orphaned blob from a subsequent failed SaveChanges is an accepted,
        // cheap failure mode (mirrors UploadDonorDocumentCommandHandler).
        var blobPath = $"donors/{donor.Id}/{documentType}/{Guid.NewGuid()}_{command.OriginalFileName}";
        await _blobStorage.UploadAsync(command.FileStream, blobPath, command.ContentType);

        var document = new DonorDocument
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            DocumentType = documentType,
            FileName = command.OriginalFileName,
            BlobStoragePath = blobPath,
            FileSizeBytes = command.FileSizeBytes,
            MimeType = command.ContentType,
            // Null, not the system user — DonorDocument.UploadedByUserId is
            // nullable specifically for an anonymous uploader like this one.
            UploadedByUserId = null,
            IsActive = true
        };
        await _documents.AddAsync(document, cancellationToken);

        // Single-use: burn the token immediately so a leaked token can't be
        // replayed to upload garbage files to this donor's record later.
        donor.SubmissionToken = null;
        donor.SubmissionTokenExpiresAt = null;

        // Manual audit_logs write — this command deliberately does not implement
        // IAuditableCommand (see SubmitPublicDonorDocumentCommand's remarks), so
        // AuditBehaviour never runs for it. The donor id is recorded here (server
        // side only) but never appears anywhere in the response sent back to the
        // public client.
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
                DonorId = donor.Id,
                document.DocumentType,
                document.FileName
            }),
            IpAddress = command.IpAddress,
            UserAgent = command.UserAgent,
            CreatedAt = DateTime.UtcNow
        };
        await _auditLogs.AddAsync(auditLog, cancellationToken);

        // One SaveChangesAsync call — the new document, the token burn on the
        // donor, and the audit log all commit together or not at all.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "BBBEE certificate uploaded for donor {DonorId} via public submission session.", donor.Id);

        return new SubmitPublicDonorDocumentResponseDto
        {
            Message = SuccessMessage
        };
    }
}
