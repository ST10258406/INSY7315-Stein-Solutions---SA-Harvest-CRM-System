namespace CRM.Application.Modules.Lookups.Queries.GetLookup;

using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using MediatR;

public class GetLookupQueryHandler : IRequestHandler<GetLookupQuery, List<LookupDto>>
{
    private readonly ILookupRepository _lookups;

    public GetLookupQueryHandler(ILookupRepository lookups) => _lookups = lookups;

    public Task<List<LookupDto>> Handle(GetLookupQuery request, CancellationToken cancellationToken)
        => _lookups.GetActiveAsync(request.Type, cancellationToken);
}
