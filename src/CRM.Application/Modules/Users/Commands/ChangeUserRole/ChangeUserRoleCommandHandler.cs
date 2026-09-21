namespace CRM.Application.Modules.Users.Commands.ChangeUserRole;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Entities;
using MediatR;

public class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand, UserListItemDto>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ChangeUserRoleCommandHandler(IUserRepository users, IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<UserListItemDto> Handle(ChangeUserRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdWithRoleAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"User {command.Id} was not found.");

        var existing = user.UserRoles.OrderByDescending(ur => ur.AssignedAt).FirstOrDefault();
        command.OldValues = new { RoleId = existing?.RoleId };

        // The modal preselects the current role, so re-submitting it unchanged is a
        // normal request. Clearing the tracked collection and adding a new row with the
        // SAME composite key (UserId, RoleId) leaves two tracked entries with an
        // identical key — EF Core throws on SaveChanges. Only replace the assignment
        // when the role actually changed; same "clear + re-add" pattern used for donor
        // junction rows (DonorOperationalRegion/DonorDonationType) otherwise.
        if (existing is null || existing.RoleId != command.Request.RoleId)
        {
            user.UserRoles.Clear();
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = command.Request.RoleId,
                AssignedAt = DateTime.UtcNow,
                AssignedByUserId = _currentUserService.GetCurrentUserId()
            });

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        command.EntityId = user.Id;
        command.NewValues = new { RoleId = command.Request.RoleId };

        var refreshed = await _users.GetByIdWithRoleAsync(user.Id, cancellationToken)
            ?? throw new InvalidOperationException($"User {user.Id} could not be re-read immediately after its role changed.");
        var role = refreshed.UserRoles.FirstOrDefault()?.Role;

        return new UserListItemDto
        {
            Id = refreshed.Id,
            FirstName = refreshed.FirstName,
            LastName = refreshed.LastName,
            Email = refreshed.Email,
            RoleId = role?.Id ?? Guid.Empty,
            Role = role?.Name ?? string.Empty,
            IsActive = refreshed.IsActive,
            CreatedAt = refreshed.CreatedAt
        };
    }
}
