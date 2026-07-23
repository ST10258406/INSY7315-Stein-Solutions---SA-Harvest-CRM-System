namespace CRM.Domain.Entities;

/// <summary>
/// Junction table linking a user to an assigned role.
/// No surrogate Id — the composite key (UserId, RoleId) is the natural PK
/// and is configured via Fluent API in CRM.Infrastructure.
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public DateTime AssignedAt { get; set; }

    public Guid AssignedByUserId { get; set; }
    public User AssignedByUser { get; set; } = null!;
}
