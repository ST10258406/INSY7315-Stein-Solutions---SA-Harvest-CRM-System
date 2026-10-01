namespace CRM.Application.Common.Interfaces;

/// <summary>
/// Commits all pending changes tracked by the current persistence scope.
/// Implemented in CRM.Infrastructure so the Application layer stays free of EF Core.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="work"/> in one database transaction: everything it writes —
    /// including repository statements that execute immediately (atomic claims) — becomes
    /// visible to other requests together at commit, or not at all if it throws. Use when
    /// an intermediate state must never be observable.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken = default);
}
