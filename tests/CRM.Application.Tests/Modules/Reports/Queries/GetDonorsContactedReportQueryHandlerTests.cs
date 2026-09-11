using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Reports.Dtos;
using CRM.Application.Modules.Reports.Queries.GetDonorsContactedReport;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Reports.Queries;

public class GetDonorsContactedReportQueryHandlerTests
{
    private readonly IReportsRepository _reportsMock = Substitute.For<IReportsRepository>();
    private readonly GetDonorsContactedReportQueryHandler _handler;

    public GetDonorsContactedReportQueryHandlerTests()
    {
        _handler = new GetDonorsContactedReportQueryHandler(_reportsMock);
    }

    [Fact]
    public async Task Handle_ConvertsDateRangeToInclusiveUtcBounds()
    {
        DateTime? capturedStart = null;
        DateTime? capturedEnd = null;
        _reportsMock
            .GetDonorsContactedAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedStart = ci.ArgAt<DateTime>(0);
                capturedEnd = ci.ArgAt<DateTime>(1);
                return (0, new List<ManagerContactedDto>());
            });

        await _handler.Handle(new GetDonorsContactedReportQuery
        {
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 31)
        }, CancellationToken.None);

        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), capturedStart);
        Assert.Equal(DateTimeKind.Utc, capturedStart!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, capturedEnd!.Value.Kind);
        // Inclusive of the whole last day, not just midnight.
        Assert.Equal(new DateOnly(2026, 7, 31), DateOnly.FromDateTime(capturedEnd.Value));
        Assert.Equal(23, capturedEnd.Value.Hour);
    }

    [Fact]
    public async Task Handle_PassesRelationshipManagerFilterThrough()
    {
        var managerId = Guid.NewGuid();
        Guid? captured = null;
        _reportsMock
            .GetDonorsContactedAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                captured = ci.ArgAt<Guid?>(2);
                return (0, new List<ManagerContactedDto>());
            });

        await _handler.Handle(new GetDonorsContactedReportQuery
        {
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 31),
            RelationshipManagerId = managerId
        }, CancellationToken.None);

        Assert.Equal(managerId, captured);
    }

    [Fact]
    public async Task Handle_NoManagerFilter_PassesNullThrough()
    {
        Guid? captured = Guid.NewGuid(); // seed with a non-null sentinel
        _reportsMock
            .GetDonorsContactedAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                captured = ci.ArgAt<Guid?>(2);
                return (0, new List<ManagerContactedDto>());
            });

        await _handler.Handle(new GetDonorsContactedReportQuery
        {
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 31)
        }, CancellationToken.None);

        Assert.Null(captured);
    }

    [Fact]
    public async Task Handle_ReturnsFlatDtoWithPeriodAndTotals_DoesNotSumManagerTotals()
    {
        // Regression guard for the double-counting edge case: totalDonorsContacted must
        // come straight from the repository, never be recomputed as a sum of ByManager.
        var byManager = new List<ManagerContactedDto>
        {
            new()
            {
                Manager = new ReportManagerDto { Id = Guid.NewGuid(), FullName = "Jane Doe" },
                DonorsContacted = 18,
                TotalInteractions = 24
            },
            new()
            {
                Manager = new ReportManagerDto { Id = Guid.NewGuid(), FullName = "John Smith" },
                DonorsContacted = 20, // 18 + 20 = 38, deliberately != 34 below
                TotalInteractions = 30
            }
        };
        _reportsMock
            .GetDonorsContactedAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns((34, byManager));

        var result = await _handler.Handle(new GetDonorsContactedReportQuery
        {
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 31)
        }, CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 7, 1), result.Period.StartDate);
        Assert.Equal(new DateOnly(2026, 7, 31), result.Period.EndDate);
        Assert.Equal(34, result.TotalDonorsContacted);
        Assert.Same(byManager, result.ByManager);
    }
}
