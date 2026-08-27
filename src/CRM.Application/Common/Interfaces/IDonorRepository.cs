namespace CRM.Application.Common.Interfaces;

using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;

public interface IDonorRepository
{
    /// <summary>Stages a new donor (and its owned children) for insert.</summary>
    Task AddAsync(Donor donor, CancellationToken cancellationToken = default);

    /// <summary>Stages a donor approval row for insert, to commit alongside the donor.</summary>
    Task AddApprovalAsync(DonorApproval approval, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracked donor aggregate with contacts, legal address, operational regions and
    /// donation types eagerly loaded, ready for mutation by UpdateDonor.
    /// </summary>
    Task<Donor?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Filtered, sorted, paginated donor list plus the total matching row count
    /// (counted before pagination is applied).
    /// </summary>
    Task<(List<DonorListItemDto> Items, int TotalCount)> SearchAsync(
        DonorSearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>Read-only donor detail projection, or null when no such donor exists.</summary>
    Task<DonorDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
