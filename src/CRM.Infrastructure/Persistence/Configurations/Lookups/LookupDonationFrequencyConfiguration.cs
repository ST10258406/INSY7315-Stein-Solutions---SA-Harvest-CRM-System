namespace CRM.Infrastructure.Persistence.Configurations.Lookups;

using CRM.Domain.Entities.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LookupDonationFrequencyConfiguration : IEntityTypeConfiguration<LookupDonationFrequency>
{
    public void Configure(EntityTypeBuilder<LookupDonationFrequency> builder)
    {
        builder.ToTable("lookup_donation_frequencies");

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
            new LookupDonationFrequency { Id = 1, Name = "Ad Hoc", IsActive = true, SortOrder = 1 },
            new LookupDonationFrequency { Id = 2, Name = "Once-off", IsActive = true, SortOrder = 2 },
            new LookupDonationFrequency { Id = 3, Name = "Weekly", IsActive = true, SortOrder = 3 },
            new LookupDonationFrequency { Id = 4, Name = "Monthly", IsActive = true, SortOrder = 4 },
            new LookupDonationFrequency { Id = 5, Name = "Seasonal", IsActive = true, SortOrder = 5 }
        );
    }
}
