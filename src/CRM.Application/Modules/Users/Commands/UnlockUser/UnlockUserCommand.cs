namespace CRM.Application.Modules.Users.Commands.UnlockUser;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Enums;
using MediatR;

/// <summary>
/// Clears a login lockout early (see LoginLockoutOptions) — the escape hatch for a staff
/// member who was locked out, possibly deliberately by someone guessing at their account.
/// </summary>
public class UnlockUserCommand : IRequest<UserListItemDto>, IAuditableCommand
{
    public Guid Id { get; set; }

    public string EntityType => "User";
    public AuditAction Action => AuditAction.Updated;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
