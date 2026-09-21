namespace CRM.Application.Modules.Users.Commands.ChangeUserRole;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class ChangeUserRoleCommand : IRequest<UserListItemDto>, IAuditableCommand
{
    public Guid Id { get; set; }
    public ChangeUserRoleRequest Request { get; set; } = null!;

    public string EntityType => "User";
    public AuditAction Action => AuditAction.Updated;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
