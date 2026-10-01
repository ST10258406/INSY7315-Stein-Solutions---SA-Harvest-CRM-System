namespace CRM.Application.Modules.Users.Commands.UpdateUser;

using System.Net;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserListItemDto>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateUserCommandHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ICurrentUserService currentUserService)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _currentUserService = currentUserService;
    }

    public async Task<UserListItemDto> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        // Whether the caller may edit *this* user (SuperAdmins and system users are
        // protected) is decided at the API boundary — see UserTargetAuthorizationFilter /
        // UserTargetAuthorizationHandler in CRM.API.
        var user = await _users.GetByIdWithRoleAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"User {command.Id} was not found.");

        command.OldValues = new { user.FirstName, user.LastName, user.Email };

        var previousEmail = user.Email;
        var emailChanged = !string.Equals(previousEmail, command.Request.Email, StringComparison.Ordinal);

        user.FirstName = command.Request.FirstName;
        user.LastName = command.Request.LastName;
        user.Email = command.Request.Email;

        // The email is the login and the password-reset destination, so a change must end
        // every existing session — otherwise whoever held a refresh token before the change
        // keeps access after it (same loop as ResetPasswordCommandHandler).
        if (emailChanged)
        {
            var activeTokens = await _refreshTokens.GetActiveByUserIdAsync(user.Id, cancellationToken);
            foreach (var rt in activeTokens)
            {
                rt.IsRevoked = true;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Tell the previous address after the change is committed, so the real owner
        // finds out if their account was redirected without their knowledge. Best-effort:
        // IEmailService never throws and logs every attempt to email_logs.
        if (emailChanged)
        {
            await _emailService.SendAsync(
                to: previousEmail,
                subject: "Your SA Harvest CRM login email was changed",
                htmlBody: BuildEmailChangedBody(user.FirstName),
                emailType: EmailType.AccountEmailChanged,
                sentByUserId: _currentUserService.GetCurrentUserId());
        }

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
            IsLockedOut = user.LockoutEndUtc > DateTimeOffset.UtcNow,
            LockedUntil = user.LockoutEndUtc > DateTimeOffset.UtcNow ? user.LockoutEndUtc : null,
            CreatedAt = user.CreatedAt
        };
    }

    // Deliberately doesn't include the new address — this mailbox may no longer belong to
    // the account holder, and the notice only needs to prompt them to check.
    private static string BuildEmailChangedBody(string firstName) =>
        $"<p>Hi {WebUtility.HtmlEncode(firstName)},</p><p>The login email address for your SA Harvest CRM account was just changed by an administrator, and you have been signed out of all sessions. Future sign-ins and password resets will use the new address.</p><p>If you did not expect this change, contact your SA Harvest CRM administrator immediately.</p>";
}
