using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Users.Dtos;
using MediatR;

namespace CRM.Application.Modules.Users.Queries.GetUsers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedResult<UserListItemDto>>
{
    private readonly IUserRepository _users;

    public GetUsersQueryHandler(IUserRepository users) => _users = users;

    public async Task<PaginatedResult<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var criteria = new UserSearchCriteria
        {
            Search = request.Search,
            RoleId = request.RoleId,
            IsActive = request.IsActive,
            SortBy = request.SortBy,
            SortDir = request.SortDir,
            Page = request.Page,
            PageSize = request.PageSize
        };

        var (items, totalCount) = await _users.SearchAsync(criteria, cancellationToken);

        return PaginatedResult<UserListItemDto>.Create(items, request.Page, request.PageSize, totalCount);
    }
}
