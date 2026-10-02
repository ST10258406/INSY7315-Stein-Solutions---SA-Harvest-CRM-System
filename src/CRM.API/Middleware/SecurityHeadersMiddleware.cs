namespace CRM.API.Middleware;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Adds baseline security headers to every response, error responses included. The API only
/// serves JSON, so its CSP is fully locked down. The CSP is skipped for Swagger and the Hangfire
/// dashboard (both need scripts and styles), which are Development-only / SuperAdmin-only.
/// </summary>
public class SecurityHeadersMiddleware
{
    private const string ApiContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

    private readonly RequestDelegate _next;
    private readonly bool _isDevelopment;

    public SecurityHeadersMiddleware(RequestDelegate next, bool isDevelopment)
    {
        _next = next;
        _isDevelopment = isDevelopment;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Frame-Options"] = "DENY";

            if (!_isDevelopment && !IsToolingPath(context.Request.Path))
            {
                headers["Content-Security-Policy"] = ApiContentSecurityPolicy;
            }

            headers.Remove("X-Powered-By");
            headers.Remove("Server");
            return Task.CompletedTask;
        });

        return _next(context);
    }

    private static bool IsToolingPath(PathString path) =>
        path.StartsWithSegments("/swagger") || path.StartsWithSegments("/hangfire");
}
