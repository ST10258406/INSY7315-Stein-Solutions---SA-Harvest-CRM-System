namespace CRM.Application.Modules.Dashboard.Dtos;

/// <summary>
/// GET /api/v1/dashboard/manager-activity response: distinct donors each user logged an
/// interaction with over the window, busiest first.
/// </summary>
public class ManagerActivityDto
{
    /// <summary>"weekly" (last 7 days) or "monthly" (last 30 days).</summary>
    public string Period { get; set; } = string.Empty;
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public List<ManagerActivityItemDto> Items { get; set; } = new();
}

public class ManagerActivityItemDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DonorsContacted { get; set; }
}
