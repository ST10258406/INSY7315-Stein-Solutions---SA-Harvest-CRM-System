namespace CRM.Infrastructure.Persistence.Configurations;

using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DonorContactConfiguration : IEntityTypeConfiguration<DonorContact>
{
    public void Configure(EntityTypeBuilder<DonorContact> builder)
    {
        builder.ToTable("donor_contacts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => new { x.DonorId, x.ContactType }).IsUnique();

        builder.Property(x => x.DonorId).HasColumnName("donor_id");
        builder.Property(x => x.ContactType).HasColumnName("contact_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.JobTitle).HasColumnName("job_title").HasMaxLength(255);
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);

        builder.HasOne(x => x.Donor).WithMany(x => x.Contacts).HasForeignKey(x => x.DonorId).OnDelete(DeleteBehavior.Cascade);
    }
}
