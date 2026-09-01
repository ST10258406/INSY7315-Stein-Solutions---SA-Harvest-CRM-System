namespace CRM.Application.Modules.Notifications.Queries.GetNotifications;

using CRM.Application.Modules.Notifications.Dtos;
using CRM.Application.Common.Models;
using MediatR;

/// <summary>
/// GET /api/v1/notifications — the current user's notifications, newest first.
/// Optional <see cref="IsRead"/> filter; ordering is fixed.
/// </summary>
public record GetNotificationsQuery : PaginationParams, IRequest<NotificationListDto>
{
    public bool? IsRead { get; init; }
}
