namespace CRM.Application.Modules.Vetting.Review.Commands.DecideSubmission;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using MediatR;

/// <summary>
/// Ready → Approved / Returned / Declined. The Ready check is the conditional update itself, inside
/// the transaction, so a double-click or two admins deciding at once gives exactly one winner and one 409.
/// The donor's own status is deliberately untouched.
/// </summary>
public class DecideSubmissionCommandHandler : IRequestHandler<DecideSubmissionCommand, VettingSubmissionDto>
{
    private readonly IVettingSubmissionRepository _submissions;
    private readonly IVettingOfficerRepository _officers;
    private readonly IUnitOfWork _unitOfWork;

    public DecideSubmissionCommandHandler(
        IVettingSubmissionRepository submissions, IVettingOfficerRepository officers, IUnitOfWork unitOfWork)
    {
        _submissions = submissions;
        _officers = officers;
        _unitOfWork = unitOfWork;
    }

    public async Task<VettingSubmissionDto> Handle(DecideSubmissionCommand command, CancellationToken cancellationToken)
    {
        var officerId = await _submissions.GetOfficerIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(VettingSubmission), command.Id);

        var decision = Enum.Parse<VettingDecision>(command.Decision, ignoreCase: true);
        var newState = decision switch
        {
            VettingDecision.Approve => VettingApprovalState.Approved,
            VettingDecision.Return => VettingApprovalState.Returned,
            _ => VettingApprovalState.Declined
        };
        var reason = string.IsNullOrWhiteSpace(command.Reason) ? null : command.Reason.Trim();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (!await _submissions.TryRecordDecisionAsync(command.Id, newState, decision, reason, cancellationToken))
                throw new ConflictException("Only a submission that is Ready for review can be decided.");

            await _officers.RecalculateWorkloadAsync(officerId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);

        command.EntityId = command.Id;
        command.OldValues = new { ApprovalState = nameof(VettingApprovalState.Ready) };
        command.NewValues = new { ApprovalState = newState.ToString(), Decision = decision.ToString(), Reason = reason };

        return await _submissions.GetDtoByIdAsync(command.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"VettingSubmission {command.Id} could not be re-read immediately after being decided.");
    }
}
