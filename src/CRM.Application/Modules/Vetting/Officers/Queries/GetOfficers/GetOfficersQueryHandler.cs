namespace CRM.Application.Modules.Vetting.Officers.Queries.GetOfficers;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Officers.Dtos;
using MediatR;

public class GetOfficersQueryHandler : IRequestHandler<GetOfficersQuery, List<VettingOfficerDto>>
{
    private readonly IVettingOfficerRepository _officers;

    public GetOfficersQueryHandler(IVettingOfficerRepository officers) => _officers = officers;

    public Task<List<VettingOfficerDto>> Handle(GetOfficersQuery request, CancellationToken cancellationToken)
        => _officers.GetAllAsync(cancellationToken);
}
