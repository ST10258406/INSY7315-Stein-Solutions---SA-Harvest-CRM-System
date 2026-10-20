using AutoMapper;
using CRM.Application.Modules.Vetting.Mappings;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.Infrastructure.Tests.Persistence.Repositories;

public class VettingOfficerRepositoryTests
{
    private readonly DbContextOptions<CrmDbContext> _options = new DbContextOptionsBuilder<CrmDbContext>()
        .UseNpgsql(TestPostgres.ConnectionString("crm_test_vettingofficerrepo"))
        .Options;

    private readonly IMapper _mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<VettingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task RecalculateWorkloadAsync_CountsOpenVisitsAndOpenSubmissions_Only()
    {
        await using var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();
        var donor = await VettingTestData.AddDonorAsync(context);

        // Stale value proves it is recounted, not incremented.
        var officer = new VettingOfficer { Id = Guid.NewGuid(), FullName = "Thandi Mokoena", Initials = "TM", AssignedRegion = "Gauteng North", WorkloadCount = 99 };
        var other = new VettingOfficer { Id = Guid.NewGuid(), FullName = "Other Officer", Initials = "OO", AssignedRegion = "Elsewhere" };
        context.VettingOfficers.AddRange(officer, other);

        VettingSiteVisit Visit(VettingOfficer o, SiteVisitStatus s) => new()
        {
            Id = Guid.NewGuid(), DonorId = donor.Id, OfficerId = o.Id, LocationLabel = "Midrand", VisitStatus = s,
            ScheduledDate = new DateOnly(2026, 10, 21), ScheduledTime = new TimeOnly(9, 0)
        };
        VettingSubmission Submission(VettingOfficer o, VettingApprovalState s) => new()
        {
            Id = Guid.NewGuid(), DonorId = donor.Id, OfficerId = o.Id, ApprovalState = s,
            FormReference = $"VET-T-{Guid.NewGuid():N}"[..20]
        };

        context.VettingSiteVisits.AddRange(
            Visit(officer, SiteVisitStatus.ToDo),        // counts
            Visit(officer, SiteVisitStatus.InProgress),  // counts
            Visit(officer, SiteVisitStatus.Completed),   // ignored
            Visit(other, SiteVisitStatus.ToDo));         // other officer
        context.VettingSubmissions.AddRange(
            Submission(officer, VettingApprovalState.New),      // counts
            Submission(officer, VettingApprovalState.Ready),    // counts
            Submission(officer, VettingApprovalState.Returned), // counts
            Submission(officer, VettingApprovalState.Approved), // ignored
            Submission(officer, VettingApprovalState.Declined), // ignored
            Submission(other, VettingApprovalState.New));       // other officer
        await context.SaveChangesAsync();

        await new VettingOfficerRepository(context, _mapper).RecalculateWorkloadAsync(officer.Id);
        await context.SaveChangesAsync();

        await using var read = new CrmDbContext(_options);
        Assert.Equal(5, (await read.VettingOfficers.SingleAsync(o => o.Id == officer.Id)).WorkloadCount);
    }
}
