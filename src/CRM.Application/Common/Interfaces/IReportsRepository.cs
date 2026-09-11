namespace CRM.Application.Common.Interfaces;

using CRM.Application.Modules.Reports.Dtos;

/// <summary>
/// Read-only aggregation queries over <c>interaction_logs</c> for the reporting module.
/// Never touches write paths — every query here is AsNoTracking.
/// </summary>
public interface IReportsRepository
{
    /// <summary>
    /// Donors-contacted KPI for the inclusive UTC range [<paramref name="startUtc"/>,
    /// <paramref name="endUtc"/>], optionally scoped to one relationship manager.
    /// TotalDonorsContacted is COUNT(DISTINCT donor_id) over every matching interaction
    /// regardless of manager assignment; ByManager only includes donors that currently
    /// have a relationship manager assigned, so its DonorsContacted values are not
    /// guaranteed to sum to the total.
    /// </summary>
    Task<(int TotalDonorsContacted, List<ManagerContactedDto> ByManager)> GetDonorsContactedAsync(
        DateTime startUtc,
        DateTime endUtc,
        Guid? relationshipManagerId,
        CancellationToken cancellationToken = default);
}
