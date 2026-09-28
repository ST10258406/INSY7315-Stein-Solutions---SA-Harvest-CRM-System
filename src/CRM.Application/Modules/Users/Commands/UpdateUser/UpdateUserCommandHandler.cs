namespace CRM.Application.Modules.Users.Commands.UpdateUser;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Dtos;
using MediatR;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserListItemDto>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserListItemDto> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdWithRoleAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"User {command.Id} was not found.");

        command.OldValues = new { user.FirstName, user.LastName, user.Email };

        user.FirstName = command.Request.FirstName;
        user.LastName = command.Request.LastName;
        user.Email = command.Request.Email;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = user.Id;
        command.NewValues = new { user.FirstName, user.LastName, user.Email };

        var role = user.UserRoles.OrderByDescending(ur => ur.AssignedAt).FirstOrDefault()?.Role;

        return new UserListItemDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            RoleId = role?.Id ?? Guid.Empty,
            Role = role?.Name ?? string.Empty,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
