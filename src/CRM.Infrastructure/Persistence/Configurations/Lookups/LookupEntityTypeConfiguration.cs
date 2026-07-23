namespace CRM.Infrastructure.Persistence.Configurations.Lookups;

using CRM.Domain.Entities.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LookupEntityTypeConfiguration : IEntityTypeConfiguration<LookupEntityType>
{
    public void Configure(EntityTypeBuilder<LookupEntityType> builder)
    {
        builder.ToTable("lookup_entity_types");

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
            new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true, SortOrder = 1 },
            new LookupEntityType { Id = 2, Name = "Public Company", IsActive = true, SortOrder = 2 },
            new LookupEntityType { Id = 3, Name = "Close Corporation", IsActive = true, SortOrder = 3 },
            new LookupEntityType { Id = 4, Name = "Trust", IsActive = true, SortOrder = 4 },
            new LookupEntityType { Id = 5, Name = "Non Profit", IsActive = true, SortOrder = 5 },
            new LookupEntityType { Id = 6, Name = "Association", IsActive = true, SortOrder = 6 }
        );
    }
}
