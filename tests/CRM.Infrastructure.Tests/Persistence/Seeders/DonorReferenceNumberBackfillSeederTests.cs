using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Seeders;
using CRM.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Tests.Persistence.Seeders;

public class DonorReferenceNumberBackfillSeederTests
{
    private readonly DbContextOptions<CrmDbContext> _options;
    private Guid _creatorId;

    public DonorReferenceNumberBackfillSeederTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_donorrefbackfillseeder"))
            .Options;
    }

    private async Task<CrmDbContext> CreateSeededContextAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        context.Donors.RemoveRange(context.Donors);
        await context.SaveChangesAsync();

        if (!await context.LookupCompanyTypes.AnyAsync())
            context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true });
        if (!await context.LookupEntityTypes.AnyAsync())
            context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true });
        if (!await context.LookupDonationFrequencies.AnyAsync())
            context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true });

        var creator = await context.Users.FirstOrDefaultAsync(u => u.Email == "backfill-seeder-fixture@example.com");
        if (creator is null)
        {
            creator = new User
            {
                Id = Guid.NewGuid(),
                Email = "backfill-seeder-fixture@example.com",
                FirstName = "Fixture",
                LastName = "User",
                PasswordHash = "hash",
                IsActive = true
            };
            context.Users.Add(creator);
        }

        await context.SaveChangesAsync();
        _creatorId = creator.Id;
        return context;
    }

    private Donor MakeLegacyDonor(string placeholderSuffix, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        ReferenceNumber = $"DON-LEGACY-{placeholderSuffix}",
        CompanyName = $"Legacy Co {placeholderSuffix}",
        RegisteredCompanyName = $"Legacy Co {placeholderSuffix}",
        CompanyTypeId = 1,
        EntityTypeId = 1,
        DonationFrequencyId = 1,
        CollectionAddress = "Gate 1",
        Status = DonorStatus.Active,
        SubmissionSource = SubmissionSource.ManualCapture,
        CreatedByUserId = _creatorId,
        CreatedAt = createdAt
    };

    [Fact]
    public async Task SeedAsync_LegacyPlaceholderRows_GetsRealPerYearReferenceNumbers()
    {
        using var context = await CreateSeededContextAsync();

        var older = MakeLegacyDonor("aaaaaaaa", new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = MakeLegacyDonor("bbbbbbbb", new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        context.Donors.AddRange(older, newer);
        await context.SaveChangesAsync();

        await DonorReferenceNumberBackfillSeeder.SeedAsync(context);
        context.ChangeTracker.Clear();

        var reloadedOlder = await context.Donors.SingleAsync(d => d.Id == older.Id);
        var reloadedNewer = await context.Donors.SingleAsync(d => d.Id == newer.Id);

        // Ordered by CreatedAt within the year — the earlier row gets 00001.
        Assert.Equal("DON-2024-00001", reloadedOlder.ReferenceNumber);
        Assert.Equal("DON-2024-00002", reloadedNewer.ReferenceNumber);
    }

    [Fact]
    public async Task SeedAsync_PartitionsByTheDonorsOwnCreatedAtYear_NotTheCurrentYear()
    {
        using var context = await CreateSeededContextAsync();

        var donor2023 = MakeLegacyDonor("cccccccc", new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        context.Donors.Add(donor2023);
        await context.SaveChangesAsync();

        await DonorReferenceNumberBackfillSeeder.SeedAsync(context);
        context.ChangeTracker.Clear();

        var reloaded = await context.Donors.SingleAsync(d => d.Id == donor2023.Id);
        Assert.StartsWith("DON-2023-", reloaded.ReferenceNumber);
    }

    [Fact]
    public async Task SeedAsync_AdvancesTheYearSequence_SoTheNextRealAllocationDoesNotCollide()
    {
        using var context = await CreateSeededContextAsync();

        var year = 2022;
        var sequenceName = $"donor_reference_seq_{year}";
#pragma warning disable EF1002
        await context.Database.ExecuteSqlRawAsync($"DROP SEQUENCE IF EXISTS {sequenceName}");
#pragma warning restore EF1002

        var legacy1 = MakeLegacyDonor("dddddddd", new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var legacy2 = MakeLegacyDonor("eeeeeeee", new DateTime(year, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        context.Donors.AddRange(legacy1, legacy2);
        await context.SaveChangesAsync();

        await DonorReferenceNumberBackfillSeeder.SeedAsync(context);

        // IDonorRepository.GetNextReferenceNumberAsync always allocates for the
        // CURRENT year, so to prove the *2022* sequence specifically continued
        // past the 2 backfilled rows, read the sequence directly instead.
        var next = await context.Database
            .SqlQuery<long>($"SELECT nextval({sequenceName}) AS \"Value\"")
            .SingleAsync();

        Assert.Equal(3, next); // continues after the 2 rows backfilled above
    }

    [Fact]
    public async Task SeedAsync_NoLegacyRows_IsANoOp()
    {
        using var context = await CreateSeededContextAsync();

        var normal = MakeLegacyDonor("shouldnotmatter", DateTime.UtcNow);
        normal.ReferenceNumber = "DON-2026-00042"; // not a legacy placeholder
        context.Donors.Add(normal);
        await context.SaveChangesAsync();

        await DonorReferenceNumberBackfillSeeder.SeedAsync(context);
        context.ChangeTracker.Clear();

        var reloaded = await context.Donors.SingleAsync(d => d.Id == normal.Id);
        Assert.Equal("DON-2026-00042", reloaded.ReferenceNumber);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_IsIdempotent()
    {
        using var context = await CreateSeededContextAsync();

        var legacy = MakeLegacyDonor("ffffffff", new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        context.Donors.Add(legacy);
        await context.SaveChangesAsync();

        await DonorReferenceNumberBackfillSeeder.SeedAsync(context);
        context.ChangeTracker.Clear();
        var afterFirstRun = (await context.Donors.SingleAsync(d => d.Id == legacy.Id)).ReferenceNumber;

        // Second run should find no more "DON-LEGACY-" rows and do nothing.
        await DonorReferenceNumberBackfillSeeder.SeedAsync(context);
        context.ChangeTracker.Clear();
        var afterSecondRun = (await context.Donors.SingleAsync(d => d.Id == legacy.Id)).ReferenceNumber;

        Assert.Equal(afterFirstRun, afterSecondRun);
        Assert.Equal("DON-2021-00001", afterSecondRun);
    }
}
