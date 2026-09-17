using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using CRM.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Tests.Persistence.Repositories;

public class DashboardRepositoryTests
{
    private readonly DbContextOptions<CrmDbContext> _options;

    private Guid _creatorId;
    private Guid _managerAId;
    private Guid _managerBId;

    public DashboardRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_dashboardrepo"))
            .Options;
    }

    private async Task<CrmDbContext> CreateSeededContextAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        context.InteractionLogs.RemoveRange(context.InteractionLogs);
        context.DonorTasks.RemoveRange(context.DonorTasks);
        context.DonorApprovals.RemoveRange(context.DonorApprovals);
        context.Donors.RemoveRange(context.Donors);
        await context.SaveChangesAsync();

        if (!await context.LookupCompanyTypes.AnyAsync(l => l.Id == 1))
            context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true, SortOrder = 1 });
        if (!await context.LookupEntityTypes.AnyAsync(l => l.Id == 1))
            context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true, SortOrder = 1 });
        if (!await context.LookupDonationFrequencies.AnyAsync(l => l.Id == 1))
            context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true, SortOrder = 1 });
        await context.SaveChangesAsync();

        var creator = await GetOrAddUser(context, "dashboard-creator@example.com", "Case", "Worker");
        var managerA = await GetOrAddUser(context, "dashboard-manager-a@example.com", "Ann", "Manager");
        var managerB = await GetOrAddUser(context, "dashboard-manager-b@example.com", "Ben", "Manager");
        await context.SaveChangesAsync();

        _creatorId = creator.Id;
        _managerAId = managerA.Id;
        _managerBId = managerB.Id;

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

    private Donor MakeDonor(string name, DonorStatus status = DonorStatus.Active, Guid? relationshipManagerId = null, DateTime? followUpDate = null) => new()
    {
        Id = Guid.NewGuid(),
        ReferenceNumber = Guid.NewGuid().ToString("N")[..20],
        CompanyName = name,
        CompanyTypeId = 1,
        EntityTypeId = 1,
        DonationFrequencyId = 1,
        RegisteredCompanyName = name + " (Pty) Ltd",
        IncomeTaxNumber = "9012345678",
        Status = status,
        SubmissionSource = SubmissionSource.ManualCapture,
        RelationshipManagerId = relationshipManagerId,
        FollowUpDate = followUpDate,
        CreatedByUserId = _creatorId
    };

    private DonorApproval MakeApproval(Guid donorId, ApprovalStatus status) => new()
    {
        Id = Guid.NewGuid(),
        DonorId = donorId,
        RequestedByUserId = _creatorId,
        Status = status
    };

    private DonorTask MakeTask(Guid donorId, Guid assigneeId, bool isCompleted) => new()
    {
        Id = Guid.NewGuid(),
        DonorId = donorId,
        Title = "Follow up",
        DueDate = DateTime.UtcNow.AddDays(1),
        IsCompleted = isCompleted,
        AssignedToUserId = assigneeId,
        CreatedByUserId = _creatorId,
        CompletedByUserId = isCompleted ? assigneeId : null
    };

    private InteractionLog MakeLog(Guid donorId, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        DonorId = donorId,
        CreatedByUserId = _creatorId,
        InteractionType = InteractionType.Note,
        Subject = "Subject",
        Body = "Body",
        CreatedAt = createdAt
    };

    [Fact]
    public async Task GetTotalDonorsCountAsync_CountsEveryDonorRegardlessOfStatus()
    {
        using var context = await CreateSeededContextAsync();
        context.Donors.AddRange(
            MakeDonor("Active Co", DonorStatus.Active),
            MakeDonor("Pending Co", DonorStatus.PendingReview),
            MakeDonor("Rejected Co", DonorStatus.Rejected));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new DashboardRepository(context);

        Assert.Equal(3, await repo.GetTotalDonorsCountAsync());
    }

    [Fact]
    public async Task GetActiveDonorsCountAsync_CountsOnlyActiveStatus()
    {
        using var context = await CreateSeededContextAsync();
        context.Donors.AddRange(
            MakeDonor("Active Co 1", DonorStatus.Active),
            MakeDonor("Active Co 2", DonorStatus.Active),
            MakeDonor("Pending Co", DonorStatus.PendingReview));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new DashboardRepository(context);

        Assert.Equal(2, await repo.GetActiveDonorsCountAsync());
    }

    [Fact]
    public async Task GetPendingApprovalsCountAsync_CountsOnlyPendingStatus()
    {
        using var context = await CreateSeededContextAsync();
        var d1 = MakeDonor("Pending Approval Co", DonorStatus.PendingReview);
        var d2 = MakeDonor("Approved Co", DonorStatus.Active);
        var d3 = MakeDonor("Rejected Co", DonorStatus.Rejected);
        context.Donors.AddRange(d1, d2, d3);
        await context.SaveChangesAsync();

        context.DonorApprovals.AddRange(
            MakeApproval(d1.Id, ApprovalStatus.Pending),
            MakeApproval(d2.Id, ApprovalStatus.Approved),
            MakeApproval(d3.Id, ApprovalStatus.Rejected));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new DashboardRepository(context);

        Assert.Equal(1, await repo.GetPendingApprovalsCountAsync());
    }

    [Fact]
    public async Task GetOpenTaskCountAsync_ScopesToAssigneeAndExcludesCompleted()
    {
        using var context = await CreateSeededContextAsync();
        var donor = MakeDonor("Task Co");
        context.Donors.Add(donor);
        await context.SaveChangesAsync();

        context.DonorTasks.AddRange(
            MakeTask(donor.Id, _managerAId, isCompleted: false),
            MakeTask(donor.Id, _managerAId, isCompleted: false),
            MakeTask(donor.Id, _managerAId, isCompleted: true),
            MakeTask(donor.Id, _managerBId, isCompleted: false));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new DashboardRepository(context);

        Assert.Equal(2, await repo.GetOpenTaskCountAsync(_managerAId));
    }

    [Fact]
    public async Task GetOverdueFollowUpCountAsync_ScopesToManagerAndExcludesFutureOrNullDates()
    {
        using var context = await CreateSeededContextAsync();
        var asOf = new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc);

        context.Donors.AddRange(
            MakeDonor("Overdue For A", relationshipManagerId: _managerAId, followUpDate: asOf.AddDays(-1)),
            MakeDonor("Also Overdue For A", relationshipManagerId: _managerAId, followUpDate: asOf.AddDays(-10)),
            MakeDonor("Future For A", relationshipManagerId: _managerAId, followUpDate: asOf.AddDays(1)),
            MakeDonor("No Follow Up For A", relationshipManagerId: _managerAId, followUpDate: null),
            MakeDonor("Overdue For B", relationshipManagerId: _managerBId, followUpDate: asOf.AddDays(-1)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new DashboardRepository(context);

        Assert.Equal(2, await repo.GetOverdueFollowUpCountAsync(_managerAId, asOf));
    }

    [Fact]
    public async Task GetOverdueFollowUpCountAsync_BoundaryIsExclusive_FollowUpDueTodayIsNotOverdue()
    {
        using var context = await CreateSeededContextAsync();
        var asOf = new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc);

        context.Donors.Add(MakeDonor("Due Today", relationshipManagerId: _managerAId, followUpDate: asOf));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new DashboardRepository(context);

        Assert.Equal(0, await repo.GetOverdueFollowUpCountAsync(_managerAId, asOf));
    }

    [Fact]
    public async Task GetDonorsContactedCountAsync_CountsDistinctDonorsNotTotalInteractions()
    {
        using var context = await CreateSeededContextAsync();
        var donorA = MakeDonor("Donor A");
        var donorB = MakeDonor("Donor B");
        context.Donors.AddRange(donorA, donorB);
        await context.SaveChangesAsync();

        var inRange = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
        context.InteractionLogs.AddRange(
            MakeLog(donorA.Id, inRange),
            MakeLog(donorA.Id, inRange.AddHours(1)),
            MakeLog(donorB.Id, inRange.AddHours(2)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new DashboardRepository(context);
        var startUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var endUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(2, await repo.GetDonorsContactedCountAsync(startUtc, endUtc));
    }

    [Fact]
    public async Task GetDonorsContactedCountAsync_EndBoundaryIsExclusive()
    {
        using var context = await CreateSeededContextAsync();
        var donor = MakeDonor("Boundary Donor");
        context.Donors.Add(donor);
        await context.SaveChangesAsync();

        var startUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var endUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        context.InteractionLogs.AddRange(
            MakeLog(donor.Id, endUtc.AddSeconds(-1)), // just inside the range
            MakeLog(donor.Id, endUtc));               // exactly on the exclusive end -> outside
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new DashboardRepository(context);

        Assert.Equal(1, await repo.GetDonorsContactedCountAsync(startUtc, endUtc));
    }
}
