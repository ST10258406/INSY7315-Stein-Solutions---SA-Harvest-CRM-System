namespace CRM.Infrastructure.Persistence;

using CRM.Application.Common.Interfaces;

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
}
