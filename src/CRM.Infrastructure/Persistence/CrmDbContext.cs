namespace CRM.Infrastructure.Persistence;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using Microsoft.EntityFrameworkCore;

public class CrmDbContext : DbContext, IApplicationDbContext
{
    public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options) { }

    // Auth
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Lookups
    public DbSet<LookupCompanyType> LookupCompanyTypes => Set<LookupCompanyType>();
    public DbSet<LookupEntityType> LookupEntityTypes => Set<LookupEntityType>();
    public DbSet<LookupOperationalRegion> LookupOperationalRegions => Set<LookupOperationalRegion>();
    public DbSet<LookupProvince> LookupProvinces => Set<LookupProvince>();
    public DbSet<LookupDonationType> LookupDonationTypes => Set<LookupDonationType>();
    public DbSet<LookupDonationFrequency> LookupDonationFrequencies => Set<LookupDonationFrequency>();
    public DbSet<LookupBbbeeStatus> LookupBbbeeStatuses => Set<LookupBbbeeStatus>();

    // Donor Core
    public DbSet<Donor> Donors => Set<Donor>();
    public DbSet<DonorLegalAddress> DonorLegalAddresses => Set<DonorLegalAddress>();
    public DbSet<DonorContact> DonorContacts => Set<DonorContact>();

    // Donor Relations
    public DbSet<DonorOperationalRegion> DonorOperationalRegions => Set<DonorOperationalRegion>();
    public DbSet<DonorDonationType> DonorDonationTypes => Set<DonorDonationType>();
    public DbSet<DonorDocument> DonorDocuments => Set<DonorDocument>();

    // Activity
    public DbSet<InteractionLog> InteractionLogs => Set<InteractionLog>();
    public DbSet<DonorTask> DonorTasks => Set<DonorTask>();

    // Workflow
    public DbSet<DonorApproval> DonorApprovals => Set<DonorApproval>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrmDbContext).Assembly);
    }
}
