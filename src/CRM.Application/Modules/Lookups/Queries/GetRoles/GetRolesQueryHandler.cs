namespace CRM.Application.Modules.Lookups.Queries.GetRoles;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Dtos;
using MediatR;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<RoleDto>>
{
    private readonly IUserRepository _users;

    public GetRolesQueryHandler(IUserRepository users) => _users = users;

    public Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        => _users.GetRolesAsync(cancellationToken);
}
