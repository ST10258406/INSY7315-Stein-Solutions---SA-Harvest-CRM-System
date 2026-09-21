namespace CRM.Domain.Enums;

/// <summary>
/// The outcome of an outbound email send. Sends are synchronous — there is
/// deliberately no Pending state.
/// </summary>
public enum EmailStatus
{
    Sent,
    Failed
}
