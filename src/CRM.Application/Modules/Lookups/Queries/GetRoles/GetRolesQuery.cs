namespace CRM.Application.Modules.Lookups.Queries.GetRoles;

using CRM.Application.Modules.Users.Dtos;
using MediatR;

/// <summary>GET /api/v1/lookups/roles — every role, for the Users screen's role dropdowns.</summary>
public record GetRolesQuery : IRequest<List<RoleDto>>;
