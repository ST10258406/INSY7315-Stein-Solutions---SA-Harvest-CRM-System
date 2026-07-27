namespace CRM.Application.Common.Behaviours;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using MediatR;
using System.Text.Json;

public class AuditBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public AuditBehaviour(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IAuditableCommand auditable)
        {
            var auditLog = new AuditLog
            {
                UserId = _currentUserService.GetCurrentUserId(),
                EntityType = auditable.EntityType,
                EntityId = auditable.EntityId,
                Action = auditable.Action,
                OldValues = auditable.OldValues is null ? null : JsonSerializer.Serialize(auditable.OldValues),
                NewValues = auditable.NewValues is null ? null : JsonSerializer.Serialize(auditable.NewValues),
                IpAddress = null,
                UserAgent = null,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
