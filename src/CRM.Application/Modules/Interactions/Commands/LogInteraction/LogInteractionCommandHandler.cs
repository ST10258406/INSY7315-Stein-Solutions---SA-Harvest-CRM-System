namespace CRM.Application.Modules.Interactions.Commands.LogInteraction;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Interactions.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MediatR;

/// <summary>
/// Appends one row to a donor's interaction timeline. If a follow-up date is supplied it
/// also moves the donor's <c>follow_up_date</c> and fires a FollowUpReminder to the
/// relationship manager.
/// </summary>
/// <remarks>
/// This is the single handler the copilot rules allow to call <see cref="INotificationService"/>
/// for a follow-up reminder — see <c>.github/copilot-instructions.md</c>.
/// </remarks>
public class LogInteractionCommandHandler : IRequestHandler<LogInteractionCommand, InteractionLogDto>
{
    private readonly IDonorRepository _donors;
    private readonly IInteractionLogRepository _interactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public LogInteractionCommandHandler(
        IDonorRepository donors,
        IInteractionLogRepository interactions,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ICurrentUserService currentUserService)
    {
        _donors = donors;
        _interactions = interactions;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    public async Task<InteractionLogDto> Handle(LogInteractionCommand command, CancellationToken cancellationToken)
    {
        if (!await _donors.ExistsAsync(command.DonorId, cancellationToken))
            throw new NotFoundException(nameof(Donor), command.DonorId);

        var log = new InteractionLog
        {
            Id = Guid.NewGuid(),
            DonorId = command.DonorId,
            CreatedByUserId = _currentUserService.GetCurrentUserId(),
            InteractionType = Enum.Parse<InteractionType>(command.InteractionType, ignoreCase: true),
            Subject = command.Subject,
            Body = command.Body,
            CreatedAt = DateTime.UtcNow
        };
        await _interactions.AddAsync(log, cancellationToken);

        Donor? donor = null;
        if (command.FollowUpDate.HasValue)
        {
            donor = await _donors.GetForUpdateAsync(command.DonorId, cancellationToken)
                ?? throw new NotFoundException(nameof(Donor), command.DonorId);
            donor.FollowUpDate = command.FollowUpDate.Value;
        }

        // Interaction row + the donor's follow-up date move commit together.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = log.Id;
        command.NewValues = new
        {
            log.Id,
            log.DonorId,
            InteractionType = log.InteractionType.ToString(),
            log.Subject,
            command.FollowUpDate
        };

        if (donor is not null && donor.RelationshipManagerId is { } managerId)
        {
            await _notificationService.CreateAsync(
                managerId,
                "Follow-up reminder",
                $"Follow up with {donor.CompanyName} by {command.FollowUpDate:yyyy-MM-dd}.",
                NotificationType.FollowUpReminder,
                donor.Id,
                nameof(Donor),
                cancellationToken);
        }

        // Re-read through the same projection the GET endpoint uses — one source of truth
        // for the DTO shape (including the joined createdBy user).
        return await _interactions.GetDtoByIdAsync(log.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"InteractionLog {log.Id} could not be re-read immediately after being created.");
    }
}
