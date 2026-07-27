using Hangfire.Dashboard;

namespace CRM.API.Extensions;

// Production/staging guard for the Hangfire dashboard.
// Requires an authenticated user with the Admin role.
// Full RBAC policy can be refined later; this closes the
// "wide open dashboard" gap flagged in PR review for now.
public class HangfireDashboardAuthorizedOnlyFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Admin");
    }
}
