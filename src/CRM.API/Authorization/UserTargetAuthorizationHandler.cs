namespace CRM.API.Authorization;

using CRM.Application.Modules.Users.Dtos;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// The one place "which users may this caller manage" is decided — mirrors
/// <see cref="RoleAssignmentAuthorizationHandler"/>, but evaluates the caller against the
/// <em>target</em> user rather than the role being assigned (security review F-01: without
/// this, an Admin could change a SuperAdmin's email, reset its password and take it over).
///
/// Policy:
/// <list type="bullet">
///   <item>System users (<see cref="CRM.Domain.Constants.SystemUsers"/>) can't be managed by anyone, SuperAdmin included.</item>
///   <item>A SuperAdmin target can only be managed by a SuperAdmin — an Admin never can.</item>
///   <item>Every other target only needs to clear the endpoint's base [Authorize(Policy = "AdminOrAbove")] gate.</item>
/// </list>
/// The target's roles come from the database (<see cref="UserAuthorizationTarget"/>),
/// never from the request body.
/// </summary>
public class UserTargetAuthorizationHandler
    : AuthorizationHandler<ManageTargetUserRequirement, UserAuthorizationTarget>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ManageTargetUserRequirement requirement,
        UserAuthorizationTarget target)
    {
        if (target.IsSystemUser)
            return Task.CompletedTask;

        if (!target.IsSuperAdmin || context.User.IsInRole("SuperAdmin"))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
