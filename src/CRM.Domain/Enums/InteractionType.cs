namespace CRM.Domain.Enums;

/// <summary>
/// The kind of interaction logged against a donor.
/// </summary>
public enum InteractionType
{
    /// <summary>Free text note added by staff.</summary>
    Note,

    /// <summary>Email sent from or forwarded to the CRM.</summary>
    Email,

    /// <summary>Phone call logged by staff.</summary>
    Call,

    /// <summary>In-person or virtual meeting.</summary>
    Meeting,

    /// <summary>Automatically created when a public form is submitted.</summary>
    FormSubmission,

    /// <summary>Automatically created when a follow-up reminder is acted on.</summary>
    FollowUp
}
