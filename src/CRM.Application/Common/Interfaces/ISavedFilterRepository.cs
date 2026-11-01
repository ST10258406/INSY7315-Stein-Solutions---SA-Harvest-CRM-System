namespace CRM.Application.Common.Interfaces;

using CRM.Application.Modules.SavedFilters.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;

/// <summary>
/// Saved donor-list filters (Issue 103). Every method takes the owner's id, so a caller can never
/// read or change another user's filter: another user's id simply isn't found.
/// </summary>
public interface ISavedFilterRepository
{
    /// <summary>The user's active filters for a page, by name. No tracking.</summary>
    Task<List<SavedFilterDto>> ListActiveAsync(Guid userId, SavedFilterPage page, CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Case-insensitive match against the user's active filter names.</summary>
    Task<bool> ActiveNameExistsAsync(Guid userId, string name, CancellationToken cancellationToken = default);

    /// <summary>Returns a <b>tracked</b> active filter owned by the user, for soft delete; null otherwise.</summary>
    Task<SavedFilter?> GetActiveForUpdateAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(SavedFilter filter, CancellationToken cancellationToken = default);
}
