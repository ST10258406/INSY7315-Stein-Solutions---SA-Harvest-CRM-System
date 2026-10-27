namespace CRM.Infrastructure.Persistence.Configurations.Vetting;

using CRM.Domain.Entities.Vetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class VettingFlagConfiguration : IEntityTypeConfiguration<VettingFlag>
{
    public void Configure(EntityTypeBuilder<VettingFlag> builder)
    {
        builder.ToTable("vetting_flags");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.Property(x => x.SubmissionId).HasColumnName("submission_id");
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.HasOne(x => x.Submission).WithMany(x => x.Flags).HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict).IsRequired();
    }
}
