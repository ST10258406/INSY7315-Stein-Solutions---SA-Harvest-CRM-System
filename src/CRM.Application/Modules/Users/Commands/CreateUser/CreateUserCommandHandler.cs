namespace CRM.Application.Modules.Users.Commands.CreateUser;

using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Utilities;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Entities;
using MediatR;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResponseDto>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IUserPasswordHasher passwordHasher)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
    }

    public async Task<CreateUserResponseDto> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var currentUserId = _currentUserService.GetCurrentUserId();

        // Whether this admin is allowed to assign req.RoleId (specifically, whether it's
        // the SuperAdmin role) is decided at the API boundary — see
        // CreateUserAuthorizationFilter / RoleAssignmentAuthorizationHandler in CRM.API.
        // Role enforcement belongs on the controller, not in an Application handler.
        var temporaryPassword = TemporaryPasswordGenerator.Generate();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = req.Email,
            IsActive = true,
            // The creating admin sees this temporary password, so it must be replaced at first login.
            MustChangePassword = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(temporaryPassword);

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = req.RoleId,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = currentUserId
        });

        // Everything the response needs is resolved BEFORE the commit. Nothing that can
        // fail may run after SaveChanges: once the user row exists, a post-commit error
        // would return 500 and the one-time temporary password would be lost for good,
        // leaving an account nobody can sign in to.
        var roleName = await _users.GetRoleNameAsync(req.RoleId, cancellationToken) ?? string.Empty;

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = user.Id;
        // The temporary password is deliberately excluded from NewValues — audit_logs
        // must never contain a plaintext credential, even a one-time one.
        command.NewValues = new { user.Id, user.FirstName, user.LastName, user.Email, RoleId = req.RoleId };

        return new CreateUserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            RoleId = req.RoleId,
            Role = roleName,
            IsActive = user.IsActive,
            // Stamped on the tracked entity by UpdatedAtInterceptor during SaveChanges.
            CreatedAt = user.CreatedAt,
            TemporaryPassword = temporaryPassword
        };
    }
}
