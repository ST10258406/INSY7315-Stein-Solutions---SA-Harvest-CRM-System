namespace CRM.API.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// The one place "which role names require an elevated role to assign" is decided —
/// mirrors <see cref="DocumentTypeAuthorizationHandler"/>. Only SuperAdmin may assign the
/// SuperAdmin role; every other role only needs to clear the endpoint's base
/// [Authorize(Policy = "AdminOrAbove")] gate.
///
/// The resource is a non-nullable string (the caller passes "" for an unresolved role,
/// never null) — AuthorizationHandler&lt;TRequirement, TResource&gt;'s base HandleAsync
/// only invokes this method when `context.Resource is TResource`, which never matches a
/// null resource against a reference-type TResource. Passing null here would silently
/// leave the requirement unsatisfied instead of "requirement doesn't apply", turning a
/// nonexistent RoleId (a 400 from CreateUserCommandValidator) into an incorrect 403.
/// </summary>
public class RoleAssignmentAuthorizationHandler
    : AuthorizationHandler<RestrictedRoleAssignmentRequirement, string>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RestrictedRoleAssignmentRequirement requirement,
        string roleName)
    {
        var needsElevatedRole = string.Equals(roleName, "SuperAdmin", System.StringComparison.Ordinal);

        if (!needsElevatedRole || context.User.IsInRole("SuperAdmin"))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
