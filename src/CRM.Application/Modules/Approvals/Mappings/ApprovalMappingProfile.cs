namespace CRM.Application.Modules.Approvals.Mappings;

using AutoMapper;
using CRM.Application.Modules.Approvals.Dtos;
using CRM.Domain.Entities;

public class ApprovalMappingProfile : Profile
{
    public ApprovalMappingProfile()
    {
        CreateMap<DonorApproval, ApprovalDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Donor, o => o.MapFrom(s => s.Donor))
            .ForMember(d => d.RequestedBy, o => o.MapFrom(s => s.RequestedByUser))
            .ForMember(d => d.ReviewedBy, o => o.MapFrom(s => s.ReviewedByUser));

        CreateMap<Donor, ApprovalDonorDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

        CreateMap<User, ApprovalUserDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FirstName + " " + s.LastName));
    }
}
