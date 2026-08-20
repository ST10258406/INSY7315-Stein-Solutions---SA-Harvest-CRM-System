namespace CRM.Application.Modules.Donors.Queries.GetDocumentDownloadUrl;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetDocumentDownloadUrlQueryHandler : IRequestHandler<GetDocumentDownloadUrlQuery, DocumentDownloadUrlDto>
{
    // SAS expiry is a hard security ceiling, not a configurable convenience —
    // never source this from config, and never bump it for testing.
    private static readonly TimeSpan SasExpiry = TimeSpan.FromMinutes(15);

    private readonly IApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorage;
    private readonly ICurrentUserService _currentUserService;

    public GetDocumentDownloadUrlQueryHandler(
        IApplicationDbContext context,
        IBlobStorageService blobStorage,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _blobStorage = blobStorage;
        _currentUserService = currentUserService;
    }

    public async Task<DocumentDownloadUrlDto> Handle(GetDocumentDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        var document = await _context.DonorDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.Id == request.DocumentId && d.DonorId == request.DonorId,
                cancellationToken);

        if (document is null)
            throw new NotFoundException(nameof(DonorDocument), request.DocumentId);

        // Per-document-type role check — the controller's [Authorize(Policy = "ProcurementOrAbove")]
        // only gets us past the endpoint gate. Whether Procurement can see *this*
        // document depends on its type, which we only know after loading it, so
        // the finer-grained rule has to live here rather than on the attribute.
        if (document.DocumentType == DocumentType.BBBEECertificate
            && !_currentUserService.GetCurrentUserRoles().Any(r => r is "Admin" or "SuperAdmin"))
        {
            throw new ForbiddenException("Insufficient role to download BBBEE certificate.");
        }

        var downloadUrl = await _blobStorage.GenerateSasUrlAsync(document.BlobStoragePath, SasExpiry);
        var expiresAt = DateTime.UtcNow.Add(SasExpiry);

        // Deliberate exception to "handlers don't write audit_logs directly" —
        // this is a Query, not a Command, so the standard AuditBehaviour pipeline
        // (which only reacts to IAuditableCommand) never sees it. Download tracking
        // still needs an audit trail, so it's written explicitly here.
        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = nameof(DonorDocument),
            EntityId = document.Id,
            Action = AuditAction.Viewed,
            UserId = _currentUserService.GetCurrentUserId(),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);

        return new DocumentDownloadUrlDto
        {
            DownloadUrl = downloadUrl,
            ExpiresAt = expiresAt,
            OriginalFileName = document.FileName
        };
    }
}
