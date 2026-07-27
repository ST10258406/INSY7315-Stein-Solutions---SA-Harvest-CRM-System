namespace CRM.Application.Common.Interfaces;

using CRM.Domain.Enums;

public interface INotificationService
{
    Task CreateAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null);
}
