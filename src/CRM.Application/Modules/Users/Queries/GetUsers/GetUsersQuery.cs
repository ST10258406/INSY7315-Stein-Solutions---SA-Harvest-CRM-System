using CRM.Application.Common.Models;
using CRM.Application.Modules.Users.Dtos;
using MediatR;

namespace CRM.Application.Modules.Users.Queries.GetUsers;

public record GetUsersQuery : PaginationParams, IRequest<PaginatedResult<UserListItemDto>>
{
    public string? Search { get; init; }
    public Guid? RoleId { get; init; }
    public bool? IsActive { get; init; }
}
