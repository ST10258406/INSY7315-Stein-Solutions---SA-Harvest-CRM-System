namespace CRM.Application.Modules.Vetting.Submissions.Commands.SubmitSubmission;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using FluentValidation.Results;
using MediatR;

/// <summary>
/// New/Returned → Ready. Blocked only by missing evidence photos; compliance problems become
/// Auto flags for the reviewer. Old Auto flags are deactivated (never deleted); Reviewer flags stay.
/// </summary>
public class SubmitSubmissionCommandHandler : IRequestHandler<SubmitSubmissionCommand, VettingSubmissionDto>
{
    private readonly IVettingOfficerRepository _officers;
    private readonly IVettingSiteVisitRepository _visits;
    private readonly IVettingSubmissionRepository _submissions;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitSubmissionCommandHandler(
        IVettingOfficerRepository officers,
        IVettingSiteVisitRepository visits,
        IVettingSubmissionRepository submissions,
        IUnitOfWork unitOfWork)
    {
        _officers = officers;
        _visits = visits;
        _submissions = submissions;
        _unitOfWork = unitOfWork;
    }

    public async Task<VettingSubmissionDto> Handle(SubmitSubmissionCommand command, CancellationToken cancellationToken)
    {
        var submission = await _submissions.GetForUpdateAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(VettingSubmission), command.Id);

        SubmissionGuards.EnsureEditable(submission);

        var missing = submission.Photos.Where(p => !p.IsCaptured).OrderBy(p => p.ShotIndex).Select(p => p.Category).ToList();
        if (missing.Count > 0)
            throw new ValidationException([new ValidationFailure(
                "Photos", $"Missing evidence photos: {string.Join(", ", missing)}.")]);

        command.OldValues = new { ApprovalState = submission.ApprovalState.ToString() };

        foreach (var flag in submission.Flags.Where(f => f.Source == VettingFlagSource.Auto))
            flag.IsActive = false;
        // No Id: a preset key on an entity reached through a tracked parent makes EF treat it as existing (UPDATE, not INSERT).
        foreach (var description in VettingFlagRules.Evaluate(submission.Details!))
            submission.Flags.Add(new VettingFlag { SubmissionId = submission.Id, Description = description, Source = VettingFlagSource.Auto });

        submission.ApprovalState = VettingApprovalState.Ready;
        submission.SubmittedAt = DateTime.UtcNow;
        submission.AdminDecision = null;
        submission.DecisionReason = null;

        if (submission.SiteVisitId is { } siteVisitId
            && await _visits.GetForUpdateAsync(siteVisitId, cancellationToken) is { } visit)
        {
            visit.VisitStatus = SiteVisitStatus.Completed;
        }

        // One transaction: save, then recount (COUNT only sees saved rows), then save the count.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _officers.RecalculateWorkloadAsync(submission.OfficerId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);

        command.EntityId = submission.Id;
        command.NewValues = new
        {
            ApprovalState = submission.ApprovalState.ToString(),
            submission.SubmittedAt,
            AutoFlags = submission.Flags.Where(f => f.IsActive && f.Source == VettingFlagSource.Auto).Select(f => f.Description).ToList()
        };

        return await _submissions.GetDtoByIdAsync(submission.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"VettingSubmission {submission.Id} could not be re-read immediately after being submitted.");
    }
}
