namespace CRM.Application.Common.Interfaces;

using CRM.Domain.Enums;

/// <summary>
/// Marker interface for MediatR commands that should be automatically audited.
/// Any command implementing this gets an audit_logs row written by AuditBehaviour
/// after it completes successfully — handlers never write audit entries themselves.
///
/// How it works: the handler sets EntityId, OldValues, and NewValues WHILE it runs
/// (e.g. after EF Core assigns the new entity's Id, or before/after the DB write),
/// then AuditBehaviour reads them once the handler is done and writes the audit_logs row.
///
/// NOTE ON MUTABILITY: OldValues and NewValues have setters because the handler
/// populates them during execution. AuditBehaviour reads them after the handler
/// completes. No code outside the handler or AuditBehaviour should set these.
/// </summary>
public interface IAuditableCommand
{
    /// <summary>
    /// e.g. "Donor", "User", "DonorTask" — matches audit_logs.entity_type.
    /// Set as a get-only property on the implementing command class.
    /// </summary>
    string EntityType { get; }

    /// <summary>
    /// The audit action performed. Uses AuditAction enum to prevent
    /// typos writing garbage values to audit_logs.
    /// </summary>
    AuditAction Action { get; }

    /// <summary>
    /// The id of the record acted upon. For Create actions this is not known
    /// until after the handler runs (EF Core assigns it) — handler sets this
    /// after SaveChangesAsync() so AuditBehaviour can read the real Id.
    /// </summary>
    Guid EntityId { get; set; }

    /// <summary>
    /// JSON-serializable snapshot before the change. Null for Created actions.
    /// Handler captures this before the DB write.
    /// </summary>
    object? OldValues { get; set; }

    /// <summary>
    /// JSON-serializable snapshot after the change. Null for Deleted/Viewed actions.
    /// Handler captures this after the DB write.
    /// </summary>
    object? NewValues { get; set; }
}
