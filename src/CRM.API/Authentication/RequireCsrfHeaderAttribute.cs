namespace CRM.API.Authentication;

using CRM.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// CSRF guard for endpoints authenticated by the refresh-token cookie (refresh, logout).
/// A cookie is attached by the browser no matter which site triggers the request, so these
/// endpoints also require a custom header. A cross-site page can't add a custom header
/// without a CORS preflight, and the preflight only succeeds for the exact origins in
/// Cors:AllowedOrigins — so a forged request never carries it. Defence in depth on top of
/// SameSite, and the only defence when SameSite=None.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireCsrfHeaderAttribute : Attribute, IAuthorizationFilter
{
    public const string HeaderName = "X-Requested-With";
    public const string HeaderValue = "XMLHttpRequest";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var headers = context.HttpContext.Request.Headers;
        if (!string.Equals(headers[HeaderName], HeaderValue, StringComparison.Ordinal))
            throw new ForbiddenException($"Missing required {HeaderName} header.");
    }
}
