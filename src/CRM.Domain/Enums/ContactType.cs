namespace CRM.Domain.Enums;

/// <summary>
/// The role a contact person plays for a donor. A donor has at most
/// one contact of each type (enforced by UNIQUE(donor_id, contact_type) in the DB).
/// </summary>
public enum ContactType
{
    /// <summary>Main point of contact — the only type with a JobTitle.</summary>
    Primary,

    /// <summary>Contact for marketing communications and impact reporting.</summary>
    Marketing,

    /// <summary>Contact for invoicing and accounts-related correspondence.</summary>
    Accounts
}
