namespace CRM.Application.Modules.Users.Commands.CreateUser;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class CreateUserCommand : IRequest<CreateUserResponseDto>, IAuditableCommand
{
    public CreateUserRequest Request { get; set; } = null!;

    public string EntityType => "User";
    public AuditAction Action => AuditAction.Created;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
