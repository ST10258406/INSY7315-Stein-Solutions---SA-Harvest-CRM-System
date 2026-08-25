namespace CRM.Application.Modules.Donors.Queries.GetDocumentDownloadUrl;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetDocumentDownloadUrlQueryHandler : IRequestHandler<GetDocumentDownloadUrlQuery, DocumentDownloadUrlDto>
{
    // SAS expiry is a hard security ceiling, not a configurable convenience —
    // never source this from config, and never bump it for testing.
    private static readonly TimeSpan SasExpiry = TimeSpan.FromMinutes(15);

    private readonly IApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorage;

    public GetDocumentDownloadUrlQueryHandler(
        IApplicationDbContext context,
        IBlobStorageService blobStorage)
    {
        _context = context;
        _blobStorage = blobStorage;
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

        // Per-document-type role authorization already happened before this handler
        // ran — see DocumentTypeAuthorizationFilter (CRM.API.Authorization). A Query
        // handler is not the place for role checks (Non-Negotiable rule).
        request.EntityId = document.Id;

        var downloadUrl = await _blobStorage.GenerateSasUrlAsync(document.BlobStoragePath, SasExpiry);
        var expiresAt = DateTime.UtcNow.Add(SasExpiry);

        return new DocumentDownloadUrlDto
        {
            DownloadUrl = downloadUrl,
            ExpiresAt = expiresAt,
            OriginalFileName = document.FileName
        };
    }
}
