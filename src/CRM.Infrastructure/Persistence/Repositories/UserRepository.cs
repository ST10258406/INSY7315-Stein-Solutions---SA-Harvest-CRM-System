namespace CRM.Infrastructure.Persistence.Repositories;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Constants;
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
            // Tracked: Login updates the failed-attempt counter / lockout on this instance.
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

    public async Task<(List<UserListItemDto> Items, int TotalCount)> SearchAsync(
        UserSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        // Never surface the non-authenticatable system actor(s) on the Users admin
        // screen — they're not real staff accounts (see SystemUsers / SystemUserSeeder).
        var query = _context.Users.AsNoTracking().Where(u => !u.Email.ToLower().EndsWith(SystemUsers.ReservedEmailDomain));

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search) ||
                u.Email.ToLower().Contains(search));
        }

        if (criteria.RoleId.HasValue)
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == criteria.RoleId.Value));

        if (criteria.IsActive.HasValue)
            query = query.Where(u => u.IsActive == criteria.IsActive.Value);

        // Whitelist sortBy against known fields - never build dynamic LINQ from an arbitrary string.
        var sortDir = criteria.SortDir.ToLowerInvariant();
        query = (criteria.SortBy?.ToLowerInvariant(), sortDir) switch
        {
            ("name", "desc") => query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName),
            ("name", _) => query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
            ("email", "desc") => query.OrderByDescending(u => u.Email),
            ("email", _) => query.OrderBy(u => u.Email),
            ("createdat", "asc") => query.OrderBy(u => u.CreatedAt),
            ("createdat", _) => query.OrderByDescending(u => u.CreatedAt),
            (_, "desc") => query.OrderByDescending(u => u.CreatedAt),
            _ => query.OrderByDescending(u => u.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ProjectTo<UserListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<User?> GetByIdWithRoleAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            // Excludes the system actor — see this method's interface remarks. Filtering
            // the list alone isn't enough since a direct PATCH by id would still reach it.
            .Where(u => !u.Email.ToLower().EndsWith(SystemUsers.ReservedEmailDomain))
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<UserAuthorizationTarget?> GetAuthorizationTargetAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserAuthorizationTarget
            {
                Id = u.Id,
                Email = u.Email,
                RoleNames = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

    public Task<User?> GetByIdWithRolesReadOnlyAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        return Task.CompletedTask;
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        => _context.Users.AsNoTracking()
            .AnyAsync(u => u.Email == email && (excludeUserId == null || u.Id != excludeUserId.Value), cancellationToken);

    public Task<List<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
        => _context.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .ProjectTo<RoleDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

    public Task<bool> RoleExistsAsync(Guid roleId, CancellationToken cancellationToken = default)
        => _context.Roles.AsNoTracking().AnyAsync(r => r.Id == roleId, cancellationToken);

    public Task<string?> GetRoleNameAsync(Guid roleId, CancellationToken cancellationToken = default)
        => _context.Roles.AsNoTracking().Where(r => r.Id == roleId).Select(r => (string?)r.Name).FirstOrDefaultAsync(cancellationToken);
}
