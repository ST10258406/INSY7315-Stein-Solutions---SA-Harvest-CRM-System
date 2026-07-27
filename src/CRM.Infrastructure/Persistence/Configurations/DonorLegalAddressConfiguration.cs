namespace CRM.Infrastructure.Persistence.Configurations;

using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DonorLegalAddressConfiguration : IEntityTypeConfiguration<DonorLegalAddress>
{
    public void Configure(EntityTypeBuilder<DonorLegalAddress> builder)
    {
        builder.ToTable("donor_legal_addresses");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => x.DonorId).IsUnique();
        
        builder.Property(x => x.DonorId).HasColumnName("donor_id");
        builder.Property(x => x.StreetAddress).HasColumnName("street_name_number").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Suburb).HasColumnName("suburb").HasMaxLength(100).IsRequired();
        builder.Property(x => x.City).HasColumnName("city").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProvinceId).HasColumnName("province_id");
        builder.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(10).IsRequired();

        builder.HasOne(x => x.Donor).WithOne(x => x.LegalAddress).HasForeignKey<DonorLegalAddress>(x => x.DonorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Province).WithMany().HasForeignKey(x => x.ProvinceId).OnDelete(DeleteBehavior.Restrict);
    }
}
