namespace CRM.Infrastructure.Persistence;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <summary>
/// EF Core backed <see cref="IUnitOfWork"/>. Registered as scoped so that it shares the
/// same <see cref="CrmDbContext"/> instance as every repository resolved in the same scope,
/// which is what makes multi-repository mutations commit in a single transaction.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly CrmDbContext _context;

    public UnitOfWork(CrmDbContext context) => _context = context;

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This record was changed by someone else. Reload it and try again.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // A concurrent request won the race past the handler's own duplicate check.
            throw new ConflictException("This already exists. Reload and try again.");
        }
    }

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
