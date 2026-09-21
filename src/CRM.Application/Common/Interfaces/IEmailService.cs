namespace CRM.Application.Common.Interfaces;

using CRM.Application.Common.Models;
using CRM.Domain.Enums;

public interface IEmailService
{
    /// <summary>
    /// Sends an email and logs the attempt (success or failure) to EmailLog.
    /// Best-effort: never throws — a failed send is logged, not propagated,
    /// since in-app notifications are the primary channel by default. Callers that
    /// need to react to a failed send synchronously (e.g. surface an error to an
    /// interactive caller) inspect the returned <see cref="EmailSendResult.Status"/>.
    /// </summary>
    /// <param name="emailType">What kind of email this is, for the EmailLog audit trail.</param>
    /// <param name="donorId">The donor this email relates to, if any.</param>
    /// <param name="sentByUserId">The user who triggered the send, if any (null for system-sent emails).</param>
    Task<EmailSendResult> SendAsync(
        string to,
        string subject,
        string htmlBody,
        EmailType emailType,
        Guid? donorId = null,
        Guid? sentByUserId = null);
}
