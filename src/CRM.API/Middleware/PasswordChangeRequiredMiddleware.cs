namespace CRM.API.Middleware;

using System.Text.Json;
using CRM.API.Authentication;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Server-side enforcement of the forced password change. JWT validation stamps a
/// <see cref="ClaimName"/> claim from the database on every request, so the flag takes
/// effect (and clears) immediately without waiting for a new access token. While it is
/// present, only endpoints marked <see cref="AllowWhenPasswordChangeRequiredAttribute"/>
/// are reachable; everything else gets 403 with a machine-readable code.
/// </summary>
public class PasswordChangeRequiredMiddleware
{
    public const string ClaimName = "must_change_password";
    public const string ErrorCode = "PASSWORD_CHANGE_REQUIRED";

    private readonly RequestDelegate _next;

    public PasswordChangeRequiredMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var mustChange = context.User.Identity?.IsAuthenticated == true
            && context.User.HasClaim(ClaimName, "true");

        if (mustChange
            && context.GetEndpoint()?.Metadata.GetMetadata<AllowWhenPasswordChangeRequiredAttribute>() is null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var envelope = new
            {
                status = StatusCodes.Status403Forbidden,
                code = ErrorCode,
                message = "You must change your password before continuing.",
                errors = (object?)null,
                traceId = context.TraceIdentifier
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(envelope,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            return;
        }

        await _next(context);
    }
}
