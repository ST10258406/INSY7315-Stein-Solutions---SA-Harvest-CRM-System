using CRM.API.Extensions;
using CRM.Application.Common.Interfaces;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Seeders;
using Hangfire;
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

if (app.Environment.EnvironmentName != "Testing")
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await context.Database.MigrateAsync();
    }

    await DatabaseSeeder.SeedAsync(app.Services);

    // Daily 04:00 UTC = 06:00 SAST (UTC+2). AddOrUpdate is declarative — safe to
    // call on every startup; it just keeps the schedule in sync.
    RecurringJob.AddOrUpdate<ITaskDueNotificationJob>(
        "task-due-notifications",
        job => job.RunAsync(CancellationToken.None),
        "0 4 * * *");
}

app.UseApiMiddleware();
app.MapControllers();

app.Run();