namespace CRM.API.Extensions;

using CRM.API.Middleware;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiMiddleware(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        app.UseMiddleware<ExceptionHandlingMiddleware>();

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
