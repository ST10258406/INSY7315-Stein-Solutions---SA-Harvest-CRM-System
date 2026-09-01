namespace CRM.Application.Modules.Interactions.Queries.GetDonorInteractions;

using CRM.Application.Common.Models;
using CRM.Application.Modules.Interactions.Dtos;
using MediatR;

/// <summary>
/// GET /api/v1/donors/{id}/interactions — newest first, optional <see cref="InteractionType"/> filter.
/// Ordering is fixed (created_at DESC), so the inherited SortBy/SortDir are ignored.
/// </summary>
public record GetDonorInteractionsQuery : PaginationParams, IRequest<PaginatedResult<InteractionLogDto>>
{
    public Guid DonorId { get; init; }
    public string? InteractionType { get; init; }
}
