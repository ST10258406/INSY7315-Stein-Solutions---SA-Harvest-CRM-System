namespace CRM.Application.Modules.Donors.Commands.SendPublicFormInvite;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Utilities;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Enums;
using MediatR;

/// <summary>
/// Invites a prospective, not-yet-a-donor contact to complete the public
/// onboarding form. There is no Donor record to attach this to (that's the
/// point — it's how one gets created), so unlike SendDonorEmailCommandHandler
/// this never touches InteractionLog; EmailLog (EmailType.PublicFormInvite,
/// DonorId null) is the only trace. Same interactive-caller discipline as
/// SendDonorEmailCommandHandler: a failed send must be surfaced to the caller,
/// not swallowed.
/// </summary>
public class SendPublicFormInviteCommandHandler : IRequestHandler<SendPublicFormInviteCommand, SendPublicFormInviteResponseDto>
{
    private const string SuccessMessage = "Invitation sent.";

    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUserService;

    public SendPublicFormInviteCommandHandler(IEmailService emailService, ICurrentUserService currentUserService)
    {
        _emailService = emailService;
        _currentUserService = currentUserService;
    }

    public async Task<SendPublicFormInviteResponseDto> Handle(SendPublicFormInviteCommand command, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();

        var result = await _emailService.SendAsync(
            to: command.To,
            subject: command.Subject,
            htmlBody: BuildHtmlBody(command.Body, command.PublicFormUrl),
            emailType: EmailType.PublicFormInvite,
            donorId: null,
            sentByUserId: currentUserId);

        if (result.Status != EmailStatus.Sent)
        {
            throw new EmailDeliveryException(
                "The invitation could not be sent. Please try again, or share the link with them another way.");
        }

        command.EntityId = result.EmailLogId;
        command.NewValues = new { EmailLogId = result.EmailLogId, command.To };

        return new SendPublicFormInviteResponseDto { Message = SuccessMessage };
    }

    /// <summary>
    /// Plain-text-only compose body, built exactly like SendDonorEmailCommandHandler's
    /// (see <see cref="EmailBodyHtml.Paragraphs"/>). The link itself is never part of
    /// the user-editable text: it's appended here as a fixed, server-built button, so
    /// it is always correct and always clickable regardless of what the sender typed.
    /// </summary>
    private static string BuildHtmlBody(string plainTextBody, string publicFormUrl) =>
        EmailBodyHtml.Paragraphs(plainTextBody) + EmailBodyHtml.Button(publicFormUrl, "Complete the donor registration form");
}
