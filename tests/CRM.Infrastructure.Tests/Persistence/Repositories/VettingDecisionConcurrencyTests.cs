using AutoMapper;
using CRM.Application.Common.Exceptions;
using CRM.Application.Modules.Vetting.Mappings;
using CRM.Application.Modules.Vetting.Review.Commands.DecideSubmission;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.Infrastructure.Tests.Persistence.Repositories;

/// <summary>
/// The decision's Ready check is a conditional UPDATE inside a transaction. A single-threaded test
/// can't prove that, so this runs the real handler twice at once, each with its own DbContext and
/// unit of work (like two admins' requests), against real Postgres.
/// </summary>
public class VettingDecisionConcurrencyTests
{
    private readonly DbContextOptions<CrmDbContext> _options = new DbContextOptionsBuilder<CrmDbContext>()
        .UseNpgsql(TestPostgres.ConnectionString("crm_test_vettingdecision"))
        .Options;

    private readonly IMapper _mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<VettingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task TwoParallelApproves_ExactlyOneWins_TheOtherIs409()
    {
        Guid officerId;
        var submissionIds = new List<Guid>();
        await using (var context = new CrmDbContext(_options))
        {
            await context.Database.EnsureCreatedAsync();
            var donor = await VettingTestData.AddDonorAsync(context);
            var officer = new VettingOfficer { Id = Guid.NewGuid(), FullName = "Thandi Mokoena", Initials = "TM", AssignedRegion = "Gauteng North", WorkloadCount = 5 };
            context.VettingOfficers.Add(officer);
            officerId = officer.Id;
            for (var i = 0; i < 5; i++)
            {
                var s = new VettingSubmission
                {
                    Id = Guid.NewGuid(), DonorId = donor.Id, OfficerId = officer.Id,
                    FormReference = $"VET-T-{Guid.NewGuid():N}"[..20], ApprovalState = VettingApprovalState.Ready
                };
                context.VettingSubmissions.Add(s);
                submissionIds.Add(s.Id);
            }
            await context.SaveChangesAsync();
        }

        foreach (var id in submissionIds) // 5 rounds, so a lucky interleaving can't pass it by chance
        {
            var outcomes = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
            {
                await using var context = new CrmDbContext(_options);
                var handler = new DecideSubmissionCommandHandler(
                    new VettingSubmissionRepository(context, _mapper), new VettingOfficerRepository(context, _mapper), new UnitOfWork(context));
                try
                {
                    await handler.Handle(new DecideSubmissionCommand { Id = id, Decision = "Approve" }, CancellationToken.None);
                    return "ok";
                }
                catch (ConflictException)
                {
                    return "409";
                }
            }));

            Assert.Equal(["409", "ok"], outcomes.Order());
        }

        await using var read = new CrmDbContext(_options);
        Assert.All(await read.VettingSubmissions.Where(s => submissionIds.Contains(s.Id)).ToListAsync(), s =>
        {
            Assert.Equal(VettingApprovalState.Approved, s.ApprovalState);
            Assert.Equal(VettingDecision.Approve, s.AdminDecision);
        });
        Assert.Equal(0, (await read.VettingOfficers.SingleAsync(o => o.Id == officerId)).WorkloadCount);
    }
}
