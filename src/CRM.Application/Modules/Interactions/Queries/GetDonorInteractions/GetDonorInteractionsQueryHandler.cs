namespace CRM.Application.Modules.Interactions.Queries.GetDonorInteractions;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Interactions.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MediatR;

public class GetDonorInteractionsQueryHandler
    : IRequestHandler<GetDonorInteractionsQuery, PaginatedResult<InteractionLogDto>>
{
    // Same 15-minute security ceiling as document downloads (Sprint 3, #35) — never
    // sourced from config, never widened for testing.
    private static readonly TimeSpan SasExpiry = TimeSpan.FromMinutes(15);

    private readonly IDonorRepository _donors;
    private readonly IInteractionLogRepository _interactions;
    private readonly IBlobStorageService _blobStorage;

    public GetDonorInteractionsQueryHandler(
        IDonorRepository donors,
        IInteractionLogRepository interactions,
        IBlobStorageService blobStorage)
    {
        _donors = donors;
        _interactions = interactions;
        _blobStorage = blobStorage;
    }

    public async Task<PaginatedResult<InteractionLogDto>> Handle(
        GetDonorInteractionsQuery request, CancellationToken cancellationToken)
    {
        if (!await _donors.ExistsAsync(request.DonorId, cancellationToken))
            throw new NotFoundException(nameof(Donor), request.DonorId);

        InteractionType? type = null;
        if (!string.IsNullOrWhiteSpace(request.InteractionType)
            && Enum.TryParse<InteractionType>(request.InteractionType, true, out var parsed))
        {
            type = parsed;
        }

        var (items, totalCount) = await _interactions.GetByDonorAsync(
            request.DonorId, type, request.Page, request.PageSize, cancellationToken);

        // The repository returns the stored blob path in EmailAttachmentUrl; swap each
        // present one for a short-lived SAS URL. Raw blob paths never leave the API.
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.EmailAttachmentUrl))
                item.EmailAttachmentUrl = await _blobStorage.GenerateSasUrlAsync(item.EmailAttachmentUrl, SasExpiry);
        }

        return PaginatedResult<InteractionLogDto>.Create(items, request.Page, request.PageSize, totalCount);
    }
}
