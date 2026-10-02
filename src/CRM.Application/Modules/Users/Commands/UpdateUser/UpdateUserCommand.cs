namespace CRM.Application.Modules.Users.Commands.UpdateUser;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class UpdateUserCommand : IRequest<UserListItemDto>, IAuditableCommand
{
    public Guid Id { get; set; }
    public UpdateUserRequest Request { get; set; } = null!;

    public string EntityType => "User";
    public AuditAction Action => AuditAction.Updated;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
