namespace CRM.Application.Common.Interfaces;

using CRM.Application.Modules.Notifications.Dtos;
using CRM.Domain.Entities;

/// <summary>
/// Read + read-state access for <c>notifications</c>. The write side (creating rows) is
/// <see cref="INotificationService"/>.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// One user's notifications, newest first, optionally filtered by read state, plus the
    /// total matching count (before pagination).
    /// </summary>
    Task<(List<NotificationDto> Items, int TotalCount)> GetForUserAsync(
        Guid userId, bool? isRead, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Cheap COUNT(*) of a user's unread notifications (uses the partial index).</summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Tracked notification by id for the mark-read handler. Null when missing.</summary>
    Task<Notification?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks every one of the user's unread notifications read in a single UPDATE
    /// (no fetch-then-loop). Returns the number of rows affected.
    /// </summary>
    Task<int> MarkAllReadAsync(Guid userId, DateTime readAt, CancellationToken cancellationToken = default);
}
