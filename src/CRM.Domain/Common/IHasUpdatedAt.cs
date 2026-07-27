namespace CRM.Domain.Common;

/// <summary>
/// Marker interface for entities that track a last-modified timestamp.
/// Implemented by BaseEntity so every main entity gets it automatically.
/// Deliberately NOT implemented by InteractionLog or AuditLog — both are
/// append-only tables with no UpdatedAt column by design.
/// </summary>
public interface IHasUpdatedAt
{
    DateTime UpdatedAt { get; set; }
}
