namespace CRM.Application.Common.Interfaces;

using CRM.Application.Modules.Approvals.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;

public interface IApprovalRepository
{
    /// <summary>
    /// Filtered, paginated approval queue (newest first) plus the total matching count
    /// (counted before pagination). Projects the nested donor and submitter.
    /// </summary>
    Task<(List<ApprovalDto> Items, int TotalCount)> GetApprovalsAsync(
        ApprovalStatus status, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracked approval with its <see cref="DonorApproval.Donor"/> loaded, ready for the
    /// approve / reject handlers to mutate both rows in one transaction. Null when missing.
    /// </summary>
    Task<DonorApproval?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}
