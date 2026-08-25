namespace CRM.Application.Modules.Lookups.Mappings;

using AutoMapper;
using CRM.Application.Common.Models;
using CRM.Domain.Entities.Lookups;

public class LookupsMappingProfile : Profile
{
    public LookupsMappingProfile()
    {
        CreateMap<LookupCompanyType, LookupDto>();
        CreateMap<LookupEntityType, LookupDto>();
        CreateMap<LookupDonationFrequency, LookupDto>();
        CreateMap<LookupDonationType, LookupDto>();
        CreateMap<LookupBbbeeStatus, LookupDto>();

        CreateMap<LookupOperationalRegion, CodedLookupDto>();
        CreateMap<LookupProvince, CodedLookupDto>();
    }
}
