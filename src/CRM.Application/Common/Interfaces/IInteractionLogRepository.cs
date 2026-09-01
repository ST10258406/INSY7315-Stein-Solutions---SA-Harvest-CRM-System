namespace CRM.Application.Common.Interfaces;

using CRM.Application.Modules.Interactions.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;

/// <summary>
/// Reads and appends to <c>interaction_logs</c>. Append-only: there is deliberately no
/// update or delete method here.
/// </summary>
public interface IInteractionLogRepository
{
    /// <summary>Stages a new interaction row for insert. Commit with <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    Task AddAsync(InteractionLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Newest-first page of a donor's interactions, optionally filtered by type, plus the
    /// total matching row count (counted before pagination). <c>EmailAttachmentUrl</c> on
    /// each item is the stored blob path — the caller swaps it for a SAS URL.
    /// </summary>
    Task<(List<InteractionLogDto> Items, int TotalCount)> GetByDonorAsync(
        Guid donorId,
        InteractionType? interactionType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Single interaction projected to its DTO, or null. Used to re-read after a write.</summary>
    Task<InteractionLogDto?> GetDtoByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
