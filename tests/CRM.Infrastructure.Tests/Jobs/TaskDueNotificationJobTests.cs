using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using CRM.Infrastructure.Jobs;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Services;
using CRM.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.Infrastructure.Tests.Jobs;

public class TaskDueNotificationJobTests
{
    private readonly DbContextOptions<CrmDbContext> _options;

    private Guid _donorId;
    private Guid _assigneeId;
    private Guid _creatorId;

    public TaskDueNotificationJobTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_taskduejob"))
            .Options;
    }

    private async Task<CrmDbContext> CreateSeededContextAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        context.Notifications.RemoveRange(context.Notifications);
        context.DonorTasks.RemoveRange(context.DonorTasks);
        context.Donors.RemoveRange(context.Donors);
        await context.SaveChangesAsync();

        if (!await context.LookupCompanyTypes.AnyAsync(l => l.Id == 1))
            context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true, SortOrder = 1 });
        if (!await context.LookupEntityTypes.AnyAsync(l => l.Id == 1))
            context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true, SortOrder = 1 });
        if (!await context.LookupDonationFrequencies.AnyAsync(l => l.Id == 1))
            context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true, SortOrder = 1 });

        var assignee = await GetOrAddUser(context, "taskdue-assignee@example.com", "Ann", "Assignee");
        var creator = await GetOrAddUser(context, "taskdue-creator@example.com", "Cody", "Creator");
        await context.SaveChangesAsync();
        _assigneeId = assignee.Id;
        _creatorId = creator.Id;

        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = Guid.NewGuid().ToString("N")[..20],
            CompanyName = "Due Co",
            CompanyTypeId = 1,
            EntityTypeId = 1,
            DonationFrequencyId = 1,
            RegisteredCompanyName = "Due Co (Pty) Ltd",
            IncomeTaxNumber = "9012345678",
            Status = DonorStatus.Active,
            SubmissionSource = SubmissionSource.ManualCapture,
            CreatedByUserId = _creatorId
        };
        context.Donors.Add(donor);
        await context.SaveChangesAsync();
        _donorId = donor.Id;

        context.ChangeTracker.Clear();
        return context;
    }

    private static async Task<User> GetOrAddUser(CrmDbContext context, string email, string first, string last)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            user = new User { Id = Guid.NewGuid(), Email = email, FirstName = first, LastName = last, PasswordHash = "hash", IsActive = true };
            context.Users.Add(user);
        }
        return user;
    }

    private DonorTask MakeTask(DateTime dueDate, bool completed = false, Guid? assignee = null) => new()
    {
        Id = Guid.NewGuid(),
        DonorId = _donorId,
        Title = "Follow up",
        DueDate = DateTime.SpecifyKind(dueDate, DateTimeKind.Utc),
        IsCompleted = completed,
        CompletedAt = completed ? DateTime.UtcNow : null,
        CompletedByUserId = completed ? _creatorId : null,
        AssignedToUserId = assignee ?? _assigneeId,
        CreatedByUserId = _creatorId
    };

    private static TaskDueNotificationJob MakeJob(CrmDbContext context) =>
        new(context, new NotificationService(context), NullLogger<TaskDueNotificationJob>.Instance);

    [Fact]
    public async Task RunAsync_DueOpenTask_CreatesOneTaskDueNotificationForTheAssignee()
    {
        using var context = await CreateSeededContextAsync();
        var task = MakeTask(DateTime.UtcNow.Date); // due today
        context.DonorTasks.Add(task);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await MakeJob(context).RunAsync(CancellationToken.None);

        using var verify = new CrmDbContext(_options);
        var n = await verify.Notifications.AsNoTracking().SingleAsync();
        Assert.Equal(_assigneeId, n.UserId);
        Assert.Equal(NotificationType.TaskDue, n.NotificationType);
        Assert.Equal(task.Id, n.RelatedEntityId);
        Assert.Equal("DonorTask", n.RelatedEntityType);
    }

    [Fact]
    public async Task RunAsync_OverdueTask_IsAlsoNotified()
    {
        using var context = await CreateSeededContextAsync();
        context.DonorTasks.Add(MakeTask(DateTime.UtcNow.Date.AddDays(-3)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await MakeJob(context).RunAsync(CancellationToken.None);

        using var verify = new CrmDbContext(_options);
        Assert.Equal(1, await verify.Notifications.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task RunAsync_CompletedOrFutureTasks_AreIgnored()
    {
        using var context = await CreateSeededContextAsync();
        context.DonorTasks.AddRange(
            MakeTask(DateTime.UtcNow.Date, completed: true),      // due today but done
            MakeTask(DateTime.UtcNow.Date.AddDays(2)));           // not due yet
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await MakeJob(context).RunAsync(CancellationToken.None);

        using var verify = new CrmDbContext(_options);
        Assert.Equal(0, await verify.Notifications.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task RunAsync_IsIdempotent_SecondRunCreatesNoDuplicate()
    {
        using var context = await CreateSeededContextAsync();
        context.DonorTasks.Add(MakeTask(DateTime.UtcNow.Date.AddDays(-1)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await MakeJob(context).RunAsync(CancellationToken.None);
        await MakeJob(context).RunAsync(CancellationToken.None);

        using var verify = new CrmDbContext(_options);
        Assert.Equal(1, await verify.Notifications.AsNoTracking().CountAsync(n => n.NotificationType == NotificationType.TaskDue));
    }

    [Fact]
    public async Task RunAsync_NotifiesEachAssignee_NotTheCreator()
    {
        using var context = await CreateSeededContextAsync();
        var otherAssignee = await GetOrAddUser(context, "taskdue-other@example.com", "Otto", "Other");
        await context.SaveChangesAsync();

        context.DonorTasks.AddRange(
            MakeTask(DateTime.UtcNow.Date, assignee: _assigneeId),
            MakeTask(DateTime.UtcNow.Date, assignee: otherAssignee.Id));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await MakeJob(context).RunAsync(CancellationToken.None);

        using var verify = new CrmDbContext(_options);
        var userIds = await verify.Notifications.AsNoTracking().Select(n => n.UserId).ToListAsync();
        Assert.Equal(2, userIds.Count);
        Assert.Contains(_assigneeId, userIds);
        Assert.Contains(otherAssignee.Id, userIds);
        Assert.DoesNotContain(_creatorId, userIds);
    }

    [Fact]
    public async Task RunAsync_NoDueTasks_DoesNothing()
    {
        using var context = await CreateSeededContextAsync();
        context.DonorTasks.Add(MakeTask(DateTime.UtcNow.Date.AddDays(5)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await MakeJob(context).RunAsync(CancellationToken.None);

        using var verify = new CrmDbContext(_options);
        Assert.Equal(0, await verify.Notifications.AsNoTracking().CountAsync());
    }
}
