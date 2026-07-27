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


    }
}
