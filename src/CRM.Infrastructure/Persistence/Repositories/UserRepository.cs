namespace CRM.Infrastructure.Persistence.Repositories;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class UserRepository : IUserRepository
{
    private readonly CrmDbContext _context;
    private readonly IMapper _mapper;

    public UserRepository(CrmDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default)
        => _context.Users
            .Include(u => u.UserRoles!)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking() // we're only reading the user (not modifying it — the refresh token is a separate entity being added)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<List<Guid>> GetActiveUserIdsByRoleAsync(string roleName, CancellationToken cancellationToken = default)
        => _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == roleName))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

    public Task<List<RelationshipManagerDto>> GetActiveByRoleAsync(string roleName, CancellationToken cancellationToken = default)
        => _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == roleName))
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .ProjectTo<RelationshipManagerDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAndActiveAsync(Guid userId, CancellationToken cancellationToken = default)
        => _context.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.IsActive, cancellationToken);
}
