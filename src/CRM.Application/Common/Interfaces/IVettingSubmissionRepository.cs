namespace CRM.Application.Common.Interfaces;

using CRM.Application.Common.Models;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Entities.Vetting;

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
    /// Feeds PATCH and submit. Null when not found.
    /// </summary>
    Task<VettingSubmission?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The full read model (details, active flags, photos, filled file slots), or null.</summary>
    Task<VettingSubmissionDto?> GetDtoByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Filtered page, newest first, plus the total match count before paging.</summary>
    Task<(List<VettingSubmissionListItemDto> Items, int TotalCount)> SearchAsync(
        VettingSubmissionSearchCriteria criteria, CancellationToken cancellationToken = default);
}
