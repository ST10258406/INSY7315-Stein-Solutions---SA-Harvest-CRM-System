namespace CRM.Infrastructure.Persistence.Configurations;

using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DonorOperationalRegionConfiguration : IEntityTypeConfiguration<DonorOperationalRegion>
{
    public void Configure(EntityTypeBuilder<DonorOperationalRegion> builder)
    {
        builder.ToTable("donor_operational_regions");

        builder.HasKey(x => new { x.DonorId, x.OperationalRegionId });

        builder.Property(x => x.DonorId).HasColumnName("donor_id");
        builder.Property(x => x.OperationalRegionId).HasColumnName("operational_region_id");

        builder.HasOne(x => x.Donor).WithMany(x => x.OperationalRegions).HasForeignKey(x => x.DonorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.OperationalRegion).WithMany().HasForeignKey(x => x.OperationalRegionId).OnDelete(DeleteBehavior.Restrict);
    }
}
