namespace CRM.Infrastructure.Persistence.Configurations.Lookups;

using CRM.Domain.Entities.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LookupBbbeeStatusConfiguration : IEntityTypeConfiguration<LookupBbbeeStatus>
{
    public void Configure(EntityTypeBuilder<LookupBbbeeStatus> builder)
    {
        builder.ToTable("lookup_bbbee_statuses");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("smallint");

        builder.HasIndex(x => x.Name).IsUnique();
        
        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(255);

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.HasData(
            new LookupBbbeeStatus { Id = 1, Name = "Level 1", Description = null, IsActive = true, SortOrder = 1 },
            new LookupBbbeeStatus { Id = 2, Name = "Level 2", Description = null, IsActive = true, SortOrder = 2 },
            new LookupBbbeeStatus { Id = 3, Name = "Level 3", Description = null, IsActive = true, SortOrder = 3 },
            new LookupBbbeeStatus { Id = 4, Name = "Level 4", Description = null, IsActive = true, SortOrder = 4 },
            new LookupBbbeeStatus { Id = 5, Name = "Level 5", Description = null, IsActive = true, SortOrder = 5 },
            new LookupBbbeeStatus { Id = 6, Name = "Level 6", Description = null, IsActive = true, SortOrder = 6 },
            new LookupBbbeeStatus { Id = 7, Name = "Level 7", Description = null, IsActive = true, SortOrder = 7 },
            new LookupBbbeeStatus { Id = 8, Name = "Level 8", Description = null, IsActive = true, SortOrder = 8 },
            new LookupBbbeeStatus { Id = 9, Name = "Exempt Micro Enterprise", Description = null, IsActive = true, SortOrder = 9 },
            new LookupBbbeeStatus { Id = 10, Name = "Non-Compliant", Description = null, IsActive = true, SortOrder = 10 },
            new LookupBbbeeStatus { Id = 11, Name = "Not Applicable", Description = null, IsActive = true, SortOrder = 11 }
        );
    }
}
