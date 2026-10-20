namespace CRM.Infrastructure.Persistence.Configurations.Vetting;

using CRM.Domain.Entities.Vetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class VettingSubmissionConfiguration : IEntityTypeConfiguration<VettingSubmission>
{
    public void Configure(EntityTypeBuilder<VettingSubmission> builder)
    {
        builder.ToTable("vetting_submissions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => x.FormReference).IsUnique().HasDatabaseName("idx_vetting_submissions_form_reference");
        builder.HasIndex(x => x.ApprovalState).HasDatabaseName("idx_vetting_submissions_approval_state");
        builder.HasIndex(x => x.CboId).HasDatabaseName("idx_vetting_submissions_cbo");
        builder.HasIndex(x => x.OfficerId).HasDatabaseName("idx_vetting_submissions_officer");

        builder.Property(x => x.FormReference).HasColumnName("form_reference").HasMaxLength(20).IsRequired();
        builder.Property(x => x.CboId).HasColumnName("cbo_id");
        builder.Property(x => x.OfficerId).HasColumnName("officer_id");
        builder.Property(x => x.SiteVisitId).HasColumnName("site_visit_id");
        builder.Property(x => x.ApprovalState).HasColumnName("approval_state").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(x => x.AdminDecision).HasColumnName("admin_decision").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.DecisionReason).HasColumnName("decision_reason");

        builder.HasOne(x => x.Cbo).WithMany(x => x.Submissions).HasForeignKey(x => x.CboId).OnDelete(DeleteBehavior.Restrict).IsRequired();
        builder.HasOne(x => x.Officer).WithMany(x => x.Submissions).HasForeignKey(x => x.OfficerId).OnDelete(DeleteBehavior.Restrict).IsRequired();
        builder.HasOne(x => x.SiteVisit).WithMany().HasForeignKey(x => x.SiteVisitId).OnDelete(DeleteBehavior.Restrict);
    }
}
