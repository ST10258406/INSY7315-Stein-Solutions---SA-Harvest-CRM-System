namespace CRM.Infrastructure.Persistence.Configurations;

using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", t => t.HasCheckConstraint(
            "chk_notifications_type",
            "notification_type IN ('FollowUpReminder', 'NewDonorPendingReview', 'TaskDue', 'TaskAssigned', 'DonorApproved', 'DonorRejected')"));

        // No BaseEntity — configure Id and CreatedAt directly, no updated_at column
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.Property(x => x.UserId).HasColumnName("user_id");

        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Message).HasColumnName("message").HasColumnType("text").IsRequired();
        builder.Property(x => x.IsRead).HasColumnName("is_read");
        builder.Property(x => x.ReadAt).HasColumnName("read_at");

        builder.Property(x => x.NotificationType).HasColumnName("notification_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.RelatedEntityId).HasColumnName("related_entity_id");
        builder.Property(x => x.RelatedEntityType).HasColumnName("related_entity_type").HasMaxLength(50);

        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_notifications_user_id");
        builder.HasIndex(x => x.IsRead).HasDatabaseName("idx_notifications_unread").HasFilter("is_read = false");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_notifications_created_at").IsDescending();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict).IsRequired();
    }
}
