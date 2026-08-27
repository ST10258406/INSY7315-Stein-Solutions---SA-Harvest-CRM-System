using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CRM.Application.Modules.Auth.Commands.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public ChangePasswordCommandHandler(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();

        // Tracked — we're updating this entity.
        var user = await _users.GetByIdAsync(currentUserId, ct);

        if (user is null)
            throw new NotFoundException("User not found."); // shouldn't happen if the token is valid, but don't assume

        var verifyResult = _passwordHasher.VerifyHashedPassword(
            user, user.PasswordHash, request.CurrentPassword);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("CurrentPassword", "Current password is incorrect.") });
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("NewPassword", "New password cannot be the same as the current password.") });
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
