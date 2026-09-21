namespace CRM.Application.Modules.Users.Commands.CreateUser;

using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Utilities;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResponseDto>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public CreateUserCommandHandler(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<CreateUserResponseDto> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var currentUserId = _currentUserService.GetCurrentUserId();

        var temporaryPassword = TemporaryPasswordGenerator.Generate();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = req.Email,
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, temporaryPassword);

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = req.RoleId,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = currentUserId
        });

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = user.Id;
        // The temporary password is deliberately excluded from NewValues — audit_logs
        // must never contain a plaintext credential, even a one-time one.
        command.NewValues = new { user.Id, user.FirstName, user.LastName, user.Email, RoleId = req.RoleId };

        var created = await _users.GetByIdWithRoleAsync(user.Id, cancellationToken)
            ?? throw new InvalidOperationException($"User {user.Id} could not be re-read immediately after being created.");

        var role = created.UserRoles.FirstOrDefault()?.Role;

        return new CreateUserResponseDto
        {
            Id = created.Id,
            FirstName = created.FirstName,
            LastName = created.LastName,
            Email = created.Email,
            RoleId = role?.Id ?? req.RoleId,
            Role = role?.Name ?? string.Empty,
            IsActive = created.IsActive,
            CreatedAt = created.CreatedAt,
            TemporaryPassword = temporaryPassword
        };
    }
}
