namespace CRM.Infrastructure.Persistence.Configurations.Lookups;

using CRM.Domain.Entities.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LookupCompanyTypeConfiguration : IEntityTypeConfiguration<LookupCompanyType>
{
    public void Configure(EntityTypeBuilder<LookupCompanyType> builder)
    {
        builder.ToTable("lookup_company_types");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("smallint");

        builder.HasIndex(x => x.Name).IsUnique();
        
        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.HasData(
            new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true, SortOrder = 1 },
            new LookupCompanyType { Id = 2, Name = "Farmer", IsActive = true, SortOrder = 2 },
            new LookupCompanyType { Id = 3, Name = "Distributor", IsActive = true, SortOrder = 3 },
            new LookupCompanyType { Id = 4, Name = "Retailer", IsActive = true, SortOrder = 4 },
            new LookupCompanyType { Id = 5, Name = "Packhouse", IsActive = true, SortOrder = 5 },
            new LookupCompanyType { Id = 6, Name = "Financial", IsActive = true, SortOrder = 6 },
            new LookupCompanyType { Id = 7, Name = "Market", IsActive = true, SortOrder = 7 },
            new LookupCompanyType { Id = 8, Name = "Prepared Food", IsActive = true, SortOrder = 8 },
            new LookupCompanyType { Id = 9, Name = "Butcher", IsActive = true, SortOrder = 9 },
            new LookupCompanyType { Id = 10, Name = "Dairy", IsActive = true, SortOrder = 10 },
            new LookupCompanyType { Id = 11, Name = "Broker", IsActive = true, SortOrder = 11 },
            new LookupCompanyType { Id = 12, Name = "Mill", IsActive = true, SortOrder = 12 },
            new LookupCompanyType { Id = 13, Name = "Cold Storage", IsActive = true, SortOrder = 13 },
            new LookupCompanyType { Id = 14, Name = "Packaging", IsActive = true, SortOrder = 14 },
            new LookupCompanyType { Id = 15, Name = "Other", IsActive = true, SortOrder = 15 }
        );
    }
}
