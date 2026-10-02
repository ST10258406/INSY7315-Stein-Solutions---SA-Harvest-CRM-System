namespace CRM.Infrastructure.Services;

using System.Net.Http.Json;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Services.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Sends transactional email via the Brevo REST API (api-key auth and base
/// address are configured on the injected <see cref="HttpClient"/> — see
/// AddInfrastructureServices). Bodies are wrapped in the branded <see cref="EmailLayout"/>
/// before sending (EmailLog keeps the unwrapped body). Everything goes out from the one verified system sender;
/// user-composed correspondence (see <see cref="StaffComposedEmailTypes"/>) is presented as the
/// signed-in staff member — their name on the sender and their address as Reply-To — so replies
/// reach them directly. Every attempt is logged to EmailLog, success or failure, before this method returns. Never throws: email is
/// best-effort, in-app notifications are the primary channel.
/// </summary>
public class EmailService : IEmailService
{
    private const int MaxErrorMessageLength = 1000;

    /// <summary>
    /// Only mail a staff member deliberately composes carries their identity. System mail (password reset,
    /// onboarding confirmation, account notices) stays plain system-branded even when a user id is
    /// attached for auditing — Reply-To on a password reset would invite replies to an unrelated person.
    /// </summary>
    private static readonly EmailType[] StaffComposedEmailTypes = [EmailType.DonorCorrespondence, EmailType.PublicFormInvite];

    private readonly HttpClient _httpClient;
    private readonly BrevoSettings _settings;
    private readonly IEmailLogRepository _emailLogs;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        HttpClient httpClient,
        IOptions<BrevoSettings> settings,
        IEmailLogRepository emailLogs,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        ILogger<EmailService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _emailLogs = emailLogs;
        _users = users;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(
        string to,
        string subject,
        string htmlBody,
        EmailType emailType,
        Guid? donorId = null,
        Guid? sentByUserId = null)
    {
        var emailLog = new EmailLog
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            DonorId = donorId,
            SentByUserId = sentByUserId,
            EmailType = emailType,
            ToAddress = to,
            Subject = subject,
            Body = emailType == EmailType.PasswordReset ? "[redacted password-reset email]" : htmlBody
        };

        try
        {
            var senderName = _settings.SenderName;
            BrevoReplyTo? replyTo = null;
            var staffComposed = StaffComposedEmailTypes.Contains(emailType);

            if (sentByUserId is { } userId && staffComposed)
            {
                var staff = await _users.GetSenderIdentityAsync(userId);
                if (staff is not null)
                {
                    senderName = $"{staff.Name} via {_settings.SenderName}";
                    replyTo = new BrevoReplyTo { Name = staff.Name, Email = staff.Email };
                }
            }

            var request = new BrevoSendEmailRequest
            {
                Sender = new BrevoSender { Name = senderName, Email = _settings.SenderEmail },
                ReplyTo = replyTo,
                To = new List<BrevoRecipient> { new() { Email = to } },
                Subject = subject,
                HtmlContent = EmailLayout.Wrap(htmlBody, subject, staffComposed, _settings, replyTo?.Name, replyTo?.Email)
            };

            using var response = await _httpClient.PostAsJsonAsync("smtp/email", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BrevoSendEmailResponse>();
                emailLog.Status = EmailStatus.Sent;
                emailLog.ProviderMessageId = result?.MessageId;
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                emailLog.Status = EmailStatus.Failed;
                emailLog.ErrorMessage = Truncate($"Brevo API returned {(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}");
                _logger.LogError(
                    "Failed to send email to {To} via Brevo: {StatusCode} {ReasonPhrase}",
                    to, response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (Exception ex)
        {
            emailLog.Status = EmailStatus.Failed;
            emailLog.ErrorMessage = Truncate(ex.Message);
            _logger.LogError(ex, "Exception while sending email to {To} via Brevo.", to);
        }

        await _emailLogs.AddAsync(emailLog);
        await _unitOfWork.SaveChangesAsync();

        return new EmailSendResult(emailLog.Id, emailLog.Status);
    }

    private static string Truncate(string value) =>
        value.Length <= MaxErrorMessageLength ? value : value[..MaxErrorMessageLength];
}
