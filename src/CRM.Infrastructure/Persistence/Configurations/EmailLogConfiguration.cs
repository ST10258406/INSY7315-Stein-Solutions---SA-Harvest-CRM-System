namespace CRM.Infrastructure.Persistence.Configurations;

using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("email_logs");

        // No BaseEntity, configure Id and CreatedAt directly
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.Property(x => x.DonorId).HasColumnName("donor_id");
        builder.Property(x => x.SentByUserId).HasColumnName("sent_by_user_id");

        builder.Property(x => x.EmailType).HasColumnName("email_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ToAddress).HasColumnName("to_address").HasMaxLength(320).IsRequired();
        builder.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Body).HasColumnName("body").HasColumnType("text").IsRequired();

        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ProviderMessageId).HasColumnName("provider_message_id").HasMaxLength(255);
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message").HasColumnType("text");

        builder.HasOne(x => x.Donor).WithMany().HasForeignKey(x => x.DonorId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne(x => x.SentByUser).WithMany().HasForeignKey(x => x.SentByUserId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
