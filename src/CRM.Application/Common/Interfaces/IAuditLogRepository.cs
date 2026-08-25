namespace CRM.Application.Common.Interfaces;

using CRM.Domain.Entities;

public interface IAuditLogRepository
{
    /// <summary>Stages an audit log row for insert. Commit with <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
}
