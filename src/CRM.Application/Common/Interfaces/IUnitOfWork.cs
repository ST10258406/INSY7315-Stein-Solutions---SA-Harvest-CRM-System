namespace CRM.Application.Common.Interfaces;

/// <summary>
/// Commits all pending changes tracked by the current persistence scope.
/// Implemented in CRM.Infrastructure so the Application layer stays free of EF Core.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
