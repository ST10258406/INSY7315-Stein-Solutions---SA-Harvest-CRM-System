namespace CRM.Application.Modules.Users.Commands.SetUserActiveStatus;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Dtos;
using MediatR;

public class SetUserActiveStatusCommandHandler : IRequestHandler<SetUserActiveStatusCommand, UserListItemDto>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SetUserActiveStatusCommandHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<UserListItemDto> Handle(SetUserActiveStatusCommand command, CancellationToken cancellationToken)
    {
        // Non-null is guaranteed by SetUserActiveStatusCommandValidator (ValidationBehaviour
        // runs first) — a missing value must never fall through to "deactivate".
        var isActive = command.IsActive!.Value;

        // A user cannot deactivate their own account — mirrors the "You" badge/disabled
        // menu item on the Users screen; enforced here too since the frontend guard is
        // cosmetic only.
        if (command.Id == _currentUserService.GetCurrentUserId() && !isActive)
            throw new ForbiddenException("You cannot deactivate your own account.");

        var user = await _users.GetByIdWithRoleAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"User {command.Id} was not found.");

        command.OldValues = new { user.IsActive };

        user.IsActive = isActive;

        // Deactivating a user must kill their access immediately, not just block future
        // logins — otherwise an existing refresh token keeps working indefinitely (Login
        // and Refresh both reject IsActive == false, but only for new/renewed tokens).
        if (!isActive)
        {
            var activeTokens = await _refreshTokens.GetActiveByUserIdAsync(user.Id, cancellationToken);
            foreach (var rt in activeTokens)
            {
                rt.Revoke(DateTimeOffset.UtcNow);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = user.Id;
        command.NewValues = new { user.IsActive };

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
            IsLockedOut = user.LockoutEndUtc > DateTimeOffset.UtcNow,
            LockedUntil = user.LockoutEndUtc > DateTimeOffset.UtcNow ? user.LockoutEndUtc : null,
            CreatedAt = user.CreatedAt
        };
    }
}
