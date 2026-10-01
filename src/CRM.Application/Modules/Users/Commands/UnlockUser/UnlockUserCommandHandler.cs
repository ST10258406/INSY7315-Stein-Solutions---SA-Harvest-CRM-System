namespace CRM.Application.Modules.Users.Commands.UnlockUser;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Dtos;
using MediatR;

public class UnlockUserCommandHandler : IRequestHandler<UnlockUserCommand, UserListItemDto>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public UnlockUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserListItemDto> Handle(UnlockUserCommand command, CancellationToken cancellationToken)
    {
        // Whether the caller may unlock *this* user is decided at the API boundary — see
        // UserTargetAuthorizationFilter / UserTargetAuthorizationHandler in CRM.API.
        var user = await _users.GetByIdWithRoleAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"User {command.Id} was not found.");

        command.OldValues = new { user.FailedLoginCount, user.LockoutEndUtc };

        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = user.Id;
        command.NewValues = new { user.FailedLoginCount, user.LockoutEndUtc };

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
