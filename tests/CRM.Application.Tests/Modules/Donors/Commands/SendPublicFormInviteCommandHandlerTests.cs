using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Commands.SendPublicFormInvite;
using CRM.Domain.Enums;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class SendPublicFormInviteCommandHandlerTests
{
    private readonly IEmailService _emailServiceMock = Substitute.For<IEmailService>();
    private readonly ICurrentUserService _currentUserMock = Substitute.For<ICurrentUserService>();
    private readonly SendPublicFormInviteCommandHandler _handler;

    private readonly Guid _currentUserId = Guid.NewGuid();

    public SendPublicFormInviteCommandHandlerTests()
    {
        _currentUserMock.GetCurrentUserId().Returns(_currentUserId);
        _handler = new SendPublicFormInviteCommandHandler(_emailServiceMock, _currentUserMock);
    }

    private static SendPublicFormInviteCommand MakeCommand() => new()
    {
        To = "prospect@example.com",
        Subject = "Join SA Harvest as a donor",
        Body = "Hi, we'd love to have you on board.",
        PublicFormUrl = "https://app.saharvestcrm.org/donate"
    };

    [Fact]
    public async Task Handle_SuccessfulSend_ReturnsSuccessMessage()
    {
        _emailServiceMock
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailType>(), Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .Returns(new EmailSendResult(Guid.NewGuid(), EmailStatus.Sent));

        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        Assert.Equal("Invitation sent.", result.Message);
    }

    [Fact]
    public async Task Handle_SuccessfulSend_CallsEmailServiceWithPublicFormInviteTypeNoDonorAndCurrentUser()
    {
        _emailServiceMock
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailType>(), Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .Returns(new EmailSendResult(Guid.NewGuid(), EmailStatus.Sent));

        await _handler.Handle(MakeCommand(), CancellationToken.None);

        await _emailServiceMock.Received(1).SendAsync(
            "prospect@example.com",
            "Join SA Harvest as a donor",
            Arg.Any<string>(),
            EmailType.PublicFormInvite,
            null,
            _currentUserId);
    }

    [Fact]
    public async Task Handle_SuccessfulSend_AppendsAClickableLinkBuiltFromPublicFormUrl_AndEncodesTheUserBody()
    {
        string? capturedHtmlBody = null;
        _emailServiceMock
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Do<string>(b => capturedHtmlBody = b), Arg.Any<EmailType>(), Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .Returns(new EmailSendResult(Guid.NewGuid(), EmailStatus.Sent));

        var command = MakeCommand();
        command.Body = "<script>alert('x')</script>";

        await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(capturedHtmlBody);
        Assert.DoesNotContain("<script>", capturedHtmlBody);
        Assert.Contains("&lt;script&gt;", capturedHtmlBody);
        Assert.Contains($"<a href=\"{command.PublicFormUrl}\">", capturedHtmlBody);
    }

    [Fact]
    public async Task Handle_FailedSend_ThrowsEmailDeliveryException()
    {
        _emailServiceMock
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailType>(), Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .Returns(new EmailSendResult(Guid.NewGuid(), EmailStatus.Failed));

        await Assert.ThrowsAsync<EmailDeliveryException>(
            () => _handler.Handle(MakeCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SuccessfulSend_SetsAuditEntityIdToTheEmailLogId()
    {
        var emailLogId = Guid.NewGuid();
        _emailServiceMock
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailType>(), Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .Returns(new EmailSendResult(emailLogId, EmailStatus.Sent));
        var command = MakeCommand();

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(emailLogId, command.EntityId);
        Assert.NotNull(command.NewValues);
    }
}
