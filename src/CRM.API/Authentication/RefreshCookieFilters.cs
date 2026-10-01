namespace CRM.API.Authentication;

using CRM.Application.Common.Exceptions;
using CRM.Application.Modules.Auth.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Binds an action parameter to the raw refresh token from the HttpOnly cookie (null when
/// absent), so controller actions stay transport-only: map → send → return.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromRefreshCookieAttribute : ModelBinderAttribute
{
    public FromRefreshCookieAttribute() : base(typeof(RefreshCookieModelBinder))
    {
        BindingSource = BindingSource.Special;
    }
}

public sealed class RefreshCookieModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var cookie = bindingContext.HttpContext.RequestServices.GetRequiredService<RefreshTokenCookie>();
        bindingContext.Result = ModelBindingResult.Success(cookie.Read(bindingContext.HttpContext.Request));
        return Task.CompletedTask;
    }
}

/// <summary>
/// For actions that start or rotate a session (login, refresh): on success, moves the
/// refresh token from the <see cref="IIssuesRefreshToken"/> result into the HttpOnly
/// cookie. When the session is rejected (401), deletes the cookie so the browser stops
/// presenting a dead token. The exception itself still flows to ExceptionHandlingMiddleware.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IssuesRefreshCookieAttribute : ActionFilterAttribute
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        var cookie = context.HttpContext.RequestServices.GetRequiredService<RefreshTokenCookie>();

        if (context.Exception is UnauthorizedException)
            cookie.Delete(context.HttpContext.Response);
        else if (context.Exception is null && context.Result is ObjectResult { Value: IIssuesRefreshToken issued })
            cookie.Write(context.HttpContext.Response, issued.RefreshToken, issued.RefreshTokenExpiresAt);
    }
}

/// <summary>For actions that end the session (logout): always clears the refresh cookie.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ClearsRefreshCookieAttribute : ActionFilterAttribute
{
    public override void OnActionExecuted(ActionExecutedContext context)
        => context.HttpContext.RequestServices.GetRequiredService<RefreshTokenCookie>()
            .Delete(context.HttpContext.Response);
}
