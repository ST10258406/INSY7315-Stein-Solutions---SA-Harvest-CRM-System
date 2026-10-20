using AutoMapper;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Vetting.Mappings;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.Infrastructure.Tests.Persistence.Repositories;

public class VettingSubmissionRepositoryTests
{
    private readonly DbContextOptions<CrmDbContext> _options = new DbContextOptionsBuilder<CrmDbContext>()
        .UseNpgsql(TestPostgres.ConnectionString("crm_test_vettingsubmissionrepo"))
        .Options;

    private readonly IMapper _mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<VettingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    private readonly int _year = DateTime.UtcNow.Year;

    [Fact]
    public async Task GetNextFormReferenceAsync_ManyConcurrentCallers_NeverProducesADuplicate()
    {
        await using (var context = new CrmDbContext(_options))
        {
            await context.Database.EnsureCreatedAsync();
            // Force the lazy CREATE SEQUENCE path to run under concurrency every time. Year-derived, never user input.
#pragma warning disable EF1002
            await context.Database.ExecuteSqlRawAsync($"DROP SEQUENCE IF EXISTS vetting_reference_seq_{_year}");
#pragma warning restore EF1002
        }

        const int concurrentCallers = 20;
        var results = await Task.WhenAll(Enumerable.Range(0, concurrentCallers).Select(async _ =>
        {
            // One DbContext per caller, like separate concurrent HTTP requests.
            await using var context = new CrmDbContext(_options);
            return await new VettingSubmissionRepository(context, _mapper).GetNextFormReferenceAsync();
        }));

        Assert.Equal(concurrentCallers, results.Distinct().Count());
        Assert.All(results, r => Assert.Matches($@"^VET-{_year}-\d{{4,}}$", r));
    }

    [Fact]
    public async Task Aggregate_RoundTrips_ThroughGetForUpdate_ProjectionAndSearch()
    {
        await using var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();
        var donor = await VettingTestData.AddDonorAsync(context);
        var officer = new VettingOfficer { Id = Guid.NewGuid(), FullName = "Thandi Mokoena", Initials = "TM", AssignedRegion = "Gauteng North" };
        context.VettingOfficers.Add(officer);

        var repository = new VettingSubmissionRepository(context, _mapper);
        var submission = new VettingSubmission
        {
            Id = Guid.NewGuid(),
            FormReference = $"VET-T-{Guid.NewGuid():N}"[..20], // not the sequence: the concurrency test above resets it
            DonorId = donor.Id,
            OfficerId = officer.Id,
            Details = new VettingAssessmentDetails
            {
                Id = Guid.NewGuid(), LegalName = "Ubuntu", ContactName = "Sipho", ContactEmail = "s@example.com", ContactPhone = "082",
                Province = "Gauteng", What3Words = "///a.b.c", ProgrammesSummary = "Soup", DistributionChannel = "Both",
                FeedingFrequency = "Daily", SaHarvestReliance = "Low", TransportCapacity = "Own vehicle",
                CoreFocusAreas = ["Feeding"], FeedingDays = ["Mon", "Tue"], NpoCertificatePath = "vetting/x/files/npo.pdf"
            },
            Photos = Enum.GetValues<EvidencePhotoCategory>()
                .Select(c => new VettingEvidencePhoto { Id = Guid.NewGuid(), ShotIndex = (int)c, Category = c })
                .ToList(),
            Flags =
            [
                new VettingFlag { Id = Guid.NewGuid(), Description = "Active", Source = VettingFlagSource.Auto },
                new VettingFlag { Id = Guid.NewGuid(), Description = "Old", Source = VettingFlagSource.Auto, IsActive = false }
            ]
        };
        await repository.AddAsync(submission);
        await context.SaveChangesAsync();
        // ponytail: IsActive=false is swallowed on insert (HasDefaultValue(true) sentinel); real code only deactivates by update.
        await context.VettingFlags.Where(f => f.Description == "Old").ExecuteUpdateAsync(u => u.SetProperty(f => f.IsActive, false));

        await using var read = new CrmDbContext(_options);
        var readRepository = new VettingSubmissionRepository(read, _mapper);

        var tracked = await readRepository.GetForUpdateAsync(submission.Id);
        Assert.NotNull(tracked!.Details);
        Assert.Equal(4, tracked.Photos.Count);
        Assert.Equal("Active", Assert.Single(tracked.Flags).Description);

        var dto = await readRepository.GetDtoByIdAsync(submission.Id);
        Assert.Equal(donor.CompanyName, dto!.DonorCompanyName);
        Assert.Equal("Thandi Mokoena", dto.OfficerFullName);
        Assert.Equal("New", dto.ApprovalState);
        Assert.Equal(["Mon", "Tue"], dto.Details.FeedingDays);
        Assert.Equal("Active", Assert.Single(dto.Flags).Description);
        Assert.Equal(["Kitchen", "DryStorage", "FridgeFreezer", "ToiletsWater"], dto.Photos.Select(p => p.Category));
        Assert.True(dto.Files.NpoCertificate);
        Assert.False(dto.Files.Certificates);

        var (items, total) = await readRepository.SearchAsync(new VettingSubmissionSearchCriteria
        {
            Search = donor.CompanyName.ToUpperInvariant(), ApprovalState = VettingApprovalState.New
        });
        Assert.Equal(1, total);
        Assert.Equal(1, Assert.Single(items).ActiveFlagCount);
    }

