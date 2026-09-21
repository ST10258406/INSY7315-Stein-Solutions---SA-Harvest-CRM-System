namespace CRM.Domain.Enums;

/// <summary>
/// The kind of outbound email an <see cref="Entities.EmailLog"/> row records.
/// </summary>
public enum EmailType
{
    /// <summary>A user-initiated password reset link.</summary>
    PasswordReset,

    /// <summary>Confirmation sent to a donor after submitting the public onboarding form.</summary>
    OnboardingConfirmation,

    /// <summary>Ad-hoc correspondence sent to a donor by a CRM user.</summary>
    DonorCorrespondence,

    /// <summary>Invitation to a prospective (not-yet-a-donor) contact to complete the public onboarding form.</summary>
    PublicFormInvite
}
