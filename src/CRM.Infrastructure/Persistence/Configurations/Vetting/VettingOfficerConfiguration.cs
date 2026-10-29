namespace CRM.Infrastructure.Persistence.Configurations.Vetting;

using CRM.Domain.Entities.Vetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class VettingOfficerConfiguration : IEntityTypeConfiguration<VettingOfficer>
{
    public void Configure(EntityTypeBuilder<VettingOfficer> builder)
    {
        builder.ToTable("vetting_officers");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Initials).HasColumnName("initials").HasMaxLength(5).IsRequired();
        builder.Property(x => x.AssignedRegion).HasColumnName("assigned_region").HasMaxLength(50).IsRequired();
        builder.Property(x => x.WorkloadCount).HasColumnName("workload_count").IsRequired().HasDefaultValue(0);
    }
}
