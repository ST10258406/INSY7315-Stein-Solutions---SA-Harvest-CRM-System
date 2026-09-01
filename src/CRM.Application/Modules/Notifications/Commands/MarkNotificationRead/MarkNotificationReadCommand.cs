namespace CRM.Application.Modules.Notifications.Commands.MarkNotificationRead;

using MediatR;

/// <summary>PATCH /api/v1/notifications/{id}/read — marks one of the caller's notifications read.</summary>
public class MarkNotificationReadCommand : IRequest
{
    public Guid Id { get; set; }
}
