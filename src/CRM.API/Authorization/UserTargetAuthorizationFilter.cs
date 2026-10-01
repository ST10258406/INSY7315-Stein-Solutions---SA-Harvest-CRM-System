namespace CRM.API.Authorization;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// Runs the target-user-aware check *before* the controller action executes, so the
/// Users command handlers stay role-agnostic — role logic belongs in
/// <see cref="UserTargetAuthorizationHandler"/>, not in Application handlers and not
/// inline in the controller. Mirrors <see cref="DocumentTypeAuthorizationFilter"/>.
///
/// Apply via [TypeFilter(typeof(UserTargetAuthorizationFilter))] on an action whose
/// route has an "id" (target user) parameter. An unknown id is let through so the
/// command handler returns its usual 404.
/// </summary>
public class UserTargetAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IUserRepository _users;
    private readonly IAuthorizationService _authorizationService;

    public UserTargetAuthorizationFilter(IUserRepository users, IAuthorizationService authorizationService)
    {
        _users = users;
        _authorizationService = authorizationService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // The {id:guid} route constraint guarantees this parses.
        var targetId = Guid.Parse(context.RouteData.Values["id"]!.ToString()!);

        var target = await _users.GetAuthorizationTargetAsync(targetId, context.HttpContext.RequestAborted);
        if (target is null)
            return;

        var result = await _authorizationService.AuthorizeAsync(
            context.HttpContext.User, target, new ManageTargetUserRequirement());

        if (!result.Succeeded)
            throw new ForbiddenException("You do not have permission to manage this user.");
    }
}
