namespace CRM.Infrastructure.Persistence.Configurations.Vetting;

using CRM.Domain.Entities.Vetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CboOrganisationConfiguration : IEntityTypeConfiguration<CboOrganisation>
{
    public void Configure(EntityTypeBuilder<CboOrganisation> builder)
    {
        builder.ToTable("cbo_organisations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("idx_cbo_organisations_name");

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Province).HasColumnName("province").HasMaxLength(30).IsRequired();
        builder.Property(x => x.AreaSuburb).HasColumnName("area_suburb").HasMaxLength(100).IsRequired();
        builder.Property(x => x.MealsPerDay).HasColumnName("meals_per_day");
        builder.Property(x => x.PeopleServed).HasColumnName("people_served");
        builder.Property(x => x.VettingStatus).HasColumnName("vetting_status").HasConversion<string>().HasMaxLength(20).IsRequired();
    }
}
