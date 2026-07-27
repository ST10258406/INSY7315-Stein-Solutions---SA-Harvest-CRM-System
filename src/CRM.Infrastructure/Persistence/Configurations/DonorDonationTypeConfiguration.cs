namespace CRM.Infrastructure.Persistence.Configurations;

using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DonorDonationTypeConfiguration : IEntityTypeConfiguration<DonorDonationType>
{
    public void Configure(EntityTypeBuilder<DonorDonationType> builder)
    {
        builder.ToTable("donor_donation_types");

        builder.HasKey(x => new { x.DonorId, x.DonationTypeId });

        builder.Property(x => x.DonorId).HasColumnName("donor_id");
        builder.Property(x => x.DonationTypeId).HasColumnName("donation_type_id");

        builder.HasOne(x => x.Donor).WithMany(x => x.DonationTypes).HasForeignKey(x => x.DonorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.DonationType).WithMany().HasForeignKey(x => x.DonationTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
