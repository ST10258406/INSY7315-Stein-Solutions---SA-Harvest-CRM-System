namespace CRM.Application.Modules.Users.Commands.SetUserActiveStatus;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class SetUserActiveStatusCommand : IRequest<UserListItemDto>, IAuditableCommand
{
    public Guid Id { get; set; }
    // Nullable so a missing value reaches the validator and is rejected, instead of
    // defaulting to false (a destructive deactivation). See SetUserActiveStatusRequest.
    public bool? IsActive { get; set; }

    public string EntityType => "User";
    public AuditAction Action => IsActive == true ? AuditAction.Updated : AuditAction.Deleted;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
