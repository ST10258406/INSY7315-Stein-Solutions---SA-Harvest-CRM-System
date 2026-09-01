namespace CRM.Infrastructure.Persistence.Repositories;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly CrmDbContext _context;

    public RefreshTokenRepository(CrmDbContext context) => _context = context;

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _context.RefreshTokens.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        => _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

    public Task<RefreshToken?> GetByTokenWithUserAndRolesAsync(string token, CancellationToken cancellationToken = default)
        => _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles!)
                    .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

    public Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);
}
