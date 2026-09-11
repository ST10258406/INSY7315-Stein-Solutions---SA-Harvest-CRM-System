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

    /// <summary>
    /// Current donor count per active operational region. Sourced from
    /// <c>lookup_operational_regions</c> (not from donors that happen to exist), so a region
    /// with no donors still appears with a count of 0. A donor operating in more than one
    /// region is counted once per region via <c>donor_operational_regions</c>.
    /// </summary>
    Task<List<DonorsByRegionDto>> GetDonorsByRegionAsync(CancellationToken cancellationToken = default);
}
