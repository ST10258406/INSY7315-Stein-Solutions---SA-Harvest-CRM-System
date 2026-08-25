namespace CRM.Application.Modules.Lookups.Queries.GetLookup;

using CRM.Application.Common.Models;
using MediatR;

public enum LookupType
{
    CompanyTypes,
    EntityTypes,
    DonationTypes,
    DonationFrequencies,
    BbbeeStatuses
}

public class GetLookupQuery : IRequest<List<LookupDto>>
{
    public LookupType Type { get; set; }

    public GetLookupQuery(LookupType type) => Type = type;
}
