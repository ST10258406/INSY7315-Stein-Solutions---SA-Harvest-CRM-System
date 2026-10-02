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
        builder.Property(x => x.FileSizeBytes).HasColumnName("file_size_bytes").IsRequired();
        builder.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
        builder.Property(x => x.UploadedByUserId).HasColumnName("uploaded_by_user_id");

        builder.HasOne(x => x.Donor).WithMany(x => x.Documents).HasForeignKey(x => x.DonorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        // GLOBAL QUERY FILTER — soft-deleted documents are invisible to EVERY query on this
        // entity, including Donor.Documents in the donor-detail projection, the download
        // lookup and the authorization filter's type lookup (security review F-06). "Deleted"
        // has to mean gone from the user's point of view: no listing, no fresh SAS URL, for
        // any role. A filter rather than per-query `IsActive` checks so a future query can't
        // forget it. The rows stay in the table (soft delete). Code that genuinely needs
        // inactive rows — an audit/history view, a test asserting the soft delete — must opt
        // out explicitly with .IgnoreQueryFilters().
        builder.HasQueryFilter(x => x.IsActive);
    }
}
