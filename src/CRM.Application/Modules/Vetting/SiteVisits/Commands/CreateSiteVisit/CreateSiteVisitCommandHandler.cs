namespace CRM.Application.Modules.Vetting.SiteVisits.Commands.CreateSiteVisit;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.SiteVisits.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using FluentValidation.Results;
using MediatR;

public class CreateSiteVisitCommandHandler : IRequestHandler<CreateSiteVisitCommand, VettingSiteVisitDto>
{
    private readonly IDonorRepository _donors;
    private readonly IVettingOfficerRepository _officers;
    private readonly IVettingSiteVisitRepository _visits;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSiteVisitCommandHandler(
        IDonorRepository donors, IVettingOfficerRepository officers, IVettingSiteVisitRepository visits, IUnitOfWork unitOfWork)
    {
        _donors = donors;
        _officers = officers;
        _visits = visits;
        _unitOfWork = unitOfWork;
    }

    public async Task<VettingSiteVisitDto> Handle(CreateSiteVisitCommand command, CancellationToken cancellationToken)
    {
        if (!await _donors.ExistsAsync(command.DonorId, cancellationToken))
            throw new NotFoundException(nameof(Donor), command.DonorId);

        if (!await _officers.ExistsAsync(command.OfficerId, cancellationToken))
            throw new NotFoundException(nameof(VettingOfficer), command.OfficerId);

        var locationLabel = command.LocationLabel
            ?? await _donors.GetLegalAddressSuburbAsync(command.DonorId, cancellationToken);
        if (string.IsNullOrWhiteSpace(locationLabel))
            throw new ValidationException([
                new ValidationFailure(nameof(command.LocationLabel), "locationLabel is required when the donor has no legal-address suburb.")]);

        var visit = new VettingSiteVisit
        {
            Id = Guid.NewGuid(),
            DonorId = command.DonorId,
            OfficerId = command.OfficerId,
            ScheduledDate = command.ScheduledDate!.Value,
            ScheduledTime = command.ScheduledTime!.Value,
            LocationLabel = locationLabel,
            VisitNotes = command.VisitNotes,
            VisitStatus = SiteVisitStatus.ToDo
        };
        await _visits.AddAsync(visit, cancellationToken);

        // Save first: the workload COUNT only sees saved rows.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _officers.RecalculateWorkloadAsync(visit.OfficerId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);

        command.EntityId = visit.Id;
        command.NewValues = new { visit.Id, visit.DonorId, visit.OfficerId, visit.ScheduledDate, visit.ScheduledTime, visit.LocationLabel };

        return await _visits.GetDtoByIdAsync(visit.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"VettingSiteVisit {visit.Id} could not be re-read immediately after being created.");
    }
}
