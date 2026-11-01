namespace CRM.Application.Modules.Vetting.Officers.Commands.CreateOfficer;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Officers.Dtos;
using CRM.Domain.Entities.Vetting;
using MediatR;

public class CreateOfficerCommandHandler : IRequestHandler<CreateOfficerCommand, VettingOfficerDto>
{
    private readonly IVettingOfficerRepository _officers;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOfficerCommandHandler(IVettingOfficerRepository officers, IUnitOfWork unitOfWork)
    {
        _officers = officers;
        _unitOfWork = unitOfWork;
    }

    public async Task<VettingOfficerDto> Handle(CreateOfficerCommand command, CancellationToken cancellationToken)
    {
        var officer = new VettingOfficer
        {
            Id = Guid.NewGuid(),
            FullName = command.FullName,
            Initials = command.Initials,
            AssignedRegion = command.AssignedRegion
        };
        await _officers.AddAsync(officer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = officer.Id;
        command.NewValues = new { officer.Id, officer.FullName, officer.Initials, officer.AssignedRegion };

        return await _officers.GetDtoByIdAsync(officer.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"VettingOfficer {officer.Id} could not be re-read immediately after being created.");
    }
}
