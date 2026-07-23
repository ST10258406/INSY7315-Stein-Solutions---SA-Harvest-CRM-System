namespace CRM.Domain.Entities;

using CRM.Domain.Enums;

public class AuditLog
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string EntityType { get; set; } = string.Empty; // stays string — open-ended, grows as new entities get audited
    public Guid EntityId { get; set; }

    public AuditAction Action { get; set; }

    public string? OldValues { get; set; }
    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
