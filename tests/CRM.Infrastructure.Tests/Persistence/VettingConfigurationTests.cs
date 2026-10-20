using CRM.Domain.Constants;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CRM.Infrastructure.Tests.Persistence;

public class VettingConfigurationTests
{
    private readonly DbContextOptions<CrmDbContext> _options;

    public VettingConfigurationTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_vetting_schema"))
            .Options;
    }

    private async Task<CrmDbContext> CreateContextAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task<VettingSubmission> AddSubmissionAsync(CrmDbContext context)
    {
        var donor = await VettingTestData.AddDonorAsync(context);
        var officer = new VettingOfficer { Id = Guid.NewGuid(), FullName = "Thandi Mokoena", Initials = "TM", AssignedRegion = "Gauteng North" };
        var submission = new VettingSubmission
        {
            Id = Guid.NewGuid(),
            FormReference = $"VET-T-{Guid.NewGuid():N}"[..20],
            DonorId = donor.Id,
            Officer = officer
        };
        context.VettingSubmissions.Add(submission);
        await context.SaveChangesAsync();
        return submission;
    }

    private static VettingAssessmentDetails NewDetails(Guid submissionId) => new()
    {
        Id = Guid.NewGuid(),
        SubmissionId = submissionId,
        LegalName = "Ubuntu Soup Kitchen NPC",
        ContactName = "Sipho Dlamini",
        ContactEmail = "sipho@example.com",
        ContactPhone = "+27 82 000 0000",
        Province = "Gauteng",
        What3Words = "///apple.orange.banana",
        CoreFocusAreas = ["Feeding", "Care & support"],
        TargetPopulation = ["Children"],
        ProgrammesSummary = "Daily soup kitchen.",
        DistributionChannel = "Direct to beneficiaries",
        BeneficiaryRaces = ["African", "Indian/Asian"],
        BeneficiaryGenders = ["Female", "Male"],
        BeneficiaryAgeGroups = ["0–5", "60+"],
        FeedingFrequency = "Weekdays only",
        SaHarvestReliance = "High",
        TransportCapacity = "None — delivery required",
        MealsProvided = ["Lunch"],
        FeedingDays = ["Mon", "Wed", "Fri"],
        FoodStorageTypes = ["Dry"],
        InfrastructureChecks = []
    };

    [Fact]
    public async Task Details_JsonbLists_RoundTrip_AndEditsPersist()
    {
        await using var context = await CreateContextAsync();
        var submission = await AddSubmissionAsync(context);
        var details = NewDetails(submission.Id);
        context.VettingAssessmentDetails.Add(details);
        await context.SaveChangesAsync();

        await using (var read = new CrmDbContext(_options))
        {
            var stored = await read.VettingAssessmentDetails.SingleAsync(d => d.Id == details.Id);
            Assert.Equal(details.CoreFocusAreas, stored.CoreFocusAreas);
            Assert.Equal(details.TargetPopulation, stored.TargetPopulation);
            Assert.Equal(details.BeneficiaryRaces, stored.BeneficiaryRaces);
            Assert.Equal(details.BeneficiaryGenders, stored.BeneficiaryGenders);
            Assert.Equal(details.BeneficiaryAgeGroups, stored.BeneficiaryAgeGroups);
            Assert.Equal(details.MealsProvided, stored.MealsProvided);
            Assert.Equal(details.FeedingDays, stored.FeedingDays);
            Assert.Equal(details.FoodStorageTypes, stored.FoodStorageTypes);
            Assert.Empty(stored.InfrastructureChecks);
            Assert.Equal(0, stored.VolunteersCount);

            // In-place mutation: proves change tracking sees edits inside the list.
            stored.FeedingDays.Add("Sat");
            await read.SaveChangesAsync();
        }

        await using var reload = new CrmDbContext(_options);
        var reloaded = await reload.VettingAssessmentDetails.AsNoTracking().SingleAsync(d => d.Id == details.Id);
        Assert.Equal(["Mon", "Wed", "Fri", "Sat"], reloaded.FeedingDays);

        var columnType = await reload.Database
            .SqlQueryRaw<string>("SELECT data_type AS \"Value\" FROM information_schema.columns WHERE table_name = 'vetting_assessment_details' AND column_name = 'feeding_days'")
            .SingleAsync();
        Assert.Equal("jsonb", columnType);
    }

    [Fact]
    public async Task SecondDetailsRowForSameSubmission_ViolatesUniqueIndex()
    {
        await using var context = await CreateContextAsync();
        var submission = await AddSubmissionAsync(context);
        context.VettingAssessmentDetails.Add(NewDetails(submission.Id));
        await context.SaveChangesAsync();

        // Fresh context: in the same one, EF would treat this as replacing the tracked dependent.
        await using var other = new CrmDbContext(_options);
        other.VettingAssessmentDetails.Add(NewDetails(submission.Id));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => other.SaveChangesAsync());

        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, pg.SqlState);
        Assert.Equal("idx_vetting_assessment_details_submission", pg.ConstraintName);
    }

    [Fact]
    public async Task ShotIndexOutOfRange_ViolatesCheckConstraint()
    {
        await using var context = await CreateContextAsync();
        var submission = await AddSubmissionAsync(context);

        context.VettingEvidencePhotos.Add(new VettingEvidencePhoto
        {
            Id = Guid.NewGuid(),
            SubmissionId = submission.Id,
            ShotIndex = 4,
            Category = EvidencePhotoCategory.Kitchen
        });
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.CheckViolation, pg.SqlState);
        Assert.Equal("chk_vetting_evidence_photos_shot_index", pg.ConstraintName);
    }

    [Fact]
    public async Task VettingOptionsProvinces_MatchLookupSeederProvinces()
    {
        await using var context = await CreateContextAsync();
        await LookupSeeder.SeedAsync(context);

        var seeded = await context.LookupProvinces.Select(p => p.Name).ToListAsync();

        Assert.Equal(VettingOptions.Provinces.Order(), seeded.Order());
    }
}
