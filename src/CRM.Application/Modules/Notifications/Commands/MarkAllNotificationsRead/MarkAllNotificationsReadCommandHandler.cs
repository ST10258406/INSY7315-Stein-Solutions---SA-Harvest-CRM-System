namespace CRM.Application.Modules.Notifications.Commands.MarkAllNotificationsRead;

using CRM.Application.Common.Interfaces;
using MediatR;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand>
{
    private readonly INotificationRepository _notifications;
    private readonly ICurrentUserService _currentUserService;

    public MarkAllNotificationsReadCommandHandler(
        INotificationRepository notifications, ICurrentUserService currentUserService)
    {
        _notifications = notifications;
        _currentUserService = currentUserService;
    }

    public async Task Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        // Single UPDATE, self-committing — no IUnitOfWork round trip needed. The
        // affected-row count is intentionally discarded (endpoint returns 204).
        await _notifications.MarkAllReadAsync(
            _currentUserService.GetCurrentUserId(), DateTime.UtcNow, cancellationToken);
    }
}
