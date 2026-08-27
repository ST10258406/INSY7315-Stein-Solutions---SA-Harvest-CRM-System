namespace CRM.Application.Modules.Lookups.Queries.GetCodedLookup;

using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using MediatR;

public class GetCodedLookupQueryHandler : IRequestHandler<GetCodedLookupQuery, List<CodedLookupDto>>
{
    private readonly ILookupRepository _lookups;

    public GetCodedLookupQueryHandler(ILookupRepository lookups) => _lookups = lookups;

    public Task<List<CodedLookupDto>> Handle(GetCodedLookupQuery request, CancellationToken cancellationToken)
        => _lookups.GetActiveCodedAsync(request.Type, cancellationToken);
}
