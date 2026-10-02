using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Dashboard.Queries.GetManagerActivity;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Dashboard.Queries;

public class GetManagerActivityQueryHandlerTests
{
    private readonly IDashboardRepository _dashboardMock = Substitute.For<IDashboardRepository>();
    private readonly GetManagerActivityQueryHandler _handler;

    public GetManagerActivityQueryHandlerTests()
    {
        _handler = new GetManagerActivityQueryHandler(_dashboardMock);
    }

    [Theory]
    [InlineData("weekly", 7)]
    [InlineData("monthly", 30)]
    [InlineData("anything-else", 7)]
    public async Task Handle_UsesRollingWindowForPeriod(string period, int days)
    {
        DateTime from = default, to = default;
        _dashboardMock.GetDonorsContactedByUserAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci => { from = ci.ArgAt<DateTime>(0); to = ci.ArgAt<DateTime>(1); return new List<ManagerActivityRow>(); });

        await _handler.Handle(new GetManagerActivityQuery(period), CancellationToken.None);

        Assert.Equal(days, (to - from).TotalDays, 3);
    }

    [Fact]
    public async Task Handle_MapsRowsToFullNames()
    {
        var id = Guid.NewGuid();
        _dashboardMock.GetDonorsContactedByUserAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ManagerActivityRow> { new(id, "Ada", "Lovelace", 12) });

        var result = await _handler.Handle(new GetManagerActivityQuery("monthly"), CancellationToken.None);

        Assert.Equal("monthly", result.Period);
        var item = Assert.Single(result.Items);
        Assert.Equal(id, item.UserId);
        Assert.Equal("Ada Lovelace", item.Name);
        Assert.Equal(12, item.DonorsContacted);
    }
}
