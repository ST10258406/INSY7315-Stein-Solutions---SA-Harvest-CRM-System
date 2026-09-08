namespace CRM.Application.Modules.Notifications.Mappings;

using AutoMapper;
using CRM.Application.Modules.Notifications.Dtos;
using CRM.Domain.Entities;

public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        CreateMap<Notification, NotificationDto>()
            .ForMember(d => d.NotificationType, o => o.MapFrom(s => s.NotificationType.ToString()));
    }
}
