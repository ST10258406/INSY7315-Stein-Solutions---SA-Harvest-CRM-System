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
/// AddInfrastructureServices). Every attempt is logged to EmailLog, success
/// or failure, before this method returns. Never throws: email is
/// best-effort, in-app notifications are the primary channel.
/// </summary>
public class EmailService : IEmailService
{
    private const int MaxErrorMessageLength = 1000;

    private readonly HttpClient _httpClient;
    private readonly BrevoSettings _settings;
    private readonly IEmailLogRepository _emailLogs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        HttpClient httpClient,
        IOptions<BrevoSettings> settings,
        IEmailLogRepository emailLogs,
        IUnitOfWork unitOfWork,
        ILogger<EmailService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _emailLogs = emailLogs;
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
            var request = new BrevoSendEmailRequest
            {
                Sender = new BrevoSender { Name = _settings.SenderName, Email = _settings.SenderEmail },
                To = new List<BrevoRecipient> { new() { Email = to } },
                Subject = subject,
                HtmlContent = htmlBody
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
