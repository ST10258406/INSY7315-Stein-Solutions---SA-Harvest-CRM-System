namespace CRM.Application.Modules.Reports.Dtos;

/// <summary>
/// Flat response for GET /api/v1/reports/donors-contacted. Kept deliberately shallow —
/// this feeds a dashboard chart directly, so no extra nested metadata beyond what's below.
/// </summary>
public class DonorsContactedReportDto
{
    public ReportPeriodDto Period { get; set; } = null!;

    /// <summary>
    /// Distinct donor count across the whole period (and, if a manager filter was applied,
    /// scoped to that manager only). Computed independently of <see cref="ByManager"/> —
    /// never a sum of its per-manager DonorsContacted values.
    /// </summary>
    public int TotalDonorsContacted { get; set; }

    public List<ManagerContactedDto> ByManager { get; set; } = new();
}

public class ReportPeriodDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public class ManagerContactedDto
{
    public ReportManagerDto Manager { get; set; } = null!;

    /// <summary>Distinct donors this manager had at least one interaction with in the period.</summary>
    public int DonorsContacted { get; set; }

    /// <summary>Total interaction rows for this manager's donors in the period (not distinct).</summary>
    public int TotalInteractions { get; set; }
}

public class ReportManagerDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
}
