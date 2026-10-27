namespace CRM.Application.Modules.Vetting.Submissions.Commands.CreateSubmission;

using AutoMapper;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using FluentValidation.Results;
using MediatR;

/// <summary>
/// Creates the submission (New), its details row and the 4 empty evidence-photo slots,
/// starts the linked site visit if any, and recounts the officer's workload.
/// </summary>
public class CreateSubmissionCommandHandler : IRequestHandler<CreateSubmissionCommand, CreatedSubmissionDto>
{
    private readonly IDonorRepository _donors;
    private readonly IVettingOfficerRepository _officers;
    private readonly IVettingSiteVisitRepository _visits;
    private readonly IVettingSubmissionRepository _submissions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateSubmissionCommandHandler(
        IDonorRepository donors,
        IVettingOfficerRepository officers,
        IVettingSiteVisitRepository visits,
        IVettingSubmissionRepository submissions,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _donors = donors;
        _officers = officers;
        _visits = visits;
        _submissions = submissions;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CreatedSubmissionDto> Handle(CreateSubmissionCommand command, CancellationToken cancellationToken)
    {
        if (!await _donors.ExistsAsync(command.DonorId, cancellationToken))
            throw new NotFoundException(nameof(Donor), command.DonorId);

        if (!await _officers.ExistsAsync(command.OfficerId, cancellationToken))
            throw new NotFoundException(nameof(VettingOfficer), command.OfficerId);

        if (command.SiteVisitId is { } siteVisitId)
        {
            var visit = await _visits.GetForUpdateAsync(siteVisitId, cancellationToken)
                ?? throw new NotFoundException(nameof(VettingSiteVisit), siteVisitId);
            if (visit.DonorId != command.DonorId || visit.OfficerId != command.OfficerId)
                throw new ValidationException([new ValidationFailure(
                    nameof(command.SiteVisitId), "The site visit must be for the same donor and officer.")]);
            if (visit.VisitStatus == SiteVisitStatus.Completed || await _submissions.IsSiteVisitLinkedAsync(siteVisitId, cancellationToken))
                throw new ConflictException("The site visit is already completed or linked to another submission.");
            visit.VisitStatus = SiteVisitStatus.InProgress;
        }

        var submission = new VettingSubmission
        {
            Id = Guid.NewGuid(),
            FormReference = await _submissions.GetNextFormReferenceAsync(cancellationToken),
            DonorId = command.DonorId,
            OfficerId = command.OfficerId,
            SiteVisitId = command.SiteVisitId,
            ApprovalState = VettingApprovalState.New,
            Details = _mapper.Map<VettingAssessmentDetails>(command.Details),
            Photos = Enum.GetValues<EvidencePhotoCategory>()
                .Select(category => new VettingEvidencePhoto { Id = Guid.NewGuid(), ShotIndex = (int)category, Category = category })
                .ToList()
        };
        submission.Details.Id = Guid.NewGuid();
        await _submissions.AddAsync(submission, cancellationToken);

        // Save first: the workload COUNT only sees saved rows.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _officers.RecalculateWorkloadAsync(submission.OfficerId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);

        command.EntityId = submission.Id;
        command.NewValues = new { submission.Id, submission.FormReference, submission.DonorId, submission.OfficerId, submission.SiteVisitId };

        return new CreatedSubmissionDto(submission.Id, submission.FormReference);
    }
}
