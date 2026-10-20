namespace CRM.Infrastructure.Persistence.Repositories;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.SavedFilters.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public class SavedFilterRepository : ISavedFilterRepository
{
    private readonly CrmDbContext _context;

    public SavedFilterRepository(CrmDbContext context)
    {
        _context = context;
    }

    private IQueryable<SavedFilter> Active(Guid userId) =>
        _context.SavedFilters.Where(f => f.UserId == userId && f.IsActive);

    public Task<List<SavedFilterDto>> ListActiveAsync(Guid userId, SavedFilterPage page, CancellationToken cancellationToken = default)
        => Active(userId)
            .AsNoTracking()
            .Where(f => f.Page == page)
            .OrderBy(f => f.Name)
            .Select(f => new SavedFilterDto(f.Id, f.Name, f.Page.ToString(), f.QueryString))
            .ToListAsync(cancellationToken);

    public Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default)
        => Active(userId).AsNoTracking().CountAsync(cancellationToken);

    public Task<bool> ActiveNameExistsAsync(Guid userId, string name, CancellationToken cancellationToken = default)
    {
        var lowered = name.ToLower();
        return Active(userId).AsNoTracking().AnyAsync(f => f.Name.ToLower() == lowered, cancellationToken);
    }

    public Task<SavedFilter?> GetActiveForUpdateAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        => Active(userId).FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task AddAsync(SavedFilter filter, CancellationToken cancellationToken = default)
        => await _context.SavedFilters.AddAsync(filter, cancellationToken);
}
