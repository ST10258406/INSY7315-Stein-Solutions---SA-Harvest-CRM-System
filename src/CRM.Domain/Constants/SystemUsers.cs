namespace CRM.Domain.Constants;

/// <summary>
/// Well-known, non-authenticatable "actor" users that own records created by a
/// process rather than a logged-in person. There is no login for these — they
/// only exist so that non-nullable "created/requested/acted by" foreign keys
/// (e.g. Donor.CreatedByUserId, DonorApproval.RequestedByUserId) have something
/// valid to point at when the real actor is an anonymous public form, not a
/// staff member.
///
/// A system user is identified by the reserved <see cref="ReservedEmailDomain"/>
/// (a reserved-address rule rather than a column, so no migration/backfill is
/// needed). The rule is only safe because it is closed in both directions: system
/// users can never be loaded for editing (so they can't be renamed *out* of the
/// domain), and the user create/update validators reject any address in the domain
/// (so a staff account can't be renamed *into* it).
/// </summary>
public static class SystemUsers
{
    /// <summary>
    /// Every address in this domain belongs to a system user. ".local" is reserved for
    /// multicast DNS (RFC 6762), so no real mailbox can ever exist here.
    /// </summary>
    public const string ReservedEmailDomain = "@system.local";

    /// <summary>
    /// Owns every donor (and its approval/interaction log) created via the public
    /// onboarding form (POST /public/donors/submit). Seeded once by SystemUserSeeder;
    /// never assigned a role, never active, never able to authenticate.
    /// </summary>
    public const string PublicFormEmail = "public-form" + ReservedEmailDomain;

    /// <summary>True when <paramref name="email"/> is in the reserved system-user domain (case-insensitive).</summary>
    public static bool IsSystemUserEmail(string? email)
        => email is not null && email.Trim().EndsWith(ReservedEmailDomain, StringComparison.OrdinalIgnoreCase);
}
