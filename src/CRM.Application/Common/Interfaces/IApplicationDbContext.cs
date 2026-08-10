namespace CRM.Application.Common.Interfaces;

using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Donor> Donors { get; }
    DbSet<InteractionLog> InteractionLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
