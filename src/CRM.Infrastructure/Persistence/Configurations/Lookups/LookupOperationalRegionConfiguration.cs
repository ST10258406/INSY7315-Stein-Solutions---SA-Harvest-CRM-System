namespace CRM.Infrastructure.Persistence.Configurations.Lookups;

using CRM.Domain.Entities.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LookupOperationalRegionConfiguration : IEntityTypeConfiguration<LookupOperationalRegion>
{
    public void Configure(EntityTypeBuilder<LookupOperationalRegion> builder)
    {
        builder.ToTable("lookup_operational_regions");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("smallint");

        builder.HasIndex(x => x.Name).IsUnique();
        
        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(10)
            .IsRequired();
            
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.HasData(
            new LookupOperationalRegion { Id = 1, Code = "JHB", Name = "Johannesburg", IsActive = true, SortOrder = 1 },
            new LookupOperationalRegion { Id = 2, Code = "CPT", Name = "Cape Town", IsActive = true, SortOrder = 2 },
            new LookupOperationalRegion { Id = 3, Code = "KZN", Name = "KwaZulu-Natal", IsActive = true, SortOrder = 3 },
            new LookupOperationalRegion { Id = 4, Code = "EC", Name = "Eastern Cape", IsActive = true, SortOrder = 4 },
            new LookupOperationalRegion { Id = 5, Code = "BFN", Name = "Bloemfontein", IsActive = true, SortOrder = 5 },
            new LookupOperationalRegion { Id = 6, Code = "MPU", Name = "Mpumalanga", IsActive = true, SortOrder = 6 },
            new LookupOperationalRegion { Id = 7, Code = "LIM", Name = "Limpopo", IsActive = true, SortOrder = 7 }
        );
    }
}
