namespace CRM.API.Extensions;

using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
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

    /// <summary>
    /// Per-user (not per-IP — multiple staff can share an office network) limit on
    /// POST /donors/{id}/interactions/email. Configurable via
    /// RateLimiting:DonorEmail:PermitLimit / :WindowMinutes in appsettings.json;
    /// defaults below apply if that section is absent.
    /// </summary>
    public const string DonorEmailPolicy = "DonorEmail";

    /// <summary>Strict per-IP limit on POST /auth/login (default 10/minute) — F-02.</summary>
    public const string AuthLoginPolicy = "AuthLogin";

    /// <summary>Strict per-IP limit on POST /auth/reset-password (default 10/minute) — F-02.</summary>
    public const string AuthResetPasswordPolicy = "AuthResetPassword";

    /// <summary>
    /// Per-IP limit on POST /auth/refresh (default 20/minute). Looser than login because
    /// every signed-in staff member behind one office NAT refreshes through it, but still
    /// far below the 100/minute global limiter.
    /// </summary>
    public const string AuthRefreshPolicy = "AuthRefresh";

    private const int DefaultDonorEmailPermitLimit = 20;
    private const int DefaultDonorEmailWindowMinutes = 60;

    /// <summary>
    /// Rate limiting for authenticated staff endpoints, partitioned by the caller's
    /// user id (ClaimTypes.NameIdentifier — the same claim ICurrentUserService reads)
    /// rather than IP, since staff commonly share an office network. Registered via
    /// its own AddRateLimiter call, additive to <see cref="AddPublicApiRateLimiting"/>'s
    /// (ASP.NET Core's rate limiter options are configured additively — see
    /// Microsoft.Extensions.Options — so both sets of named policies coexist).
    /// </summary>
    public static IServiceCollection AddAuthenticatedApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue<int?>("RateLimiting:DonorEmail:PermitLimit") ?? DefaultDonorEmailPermitLimit;
        var windowMinutes = configuration.GetValue<int?>("RateLimiting:DonorEmail:WindowMinutes") ?? DefaultDonorEmailWindowMinutes;

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(DonorEmailPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetUserId(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromMinutes(windowMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    /// <summary>
    /// Strict per-IP policies for the anonymous auth endpoints (F-02). The per-account
    /// lockout in LoginCommandHandler is the second layer. Limits are read from
    /// RateLimiting:Auth:{Login|ResetPassword|Refresh}:PermitLimit / :WindowMinutes when
    /// a partition is first created (so test-host overrides apply), with the defaults below.
    /// </summary>
    public static IServiceCollection AddAuthEndpointRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            AddStrictIpPolicy(options, configuration, AuthLoginPolicy, "RateLimiting:Auth:Login", defaultPermitLimit: 10);
            AddStrictIpPolicy(options, configuration, AuthResetPasswordPolicy, "RateLimiting:Auth:ResetPassword", defaultPermitLimit: 10);
            AddStrictIpPolicy(options, configuration, AuthRefreshPolicy, "RateLimiting:Auth:Refresh", defaultPermitLimit: 20);
        });

        return services;
    }

    private static void AddStrictIpPolicy(
        RateLimiterOptions options, IConfiguration configuration, string policyName, string section, int defaultPermitLimit)
    {
        options.AddPolicy(policyName, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: GetClientIp(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = configuration.GetValue<int?>($"{section}:PermitLimit") ?? defaultPermitLimit,
                    Window = TimeSpan.FromMinutes(configuration.GetValue<int?>($"{section}:WindowMinutes") ?? 1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));
    }

    private static string GetUserId(HttpContext context) =>
        context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

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

                // Fixed-window limiters report when the window resets; tell well-behaved
                // clients exactly how long to back off.
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

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
