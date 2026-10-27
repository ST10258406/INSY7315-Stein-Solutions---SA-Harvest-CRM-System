namespace CRM.Application.Modules.Vetting.Officers.Queries.GetOfficerById;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Officers.Dtos;
using CRM.Domain.Entities.Vetting;
using MediatR;

public class GetOfficerByIdQueryHandler : IRequestHandler<GetOfficerByIdQuery, VettingOfficerDto>
{
    private readonly IVettingOfficerRepository _officers;

    public GetOfficerByIdQueryHandler(IVettingOfficerRepository officers) => _officers = officers;

    public async Task<VettingOfficerDto> Handle(GetOfficerByIdQuery request, CancellationToken cancellationToken)
        => await _officers.GetDtoByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(VettingOfficer), request.Id);
}
