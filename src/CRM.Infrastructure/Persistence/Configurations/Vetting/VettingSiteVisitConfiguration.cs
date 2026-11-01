namespace CRM.Infrastructure.Persistence.Configurations.Vetting;

using CRM.Domain.Entities.Vetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class VettingSiteVisitConfiguration : IEntityTypeConfiguration<VettingSiteVisit>
{
    public void Configure(EntityTypeBuilder<VettingSiteVisit> builder)
    {
        builder.ToTable("vetting_site_visits");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => new { x.ScheduledDate, x.OfficerId }).HasDatabaseName("idx_vetting_site_visits_date_officer");

        builder.Property(x => x.DonorId).HasColumnName("donor_id");
        builder.Property(x => x.OfficerId).HasColumnName("officer_id");
        builder.Property(x => x.ScheduledDate).HasColumnName("scheduled_date").IsRequired();
        builder.Property(x => x.ScheduledTime).HasColumnName("scheduled_time").IsRequired();
        builder.Property(x => x.LocationLabel).HasColumnName("location_label").HasMaxLength(100).IsRequired();
        builder.Property(x => x.VisitNotes).HasColumnName("visit_notes").HasMaxLength(255);
        builder.Property(x => x.VisitStatus).HasColumnName("visit_status").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(x => x.Donor).WithMany().HasForeignKey(x => x.DonorId).OnDelete(DeleteBehavior.Restrict).IsRequired();
        builder.HasOne(x => x.Officer).WithMany(x => x.SiteVisits).HasForeignKey(x => x.OfficerId).OnDelete(DeleteBehavior.Restrict).IsRequired();
    }
}
