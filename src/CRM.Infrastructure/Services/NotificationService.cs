namespace CRM.Infrastructure.Services;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;

/// <summary>
/// EF Core backed <see cref="INotificationService"/>. Scoped, so it shares the same
/// <see cref="CrmDbContext"/> as the repositories and <see cref="IUnitOfWork"/> resolved
/// in the same scope.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly CrmDbContext _context;

    public NotificationService(CrmDbContext context) => _context = context;

    public async Task CreateAsync(
        Guid userId,
        string title,
        string message,
        NotificationType type,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default)
    {
        _context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            NotificationType = type,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
