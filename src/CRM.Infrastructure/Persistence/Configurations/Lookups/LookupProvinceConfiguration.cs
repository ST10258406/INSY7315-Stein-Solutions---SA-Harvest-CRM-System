namespace CRM.Infrastructure.Persistence.Configurations.Lookups;

using CRM.Domain.Entities.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LookupProvinceConfiguration : IEntityTypeConfiguration<LookupProvince>
{
    public void Configure(EntityTypeBuilder<LookupProvince> builder)
    {
        builder.ToTable("lookup_provinces");

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
            .HasMaxLength(5)
            .IsRequired();
            
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);


    }
}
