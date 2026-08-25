namespace CRM.Application.Modules.Lookups.Queries.GetCodedLookup;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetCodedLookupQueryHandler : IRequestHandler<GetCodedLookupQuery, List<CodedLookupDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCodedLookupQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<List<CodedLookupDto>> Handle(GetCodedLookupQuery request, CancellationToken cancellationToken)
    {
        return request.Type switch
        {
            CodedLookupType.OperationalRegions => _context.LookupOperationalRegions.AsNoTracking()
                .ProjectTo<CodedLookupDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),

            CodedLookupType.Provinces => _context.LookupProvinces.AsNoTracking()
                .ProjectTo<CodedLookupDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),

            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Type, "Unsupported lookup type.")
        };
    }
}
