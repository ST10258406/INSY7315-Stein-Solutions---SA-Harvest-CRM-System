namespace CRM.Application.Modules.Notifications.Queries.GetNotifications;

using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Notifications.Dtos;
using MediatR;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, NotificationListDto>
{
    private readonly INotificationRepository _notifications;
    private readonly ICurrentUserService _currentUserService;

    public GetNotificationsQueryHandler(INotificationRepository notifications, ICurrentUserService currentUserService)
    {
        _notifications = notifications;
        _currentUserService = currentUserService;
    }

    public async Task<NotificationListDto> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetCurrentUserId();

        var (items, totalCount) = await _notifications.GetForUserAsync(
            userId, request.IsRead, request.Page, request.PageSize, cancellationToken);

        var unreadCount = await _notifications.GetUnreadCountAsync(userId, cancellationToken);

        return new NotificationListDto
        {
            Data = items,
            Pagination = new PaginationMeta
            {
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            },
            UnreadCount = unreadCount
        };
    }
}
