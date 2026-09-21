using System.Net;
using System.Text;
using System.Text.Json;
using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Services;
using CRM.Infrastructure.Services.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CRM.Infrastructure.Tests.Services;

public class EmailServiceTests
{
    private readonly IEmailLogRepository _emailLogs = Substitute.For<IEmailLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<EmailService> _logger = Substitute.For<ILogger<EmailService>>();

    private readonly BrevoSettings _settings = new()
    {
        ApiKey = "test-api-key",
        SenderEmail = "noreply@saharvestcrm.org",
        SenderName = "SA Harvest CRM"
    };

    private EmailService CreateSut(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.brevo.com/v3/") };
        return new EmailService(httpClient, Options.Create(_settings), _emailLogs, _unitOfWork, _logger);
    }

    [Fact]
    public async Task SendAsync_OnSuccess_LogsSentWithProviderMessageId()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { messageId = "brevo-message-id-123" }),
                Encoding.UTF8,
                "application/json")
        });
        var sut = CreateSut(handler);
        var donorId = Guid.NewGuid();

        await sut.SendAsync(
            to: "donor@example.com",
            subject: "Welcome",
            htmlBody: "<p>Hi</p>",
            emailType: EmailType.OnboardingConfirmation,
            donorId: donorId);

        await _emailLogs.Received(1).AddAsync(
            Arg.Is<EmailLog>(log =>
                log.Status == EmailStatus.Sent &&
                log.ProviderMessageId == "brevo-message-id-123" &&
                log.ErrorMessage == null &&
                log.ToAddress == "donor@example.com" &&
                log.Subject == "Welcome" &&
                log.Body == "<p>Hi</p>" &&
                log.EmailType == EmailType.OnboardingConfirmation &&
                log.DonorId == donorId),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_OnFailureResponse_LogsFailedWithErrorMessageAndDoesNotThrow()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { message = "Key not found" }),
                Encoding.UTF8,
                "application/json")
        });
        var sut = CreateSut(handler);

        var exception = await Record.ExceptionAsync(() =>
            sut.SendAsync("user@example.com", "Reset your password", "<p>Reset</p>", EmailType.PasswordReset));

        Assert.Null(exception);
        await _emailLogs.Received(1).AddAsync(
            Arg.Is<EmailLog>(log =>
                log.Status == EmailStatus.Failed &&
                log.ProviderMessageId == null &&
                log.ErrorMessage != null &&
                log.ErrorMessage.Contains("401")),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_OnNetworkException_LogsFailedAndDoesNotThrow()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("Connection refused"));
        var sut = CreateSut(handler);

        var exception = await Record.ExceptionAsync(() =>
            sut.SendAsync("user@example.com", "Reset your password", "<p>Reset</p>", EmailType.PasswordReset));

        Assert.Null(exception);
        await _emailLogs.Received(1).AddAsync(
            Arg.Is<EmailLog>(log =>
                log.Status == EmailStatus.Failed &&
                log.ErrorMessage != null &&
                log.ErrorMessage.Contains("Connection refused")),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
