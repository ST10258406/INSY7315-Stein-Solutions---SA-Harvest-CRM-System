namespace CRM.Application.Modules.SavedFilters.Queries.GetSavedFilters;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.SavedFilters.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class GetSavedFiltersQueryHandler : IRequestHandler<GetSavedFiltersQuery, List<SavedFilterDto>>
{
    private readonly ISavedFilterRepository _savedFilters;
    private readonly ICurrentUserService _currentUserService;

    public GetSavedFiltersQueryHandler(ISavedFilterRepository savedFilters, ICurrentUserService currentUserService)
    {
        _savedFilters = savedFilters;
        _currentUserService = currentUserService;
    }

    public Task<List<SavedFilterDto>> Handle(GetSavedFiltersQuery request, CancellationToken cancellationToken) =>
        _savedFilters.ListActiveAsync(
            _currentUserService.GetCurrentUserId(),
            Enum.Parse<SavedFilterPage>(request.Page, true),
            cancellationToken);
}
