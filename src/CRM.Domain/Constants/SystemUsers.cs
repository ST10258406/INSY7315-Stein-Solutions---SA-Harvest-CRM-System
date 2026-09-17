namespace CRM.Domain.Constants;

/// <summary>
/// Well-known, non-authenticatable "actor" users that own records created by a
/// process rather than a logged-in person. There is no login for these — they
/// only exist so that non-nullable "created/requested/acted by" foreign keys
/// (e.g. Donor.CreatedByUserId, DonorApproval.RequestedByUserId) have something
/// valid to point at when the real actor is an anonymous public form, not a
/// staff member.
/// </summary>
public static class SystemUsers
{
    /// <summary>
    /// Owns every donor (and its approval/interaction log) created via the public
    /// onboarding form (POST /public/donors/submit). Seeded once by SystemUserSeeder;
    /// never assigned a role, never active, never able to authenticate.
    /// </summary>
    public const string PublicFormEmail = "public-form@system.local";
}
