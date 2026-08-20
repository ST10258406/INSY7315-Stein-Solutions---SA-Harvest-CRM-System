namespace CRM.Application.Common.Interfaces;

using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using Microsoft.EntityFrameworkCore;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Donor> Donors { get; }
    DbSet<InteractionLog> InteractionLogs { get; }
    DbSet<DonorApproval> DonorApprovals { get; }
    DbSet<LookupCompanyType> LookupCompanyTypes { get; }
    DbSet<LookupEntityType> LookupEntityTypes { get; }
    DbSet<LookupOperationalRegion> LookupOperationalRegions { get; }
    DbSet<LookupProvince> LookupProvinces { get; }
    DbSet<LookupDonationType> LookupDonationTypes { get; }
    DbSet<LookupDonationFrequency> LookupDonationFrequencies { get; }
    DbSet<LookupBbbeeStatus> LookupBbbeeStatuses { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
