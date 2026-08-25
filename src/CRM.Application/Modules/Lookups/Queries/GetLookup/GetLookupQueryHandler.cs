namespace CRM.Application.Modules.Lookups.Queries.GetLookup;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetLookupQueryHandler : IRequestHandler<GetLookupQuery, List<LookupDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetLookupQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<List<LookupDto>> Handle(GetLookupQuery request, CancellationToken cancellationToken)
    {
        return request.Type switch
        {
            LookupType.CompanyTypes => _context.LookupCompanyTypes.AsNoTracking()
                .ProjectTo<LookupDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),

            LookupType.EntityTypes => _context.LookupEntityTypes.AsNoTracking()
                .ProjectTo<LookupDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),

            LookupType.DonationTypes => _context.LookupDonationTypes.AsNoTracking()
                .ProjectTo<LookupDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),

            LookupType.DonationFrequencies => _context.LookupDonationFrequencies.AsNoTracking()
                .ProjectTo<LookupDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),

            LookupType.BbbeeStatuses => _context.LookupBbbeeStatuses.AsNoTracking()
                .ProjectTo<LookupDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),

            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Type, "Unsupported lookup type.")
        };
    }
}
