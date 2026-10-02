using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Dashboard.Queries.GetDashboardStats;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Dashboard.Queries;

public class GetDashboardStatsQueryHandlerTests
{
    private readonly IDashboardRepository _dashboardMock = Substitute.For<IDashboardRepository>();
    private readonly ICurrentUserService _currentUserMock = Substitute.For<ICurrentUserService>();
    private readonly GetDashboardStatsQueryHandler _handler;

    private readonly Guid _currentUserId = Guid.NewGuid();

    public GetDashboardStatsQueryHandlerTests()
    {
        _currentUserMock.GetCurrentUserId().Returns(_currentUserId);
        _currentUserMock.GetCurrentUserRoles().Returns(new List<string> { "Procurement" });

        _dashboardMock.GetTotalDonorsCountAsync(Arg.Any<CancellationToken>()).Returns(100);
        _dashboardMock.GetActiveDonorsCountAsync(Arg.Any<CancellationToken>()).Returns(87);
        _dashboardMock.GetPendingApprovalsCountAsync(Arg.Any<CancellationToken>()).Returns(3);
        _dashboardMock.GetOpenTaskCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(5);
        _dashboardMock.GetOverdueFollowUpCountAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(2);
        _dashboardMock.GetDonorsContactedCountAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(34);

        _handler = new GetDashboardStatsQueryHandler(_dashboardMock, _currentUserMock);
    }

    [Fact]
    public async Task Handle_ScopesOpenTasksAndOverdueFollowUpsToCurrentUser_NeverFromTheRequest()
    {
        await _handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        await _dashboardMock.Received(1).GetOpenTaskCountAsync(_currentUserId, Arg.Any<CancellationToken>());
        await _dashboardMock.Received(1).GetOverdueFollowUpCountAsync(_currentUserId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OverdueFollowUpBoundary_IsTodayAtMidnightUtc()
    {
        DateTime captured = default;
        _dashboardMock.GetOverdueFollowUpCountAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                captured = ci.ArgAt<DateTime>(1);
                return 2;
            });

        await _handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        var expectedToday = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        Assert.Equal(expectedToday, captured);
        Assert.Equal(DateTimeKind.Utc, captured.Kind);
    }

    [Fact]
    public async Task Handle_DonorsContactedRanges_UseCalendarMonthBoundaries_NotARollingWindow()
    {
        var seenRanges = new List<(DateTime Start, DateTime End)>();
        _dashboardMock.GetDonorsContactedCountAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                seenRanges.Add((ci.ArgAt<DateTime>(0), ci.ArgAt<DateTime>(1)));
                return 0;
            });

        await _handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        var now = DateTime.UtcNow;
        var startOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfNextMonth = startOfThisMonth.AddMonths(1);
        var startOfLastMonth = startOfThisMonth.AddMonths(-1);

        Assert.Equal(2, seenRanges.Count);
        Assert.Contains((startOfThisMonth, startOfNextMonth), seenRanges);
        Assert.Contains((startOfLastMonth, startOfThisMonth), seenRanges);
        Assert.All(seenRanges, r => Assert.Equal(DateTimeKind.Utc, r.Start.Kind));
        Assert.All(seenRanges, r => Assert.Equal(DateTimeKind.Utc, r.End.Kind));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("SuperAdmin")]
    public async Task Handle_AdminOrSuperAdmin_ReturnsRealPendingApprovalsCount(string role)
    {
        _currentUserMock.GetCurrentUserRoles().Returns(new List<string> { role });

        var result = await _handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        Assert.Equal(3, result.PendingApprovals);
        await _dashboardMock.Received(1).GetPendingApprovalsCountAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Procurement")]
    [InlineData("Marketing")]
    public async Task Handle_NonAdminRole_ForcesPendingApprovalsToZero_AndSkipsTheQuery(string role)
    {
        _currentUserMock.GetCurrentUserRoles().Returns(new List<string> { role });

        var result = await _handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        Assert.Equal(0, result.PendingApprovals);
        await _dashboardMock.DidNotReceive().GetPendingApprovalsCountAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoRolesAtAll_ForcesPendingApprovalsToZero()
    {
        _currentUserMock.GetCurrentUserRoles().Returns(new List<string>());

        var result = await _handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        Assert.Equal(0, result.PendingApprovals);
    }

    [Fact]
    public async Task Handle_MapsEveryFieldFromTheRepository()
    {
        _currentUserMock.GetCurrentUserRoles().Returns(new List<string> { "Admin" });
        _dashboardMock.GetDonorsContactedCountAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(34, 41);

        var result = await _handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        Assert.Equal(100, result.TotalDonors);
        Assert.Equal(87, result.ActiveDonors);
        Assert.Equal(3, result.PendingApprovals);
        Assert.Equal(5, result.MyOpenTasks);
        Assert.Equal(2, result.MyOverdueFollowUps);
        Assert.Equal(34, result.DonorsContactedThisMonth);
        Assert.Equal(41, result.DonorsContactedLastMonth);
    }
}
