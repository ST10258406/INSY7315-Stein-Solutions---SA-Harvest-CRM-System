using CRM.API.Extensions;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .WriteTo.Console());

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Refuse to start outside Development with localhost/empty CORS origins or frontend URL,
// or a short JWT secret. Runs after Build() so every configuration source (including test
// host overrides) is in place, and before migrations so a bad deploy fails fast.
ProductionConfigurationGuard.Validate(app.Configuration, app.Environment);

if (app.Environment.EnvironmentName != "Testing")
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await context.Database.MigrateAsync();
    }

    await DatabaseSeeder.SeedAsync(app.Services, app.Environment.IsDevelopment());

    app.Services.RegisterCrmRecurringJobs();
}

app.UseApiMiddleware();
app.MapControllers();

app.Run();