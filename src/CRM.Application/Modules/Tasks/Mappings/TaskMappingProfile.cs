namespace CRM.Application.Modules.Tasks.Mappings;

using AutoMapper;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Domain.Entities;

public class TaskMappingProfile : Profile
{
    public TaskMappingProfile()
    {
        CreateMap<DonorTask, TaskDto>()
            .ForMember(d => d.Donor, o => o.MapFrom(s => s.Donor))
            .ForMember(d => d.AssignedTo, o => o.MapFrom(s => s.AssignedToUser))
            .ForMember(d => d.CreatedBy, o => o.MapFrom(s => s.CreatedByUser))
            .ForMember(d => d.CompletedBy, o => o.MapFrom(s => s.CompletedByUser));

        CreateMap<Donor, TaskDonorDto>();

        CreateMap<User, TaskUserDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FirstName + " " + s.LastName));
    }
}
