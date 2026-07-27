namespace CRM.Infrastructure.Services;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Enums;

public class NotificationService : INotificationService
{
    public Task CreateAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        // TODO: Skeleton
        return Task.CompletedTask;
    }
}
