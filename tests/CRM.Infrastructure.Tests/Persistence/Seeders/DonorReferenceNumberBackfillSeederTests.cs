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
    public async Task SeedAsync_LegacyRowSharesAYearWithAnAlreadyRealNumber_ContinuesAfterItInsteadOfColliding()
    {
        // Regression test: numbering legacy rows purely from "how many are in
        // this batch" (starting back at 00001) would either violate the unique
        // index against an already-real DON-2025-NNNNN row for the same year,
        // or — if it happened not to collide outright — could still leave the
        // sequence set too low. This proves the legacy row is numbered to
        // continue AFTER the highest already-real number for that year.
        // Uses a year no other test in this file touches — the underlying
        // Postgres sequence for a year isn't reset between tests (only the
        // Donors rows are), so reusing one across tests is its own hazard.
        using var context = await CreateSeededContextAsync();

        var alreadyReal = MakeLegacyDonor("real", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        alreadyReal.ReferenceNumber = "DON-2025-00005"; // not a legacy placeholder
        var legacy = MakeLegacyDonor("gggggggg", new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        context.Donors.AddRange(alreadyReal, legacy);
        await context.SaveChangesAsync();

#pragma warning disable EF1002
        await context.Database.ExecuteSqlRawAsync("DROP SEQUENCE IF EXISTS donor_reference_seq_2025");
#pragma warning restore EF1002

        await DonorReferenceNumberBackfillSeeder.SeedAsync(context);
        context.ChangeTracker.Clear();

        var reloadedLegacy = await context.Donors.SingleAsync(d => d.Id == legacy.Id);
        Assert.Equal("DON-2025-00006", reloadedLegacy.ReferenceNumber); // continues after 00005, not 00001

        var sequenceName = "donor_reference_seq_2025";
        var next = await context.Database
            .SqlQuery<long>($"SELECT nextval({sequenceName}) AS \"Value\"")
            .SingleAsync();
        Assert.Equal(7, next); // sequence left at 6 (the highest assigned), so nextval() = 7
    }

    [Fact]
    public async Task SeedAsync_SequenceAlreadyAheadOfTheLegacyBatch_NeverMovesItBackward()
    {
        // Regression test for the sequence-regression half of the same class of
        // bug: if the real per-year sequence was already advanced past what
        // this (small) legacy batch computes, setval must never move it back
        // down — doing so would let a future real submission reissue an
        // already-used number.
        using var context = await CreateSeededContextAsync();

        var year = 2020;
        var sequenceName = $"donor_reference_seq_{year}";
#pragma warning disable EF1002
        await context.Database.ExecuteSqlRawAsync($"DROP SEQUENCE IF EXISTS {sequenceName}");
        await context.Database.ExecuteSqlRawAsync($"CREATE SEQUENCE {sequenceName} START 1");
        await context.Database.ExecuteSqlRawAsync($"SELECT setval('{sequenceName}', 50)"); // already well ahead
#pragma warning restore EF1002

        var legacy = MakeLegacyDonor("hhhhhhhh", new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        context.Donors.Add(legacy);
        await context.SaveChangesAsync();

        await DonorReferenceNumberBackfillSeeder.SeedAsync(context);

        var next = await context.Database
            .SqlQuery<long>($"SELECT nextval({sequenceName}) AS \"Value\"")
            .SingleAsync();
        Assert.Equal(51, next); // unchanged by the small legacy batch — still continues from 50, not from 1
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
