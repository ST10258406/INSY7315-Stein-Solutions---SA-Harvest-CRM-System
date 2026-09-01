namespace CRM.Application.Modules.Notifications.Commands.MarkAllNotificationsRead;

using MediatR;

/// <summary>PATCH /api/v1/notifications/read-all — marks all of the caller's unread notifications read.</summary>
public class MarkAllNotificationsReadCommand : IRequest
{
}
