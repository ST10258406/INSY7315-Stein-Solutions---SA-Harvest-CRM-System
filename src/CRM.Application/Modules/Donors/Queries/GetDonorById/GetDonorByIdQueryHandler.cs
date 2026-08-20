using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CRM.Application.Modules.Donors.Queries.GetDonorById;

public class GetDonorByIdQueryHandler : IRequestHandler<GetDonorByIdQuery, DonorDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetDonorByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<DonorDetailDto> Handle(GetDonorByIdQuery request, CancellationToken cancellationToken)
    {
        var donor = await _context.Donors
            .AsNoTracking()
            .Where(d => d.Id == request.Id)
            .ProjectTo<DonorDetailDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (donor is null)
            throw new NotFoundException(nameof(Donor), request.Id);

        return donor;
    }
}
