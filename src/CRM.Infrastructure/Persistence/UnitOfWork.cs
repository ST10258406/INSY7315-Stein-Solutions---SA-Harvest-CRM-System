namespace CRM.Infrastructure.Persistence;

using CRM.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// EF Core backed <see cref="IUnitOfWork"/>. Registered as scoped so that it shares the
/// same <see cref="CrmDbContext"/> instance as every repository resolved in the same scope,
/// which is what makes multi-repository mutations commit in a single transaction.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly CrmDbContext _context;

    public UnitOfWork(CrmDbContext context) => _context = context;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken = default)
    {
        // The InMemory provider used by CRM.API.Tests has no transactions; there the work
        // just runs. Nested calls join the transaction already in progress.
        if (!_context.Database.IsRelational() || _context.Database.CurrentTransaction is not null)
            return await work();

        // No retrying execution strategy is configured, so a plain user transaction is safe.
        // ExecuteUpdate/ExecuteDelete and SaveChanges all enlist in it.
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var result = await work();
        await transaction.CommitAsync(cancellationToken);
        return result; // an exception before commit disposes the transaction → rollback
    }
}
