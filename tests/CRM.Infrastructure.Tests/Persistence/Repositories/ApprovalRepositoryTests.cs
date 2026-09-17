using AutoMapper;
using CRM.Application.Modules.Approvals.Mappings;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using CRM.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.Infrastructure.Tests.Persistence.Repositories;

public class ApprovalRepositoryTests
{
    private readonly DbContextOptions<CrmDbContext> _options;
    private readonly IMapper _mapper;

    private Guid _submitterId;

    public ApprovalRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_approvalrepo"))
            .Options;

        var config = new MapperConfiguration(cfg => cfg.AddProfile<ApprovalMappingProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    private async Task<CrmDbContext> CreateSeededContextAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        context.DonorApprovals.RemoveRange(context.DonorApprovals);
        context.Donors.RemoveRange(context.Donors);
        await context.SaveChangesAsync();

        if (!await context.LookupCompanyTypes.AnyAsync(l => l.Id == 1))
            context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true, SortOrder = 1 });
        if (!await context.LookupEntityTypes.AnyAsync(l => l.Id == 1))
            context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true, SortOrder = 1 });
        if (!await context.LookupDonationFrequencies.AnyAsync(l => l.Id == 1))
            context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true, SortOrder = 1 });

        var submitter = await context.Users.FirstOrDefaultAsync(u => u.Email == "approval-submitter@example.com");
        if (submitter is null)
        {
            submitter = new User { Id = Guid.NewGuid(), Email = "approval-submitter@example.com", FirstName = "Sam", LastName = "Submitter", PasswordHash = "hash", IsActive = true };
            context.Users.Add(submitter);
        }
        await context.SaveChangesAsync();
        _submitterId = submitter.Id;

        context.ChangeTracker.Clear();
        return context;
    }

    private Donor MakeDonor(string name) => new()
    {
        Id = Guid.NewGuid(),
        ReferenceNumber = Guid.NewGuid().ToString("N")[..20],
        CompanyName = name,
        CompanyTypeId = 1,
        EntityTypeId = 1,
        DonationFrequencyId = 1,
        RegisteredCompanyName = name + " (Pty) Ltd",
        IncomeTaxNumber = "9012345678",
        Status = DonorStatus.PendingReview,
        SubmissionSource = SubmissionSource.ManualCapture,
        CreatedByUserId = _submitterId
    };

    private DonorApproval MakeApproval(Donor donor, ApprovalStatus status) => new()
    {
        Id = Guid.NewGuid(),
        DonorId = donor.Id,
        RequestedByUserId = _submitterId,
        Status = status
    };

    [Fact]
    public async Task GetApprovalsAsync_FiltersByStatus_AndProjectsNestedDonorAndSubmitter()
    {
        using var context = await CreateSeededContextAsync();
        var d1 = MakeDonor("Pending Co");
        var d2 = MakeDonor("Approved Co");
        context.Donors.AddRange(d1, d2);
        context.DonorApprovals.AddRange(
            MakeApproval(d1, ApprovalStatus.Pending),
            MakeApproval(d2, ApprovalStatus.Approved));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new ApprovalRepository(context, _mapper);
        var (items, total) = await repo.GetApprovalsAsync(ApprovalStatus.Pending, page: 1, pageSize: 20);

        Assert.Equal(1, total);
        var dto = Assert.Single(items);
        Assert.Equal("Pending", dto.Status);
        Assert.Equal("Pending Co", dto.Donor.CompanyName);
        Assert.Equal("PendingReview", dto.Donor.Status);
        Assert.Equal("Sam Submitter", dto.RequestedBy.FullName);
        Assert.Null(dto.ReviewedBy);
    }

    [Fact]
    public async Task GetApprovalsAsync_PaginatesWithFullCount()
    {
        using var context = await CreateSeededContextAsync();
        for (var i = 0; i < 5; i++)
        {
            var d = MakeDonor($"Co {i}");
            context.Donors.Add(d);
            context.DonorApprovals.Add(MakeApproval(d, ApprovalStatus.Pending));
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new ApprovalRepository(context, _mapper);
        var (items, total) = await repo.GetApprovalsAsync(ApprovalStatus.Pending, page: 2, pageSize: 2);

        Assert.Equal(5, total);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetForUpdateAsync_ReturnsTrackedApprovalWithDonor_ThatCommitsBothOnSave()
    {
        using var context = await CreateSeededContextAsync();
        var donor = MakeDonor("Mutate Co");
        context.Donors.Add(donor);
        var approval = MakeApproval(donor, ApprovalStatus.Pending);
        context.DonorApprovals.Add(approval);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new ApprovalRepository(context, _mapper);
        var tracked = await repo.GetForUpdateAsync(approval.Id);
        Assert.NotNull(tracked);
        Assert.NotNull(tracked!.Donor);

        tracked.Status = ApprovalStatus.Approved;
        tracked.Donor.Status = DonorStatus.Active;
        await context.SaveChangesAsync();

        using var verify = new CrmDbContext(_options);
        var reloadedApproval = await verify.DonorApprovals.AsNoTracking().SingleAsync(a => a.Id == approval.Id);
        var reloadedDonor = await verify.Donors.AsNoTracking().SingleAsync(d => d.Id == donor.Id);
        Assert.Equal(ApprovalStatus.Approved, reloadedApproval.Status);
        Assert.Equal(DonorStatus.Active, reloadedDonor.Status);
    }

    [Fact]
    public async Task GetForUpdateAsync_UnknownId_ReturnsNull()
    {
        using var context = await CreateSeededContextAsync();
        var repo = new ApprovalRepository(context, _mapper);

        Assert.Null(await repo.GetForUpdateAsync(Guid.NewGuid()));
    }
}
