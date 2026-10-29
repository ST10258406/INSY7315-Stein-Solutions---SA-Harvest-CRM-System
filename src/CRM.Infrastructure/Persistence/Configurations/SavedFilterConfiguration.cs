namespace CRM.Infrastructure.Persistence.Configurations;

using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SavedFilterConfiguration : IEntityTypeConfiguration<SavedFilter>
{
    public void Configure(EntityTypeBuilder<SavedFilter> builder)
    {
        builder.ToTable("saved_filters", t => t.HasCheckConstraint("chk_saved_filters_page", "page IN ('Donors')"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Page).HasColumnName("page").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.QueryString).HasColumnName("query_string").HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        // A user can't have two active filters with the same name; deleted ones free the name up.
        builder.HasIndex(x => new { x.UserId, x.Name })
            .IsUnique()
            .HasFilter("is_active = true")
            .HasDatabaseName("ux_saved_filters_user_name_active");

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict).IsRequired();
    }
}
