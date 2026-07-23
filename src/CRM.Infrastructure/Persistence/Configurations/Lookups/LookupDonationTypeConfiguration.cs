namespace CRM.Infrastructure.Persistence.Configurations.Lookups;

using CRM.Domain.Entities.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LookupDonationTypeConfiguration : IEntityTypeConfiguration<LookupDonationType>
{
    public void Configure(EntityTypeBuilder<LookupDonationType> builder)
    {
        builder.ToTable("lookup_donation_types");

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
            new LookupDonationType { Id = 1, Name = "Bakery", IsActive = true, SortOrder = 1 },
            new LookupDonationType { Id = 2, Name = "Beverages", IsActive = true, SortOrder = 2 },
            new LookupDonationType { Id = 3, Name = "Dairy", IsActive = true, SortOrder = 3 },
            new LookupDonationType { Id = 4, Name = "Dry Goods", IsActive = true, SortOrder = 4 },
            new LookupDonationType { Id = 5, Name = "Financial", IsActive = true, SortOrder = 5 },
            new LookupDonationType { Id = 6, Name = "Fruit", IsActive = true, SortOrder = 6 },
            new LookupDonationType { Id = 7, Name = "Meat", IsActive = true, SortOrder = 7 },
            new LookupDonationType { Id = 8, Name = "Non Food", IsActive = true, SortOrder = 8 },
            new LookupDonationType { Id = 9, Name = "Prepared Food", IsActive = true, SortOrder = 9 },
            new LookupDonationType { Id = 10, Name = "Vegetables", IsActive = true, SortOrder = 10 },
            new LookupDonationType { Id = 11, Name = "Other", IsActive = true, SortOrder = 11 }
        );
    }
}
