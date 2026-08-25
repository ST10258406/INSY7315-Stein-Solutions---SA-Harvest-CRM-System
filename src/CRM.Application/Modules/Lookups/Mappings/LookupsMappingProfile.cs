namespace CRM.Application.Modules.Lookups.Mappings;

using AutoMapper;
using CRM.Application.Common.Models;
using CRM.Domain.Entities.Lookups;

public class LookupsMappingProfile : Profile
{
    public LookupsMappingProfile()
    {
        CreateMap<LookupOperationalRegion, CodedLookupDto>();
        CreateMap<LookupProvince, CodedLookupDto>();
    }
}
