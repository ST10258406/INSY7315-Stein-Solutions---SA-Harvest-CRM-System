namespace CRM.Application.Common.Interfaces;

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

    /// <summary>True when a user with this id exists and is active.</summary>
    Task<bool> ExistsAndActiveAsync(Guid userId, CancellationToken cancellationToken = default);
}
