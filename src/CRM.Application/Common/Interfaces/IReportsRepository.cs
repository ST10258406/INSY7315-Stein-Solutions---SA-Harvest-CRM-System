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

    /// <summary>
    /// Current donor count per active donation type. Sourced from
    /// <c>lookup_donation_types</c> (not from donors that happen to exist), so a donation
    /// type with no donors still appears with a count of 0. A donor offering more than one
    /// donation type is counted once per type via <c>donor_donation_types</c>.
    /// </summary>
    Task<List<DonorsByTypeDto>> GetDonorsByTypeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Current donor count per <see cref="CRM.Domain.Enums.DonorStatus"/> value. Iterates over
    /// every enum value (not just statuses with existing rows), so a status with no donors
    /// still appears with a count of 0. donor_status is a single-value column on donors, so
    /// unlike region/type there is no join table and no dedup concern — counts sum to the
    /// total donor count. Returned in a fixed display order (Active, PendingReview, Lapsed,
    /// Rejected) so a pie chart's color/slice assignment stays stable between requests.
    /// </summary>
    Task<List<DonorsByStatusDto>> GetDonorsByStatusAsync(CancellationToken cancellationToken = default);
}
