namespace CRM.Infrastructure.Persistence.Repositories;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly CrmDbContext _context;

    public AuditLogRepository(CrmDbContext context) => _context = context;

    public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Add(auditLog);
        return Task.CompletedTask;
    }
}
