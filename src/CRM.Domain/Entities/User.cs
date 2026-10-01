namespace CRM.Domain.Entities;

using CRM.Domain.Common;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public string? PasswordResetToken { get; set; }
    public DateTimeOffset? PasswordResetTokenExpiresAt { get; set; }

    /// <summary>Consecutive failed logins since the last success or lockout (see LoginCommandHandler).</summary>
    public int FailedLoginCount { get; set; }

    /// <summary>While in the future, every login is rejected — even with the correct password.</summary>
    public DateTimeOffset? LockoutEndUtc { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
