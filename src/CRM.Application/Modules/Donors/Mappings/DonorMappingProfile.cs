using AutoMapper;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;

namespace CRM.Application.Modules.Donors.Mappings;

public class DonorMappingProfile : Profile
{
    public DonorMappingProfile()
    {
        CreateMap<Donor, DonorListItemDto>()
            .ForMember(d => d.CompanyType, o => o.MapFrom(s => s.CompanyType.Name))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.SubmissionSource, o => o.MapFrom(s => s.SubmissionSource.ToString()))
            .ForMember(d => d.RelationshipManager, o => o.MapFrom(s => s.RelationshipManager))
            .ForMember(d => d.LastInteractionDate, o => o.MapFrom(s =>
                s.InteractionLogs.OrderByDescending(i => i.CreatedAt).Select(i => (DateTime?)i.CreatedAt).FirstOrDefault()))
            .ForMember(d => d.LastInteractionType, o => o.MapFrom(s =>
                s.InteractionLogs.OrderByDescending(i => i.CreatedAt).Select(i => (string?)i.InteractionType.ToString()).FirstOrDefault()))
            .ForMember(d => d.OperationalRegions, o => o.MapFrom(s => s.OperationalRegions.Select(r => r.OperationalRegion.Code)))
            .ForMember(d => d.DonationFrequency, o => o.MapFrom(s => s.DonationFrequency.Name))
            .ForMember(d => d.DonationTypes, o => o.MapFrom(s => s.DonationTypes.Select(t => t.DonationType.Name)));

        CreateMap<User, RelationshipManagerDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FirstName + " " + s.LastName));
    }
}
