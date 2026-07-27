using Hangfire.Dashboard;

namespace CRM.API.Extensions;

// TODO: restrict to authenticated admins before any non-local deployment.
// This filter currently allows unauthenticated access to the Hangfire
// dashboard, which is acceptable for local development only.
public class HangfireDashboardNoAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
