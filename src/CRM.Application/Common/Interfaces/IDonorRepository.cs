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

    /// <summary>
    /// Atomically allocates the next donor reference number for the current UTC
    /// year, formatted "DON-{year}-{5-digit sequence}" (e.g. "DON-2026-00042").
    /// Backed by a DB-level sequence (nextval() is atomic under Postgres, even
    /// across concurrent callers) rather than a "SELECT MAX + 1" pattern, so two
    /// simultaneous public-form submissions can never receive the same number.
    /// Gaps (a number allocated but never committed, e.g. because a later step
    /// in the same request fails) are an accepted trade-off of this approach —
    /// duplicates are not.
    /// </summary>
    Task<string> GetNextReferenceNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims a donor's one-time public SubmissionToken (see
    /// Donor.SubmissionToken remarks) and returns its Id, or null if no donor has
    /// that token unexpired — used by SubmitPublicDonorDocument to resolve the
    /// follow-up BBBEE-certificate upload back to the right donor without the
    /// public client ever handling the donor's real Id.
    ///
    /// Deliberately NOT a separate "read the donor, check expiry, null the token,
    /// SaveChanges" sequence — that read-then-write pattern has a race: two
    /// concurrent requests for the same token could both read it as still valid
    /// before either write lands, and both would then proceed to upload a
    /// document. This claims (nulls the token) in the same statement that reads
    /// it, so only the request that actually wins the underlying row lock ever
    /// sees a non-null result; the loser's WHERE clause simply matches nothing.
    /// </summary>
    Task<Guid?> ClaimBySubmissionTokenAsync(string submissionToken, CancellationToken cancellationToken = default);
}
