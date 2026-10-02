namespace CRM.Application.Modules.Donors.Commands.SendDonorEmail;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Utilities;
using CRM.Application.Modules.Interactions.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MediatR;

/// <summary>
/// Compose-and-send: sends a donor email via Brevo and, only on success, appends
/// an Email interaction to the donor's timeline. Unlike the fire-and-forget
/// IEmailService callers elsewhere (password reset, onboarding confirmation), this
/// handler is a direct, interactive staff action — a failed send must be surfaced
/// to the caller (see EmailDeliveryException), not swallowed.
/// </summary>
public class SendDonorEmailCommandHandler : IRequestHandler<SendDonorEmailCommand, InteractionLogDto>
{
    private readonly IDonorRepository _donors;
    private readonly IInteractionLogRepository _interactions;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SendDonorEmailCommandHandler(
        IDonorRepository donors,
        IInteractionLogRepository interactions,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _donors = donors;
        _interactions = interactions;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<InteractionLogDto> Handle(SendDonorEmailCommand command, CancellationToken cancellationToken)
    {
        if (!await _donors.ExistsAsync(command.DonorId, cancellationToken))
            throw new NotFoundException(nameof(Donor), command.DonorId);

        var currentUserId = _currentUserService.GetCurrentUserId();

        var result = await _emailService.SendAsync(
            to: command.To,
            subject: command.Subject,
            htmlBody: BuildHtmlBody(command.Body),
            emailType: EmailType.DonorCorrespondence,
            donorId: command.DonorId,
            sentByUserId: currentUserId);

        if (result.Status != EmailStatus.Sent)
        {
            // Per the explicit product decision: a failed send must not appear in the
            // donor's activity timeline or count toward "donors contacted" reporting.
            // EmailService has already logged the failure to EmailLog; nothing further
            // to persist here — just surface it to the caller.
            throw new EmailDeliveryException(
                "The email could not be sent. Please try again, or contact the donor another way.");
        }

        var log = new InteractionLog
        {
            Id = Guid.NewGuid(),
            DonorId = command.DonorId,
            CreatedByUserId = currentUserId,
            InteractionType = InteractionType.Email,
            Subject = command.Subject,
            Body = command.Body,
            RelatedEntityId = result.EmailLogId,
            RelatedEntityType = "EmailLog",
            CreatedAt = DateTime.UtcNow
        };
        await _interactions.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = log.Id;
        command.NewValues = new
        {
            log.Id,
            log.DonorId,
            InteractionType = log.InteractionType.ToString(),
            log.Subject,
            EmailLogId = result.EmailLogId
        };

        return await _interactions.GetDtoByIdAsync(log.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"InteractionLog {log.Id} could not be re-read immediately after being created.");
    }

    /// <summary>
    /// Plain-text-only compose body (no rich text in Sprint 6) — HTML-encode every
    /// line and wrap each in its own paragraph, so a submitted "&lt;script&gt;..."
    /// (or any other tag) reaches Brevo as inert text, never as live markup.
    /// </summary>
    private static string BuildHtmlBody(string plainTextBody) => EmailBodyHtml.Paragraphs(plainTextBody);
}
