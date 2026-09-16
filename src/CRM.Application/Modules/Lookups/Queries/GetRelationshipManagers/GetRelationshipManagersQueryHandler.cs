namespace CRM.Application.Modules.Lookups.Queries.GetRelationshipManagers;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using MediatR;

public class GetRelationshipManagersQueryHandler
    : IRequestHandler<GetRelationshipManagersQuery, List<RelationshipManagerDto>>
{
    // Matches RoleSeeder / AuthorizationExtensions — the same string used everywhere else a
    // role check needs "Procurement" (there's no shared Roles constants class in this codebase).
    private const string RelationshipManagerRole = "Procurement";

    private readonly IUserRepository _users;

    public GetRelationshipManagersQueryHandler(IUserRepository users) => _users = users;

    public Task<List<RelationshipManagerDto>> Handle(GetRelationshipManagersQuery request, CancellationToken cancellationToken)
        => _users.GetActiveByRoleAsync(RelationshipManagerRole, cancellationToken);
}
