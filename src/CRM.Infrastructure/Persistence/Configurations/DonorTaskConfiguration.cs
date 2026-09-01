namespace CRM.Infrastructure.Persistence.Configurations;

using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DonorTaskConfiguration : IEntityTypeConfiguration<DonorTask>
{
    public void Configure(EntityTypeBuilder<DonorTask> builder)
    {
        builder.ToTable("donor_tasks");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.Property(x => x.DonorId).HasColumnName("donor_id");
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(x => x.DueDate).HasColumnName("due_date");
        builder.Property(x => x.IsCompleted).HasColumnName("is_completed");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");

        builder.Property(x => x.AssignedToUserId).HasColumnName("assigned_to_user_id");
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(x => x.CompletedByUserId).HasColumnName("completed_by_user_id");

        builder.HasOne(x => x.Donor).WithMany(x => x.Tasks).HasForeignKey(x => x.DonorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.AssignedToUser).WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.Restrict).IsRequired();
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict).IsRequired();
        builder.HasOne(x => x.CompletedByUser).WithMany().HasForeignKey(x => x.CompletedByUserId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
