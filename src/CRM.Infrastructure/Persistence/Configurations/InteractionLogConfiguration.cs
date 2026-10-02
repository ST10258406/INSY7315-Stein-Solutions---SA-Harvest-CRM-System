namespace CRM.Infrastructure.Persistence.Configurations;

using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class InteractionLogConfiguration : IEntityTypeConfiguration<InteractionLog>
{
    public void Configure(EntityTypeBuilder<InteractionLog> builder)
    {
        builder.ToTable("interaction_logs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.Property(x => x.DonorId).HasColumnName("donor_id");
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");

        builder.Property(x => x.InteractionType).HasColumnName("interaction_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(255);
        builder.Property(x => x.Body).HasColumnName("body").HasColumnType("text").IsRequired();
        builder.Property(x => x.EmailAttachmentUrl).HasColumnName("email_attachment_url").HasColumnType("text");

        builder.Property(x => x.RelatedEntityId).HasColumnName("related_entity_id");
        builder.Property(x => x.RelatedEntityType).HasColumnName("related_entity_type").HasMaxLength(50);

        builder.HasOne(x => x.Donor).WithMany(x => x.InteractionLogs).HasForeignKey(x => x.DonorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict).IsRequired();
    }
}
