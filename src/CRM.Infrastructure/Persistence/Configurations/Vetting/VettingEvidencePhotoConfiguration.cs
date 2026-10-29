namespace CRM.Infrastructure.Persistence.Configurations.Vetting;

using CRM.Domain.Entities.Vetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class VettingEvidencePhotoConfiguration : IEntityTypeConfiguration<VettingEvidencePhoto>
{
    public void Configure(EntityTypeBuilder<VettingEvidencePhoto> builder)
    {
        builder.ToTable("vetting_evidence_photos");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.ToTable(t => t.HasCheckConstraint("chk_vetting_evidence_photos_shot_index", "shot_index BETWEEN 0 AND 3"));

        builder.HasIndex(x => new { x.SubmissionId, x.ShotIndex }).IsUnique().HasDatabaseName("idx_vetting_evidence_photos_submission_shot");

        builder.Property(x => x.SubmissionId).HasColumnName("submission_id");
        builder.Property(x => x.ShotIndex).HasColumnName("shot_index").IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.IsCaptured).HasColumnName("is_captured").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(100);
        builder.Property(x => x.BlobStoragePath).HasColumnName("blob_storage_path").HasMaxLength(255);
        builder.Property(x => x.CapturedAt).HasColumnName("captured_at");

        builder.HasOne(x => x.Submission).WithMany(x => x.Photos).HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict).IsRequired();
    }
}
