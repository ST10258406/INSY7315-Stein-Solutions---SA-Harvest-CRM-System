namespace CRM.Application.Common.Interfaces;

using CRM.Domain.Entities;

public interface IEmailLogRepository
{
    /// <summary>Stages an email log row for insert. Commit with <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    Task AddAsync(EmailLog emailLog, CancellationToken cancellationToken = default);
}
