namespace CRM.Application.Modules.Dashboard.Queries.GetDashboardStats;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Dashboard.Dtos;
using MediatR;

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    // Mirrors the API's "AdminOrAbove" authorization policy (see AuthorizationExtensions) —
    // the same two roles that can call GET /api/v1/approvals at all.
    private static readonly string[] RolesAllowedToSeePendingApprovals = { "Admin", "SuperAdmin" };

    private readonly IDashboardRepository _dashboard;
    private readonly ICurrentUserService _currentUserService;

    public GetDashboardStatsQueryHandler(IDashboardRepository dashboard, ICurrentUserService currentUserService)
    {
        _dashboard = dashboard;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var now = DateTime.UtcNow;

        // Calendar month boundaries based on server (UTC) time — deliberately not a rolling
        // 30-day window. [startOfThisMonth, startOfNextMonth) and [startOfLastMonth, startOfThisMonth)
        // are the two half-open ranges used below.
        var startOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfNextMonth = startOfThisMonth.AddMonths(1);
        var startOfLastMonth = startOfThisMonth.AddMonths(-1);

        var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);

        // Sequential, not parallel: every call below shares one scoped DbContext instance
        // (see ServiceCollectionExtensions — all repositories are scoped alongside it), and
        // EF Core does not support concurrent operations on a single context.
        var totalDonors = await _dashboard.GetTotalDonorsCountAsync(cancellationToken);
        var activeDonors = await _dashboard.GetActiveDonorsCountAsync(cancellationToken);
        var myOpenTasks = await _dashboard.GetOpenTaskCountAsync(currentUserId, cancellationToken);
        var myOverdueFollowUps = await _dashboard.GetOverdueFollowUpCountAsync(currentUserId, today, cancellationToken);
        var donorsContactedThisMonth = await _dashboard.GetDonorsContactedCountAsync(
            startOfThisMonth, startOfNextMonth, cancellationToken);
        var donorsContactedLastMonth = await _dashboard.GetDonorsContactedCountAsync(
            startOfLastMonth, startOfThisMonth, cancellationToken);

        // Non-Admin roles cannot see the approvals queue at all (GET /api/v1/approvals is
        // AdminOrAbove-only), so this one KPI is deliberately forced to 0 for them even though
        // the underlying count exists — a documented exception to "role checks belong on the
        // controller, not the handler": this isn't about whether the endpoint can be called,
        // only about which value a single field returns, so it has to live here. Skipping the
        // query for non-Admins also avoids computing a number nobody is allowed to see.
        var roles = _currentUserService.GetCurrentUserRoles();
        var canSeeApprovals = roles.Any(r => RolesAllowedToSeePendingApprovals.Contains(r, StringComparer.OrdinalIgnoreCase));
        var pendingApprovals = canSeeApprovals
            ? await _dashboard.GetPendingApprovalsCountAsync(cancellationToken)
            : 0;

        return new DashboardStatsDto
        {
            TotalDonors = totalDonors,
            ActiveDonors = activeDonors,
            PendingApprovals = pendingApprovals,
            MyOpenTasks = myOpenTasks,
            MyOverdueFollowUps = myOverdueFollowUps,
            DonorsContactedThisMonth = donorsContactedThisMonth,
            DonorsContactedLastMonth = donorsContactedLastMonth
        };
    }
}
