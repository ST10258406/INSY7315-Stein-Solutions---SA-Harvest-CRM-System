namespace CRM.Application.Modules.Interactions.Mappings;

using AutoMapper;
using CRM.Application.Modules.Interactions.Dtos;
using CRM.Domain.Entities;

public class InteractionMappingProfile : Profile
{
    public InteractionMappingProfile()
    {
        CreateMap<InteractionLog, InteractionLogDto>()
            .ForMember(d => d.InteractionType, o => o.MapFrom(s => s.InteractionType.ToString()))
            // Projected as-is (the stored blob path); the query handler swaps it for a SAS URL.
            .ForMember(d => d.EmailAttachmentUrl, o => o.MapFrom(s => s.EmailAttachmentUrl))
            .ForMember(d => d.CreatedBy, o => o.MapFrom(s => s.CreatedByUser));

        CreateMap<User, InteractionUserDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FirstName + " " + s.LastName));
    }
}
