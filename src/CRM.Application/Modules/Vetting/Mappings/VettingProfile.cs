namespace CRM.Application.Modules.Vetting.Mappings;

using AutoMapper;
using CRM.Application.Modules.Vetting.Officers.Dtos;
using CRM.Application.Modules.Vetting.SiteVisits.Dtos;
using CRM.Domain.Entities.Vetting;

public class VettingProfile : Profile
{
    public VettingProfile()
    {
        CreateMap<VettingOfficer, VettingOfficerDto>();

        // DonorCompanyName / OfficerInitials flatten from the navigations.
        CreateMap<VettingSiteVisit, VettingSiteVisitDto>()
            .ForMember(d => d.VisitStatus, o => o.MapFrom(s => s.VisitStatus.ToString()));
    }
}
