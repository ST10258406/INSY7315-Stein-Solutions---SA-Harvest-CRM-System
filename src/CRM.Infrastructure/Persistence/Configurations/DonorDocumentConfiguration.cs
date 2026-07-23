namespace CRM.Infrastructure.Persistence.Configurations;

using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DonorDocumentConfiguration : IEntityTypeConfiguration<DonorDocument>
{
    public void Configure(EntityTypeBuilder<DonorDocument> builder)
    {
        builder.ToTable("donor_documents");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.Property(x => x.DonorId).HasColumnName("donor_id");
        builder.Property(x => x.DocumentType).HasColumnName("document_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.BlobStoragePath).HasColumnName("blob_storage_path").HasColumnType("text").IsRequired();
        builder.Property(x => x.UploadedByUserId).HasColumnName("uploaded_by_user_id");

        builder.HasOne(x => x.Donor).WithMany(x => x.Documents).HasForeignKey(x => x.DonorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
