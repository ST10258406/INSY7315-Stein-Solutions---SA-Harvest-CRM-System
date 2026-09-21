namespace CRM.Infrastructure.Persistence.Repositories;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;

public class EmailLogRepository : IEmailLogRepository
{
    private readonly CrmDbContext _context;

    public EmailLogRepository(CrmDbContext context) => _context = context;

    public Task AddAsync(EmailLog emailLog, CancellationToken cancellationToken = default)
    {
        _context.EmailLogs.Add(emailLog);
        return Task.CompletedTask;
    }
}
