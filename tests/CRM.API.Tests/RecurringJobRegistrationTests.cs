namespace CRM.API.Tests;

using CRM.API.Extensions;
using CRM.Application.Common.Interfaces;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

public class RecurringJobRegistrationTests
{
    [Fact]
    public void RegisterCrmRecurringJobs_RegistersTaskDueJob_WithDailyCron_ViaDiResolvedManager()
    {
        var services = new ServiceCollection();
        services.AddHangfire(cfg => cfg.UseInMemoryStorage());
        services.AddScoped(_ => Substitute.For<ITaskDueNotificationJob>());
        using var provider = services.BuildServiceProvider();

        // Would throw "JobStorage.Current has not been initialized" if this used the
        // static RecurringJob API instead of IRecurringJobManager (the crash we hit
        // running the container).
        provider.RegisterCrmRecurringJobs();

        using var connection = provider.GetRequiredService<JobStorage>().GetConnection();
        var job = Assert.Single(
            connection.GetRecurringJobs(),
            j => j.Id == RecurringJobRegistration.TaskDueJobId);
        Assert.Equal(RecurringJobRegistration.TaskDueCron, job.Cron);
    }

    [Fact]
    public void RegisterCrmRecurringJobs_IsIdempotent()
    {
        var services = new ServiceCollection();
        services.AddHangfire(cfg => cfg.UseInMemoryStorage());
        services.AddScoped(_ => Substitute.For<ITaskDueNotificationJob>());
        using var provider = services.BuildServiceProvider();

        provider.RegisterCrmRecurringJobs();
        provider.RegisterCrmRecurringJobs();

        using var connection = provider.GetRequiredService<JobStorage>().GetConnection();
        Assert.Single(connection.GetRecurringJobs(), j => j.Id == RecurringJobRegistration.TaskDueJobId);
    }
}
