namespace CRM.Application.Modules.Approvals.Commands.RejectDonor;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using FluentValidation.Results;
using MediatR;

public class RejectDonorCommandHandler : IRequestHandler<RejectDonorCommand>
{
    private readonly IApprovalRepository _approvals;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public RejectDonorCommandHandler(
        IApprovalRepository approvals,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ICurrentUserService currentUserService)
    {
        _approvals = approvals;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(RejectDonorCommand command, CancellationToken cancellationToken)
    {
        var approval = await _approvals.GetForUpdateAsync(command.ApprovalId, cancellationToken)
            ?? throw new NotFoundException(nameof(DonorApproval), command.ApprovalId);

        if (approval.Status != ApprovalStatus.Pending)
            throw new ValidationException(new[]
            {
                new ValidationFailure("status", $"Approval is already {approval.Status}.")
            });

        approval.Status = ApprovalStatus.Rejected;
        approval.RejectionReason = command.RejectionReason;
        approval.ReviewedByUserId = _currentUserService.GetCurrentUserId();
        approval.ReviewedAt = DateTime.UtcNow;
        approval.Donor.Status = DonorStatus.Rejected;

        // donor_approvals + donors mutated together in a single transaction.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = approval.Id;
        command.NewValues = new
        {
            ApprovalId = approval.Id,
            ApprovalStatus = nameof(ApprovalStatus.Rejected),
            approval.DonorId,
            DonorStatus = nameof(DonorStatus.Rejected),
            command.RejectionReason
        };

        // Notify the donor's relationship manager, reason included — NOT the submitter.
        if (approval.Donor.RelationshipManagerId is { } relationshipManagerId)
        {
            await _notificationService.CreateAsync(
                relationshipManagerId,
                "Donor rejected",
                $"{approval.Donor.CompanyName} was rejected. Reason: {command.RejectionReason}",
                NotificationType.DonorRejected,
                approval.DonorId,
                nameof(Donor),
                cancellationToken);
        }
    }
}
