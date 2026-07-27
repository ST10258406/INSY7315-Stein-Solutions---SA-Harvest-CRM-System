namespace CRM.Infrastructure.Persistence.Configurations;

using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        // No BaseEntity, configure Id and CreatedAt directly
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.Property(x => x.UserId).HasColumnName("user_id");
        
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(50).IsRequired();
        builder.Property(x => x.EntityId).HasColumnName("entity_id");

        builder.Property(x => x.Action).HasColumnName("action").HasConversion<string>().HasMaxLength(20).IsRequired();
        
        builder.Property(x => x.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
        builder.Property(x => x.NewValues).HasColumnName("new_values").HasColumnType("jsonb");
        
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasColumnType("text");

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
