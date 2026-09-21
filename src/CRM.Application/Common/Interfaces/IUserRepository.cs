namespace CRM.Application.Common.Interfaces;

using CRM.Application.Modules.Donors.Dtos;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Entities;

/// <summary>
/// Persistence access for <see cref="User"/>. Methods documented as "tracked" return an
/// entity whose mutations are committed by the next <see cref="IUnitOfWork.SaveChangesAsync"/>
/// call in the same scope; "read-only" methods return detached instances.
/// </summary>
public interface IUserRepository
{
    /// <summary>Read-only user with roles eagerly loaded, matched by email. Used by Login.</summary>
    Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Tracked user matched by email. Used by ForgotPassword / ResetPassword.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Tracked user matched by id. Used by ChangePassword.</summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Ids of every active user holding the named role. Used for admin notification fan-out.</summary>
    Task<List<Guid>> GetActiveUserIdsByRoleAsync(string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Active users holding the named role, ordered by name. Used to populate the
    /// "relationship manager" dropdown (GET /api/v1/lookups/relationship-managers).
    /// </summary>
    Task<List<RelationshipManagerDto>> GetActiveByRoleAsync(string roleName, CancellationToken cancellationToken = default);

    /// <summary>True when a user with this id exists and is active.</summary>
    Task<bool> ExistsAndActiveAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Paginated, filtered listing for the Users admin screen (GET /api/v1/users).</summary>
    Task<(List<UserListItemDto> Items, int TotalCount)> SearchAsync(UserSearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>Tracked user with its role eagerly loaded. Used by UpdateUser / ChangeUserRole / SetUserActiveStatus.</summary>
    Task<User?> GetByIdWithRoleAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds a new user. Caller sets Id and PasswordHash before calling.</summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>True when another user (optionally excluding <paramref name="excludeUserId"/>) already has this email.</summary>
    Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>All roles, ordered by name — backs the role dropdowns on the Users screen.</summary>
    Task<List<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>True when a role with this id exists.</summary>
    Task<bool> RoleExistsAsync(Guid roleId, CancellationToken cancellationToken = default);
}
