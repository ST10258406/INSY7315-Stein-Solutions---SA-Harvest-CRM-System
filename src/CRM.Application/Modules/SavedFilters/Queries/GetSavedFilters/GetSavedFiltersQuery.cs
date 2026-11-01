namespace CRM.Application.Modules.SavedFilters.Queries.GetSavedFilters;

using CRM.Application.Modules.SavedFilters.Dtos;
using MediatR;

/// <summary>GET /api/v1/saved-filters?page=Donors: the caller's active saved filters.</summary>
public record GetSavedFiltersQuery : IRequest<List<SavedFilterDto>>
{
    public string Page { get; init; } = "Donors";
}
