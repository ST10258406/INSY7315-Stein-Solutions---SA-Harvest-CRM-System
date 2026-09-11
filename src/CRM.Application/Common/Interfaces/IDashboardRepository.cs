namespace CRM.Application.Common.Interfaces;

/// <summary>
/// Read-only aggregation queries backing GET /api/v1/dashboard/stats. Each KPI is its own
/// small query against donors, donor_approvals, donor_tasks, or interaction_logs rather than
/// one large joined statement — easier to read, test, and index independently. Every query is
/// AsNoTracking; this repository never feeds a write path.
/// </summary>
public interface IDashboardRepository
{
    /// <summary>Global count of every donor row, regardless of status.</summary>
    Task<int> GetTotalDonorsCountAsync(CancellationToken cancellationToken = default);

    /// <summary>Global count of donors currently in <see cref="CRM.Domain.Enums.DonorStatus.Active"/> status.</summary>
    Task<int> GetActiveDonorsCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Global count of donor_approvals rows with status = Pending. Role-based suppression for
    /// non-Admin callers is a presentation concern and belongs in the query handler, not here.
    /// </summary>
    Task<int> GetPendingApprovalsCountAsync(CancellationToken cancellationToken = default);

    /// <summary>Count of donor_tasks assigned to <paramref name="assignedToUserId"/> that are not yet completed.</summary>
    Task<int> GetOpenTaskCountAsync(Guid assignedToUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Count of donors whose relationship manager is <paramref name="relationshipManagerId"/>
    /// and whose follow_up_date is strictly before <paramref name="asOfUtc"/>.
    /// </summary>
    Task<int> GetOverdueFollowUpCountAsync(
        Guid relationshipManagerId, DateTime asOfUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Global COUNT(DISTINCT donor_id) over interaction_logs with created_at in the inclusive
    /// UTC range [<paramref name="startUtc"/>, <paramref name="endUtc"/>).
    /// </summary>
    Task<int> GetDonorsContactedCountAsync(
        DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
}
