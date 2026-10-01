using AutoMapper;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Mappings;
using CRM.Application.Modules.Lookups.Mappings;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using CRM.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.Infrastructure.Tests.Persistence.Repositories;

/// <summary>
/// DonorRepository.SearchAsync is the only genuinely provider-specific query in the
/// codebase — case-insensitive search, a nav-property filter through a junction table,
/// and a correlated subquery sort. It is therefore exercised against real PostgreSQL
/// rather than an in-memory fake.
/// </summary>
public class DonorRepositoryTests
{
    private readonly DbContextOptions<CrmDbContext> _options;
    private readonly IMapper _mapper;

    private static readonly short CompanyTypeId = 1;
    private static readonly short EntityTypeId = 1;
    private static readonly short FrequencyId = 1;
    private static readonly short ProvinceId = 1;
    private static readonly short DonationTypeId = 1;

    // Set by CreateSeededContextAsync. Tests within an xunit test class run serially,
    // so a single mutable field is safe here.
    private Guid _creatorId;

    public DonorRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_donorrepository"))
            .Options;

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<DonorMappingProfile>();
            cfg.AddProfile<LookupsMappingProfile>();
        }, NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    /// <summary>Fresh context with the schema created and donor rows wiped, reference data present.</summary>
    private async Task<CrmDbContext> CreateSeededContextAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        // Wipe donor-scoped data between tests; reference data is upserted below.
        // interaction_logs is append-only (no-negotiable rule: no DELETE statements
        // against it anywhere in the codebase) — it is never targeted directly here.
        // Removing the parent Donor cascades to InteractionLog at the database level
        // (see InteractionLogConfiguration: OnDelete(DeleteBehavior.Cascade)), so no
        // EF-tracked DELETE is ever generated against interaction_logs by this fixture.
        context.DonorOperationalRegions.RemoveRange(context.DonorOperationalRegions);
        context.DonorDonationTypes.RemoveRange(context.DonorDonationTypes);
        context.DonorLegalAddresses.RemoveRange(context.DonorLegalAddresses);
        context.DonorContacts.RemoveRange(context.DonorContacts);
        context.DonorDocuments.RemoveRange(context.DonorDocuments.IgnoreQueryFilters());
        context.DonorApprovals.RemoveRange(context.DonorApprovals);
        context.Donors.RemoveRange(context.Donors);
        await context.SaveChangesAsync();

        if (!await context.LookupCompanyTypes.AnyAsync(l => l.Id == CompanyTypeId))
            context.LookupCompanyTypes.Add(new LookupCompanyType { Id = CompanyTypeId, Name = "Manufacturer", IsActive = true, SortOrder = 1 });
        if (!await context.LookupEntityTypes.AnyAsync(l => l.Id == EntityTypeId))
            context.LookupEntityTypes.Add(new LookupEntityType { Id = EntityTypeId, Name = "Private Company", IsActive = true, SortOrder = 1 });
        if (!await context.LookupDonationFrequencies.AnyAsync(l => l.Id == FrequencyId))
            context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = FrequencyId, Name = "Monthly", IsActive = true, SortOrder = 1 });
        if (!await context.LookupProvinces.AnyAsync(l => l.Id == ProvinceId))
            context.LookupProvinces.Add(new LookupProvince { Id = ProvinceId, Code = "GP", Name = "Gauteng", IsActive = true, SortOrder = 1 });
        if (!await context.LookupDonationTypes.AnyAsync(l => l.Id == DonationTypeId))
            context.LookupDonationTypes.Add(new LookupDonationType { Id = DonationTypeId, Name = "Meat", IsActive = true, SortOrder = 1 });
        foreach (var (id, code) in new[] { ((short)1, "JHB"), ((short)2, "CPT") })
        {
            if (!await context.LookupOperationalRegions.AnyAsync(l => l.Id == id))
                context.LookupOperationalRegions.Add(new LookupOperationalRegion { Id = id, Code = code, Name = code, IsActive = true, SortOrder = id });
        }

        var creator = await context.Users.FirstOrDefaultAsync(u => u.Email == "donor-repo-fixture@example.com");
        if (creator is null)
        {
            creator = new User
            {
                Id = Guid.NewGuid(),
                Email = "donor-repo-fixture@example.com",
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

    private static Donor MakeDonor(
        string companyName,
        Guid creatorId,
        DonorStatus status = DonorStatus.Active,
        short regionId = 1,
        DateTime? createdAt = null)
    {
        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = Guid.NewGuid().ToString("N")[..20],
            CompanyName = companyName,
            CompanyTypeId = CompanyTypeId,
            EntityTypeId = EntityTypeId,
            DonationFrequencyId = FrequencyId,
            RegisteredCompanyName = companyName + " (Pty) Ltd",
            TradingName = companyName,
            CompanyRegistrationNumber = "2020/000000/07",
            IncomeTaxNumber = "9012345678",
            CollectionAddress = "Gate 1",
            Status = status,
            SubmissionSource = SubmissionSource.ManualCapture,
            CreatedByUserId = creatorId,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };

        donor.OperationalRegions.Add(new DonorOperationalRegion { DonorId = donor.Id, OperationalRegionId = regionId });
        donor.DonationTypes.Add(new DonorDonationType { DonorId = donor.Id, DonationTypeId = DonationTypeId });

        return donor;
    }

    [Fact]
    public async Task SearchAsync_SearchByName_IsCaseInsensitiveAndMatchesSubstring()
    {
        using var context = await CreateSeededContextAsync();

        context.Donors.AddRange(
            MakeDonor("FoodCorp SA", _creatorId),
            MakeDonor("Other Company", _creatorId));
        await context.SaveChangesAsync();

        var repository = new DonorRepository(context, _mapper);

        // Lowercase needle against a mixed-case stored value — this is the
        // .ToLower().Contains() path translated to SQL by Npgsql.
        var (items, totalCount) = await repository.SearchAsync(new DonorSearchCriteria { Search = "foodcorp" });

        Assert.Equal(1, totalCount);
        Assert.Equal("FoodCorp SA", Assert.Single(items).CompanyName);

        // And the same needle in the other casing finds the same row.
        var (upperItems, _) = await repository.SearchAsync(new DonorSearchCriteria { Search = "FOODCORP" });
        Assert.Equal("FoodCorp SA", Assert.Single(upperItems).CompanyName);
    }

    [Fact]
    public async Task SearchAsync_FiltersByRegionCode_ThroughTheJunctionNavigationProperty()
    {
        using var context = await CreateSeededContextAsync();

        context.Donors.AddRange(
            MakeDonor("Jhb Donor", _creatorId, regionId: 1),
            MakeDonor("Cpt Donor", _creatorId, regionId: 2));
        await context.SaveChangesAsync();

        var repository = new DonorRepository(context, _mapper);

        var (items, totalCount) = await repository.SearchAsync(new DonorSearchCriteria { RegionCode = "CPT" });

        Assert.Equal(1, totalCount);
        var item = Assert.Single(items);
        Assert.Equal("Cpt Donor", item.CompanyName);
        Assert.Contains("CPT", item.OperationalRegions);
    }

    [Fact]
    public async Task SearchAsync_SortByLastInteractionDate_UsesTheLatestLogPerDonor()
    {
        using var context = await CreateSeededContextAsync();

        var recent = MakeDonor("Recent Co", _creatorId);
        var stale = MakeDonor("Stale Co", _creatorId);
        var never = MakeDonor("Never Co", _creatorId);
        context.Donors.AddRange(recent, stale, never);
        await context.SaveChangesAsync();

        // "Stale Co" has a newer log too, but an older *latest* log than "Recent Co" —
        // this is what distinguishes the correlated-subquery sort from a plain join.
        context.InteractionLogs.AddRange(
            MakeLog(recent.Id, _creatorId, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)),
            MakeLog(recent.Id, _creatorId, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            MakeLog(stale.Id, _creatorId, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)),
            MakeLog(stale.Id, _creatorId, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();

        var repository = new DonorRepository(context, _mapper);

        var (desc, _) = await repository.SearchAsync(
            new DonorSearchCriteria { SortBy = "lastInteractionDate", SortDir = "desc" });

        // Documents PostgreSQL's default null ordering, which this query inherits:
        // DESC is NULLS FIRST, so the donor with no interactions leads; among donors
        // that do have logs, the one with the newer *latest* log comes first.
        Assert.Equal(["Never Co", "Recent Co", "Stale Co"], desc.Select(d => d.CompanyName));
        Assert.Null(desc[0].LastInteractionDate);
        Assert.Equal(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), desc[1].LastInteractionDate);
        Assert.Equal(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), desc[2].LastInteractionDate);

        var (asc, _) = await repository.SearchAsync(
            new DonorSearchCriteria { SortBy = "lastInteractionDate", SortDir = "asc" });

        // ASC is NULLS LAST, so the donor with no interactions trails.
        Assert.Equal(["Stale Co", "Recent Co", "Never Co"], asc.Select(d => d.CompanyName));
    }

    private static InteractionLog MakeLog(Guid donorId, Guid userId, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        DonorId = donorId,
        CreatedByUserId = userId,
        InteractionType = InteractionType.Note,
        Body = "note",
        CreatedAt = createdAt
    };

    [Fact]
    public async Task SearchAsync_FiltersByStatus()
    {
        using var context = await CreateSeededContextAsync();

        context.Donors.AddRange(
            MakeDonor("Active Co", _creatorId, DonorStatus.Active),
            MakeDonor("Lapsed Co", _creatorId, DonorStatus.Lapsed));
        await context.SaveChangesAsync();

        var repository = new DonorRepository(context, _mapper);

        var (items, totalCount) = await repository.SearchAsync(
            new DonorSearchCriteria { Status = DonorStatus.Active });

        Assert.Equal(1, totalCount);
        Assert.Equal("Active Co", Assert.Single(items).CompanyName);
    }

    [Fact]
    public async Task SearchAsync_TotalCountIgnoresPagination()
    {
        using var context = await CreateSeededContextAsync();

        context.Donors.AddRange(Enumerable.Range(1, 25)
            .Select(i => MakeDonor($"Donor {i:00}", _creatorId)));
        await context.SaveChangesAsync();

        var repository = new DonorRepository(context, _mapper);

        var (items, totalCount) = await repository.SearchAsync(
            new DonorSearchCriteria { Page = 2, PageSize = 10, SortBy = "companyName", SortDir = "asc" });

        Assert.Equal(25, totalCount);
        Assert.Equal(10, items.Count);
        Assert.Equal("Donor 11", items[0].CompanyName);
    }

    [Fact]
    public async Task SearchAsync_NoSortBy_DefaultsToNewestFirst()
    {
        using var context = await CreateSeededContextAsync();

        context.Donors.AddRange(
            MakeDonor("Older Co", _creatorId, createdAt: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            MakeDonor("Newer Co", _creatorId, createdAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();

        var repository = new DonorRepository(context, _mapper);

        var (items, _) = await repository.SearchAsync(new DonorSearchCriteria());

        Assert.Equal(["Newer Co", "Older Co"], items.Select(d => d.CompanyName));
    }

    [Fact]
    public async Task GetDetailByIdAsync_ProjectsNestedSections()
    {
        using var context = await CreateSeededContextAsync();

        var donor = MakeDonor("FoodCorp SA", _creatorId);
        donor.LegalAddress = new DonorLegalAddress
        {
            DonorId = donor.Id,
            StreetAddress = "12 Industrial Road",
            Suburb = "Meadowdale",
            City = "Johannesburg",
            ProvinceId = ProvinceId,
            PostalCode = "1609"
        };
        donor.Contacts.Add(new DonorContact
        {
            DonorId = donor.Id,
            ContactType = ContactType.Primary,
            Name = "John Smith",
            Phone = "+27821234567",
            Email = "john@foodcorp.co.za"
        });
        context.Donors.Add(donor);
        await context.SaveChangesAsync();

        var repository = new DonorRepository(context, _mapper);

        var result = await repository.GetDetailByIdAsync(donor.Id);

        Assert.NotNull(result);
        Assert.Equal(donor.Id, result!.Id);
        Assert.Equal("Active", result.Status);
        Assert.Equal("FoodCorp SA", result.Company.CompanyName);
        Assert.Equal("Manufacturer", result.Company.CompanyType.Name);
        Assert.Equal("John Smith", result.PrimaryContact.Name);
        Assert.Null(result.MarketingContact);
        Assert.Equal("GP", result.LegalAddress!.Province.Code);
        Assert.Equal("Monthly", result.Donations.Frequency.Name);
        Assert.Contains(result.Donations.Types, t => t.Name == "Meat");
        Assert.Contains(result.Donations.OperationalRegions, r => r.Code == "JHB");
    }

    [Fact]
    public async Task GetDetailByIdAsync_ListsOnlyActiveDocuments()
    {
        // Security review F-06: a soft-deleted or superseded document must not appear in the
        // donor detail response — the global query filter on DonorDocument applies to the
        // Donor.Documents navigation inside the projection too.
        using var context = await CreateSeededContextAsync();

        var donor = MakeDonor("FoodCorp SA", _creatorId);
        var active = MakeDocument(donor.Id, DocumentType.BBBEECertificate, isActive: true);
        var deleted = MakeDocument(donor.Id, DocumentType.Signature, isActive: false);
        context.Donors.Add(donor);
        context.DonorDocuments.AddRange(active, deleted);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new DonorRepository(context, _mapper);

        var result = await repository.GetDetailByIdAsync(donor.Id);

        Assert.Equal(active.Id, Assert.Single(result!.Compliance.Documents).Id);
    }

    private static DonorDocument MakeDocument(Guid donorId, DocumentType type, bool isActive) => new()
    {
        Id = Guid.NewGuid(),
        DonorId = donorId,
        DocumentType = type,
        FileName = "file.pdf",
        BlobStoragePath = $"donors/{donorId}/{type}/{Guid.NewGuid()}_file.pdf",
        MimeType = "application/pdf",
        FileSizeBytes = 1024,
        IsActive = isActive
    };

    [Fact]
    public async Task GetDetailByIdAsync_UnknownId_ReturnsNull()
    {
        using var context = await CreateSeededContextAsync();
        var repository = new DonorRepository(context, _mapper);

        Assert.Null(await repository.GetDetailByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetForUpdateAsync_LoadsTheAggregateAndTracksIt()
    {
        using var context = await CreateSeededContextAsync();

        var donor = MakeDonor("FoodCorp SA", _creatorId);
        donor.LegalAddress = new DonorLegalAddress
        {
            DonorId = donor.Id,
            StreetAddress = "12 Industrial Road",
            Suburb = "Meadowdale",
            City = "Johannesburg",
            ProvinceId = ProvinceId,
            PostalCode = "1609"
        };
        donor.Contacts.Add(new DonorContact
        {
            DonorId = donor.Id,
            ContactType = ContactType.Primary,
            Name = "John Smith",
            Phone = "+27821234567",
            Email = "john@foodcorp.co.za"
        });
        context.Donors.Add(donor);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new DonorRepository(context, _mapper);

        var loaded = await repository.GetForUpdateAsync(donor.Id);

        Assert.NotNull(loaded);
        Assert.Single(loaded!.Contacts);
        Assert.NotNull(loaded.LegalAddress);
        Assert.Single(loaded.OperationalRegions);
        Assert.Single(loaded.DonationTypes);

        // Tracked: a mutation is picked up by SaveChangesAsync without any explicit Update call.
        loaded.CompanyName = "Renamed Co";
        await new UnitOfWork(context).SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Equal("Renamed Co", (await context.Donors.FindAsync(donor.Id))!.CompanyName);
    }

    [Fact]
    public async Task ExistsAsync_ReflectsPresence()
    {
        using var context = await CreateSeededContextAsync();

        var donor = MakeDonor("FoodCorp SA", _creatorId);
        context.Donors.Add(donor);
        await context.SaveChangesAsync();

        var repository = new DonorRepository(context, _mapper);

        Assert.True(await repository.ExistsAsync(donor.Id));
        Assert.False(await repository.ExistsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ClaimBySubmissionTokenAsync_MatchingUnexpiredToken_ReturnsDonorIdAndBurnsTheToken()
    {
        using var context = await CreateSeededContextAsync();

        var donor = MakeDonor("Doc Test Pty Ltd", _creatorId);
        donor.SubmissionToken = "matching-token";
        donor.SubmissionTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        context.Donors.Add(donor);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new DonorRepository(context, _mapper);

        var claimedId = await repository.ClaimBySubmissionTokenAsync("matching-token");

        Assert.Equal(donor.Id, claimedId);

        context.ChangeTracker.Clear();
        var reloaded = await context.Donors.FindAsync(donor.Id);
        Assert.Null(reloaded!.SubmissionToken);
        Assert.Null(reloaded.SubmissionTokenExpiresAt);
    }

    [Fact]
    public async Task ClaimBySubmissionTokenAsync_NoMatch_ReturnsNull()
    {
        using var context = await CreateSeededContextAsync();
        var repository = new DonorRepository(context, _mapper);

        Assert.Null(await repository.ClaimBySubmissionTokenAsync("does-not-exist"));
    }

    [Fact]
    public async Task ClaimBySubmissionTokenAsync_ExpiredToken_ReturnsNullAndLeavesItUnburned()
    {
        using var context = await CreateSeededContextAsync();

        var donor = MakeDonor("Expired Token Co", _creatorId);
        donor.SubmissionToken = "expired-token";
        donor.SubmissionTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        context.Donors.Add(donor);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new DonorRepository(context, _mapper);

        Assert.Null(await repository.ClaimBySubmissionTokenAsync("expired-token"));

        // Not touched — an expired claim attempt shouldn't mutate the row at all.
        context.ChangeTracker.Clear();
        var reloaded = await context.Donors.FindAsync(donor.Id);
        Assert.Equal("expired-token", reloaded!.SubmissionToken);
    }

    [Fact]
    public async Task ClaimBySubmissionTokenAsync_TwoDonorsWithNullToken_NeitherMatchesAnEmptyLookup()
    {
        // Two donors both with a null SubmissionToken (the common case once a
        // token has been consumed, or before one was ever issued) must never
        // both match an empty/null lookup — this confirms the claim compares
        // against the real token string, not an accidental "IS NULL" match.
        using var context = await CreateSeededContextAsync();
        context.Donors.AddRange(
            MakeDonor("Donor A", _creatorId),
            MakeDonor("Donor B", _creatorId));
        await context.SaveChangesAsync();

        var repository = new DonorRepository(context, _mapper);

        Assert.Null(await repository.ClaimBySubmissionTokenAsync(string.Empty));
    }

    [Fact]
    public async Task ClaimBySubmissionTokenAsync_ManyConcurrentCallersForTheSameToken_ExactlyOneWins()
    {
        // This is the regression test for the race this method exists to close:
        // a separate "read the donor, check expiry, null the token, SaveChanges"
        // sequence lets two concurrent requests for the same token both read it
        // as valid before either write lands, so both proceed to upload a
        // document — violating the single-use guarantee. A single-threaded test
        // can't catch that class of bug (same reasoning as
        // DonorRepositoryReferenceNumberTests), so this fires many callers at
        // once, each against its own DbContext/connection — the same shape as
        // separate concurrent HTTP requests each with their own scoped context —
        // against real Postgres, and asserts exactly one of them ever gets the
        // donor id back.
        using (var seedContext = await CreateSeededContextAsync())
        {
            var donor = MakeDonor("Concurrency Test Co", _creatorId);
            donor.SubmissionToken = "contended-token";
            donor.SubmissionTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
            seedContext.Donors.Add(donor);
            await seedContext.SaveChangesAsync();
        }

        const int concurrentCallers = 20;

        var results = await Task.WhenAll(Enumerable.Range(0, concurrentCallers).Select(async _ =>
        {
            using var context = new CrmDbContext(_options);
            var repository = new DonorRepository(context, _mapper);
            return await repository.ClaimBySubmissionTokenAsync("contended-token");
        }));

        Assert.Single(results, r => r is not null);
    }

    [Fact]
    public async Task AddAsync_AndAddApprovalAsync_CommitInOneUnitOfWorkCall()
    {
        using var context = await CreateSeededContextAsync();

        var repository = new DonorRepository(context, _mapper);
        var donor = MakeDonor("Committed Co", _creatorId);

        await repository.AddAsync(donor);
        await repository.AddApprovalAsync(new DonorApproval
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            RequestedByUserId = _creatorId,
            Status = ApprovalStatus.Pending
        });

        await new UnitOfWork(context).SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.True(await context.Donors.AnyAsync(d => d.Id == donor.Id));
        Assert.True(await context.DonorApprovals.AnyAsync(a => a.DonorId == donor.Id));
    }
}
