namespace CRM.API.Extensions;

using CRM.Application.Common.Interfaces;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

public static class RecurringJobRegistration
{
    /// <summary>Hangfire recurring-job id for the daily TaskDue notification sweep.</summary>
    public const string TaskDueJobId = "task-due-notifications";

    /// <summary>04:00 UTC = 06:00 SAST (UTC+2).</summary>
    public const string TaskDueCron = "0 4 * * *";

    /// <summary>
    /// Registers the project's recurring Hangfire jobs. Uses the DI-resolved
    /// <see cref="IRecurringJobManager"/> rather than the static <c>RecurringJob</c> API:
    /// the static one needs <c>JobStorage.Current</c>, which is only set once the Hangfire
    /// server has started (after <c>app.Run()</c>), so calling it at startup throws.
    /// <c>AddOrUpdate</c> is declarative — safe to run on every startup.
    /// </summary>
    public static void RegisterCrmRecurringJobs(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        manager.AddOrUpdate<ITaskDueNotificationJob>(
            TaskDueJobId,
            job => job.RunAsync(CancellationToken.None),
            TaskDueCron);
    }
}
