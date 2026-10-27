namespace CRM.Application.Modules.Vetting.Officers.Commands.UpdateOfficer;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Officers.Dtos;
using CRM.Domain.Entities.Vetting;
using MediatR;

public class UpdateOfficerCommandHandler : IRequestHandler<UpdateOfficerCommand, VettingOfficerDto>
{
    private readonly IVettingOfficerRepository _officers;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOfficerCommandHandler(IVettingOfficerRepository officers, IUnitOfWork unitOfWork)
    {
        _officers = officers;
        _unitOfWork = unitOfWork;
    }

    public async Task<VettingOfficerDto> Handle(UpdateOfficerCommand command, CancellationToken cancellationToken)
    {
        var officer = await _officers.GetForUpdateAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(VettingOfficer), command.Id);

        command.OldValues = new { officer.FullName, officer.Initials, officer.AssignedRegion };

        if (command.FullName is not null) officer.FullName = command.FullName;
        if (command.Initials is not null) officer.Initials = command.Initials;
        if (command.AssignedRegion is not null) officer.AssignedRegion = command.AssignedRegion;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = officer.Id;
        command.NewValues = new { officer.FullName, officer.Initials, officer.AssignedRegion };

        return await _officers.GetDtoByIdAsync(officer.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"VettingOfficer {officer.Id} could not be re-read immediately after being updated.");
    }
}
