namespace CRM.Application.Common.Behaviours;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using MediatR;
using System.Text.Json;

public class AuditBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IAuditLogRepository _auditLogs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AuditBehaviour(
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _auditLogs = auditLogs;
        _unitOfWork = unitOfWork;
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

            await _auditLogs.AddAsync(auditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
