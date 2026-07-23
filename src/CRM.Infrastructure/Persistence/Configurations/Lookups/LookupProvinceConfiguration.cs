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

        builder.HasData(
            new LookupProvince { Id = 1, Code = "EC", Name = "Eastern Cape", IsActive = true, SortOrder = 1 },
            new LookupProvince { Id = 2, Code = "FS", Name = "Free State", IsActive = true, SortOrder = 2 },
            new LookupProvince { Id = 3, Code = "GP", Name = "Gauteng", IsActive = true, SortOrder = 3 },
            new LookupProvince { Id = 4, Code = "KZN", Name = "KwaZulu-Natal", IsActive = true, SortOrder = 4 },
            new LookupProvince { Id = 5, Code = "LP", Name = "Limpopo", IsActive = true, SortOrder = 5 },
            new LookupProvince { Id = 6, Code = "MP", Name = "Mpumalanga", IsActive = true, SortOrder = 6 },
            new LookupProvince { Id = 7, Code = "NC", Name = "Northern Cape", IsActive = true, SortOrder = 7 },
            new LookupProvince { Id = 8, Code = "NW", Name = "North West", IsActive = true, SortOrder = 8 },
            new LookupProvince { Id = 9, Code = "WC", Name = "Western Cape", IsActive = true, SortOrder = 9 }
        );
    }
}
