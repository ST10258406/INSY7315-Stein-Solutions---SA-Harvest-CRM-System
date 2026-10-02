using AutoMapper;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Entities;

namespace CRM.Application.Modules.Users.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // A user can only ever be assigned one role in practice, even though the
        // schema (UserRole junction table) supports many — the most recently
        // assigned one wins for display purposes.
        CreateMap<User, UserListItemDto>()
            .ForMember(d => d.RoleId, o => o.MapFrom(s =>
                s.UserRoles.OrderByDescending(ur => ur.AssignedAt).Select(ur => ur.RoleId).FirstOrDefault()))
            .ForMember(d => d.Role, o => o.MapFrom(s =>
                s.UserRoles.OrderByDescending(ur => ur.AssignedAt).Select(ur => ur.Role.Name).FirstOrDefault() ?? string.Empty))
            .ForMember(d => d.IsLockedOut, o => o.MapFrom(s =>
                s.LockoutEndUtc != null && s.LockoutEndUtc > DateTimeOffset.UtcNow))
            .ForMember(d => d.LockedUntil, o => o.MapFrom(s =>
                s.LockoutEndUtc != null && s.LockoutEndUtc > DateTimeOffset.UtcNow ? s.LockoutEndUtc : null));

        CreateMap<Role, RoleDto>();
    }
}
