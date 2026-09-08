namespace CRM.Domain.Entities;

using CRM.Domain.Enums;

/// <summary>
/// An in-app notification delivered to a single user. Append-only from the
/// user's point of view — the only mutation is marking it read — so it does
/// NOT inherit <see cref="CRM.Domain.Common.BaseEntity"/> and has no UpdatedAt.
/// </summary>
public class Notification
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public NotificationType NotificationType { get; set; }

    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; } // "Donor" or "Task" — loose polymorphic discriminator, no real FK, stays a string
}