    [Fact]
    public async Task SearchAsync_SubmittedAtAscending_PutsTheLongestWaitingFirst_AndIncludesProvince()
    {
        await using var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();
        var donor = await VettingTestData.AddDonorAsync(context);
        var officer = new VettingOfficer { Id = Guid.NewGuid(), FullName = "Thandi Mokoena", Initials = "TM", AssignedRegion = "Gauteng North" };
        context.VettingOfficers.Add(officer);

        VettingSubmission Ready(DateTime submittedAt, string province) => new()
        {
            Id = Guid.NewGuid(), FormReference = $"VET-T-{Guid.NewGuid():N}"[..20], DonorId = donor.Id, OfficerId = officer.Id,
            ApprovalState = VettingApprovalState.Ready, SubmittedAt = submittedAt,
            Details = new VettingAssessmentDetails
            {
                Id = Guid.NewGuid(), LegalName = "x", ContactName = "x", ContactEmail = "x@example.com", ContactPhone = "082",
                Province = province, What3Words = "///a.b.c", ProgrammesSummary = "x", DistributionChannel = "Both",
                FeedingFrequency = "Daily", SaHarvestReliance = "Low", TransportCapacity = "Own vehicle"
            }
        };
        var newer = Ready(DateTime.UtcNow.AddHours(-1), "Limpopo");
        var older = Ready(DateTime.UtcNow.AddDays(-3), "Gauteng");
        context.VettingSubmissions.AddRange(newer, older);
        await context.SaveChangesAsync();

        var repository = new VettingSubmissionRepository(context, _mapper);
        var (oldestFirst, _) = await repository.SearchAsync(new VettingSubmissionSearchCriteria
        {
            DonorId = donor.Id, ApprovalState = VettingApprovalState.Ready, SubmittedAtAscending = true
        });
        var (newestFirst, _) = await repository.SearchAsync(new VettingSubmissionSearchCriteria
        {
            DonorId = donor.Id, ApprovalState = VettingApprovalState.Ready, SubmittedAtAscending = false
        });

        Assert.Equal([older.Id, newer.Id], oldestFirst.Select(i => i.Id));
        Assert.Equal(["Gauteng", "Limpopo"], oldestFirst.Select(i => i.Province));
        Assert.Equal([newer.Id, older.Id], newestFirst.Select(i => i.Id));
    }
}
