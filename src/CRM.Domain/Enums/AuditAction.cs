namespace CRM.Domain.Enums;

/// <summary>
/// The action recorded against an entity in the append-only audit trail.
/// </summary>
public enum AuditAction
{
    /// <summary>A new record was inserted.</summary>
    Created,

    /// <summary>An existing record was changed — old and new values captured.</summary>
    Updated,

    /// <summary>A record was soft-deleted (IsActive set to false).</summary>
    Deleted,

    /// <summary>A donor approval was actioned as approved.</summary>
    Approved,

    /// <summary>A donor approval was actioned as rejected.</summary>
    Rejected,

    /// <summary>A sensitive document was accessed (e.g. BBBEE certificate download).</summary>
    Viewed,

    /// <summary>A report was exported to PDF or Excel.</summary>
    Exported
}
