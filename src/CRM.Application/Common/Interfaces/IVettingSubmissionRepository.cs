namespace CRM.Application.Common.Interfaces;

using CRM.Application.Common.Models;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;

public interface IVettingSubmissionRepository
{
    /// <summary>
    /// Next VET-{year}-{4-digit sequence} reference from the per-year Postgres sequence
    /// vetting_reference_seq_{year}, created lazily. Never MAX()+1. A reference consumed by a
    /// request that later fails leaves a gap; duplicates are impossible.
    /// </summary>
    Task<string> GetNextFormReferenceAsync(CancellationToken cancellationToken = default);

    /// <summary>Stages a new submission (with its details and photo slots) for insert.</summary>
    Task AddAsync(VettingSubmission submission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracked aggregate in one query: submission, details, ACTIVE flags and photos.
    /// Feeds every write to a submission. The submission row is always written on save, so a racing
    /// change to it surfaces as a <see cref="Exceptions.ConflictException"/>. Null when not found.
    /// </summary>
    Task<VettingSubmission?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Untracked submission with details and photos, for download links. Null when not found.</summary>
    Task<VettingSubmission?> GetForDownloadAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>True when any submission already points at this site visit.</summary>
    Task<bool> IsSiteVisitLinkedAsync(Guid siteVisitId, CancellationToken cancellationToken = default);

    /// <summary>The full read model (details, active flags, photos, filled file slots), or null.</summary>
    Task<VettingSubmissionDto?> GetDtoByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The submission's officer, or null when the submission doesn't exist.</summary>
    Task<Guid?> GetOfficerIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an admin decision with a conditional UPDATE ... WHERE approval_state = 'Ready', so of two
    /// concurrent deciders exactly one wins. Executes immediately (not staged): call inside
    /// <see cref="IUnitOfWork.ExecuteInTransactionAsync{T}"/>. False when the submission is no longer Ready.
    /// </summary>
    Task<bool> TryRecordDecisionAsync(
        Guid id, VettingApprovalState newState, VettingDecision decision, string? reason, CancellationToken cancellationToken = default);

    /// <summary>Filtered page, newest first, plus the total match count before paging.</summary>
    Task<(List<VettingSubmissionListItemDto> Items, int TotalCount)> SearchAsync(
        VettingSubmissionSearchCriteria criteria, CancellationToken cancellationToken = default);
}
