using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using MediatR;

namespace CRM.Application.Modules.Donors.Queries.GetDonorById;

public class GetDonorByIdQueryHandler : IRequestHandler<GetDonorByIdQuery, DonorDetailDto>
{
    private readonly IDonorRepository _donors;

    public GetDonorByIdQueryHandler(IDonorRepository donors) => _donors = donors;

    public async Task<DonorDetailDto> Handle(GetDonorByIdQuery request, CancellationToken cancellationToken)
    {
        var donor = await _donors.GetDetailByIdAsync(request.Id, cancellationToken);

        if (donor is null)
            throw new NotFoundException(nameof(Donor), request.Id);

        return donor;
    }
}
