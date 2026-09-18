namespace CRM.API.Extensions;

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Rate limiting for the unauthenticated /public/* endpoints (design doc RULE 2 —
/// this must be wired up before any public endpoint is exposed, since bots will hit
/// it the moment it's deployed). Named per-tier policies are applied to individual
/// endpoints via [EnableRateLimiting("...")]; the global limiter below is a blanket
/// safety net over every request, public or authenticated.
///
/// All limiters key on <see cref="GetClientIp"/>, not the raw connection IP — behind
/// Azure App Service, RemoteIpAddress is the platform's front-end proxy for every
/// request, so partitioning on it would put every caller in one bucket and lock
/// everyone out together the moment traffic crosses the limit. ForwardedHeadersMiddleware
/// (registered in <see cref="WebApplicationExtensions.UseApiMiddleware"/>, before
/// UseRateLimiter) rewrites RemoteIpAddress from X-Forwarded-For, so this reads the
/// real client IP once that's in place.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>10 requests/hour per IP — POST /public/donors/submit and
    /// POST /public/donors/submit/document share this bucket, since a legitimate
    /// donor only calls the two of them once per submission attempt.</summary>
    public const string PublicSubmitPolicy = "PublicSubmit";

    /// <summary>60 requests/minute per IP — all GET /public/lookups/* endpoints.</summary>
    public const string PublicLookupsPolicy = "PublicLookups";

    /// <summary>10 requests/hour per IP — pre-existing policy for anonymous
    /// AuthController endpoints (e.g. forgot-password). Kept as-is here so all
    /// rate limit configuration lives in one place instead of being scattered
    /// across Program.cs/ServiceCollectionExtensions.</summary>
    public const string PublicFormPolicy = "PublicFormPolicy";

    public static IServiceCollection AddPublicApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.AddPolicy(PublicFormPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.AddPolicy(PublicSubmitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.AddPolicy(PublicLookupsPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Same envelope shape as ExceptionHandlingMiddleware, so a rate-limited
            // response looks like every other error response the API returns.
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    status = StatusCodes.Status429TooManyRequests,
                    code = "RATE_LIMITED",
                    message = "Too many requests. Please try again later.",
                    errors = (object?)null,
                    traceId = context.HttpContext.TraceIdentifier
                }, cancellationToken);
            };
        });

        return services;
    }

    private static string GetClientIp(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
