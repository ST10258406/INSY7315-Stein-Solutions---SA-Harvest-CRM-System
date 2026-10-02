using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using CRM.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Tests.Persistence.Repositories;

public class ReportsRepositoryTests
{
    private readonly DbContextOptions<CrmDbContext> _options;

    private Guid _managerAId;
    private Guid _managerBId;
    private Guid _creatorId;
    private Guid _donorForAId;
    private Guid _otherDonorForAId;
    private Guid _donorForBId;
    private Guid _unassignedDonorId;

    public ReportsRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_reportsrepo"))
            .Options;
    }

    private async Task<CrmDbContext> CreateSeededContextAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        context.InteractionLogs.RemoveRange(context.InteractionLogs);
        context.Donors.RemoveRange(context.Donors);
        context.Users.RemoveRange(context.Users);
        await context.SaveChangesAsync();

        if (!await context.LookupCompanyTypes.AnyAsync(l => l.Id == 1))
            context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true, SortOrder = 1 });
        if (!await context.LookupEntityTypes.AnyAsync(l => l.Id == 1))
            context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true, SortOrder = 1 });
        if (!await context.LookupDonationFrequencies.AnyAsync(l => l.Id == 1))
            context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true, SortOrder = 1 });
        await context.SaveChangesAsync();

        var creator = new User { Id = Guid.NewGuid(), Email = "reports-fixture@example.com", FirstName = "Case", LastName = "Worker", PasswordHash = "hash", IsActive = true };
        var managerA = new User { Id = Guid.NewGuid(), Email = "jane.doe@example.com", FirstName = "Jane", LastName = "Doe", PasswordHash = "hash", IsActive = true };
        var managerB = new User { Id = Guid.NewGuid(), Email = "john.smith@example.com", FirstName = "John", LastName = "Smith", PasswordHash = "hash", IsActive = true };
        context.Users.AddRange(creator, managerA, managerB);
        await context.SaveChangesAsync();

        _creatorId = creator.Id;
        _managerAId = managerA.Id;
        _managerBId = managerB.Id;

        var donorForA = MakeDonor("Donor For A", _creatorId, _managerAId);
        var otherDonorForA = MakeDonor("Other Donor For A", _creatorId, _managerAId);
        var donorForB = MakeDonor("Donor For B", _creatorId, _managerBId);
        var unassignedDonor = MakeDonor("Unassigned Donor", _creatorId, null);
        context.Donors.AddRange(donorForA, otherDonorForA, donorForB, unassignedDonor);
        await context.SaveChangesAsync();

        _donorForAId = donorForA.Id;
        _otherDonorForAId = otherDonorForA.Id;
        _donorForBId = donorForB.Id;
        _unassignedDonorId = unassignedDonor.Id;

        context.ChangeTracker.Clear();
        return context;
    }

    private static Donor MakeDonor(string name, Guid creatorId, Guid? rmId) => new()
    {
        Id = Guid.NewGuid(),
        ReferenceNumber = Guid.NewGuid().ToString("N")[..20],
        CompanyName = name,
        CompanyTypeId = 1,
        EntityTypeId = 1,
        DonationFrequencyId = 1,
        RegisteredCompanyName = name + " (Pty) Ltd",
        IncomeTaxNumber = "9012345678",
        Status = DonorStatus.Active,
        SubmissionSource = SubmissionSource.ManualCapture,
        RelationshipManagerId = rmId,
        CreatedByUserId = creatorId
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

    private static readonly DateTime PeriodStart = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PeriodEnd = new(2026, 7, 31, 23, 59, 59, DateTimeKind.Utc);

    [Fact]
    public async Task GetDonorsContactedAsync_DonorWithMultipleInteractions_CountsOnceNotThrice()
    {
        using var context = await CreateSeededContextAsync();
        context.InteractionLogs.AddRange(
            MakeLog(_donorForAId, new DateTime(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc)),
            MakeLog(_donorForAId, new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc)),
            MakeLog(_donorForAId, new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new ReportsRepository(context);
        var (total, byManager) = await repo.GetDonorsContactedAsync(PeriodStart, PeriodEnd, null);

        Assert.Equal(1, total);
        var managerRow = Assert.Single(byManager);
        Assert.Equal(_managerAId, managerRow.Manager.Id);
        Assert.Equal(1, managerRow.DonorsContacted);
        Assert.Equal(3, managerRow.TotalInteractions);
    }

    [Fact]
    public async Task GetDonorsContactedAsync_GroupsByManager_WithFullNameAndCorrectCounts()
    {
        using var context = await CreateSeededContextAsync();
        var day = new DateTime(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc);
        context.InteractionLogs.AddRange(
            MakeLog(_donorForAId, day),
            MakeLog(_donorForAId, day.AddHours(1)),
            MakeLog(_otherDonorForAId, day.AddHours(2)),
            MakeLog(_donorForBId, day.AddHours(3)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new ReportsRepository(context);
        var (total, byManager) = await repo.GetDonorsContactedAsync(PeriodStart, PeriodEnd, null);

        Assert.Equal(3, total);
        Assert.Equal(2, byManager.Count);

        var rowA = byManager.Single(m => m.Manager.Id == _managerAId);
        Assert.Equal("Jane Doe", rowA.Manager.FullName);
        Assert.Equal(2, rowA.DonorsContacted);
        Assert.Equal(3, rowA.TotalInteractions);

        var rowB = byManager.Single(m => m.Manager.Id == _managerBId);
        Assert.Equal("John Smith", rowB.Manager.FullName);
        Assert.Equal(1, rowB.DonorsContacted);
        Assert.Equal(1, rowB.TotalInteractions);
    }

    [Fact]
    public async Task GetDonorsContactedAsync_UnassignedDonor_CountsTowardTotalButNotByManager()
    {
        using var context = await CreateSeededContextAsync();
        var day = new DateTime(2026, 7, 12, 9, 0, 0, DateTimeKind.Utc);
        context.InteractionLogs.AddRange(
            MakeLog(_donorForAId, day),
            MakeLog(_unassignedDonorId, day));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new ReportsRepository(context);
        var (total, byManager) = await repo.GetDonorsContactedAsync(PeriodStart, PeriodEnd, null);

        Assert.Equal(2, total);
        var row = Assert.Single(byManager);
        Assert.Equal(_managerAId, row.Manager.Id);
        Assert.Equal(1, row.DonorsContacted);
    }

    [Fact]
    public async Task GetDonorsContactedAsync_ExcludesInteractionsOutsideDateRange()
    {
        using var context = await CreateSeededContextAsync();
        context.InteractionLogs.AddRange(
            MakeLog(_donorForAId, new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc)), // just before
            MakeLog(_donorForAId, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)),     // exactly at start
            MakeLog(_donorForAId, new DateTime(2026, 7, 31, 23, 59, 59, DateTimeKind.Utc)), // exactly at end
            MakeLog(_donorForAId, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)));    // just after
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new ReportsRepository(context);
        var (total, byManager) = await repo.GetDonorsContactedAsync(PeriodStart, PeriodEnd, null);

        Assert.Equal(1, total);
        Assert.Equal(2, byManager.Single().TotalInteractions);
    }

    [Fact]
    public async Task GetDonorsContactedAsync_RelationshipManagerFilter_ScopesTotalAndByManager()
    {
        using var context = await CreateSeededContextAsync();
        var day = new DateTime(2026, 7, 12, 9, 0, 0, DateTimeKind.Utc);
        context.InteractionLogs.AddRange(
            MakeLog(_donorForAId, day),
            MakeLog(_donorForBId, day));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new ReportsRepository(context);
        var (total, byManager) = await repo.GetDonorsContactedAsync(PeriodStart, PeriodEnd, _managerAId);

        Assert.Equal(1, total);
        var row = Assert.Single(byManager);
        Assert.Equal(_managerAId, row.Manager.Id);
    }

    [Fact]
    public async Task GetDonorsContactedAsync_NoInteractionsInPeriod_ReturnsZeroAndEmptyList()
    {
        using var context = await CreateSeededContextAsync();

        var repo = new ReportsRepository(context);
        var (total, byManager) = await repo.GetDonorsContactedAsync(PeriodStart, PeriodEnd, null);

        Assert.Equal(0, total);
        Assert.Empty(byManager);
    }

    private async Task<(CrmDbContext Context, short JhbId, short CptId)> CreateSeededContextForRegionsAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        context.DonorOperationalRegions.RemoveRange(context.DonorOperationalRegions);
        context.Donors.RemoveRange(context.Donors);
        context.Users.RemoveRange(context.Users);
        context.LookupOperationalRegions.RemoveRange(context.LookupOperationalRegions);
        await context.SaveChangesAsync();

        if (!await context.LookupCompanyTypes.AnyAsync(l => l.Id == 1))
            context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true, SortOrder = 1 });
        if (!await context.LookupEntityTypes.AnyAsync(l => l.Id == 1))
            context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true, SortOrder = 1 });
        if (!await context.LookupDonationFrequencies.AnyAsync(l => l.Id == 1))
            context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true, SortOrder = 1 });
        await context.SaveChangesAsync();

        var jhb = new LookupOperationalRegion { Id = 1, Code = "JHB", Name = "Johannesburg", IsActive = true, SortOrder = 1 };
        var cpt = new LookupOperationalRegion { Id = 2, Code = "CPT", Name = "Cape Town", IsActive = true, SortOrder = 2 };
        var inactive = new LookupOperationalRegion { Id = 3, Code = "DBN", Name = "Durban", IsActive = false, SortOrder = 3 };
        context.LookupOperationalRegions.AddRange(jhb, cpt, inactive);

        var creator = new User { Id = Guid.NewGuid(), Email = "regions-fixture@example.com", FirstName = "Case", LastName = "Worker", PasswordHash = "hash", IsActive = true };
        context.Users.Add(creator);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        return (context, jhb.Id, cpt.Id);
    }

    [Fact]
    public async Task GetDonorsByRegionAsync_RegionWithNoDonors_StillAppearsWithZeroCount()
    {
        var (context, _, _) = await CreateSeededContextForRegionsAsync();
        using var ctx = context;

        var repo = new ReportsRepository(ctx);
        var result = await repo.GetDonorsByRegionAsync();

        Assert.Equal(2, result.Count); // active regions only: JHB, CPT
        Assert.All(result, r => Assert.Equal(0, r.DonorCount));
    }

    [Fact]
    public async Task GetDonorsByRegionAsync_DonorInTwoRegions_CountsOnceInEachRegion()
    {
        var (context, jhbId, cptId) = await CreateSeededContextForRegionsAsync();
        using var ctx = context;

        var creatorId = ctx.Users.Single().Id;
        var donor = MakeDonor("Multi Region Donor", creatorId, null);
        ctx.Donors.Add(donor);
        await ctx.SaveChangesAsync();

        ctx.DonorOperationalRegions.AddRange(
            new DonorOperationalRegion { DonorId = donor.Id, OperationalRegionId = jhbId },
            new DonorOperationalRegion { DonorId = donor.Id, OperationalRegionId = cptId });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var repo = new ReportsRepository(ctx);
        var result = await repo.GetDonorsByRegionAsync();

        var jhb = result.Single(r => r.Region == "JHB");
        var cpt = result.Single(r => r.Region == "CPT");
        Assert.Equal(1, jhb.DonorCount);
        Assert.Equal(1, cpt.DonorCount);
    }

    [Fact]
    public async Task GetDonorsByRegionAsync_ExcludesInactiveRegions()
    {
        var (context, _, _) = await CreateSeededContextForRegionsAsync();
        using var ctx = context;

        var repo = new ReportsRepository(ctx);
        var result = await repo.GetDonorsByRegionAsync();

        Assert.DoesNotContain(result, r => r.Region == "DBN");
    }

    [Fact]
    public async Task GetDonorsByRegionAsync_ReturnsRegionCodeAndName()
    {
        var (context, _, _) = await CreateSeededContextForRegionsAsync();
        using var ctx = context;

        var repo = new ReportsRepository(ctx);
        var result = await repo.GetDonorsByRegionAsync();

        var jhb = result.Single(r => r.Region == "JHB");
        Assert.Equal("Johannesburg", jhb.RegionName);
    }

    private async Task<(CrmDbContext Context, short MeatId, short DairyId)> CreateSeededContextForTypesAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        context.DonorDonationTypes.RemoveRange(context.DonorDonationTypes);
        context.Donors.RemoveRange(context.Donors);
        context.Users.RemoveRange(context.Users);
        context.LookupDonationTypes.RemoveRange(context.LookupDonationTypes);
        await context.SaveChangesAsync();

        if (!await context.LookupCompanyTypes.AnyAsync(l => l.Id == 1))
            context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true, SortOrder = 1 });
        if (!await context.LookupEntityTypes.AnyAsync(l => l.Id == 1))
            context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true, SortOrder = 1 });
        if (!await context.LookupDonationFrequencies.AnyAsync(l => l.Id == 1))
            context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true, SortOrder = 1 });
        await context.SaveChangesAsync();

        var meat = new LookupDonationType { Id = 1, Name = "Meat", IsActive = true, SortOrder = 1 };
        var dairy = new LookupDonationType { Id = 2, Name = "Dairy", IsActive = true, SortOrder = 2 };
        var inactive = new LookupDonationType { Id = 3, Name = "Discontinued", IsActive = false, SortOrder = 3 };
        context.LookupDonationTypes.AddRange(meat, dairy, inactive);

        var creator = new User { Id = Guid.NewGuid(), Email = "types-fixture@example.com", FirstName = "Case", LastName = "Worker", PasswordHash = "hash", IsActive = true };
        context.Users.Add(creator);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        return (context, meat.Id, dairy.Id);
    }

    [Fact]
    public async Task GetDonorsByTypeAsync_TypeWithNoDonors_StillAppearsWithZeroCount()
    {
        var (context, _, _) = await CreateSeededContextForTypesAsync();
        using var ctx = context;

        var repo = new ReportsRepository(ctx);
        var result = await repo.GetDonorsByTypeAsync();

        Assert.Equal(2, result.Count); // active types only: Meat, Dairy
        Assert.All(result, t => Assert.Equal(0, t.DonorCount));
    }

    [Fact]
    public async Task GetDonorsByTypeAsync_DonorWithTwoTypes_CountsOnceInEachType()
    {
        var (context, meatId, dairyId) = await CreateSeededContextForTypesAsync();
        using var ctx = context;

        var creatorId = ctx.Users.Single().Id;
        var donor = MakeDonor("Multi Type Donor", creatorId, null);
        ctx.Donors.Add(donor);
        await ctx.SaveChangesAsync();

        ctx.DonorDonationTypes.AddRange(
            new DonorDonationType { DonorId = donor.Id, DonationTypeId = meatId },
            new DonorDonationType { DonorId = donor.Id, DonationTypeId = dairyId });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var repo = new ReportsRepository(ctx);
        var result = await repo.GetDonorsByTypeAsync();

        var meat = result.Single(t => t.DonationType == "Meat");
        var dairy = result.Single(t => t.DonationType == "Dairy");
        Assert.Equal(1, meat.DonorCount);
        Assert.Equal(1, dairy.DonorCount);
    }

    [Fact]
    public async Task GetDonorsByTypeAsync_ExcludesInactiveTypes()
    {
        var (context, _, _) = await CreateSeededContextForTypesAsync();
        using var ctx = context;

        var repo = new ReportsRepository(ctx);
        var result = await repo.GetDonorsByTypeAsync();

        Assert.DoesNotContain(result, t => t.DonationType == "Discontinued");
    }

    private async Task<CrmDbContext> CreateSeededContextForStatusAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        context.Donors.RemoveRange(context.Donors);
        context.Users.RemoveRange(context.Users);
        await context.SaveChangesAsync();

        if (!await context.LookupCompanyTypes.AnyAsync(l => l.Id == 1))
            context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true, SortOrder = 1 });
        if (!await context.LookupEntityTypes.AnyAsync(l => l.Id == 1))
            context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true, SortOrder = 1 });
        if (!await context.LookupDonationFrequencies.AnyAsync(l => l.Id == 1))
            context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true, SortOrder = 1 });
        await context.SaveChangesAsync();

        var creator = new User { Id = Guid.NewGuid(), Email = "status-fixture@example.com", FirstName = "Case", LastName = "Worker", PasswordHash = "hash", IsActive = true };
        context.Users.Add(creator);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        return context;
    }

    private static Donor MakeDonorWithStatus(string name, Guid creatorId, DonorStatus status) => new()
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
        CreatedByUserId = creatorId
    };

    [Fact]
    public async Task GetDonorsByStatusAsync_StatusWithNoDonors_StillAppearsWithZeroCount()
    {
        using var context = await CreateSeededContextForStatusAsync();

        var repo = new ReportsRepository(context);
        var result = await repo.GetDonorsByStatusAsync();

        Assert.Equal(4, result.Count);
        Assert.All(result, r => Assert.Equal(0, r.DonorCount));
    }

    [Fact]
    public async Task GetDonorsByStatusAsync_CountsDonorsPerStatus()
    {
        using var context = await CreateSeededContextForStatusAsync();
        var creatorId = context.Users.Single().Id;

        context.Donors.AddRange(
            MakeDonorWithStatus("Active Donor 1", creatorId, DonorStatus.Active),
            MakeDonorWithStatus("Active Donor 2", creatorId, DonorStatus.Active),
            MakeDonorWithStatus("Pending Donor", creatorId, DonorStatus.PendingReview),
            MakeDonorWithStatus("Rejected Donor", creatorId, DonorStatus.Rejected));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new ReportsRepository(context);
        var result = await repo.GetDonorsByStatusAsync();

        Assert.Equal(4, result.Count);
        Assert.Equal(2, result.Single(r => r.Status == "Active").DonorCount);
        Assert.Equal(1, result.Single(r => r.Status == "PendingReview").DonorCount);
        Assert.Equal(0, result.Single(r => r.Status == "Lapsed").DonorCount);
        Assert.Equal(1, result.Single(r => r.Status == "Rejected").DonorCount);
    }

    [Fact]
    public async Task GetDonorsByStatusAsync_ReturnsFixedDisplayOrder()
    {
        using var context = await CreateSeededContextForStatusAsync();

        var repo = new ReportsRepository(context);
        var result = await repo.GetDonorsByStatusAsync();

        Assert.Equal(
            new[] { "Active", "PendingReview", "Lapsed", "Rejected" },
            result.Select(r => r.Status));
    }
}
