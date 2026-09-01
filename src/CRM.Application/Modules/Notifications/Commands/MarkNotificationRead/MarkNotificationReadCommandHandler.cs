namespace CRM.Application.Modules.Notifications.Commands.MarkNotificationRead;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using MediatR;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly INotificationRepository _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public MarkNotificationReadCommandHandler(
        INotificationRepository notifications, IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notifications.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.Id);

        if (notification.UserId != _currentUserService.GetCurrentUserId())
            throw new ForbiddenException("This notification belongs to another user.");

        if (notification.IsRead)
            return; // idempotent — keep the original ReadAt

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
