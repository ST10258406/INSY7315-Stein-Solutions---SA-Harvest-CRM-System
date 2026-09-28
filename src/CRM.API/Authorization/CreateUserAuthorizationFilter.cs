namespace CRM.API.Authorization;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// Runs the role-assignment-aware check *before* the controller action executes, so
/// CreateUserCommandHandler stays role-agnostic — role logic belongs here, not in
/// Application handlers (see the repo's Clean Architecture rules) and not inline in the
/// controller. Mirrors <see cref="DocumentTypeAuthorizationFilter"/>.
///
/// Runs as an action filter (not an authorization filter) because the check needs the
/// bound request body's RoleId, which isn't available until model binding has happened.
/// Apply via [TypeFilter(typeof(CreateUserAuthorizationFilter))] on an action whose bound
/// parameter is named "request" and is a <see cref="CreateUserRequest"/>.
/// </summary>
public class CreateUserAuthorizationFilter : IAsyncActionFilter
{
    private readonly IUserRepository _users;
    private readonly IAuthorizationService _authorizationService;

    public CreateUserAuthorizationFilter(IUserRepository users, IAuthorizationService authorizationService)
    {
        _users = users;
        _authorizationService = authorizationService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionArguments.TryGetValue("request", out var value) && value is CreateUserRequest request)
        {
            // "" (never null) for an unresolved RoleId — see RoleAssignmentAuthorizationHandler's
            // remarks on why the resource must not be null.
            var roleName = await _users.GetRoleNameAsync(request.RoleId, context.HttpContext.RequestAborted) ?? string.Empty;

            var result = await _authorizationService.AuthorizeAsync(
                context.HttpContext.User, roleName, new RestrictedRoleAssignmentRequirement());

            if (!result.Succeeded)
                throw new ForbiddenException("Only a SuperAdmin can assign the SuperAdmin role.");
        }

        await next();
    }
}
