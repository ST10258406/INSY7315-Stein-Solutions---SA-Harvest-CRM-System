namespace CRM.Application.Modules.Notifications.Dtos;

using CRM.Application.Common.Models;

/// <summary>
/// GET /notifications response. Same <c>data</c> + <c>pagination</c> envelope as every
/// other list, plus <c>unreadCount</c> sitting <b>outside</b> the pagination envelope so
/// the bell badge is one cheap number regardless of the current page.
/// </summary>
public class NotificationListDto
{
    public IReadOnlyList<NotificationDto> Data { get; init; } = [];
    public PaginationMeta Pagination { get; init; } = null!;
    public int UnreadCount { get; init; }
}
