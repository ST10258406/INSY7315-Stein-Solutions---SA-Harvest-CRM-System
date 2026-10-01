namespace CRM.Application.Modules.Users.Dtos;

public class UserListItemDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>True while a login lockout is in force (see LoginLockoutOptions) — decided server-side, not by the client clock.</summary>
    public bool IsLockedOut { get; set; }

    /// <summary>When the current lockout ends; null when not locked out.</summary>
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTime CreatedAt { get; set; }
}
