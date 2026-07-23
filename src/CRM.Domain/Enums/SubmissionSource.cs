namespace CRM.Domain.Enums;

/// <summary>
/// How a donor record originally entered the system.
/// </summary>
public enum SubmissionSource
{
    /// <summary>Submitted by the donor themselves via the public onboarding form.</summary>
    PublicForm,

    /// <summary>Captured directly by an internal staff member.</summary>
    ManualCapture
}
