using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Commands.SendDonorEmail;
using CRM.Application.Modules.Interactions.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class SendDonorEmailCommandHandlerTests
{
    private readonly IDonorRepository _donorsMock = Substitute.For<IDonorRepository>();
    private readonly IInteractionLogRepository _interactionsMock = Substitute.For<IInteractionLogRepository>();
    private readonly IEmailService _emailServiceMock = Substitute.For<IEmailService>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUserMock = Substitute.For<ICurrentUserService>();
    private readonly SendDonorEmailCommandHandler _handler;

    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly List<InteractionLog> _added = [];

    public SendDonorEmailCommandHandlerTests()
    {
        _currentUserMock.GetCurrentUserId().Returns(_currentUserId);

        _interactionsMock
            .AddAsync(Arg.Do<InteractionLog>(_added.Add), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _interactionsMock
            .GetDtoByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new InteractionLogDto
            {
                Id = ci.ArgAt<Guid>(0),
                CreatedBy = new InteractionUserDto { Id = _currentUserId, FullName = "Procurement Lead" }
            });

        _handler = new SendDonorEmailCommandHandler(
            _donorsMock, _interactionsMock, _emailServiceMock, _unitOfWorkMock, _currentUserMock);
    }

    private Guid SetupExistingDonor()
    {
        var donorId = Guid.NewGuid();
        _donorsMock.ExistsAsync(donorId, Arg.Any<CancellationToken>()).Returns(true);
        return donorId;
    }

    private static SendDonorEmailCommand MakeCommand(Guid donorId) => new()
    {
        DonorId = donorId,
        To = "donor@example.com",
        Subject = "Thanks for your continued support",
        Body = "Just checking in ahead of next month's collection."
    };

    [Fact]
    public async Task Handle_SuccessfulSend_AppendsEmailInteractionLinkedToTheEmailLog()
    {
        var donorId = SetupExistingDonor();
        var emailLogId = Guid.NewGuid();
        _emailServiceMock
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailType>(), Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .Returns(new EmailSendResult(emailLogId, EmailStatus.Sent));

        var result = await _handler.Handle(MakeCommand(donorId), CancellationToken.None);

        var log = Assert.Single(_added);
        Assert.Equal(donorId, log.DonorId);
        Assert.Equal(_currentUserId, log.CreatedByUserId);
        Assert.Equal(InteractionType.Email, log.InteractionType);
        Assert.Equal("Thanks for your continued support", log.Subject);
        Assert.Equal(emailLogId, log.RelatedEntityId);
        Assert.Equal("EmailLog", log.RelatedEntityType);

        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal(log.Id, result.Id);
    }

    [Fact]
    public async Task Handle_SuccessfulSend_CallsEmailServiceWithDonorCorrespondenceTypeAndCurrentUser()
    {
        var donorId = SetupExistingDonor();
        _emailServiceMock
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailType>(), Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .Returns(new EmailSendResult(Guid.NewGuid(), EmailStatus.Sent));

        await _handler.Handle(MakeCommand(donorId), CancellationToken.None);

        await _emailServiceMock.Received(1).SendAsync(
            "donor@example.com",
            "Thanks for your continued support",
            Arg.Any<string>(),
            EmailType.DonorCorrespondence,
            donorId,
            _currentUserId);
    }

    [Fact]
    public async Task Handle_SuccessfulSend_HtmlEncodesScriptTagsInTheOutboundBody()
    {
        // The InteractionLog keeps the raw plain text (for the CRM timeline), but
        // whatever reaches IEmailService.SendAsync as htmlBody must be inert —
        // this is what actually gets sent to Brevo.
        var donorId = SetupExistingDonor();
        string? capturedHtmlBody = null;
        _emailServiceMock
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Do<string>(b => capturedHtmlBody = b), Arg.Any<EmailType>(), Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .Returns(new EmailSendResult(Guid.NewGuid(), EmailStatus.Sent));

        var command = MakeCommand(donorId);
        command.Body = "<script>alert('x')</script>";

        await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(capturedHtmlBody);
        Assert.DoesNotContain("<script>", capturedHtmlBody);
        Assert.Contains("&lt;script&gt;", capturedHtmlBody);

        // The InteractionLog body is untouched raw text — encoding is an outbound-
        // email-only concern, not something that should mangle the CRM's own record.
        Assert.Equal("<script>alert('x')</script>", _added[0].Body);
    }

    [Fact]
    public async Task Handle_FailedSend_ThrowsEmailDeliveryExceptionAndAppendsNoInteractionLog()
    {
        var donorId = SetupExistingDonor();
        _emailServiceMock
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailType>(), Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .Returns(new EmailSendResult(Guid.NewGuid(), EmailStatus.Failed));

        await Assert.ThrowsAsync<EmailDeliveryException>(
            () => _handler.Handle(MakeCommand(donorId), CancellationToken.None));

        Assert.Empty(_added);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownDonor_ThrowsNotFoundAndNeverCallsEmailService()
    {
        _donorsMock.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(MakeCommand(Guid.NewGuid()), CancellationToken.None));

        await _emailServiceMock.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailType>(), Arg.Any<Guid?>(), Arg.Any<Guid?>());
        Assert.Empty(_added);
    }

    [Fact]
    public async Task Handle_SuccessfulSend_SetsAuditEntityIdToTheNewInteractionLogId()
    {
        var donorId = SetupExistingDonor();
        _emailServiceMock
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailType>(), Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .Returns(new EmailSendResult(Guid.NewGuid(), EmailStatus.Sent));
        var command = MakeCommand(donorId);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(_added[0].Id, command.EntityId);
        Assert.NotNull(command.NewValues);
    }
}
