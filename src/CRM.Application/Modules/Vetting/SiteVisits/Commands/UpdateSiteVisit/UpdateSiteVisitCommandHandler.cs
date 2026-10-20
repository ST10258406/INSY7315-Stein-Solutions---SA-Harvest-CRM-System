namespace CRM.Application.Modules.Vetting.SiteVisits.Commands.UpdateSiteVisit;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.SiteVisits.Dtos;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using MediatR;

public class UpdateSiteVisitCommandHandler : IRequestHandler<UpdateSiteVisitCommand, VettingSiteVisitDto>
{
    private readonly IVettingOfficerRepository _officers;
    private readonly IVettingSiteVisitRepository _visits;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSiteVisitCommandHandler(
        IVettingOfficerRepository officers, IVettingSiteVisitRepository visits, IUnitOfWork unitOfWork)
    {
        _officers = officers;
        _visits = visits;
        _unitOfWork = unitOfWork;
    }

    public async Task<VettingSiteVisitDto> Handle(UpdateSiteVisitCommand command, CancellationToken cancellationToken)
    {
        var visit = await _visits.GetForUpdateAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(VettingSiteVisit), command.Id);

        command.OldValues = Snapshot(visit);
        var previousOfficerId = visit.OfficerId;

        if (command.OfficerId is { } newOfficer
            && newOfficer != visit.OfficerId
            && !await _officers.ExistsAsync(newOfficer, cancellationToken))
        {
            throw new NotFoundException(nameof(VettingOfficer), newOfficer);
        }

        if (command.ScheduledDate is { } date) visit.ScheduledDate = date;
        if (command.ScheduledTime is { } time) visit.ScheduledTime = time;
        if (command.LocationLabel is not null) visit.LocationLabel = command.LocationLabel;
        if (command.VisitNotes is not null) visit.VisitNotes = command.VisitNotes;
        if (command.VisitStatus is not null) visit.VisitStatus = Enum.Parse<SiteVisitStatus>(command.VisitStatus, ignoreCase: true);
        if (command.OfficerId is { } officerId) visit.OfficerId = officerId;

        // Save first: the workload COUNT only sees saved rows. A reassignment recounts both officers.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _officers.RecalculateWorkloadAsync(previousOfficerId, cancellationToken);
            if (visit.OfficerId != previousOfficerId)
                await _officers.RecalculateWorkloadAsync(visit.OfficerId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);

        command.EntityId = visit.Id;
        command.NewValues = Snapshot(visit);

        return await _visits.GetDtoByIdAsync(visit.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"VettingSiteVisit {visit.Id} could not be re-read immediately after being updated.");
    }

    private static object Snapshot(VettingSiteVisit v) => new
    {
        v.ScheduledDate, v.ScheduledTime, v.LocationLabel, v.VisitNotes, VisitStatus = v.VisitStatus.ToString(), v.OfficerId
    };
}
