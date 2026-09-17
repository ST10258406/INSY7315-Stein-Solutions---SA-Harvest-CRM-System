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
        // caller's, until this rewrites it from X-Forwarded-For. KnownNetworks/
        // KnownProxies are cleared because Azure's front-end IP isn't fixed; the
        // container is only reachable through that proxy, so the header can be
        // trusted here. Verify in a deployed/staging environment, not just
        // locally — there's no proxy in front locally, so this bug doesn't show up.
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
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
