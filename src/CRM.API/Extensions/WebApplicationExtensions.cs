namespace CRM.API.Extensions;

using CRM.API.Middleware;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Hosting;
using Serilog;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiMiddleware(this WebApplication app)
    {
        // Must run first, before anything reads Connection.RemoteIpAddress (the
        // rate limiter included) — Azure App Service always fronts the app with
        // its own reverse proxy, so RemoteIpAddress is that proxy's IP, not the
        // caller's, until this rewrites it from X-Forwarded-For.
        //
        // KnownNetworks/KnownProxies are cleared because Azure's front-end IP
        // isn't fixed or publishable as a static list — this is Microsoft's own
        // documented pattern for App Service specifically because the container
        // is never reachable except through that front-end (there is no public
        // IP:port that reaches Kestrel directly). Clearing those lists alone would
        // still be dangerous on its own — it means "trust whichever single peer
        // connects" — so ForwardLimit is pinned to 1 (the default, made explicit
        // here) as the actual safety boundary: only the LAST entry in
        // X-Forwarded-For is ever read, i.e. the value Azure's front-end itself
        // appended from what it observed as the real client IP. Azure's front-end
        // appends rather than overwrites, so even a caller who sends their own
        // forged X-Forwarded-For (e.g. "1.2.3.4, ...") cannot influence the
        // rightmost/trusted entry — only prepend to what gets ignored. If a second
        // proxy layer (CDN, Front Door, App Gateway) is ever placed in front of
        // this app, ForwardLimit must be revisited (1 hop would then read the
        // wrong entry), and this whole assumption must be re-verified in a real
        // deployed/staging environment — there's no proxy in front locally, so a
        // regression here doesn't show up in local testing.
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1
        };
        forwardedHeadersOptions.KnownIPNetworks.Clear();
        forwardedHeadersOptions.KnownProxies.Clear();
        app.UseForwardedHeaders(forwardedHeadersOptions);

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseSerilogRequestLogging();

        app.UseHttpsRedirection();

        app.UseCors("DefaultCorsPolicy");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseRateLimiter();

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = app.Environment.IsDevelopment()
                ? new IDashboardAuthorizationFilter[] { new HangfireDashboardNoAuthFilter() }
                : new IDashboardAuthorizationFilter[] { new HangfireDashboardAuthorizedOnlyFilter() }
        });

        app.MapHealthChecks("/health");

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "SA Harvest CRM API v1");
            });
        }

        return app;
    }
}
