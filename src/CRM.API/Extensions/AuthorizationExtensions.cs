namespace CRM.API.Extensions;

using Microsoft.Extensions.DependencyInjection;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddCrmAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("MarketingOrAbove", policy =>
                policy.RequireRole("Marketing", "Procurement", "Admin", "SuperAdmin"));

            options.AddPolicy("ProcurementOrAbove", policy =>
                policy.RequireRole("Procurement", "Admin", "SuperAdmin"));

            options.AddPolicy("AdminOrAbove", policy =>
                policy.RequireRole("Admin", "SuperAdmin"));

            options.AddPolicy("SuperAdminOnly", policy =>
                policy.RequireRole("SuperAdmin"));
        });

        return services;
    }
}
