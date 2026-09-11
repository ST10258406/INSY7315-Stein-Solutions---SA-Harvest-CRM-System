namespace CRM.Application.Modules.Dashboard.Dtos;

/// <summary>
/// GET /api/v1/dashboard/stats response. Every field is populated for every authenticated
/// caller except <see cref="PendingApprovals"/>, which the handler forces to 0 for any
/// non-Admin role — see GetDashboardStatsQueryHandler.
/// </summary>
public class DashboardStatsDto
{
    public int TotalDonors { get; set; }
    public int ActiveDonors { get; set; }
    public int PendingApprovals { get; set; }
    public int MyOpenTasks { get; set; }
    public int MyOverdueFollowUps { get; set; }
    public int DonorsContactedThisMonth { get; set; }
    public int DonorsContactedLastMonth { get; set; }
}
