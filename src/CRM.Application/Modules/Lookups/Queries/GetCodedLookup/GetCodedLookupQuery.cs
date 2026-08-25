namespace CRM.Application.Modules.Lookups.Queries.GetCodedLookup;

using CRM.Application.Common.Models;
using MediatR;

public enum CodedLookupType
{
    OperationalRegions,
    Provinces
}

public class GetCodedLookupQuery : IRequest<List<CodedLookupDto>>
{
    public CodedLookupType Type { get; set; }

    public GetCodedLookupQuery(CodedLookupType type) => Type = type;
}
