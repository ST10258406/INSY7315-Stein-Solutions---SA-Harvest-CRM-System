namespace CRM.Infrastructure.Persistence.Configurations;

using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DonorApprovalConfiguration : IEntityTypeConfiguration<DonorApproval>
{
    public void Configure(EntityTypeBuilder<DonorApproval> builder)
    {
        builder.ToTable("donor_approvals");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.Property(x => x.DonorId).HasColumnName("donor_id");
        builder.Property(x => x.RequestedByUserId).HasColumnName("requested_by_user_id");
        builder.Property(x => x.ReviewedByUserId).HasColumnName("reviewed_by_user_id");

        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.RejectionReason).HasColumnName("rejection_reason").HasColumnType("text");
        builder.Property(x => x.ReviewedAt).HasColumnName("reviewed_at");

        builder.HasOne(x => x.Donor).WithMany(x => x.Approvals).HasForeignKey(x => x.DonorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict).IsRequired();
        builder.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
