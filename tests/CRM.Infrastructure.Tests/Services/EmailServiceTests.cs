using System.Net;
using System.Text;
using System.Text.Json;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
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
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
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
        return new EmailService(httpClient, Options.Create(_settings), _emailLogs, _users, _unitOfWork, _logger);
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

    private static (FakeHttpMessageHandler Handler, List<JsonElement> Sent) CapturingHandler()
    {
        var sent = new List<JsonElement>();
        var handler = new FakeHttpMessageHandler(request =>
        {
            sent.Add(JsonDocument.Parse(request.Content!.ReadAsStringAsync().Result).RootElement.Clone());
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{\"messageId\":\"m-1\"}", Encoding.UTF8, "application/json")
            };
        });
        return (handler, sent);
    }

    [Theory]
    [InlineData(EmailType.DonorCorrespondence)]
    [InlineData(EmailType.PublicFormInvite)]
    public async Task SendAsync_StaffComposedEmail_IsPresentedAsTheUserWithReplyTo(EmailType emailType)
    {
        var userId = Guid.NewGuid();
        _users.GetSenderIdentityAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new EmailSenderIdentity("Jane Smith", "jane@example.org"));
        var (handler, sent) = CapturingHandler();

        var result = await CreateSut(handler).SendAsync(
            "donor@example.com", "Hi", "<p>Hi</p>", emailType, sentByUserId: userId);

        Assert.Equal(EmailStatus.Sent, result.Status);
        var body = Assert.Single(sent);
        Assert.Equal("Jane Smith via SA Harvest CRM", body.GetProperty("sender").GetProperty("name").GetString());
        // The verified system address stays the From — only the display name and Reply-To change.
        Assert.Equal("noreply@saharvestcrm.org", body.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("jane@example.org", body.GetProperty("replyTo").GetProperty("email").GetString());
        Assert.Equal("Jane Smith", body.GetProperty("replyTo").GetProperty("name").GetString());
    }

    [Fact]
    public async Task SendAsync_StaffComposedEmail_WhenUserNotFound_FallsBackToPlainSystemSender()
    {
        var userId = Guid.NewGuid();
        _users.GetSenderIdentityAsync(userId, Arg.Any<CancellationToken>()).Returns((EmailSenderIdentity?)null);
        var (handler, sent) = CapturingHandler();

        await CreateSut(handler).SendAsync(
            "donor@example.com", "Hi", "<p>Hi</p>", EmailType.DonorCorrespondence, sentByUserId: userId);

        var body = Assert.Single(sent);
        Assert.Equal("SA Harvest CRM", body.GetProperty("sender").GetProperty("name").GetString());
        Assert.False(body.TryGetProperty("replyTo", out _));
    }

    [Theory]
    [InlineData(EmailType.PasswordReset)]
    [InlineData(EmailType.OnboardingConfirmation)]
    [InlineData(EmailType.AccountEmailChanged)]
    public async Task SendAsync_SystemEmailTypes_NeverCarryAUsersIdentity(EmailType emailType)
    {
        var userId = Guid.NewGuid();
        _users.GetSenderIdentityAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new EmailSenderIdentity("Jane Smith", "jane@example.org"));
        var (handler, sent) = CapturingHandler();

        await CreateSut(handler).SendAsync(
            "someone@example.com", "Notice", "<p>Hi</p>", emailType, sentByUserId: userId);

        var body = Assert.Single(sent);
        Assert.Equal("SA Harvest CRM", body.GetProperty("sender").GetProperty("name").GetString());
        Assert.False(body.TryGetProperty("replyTo", out _));
        await _users.DidNotReceiveWithAnyArgs().GetSenderIdentityAsync(default);
    }

    private class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
