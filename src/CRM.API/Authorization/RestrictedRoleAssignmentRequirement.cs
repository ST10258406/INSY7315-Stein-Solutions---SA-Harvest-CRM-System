namespace CRM.API.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Requirement checked against the *name of the role being assigned* on user creation —
/// see <see cref="RoleAssignmentAuthorizationHandler"/> and <see cref="CreateUserAuthorizationFilter"/>.
/// </summary>
public class RestrictedRoleAssignmentRequirement : IAuthorizationRequirement
{
}
