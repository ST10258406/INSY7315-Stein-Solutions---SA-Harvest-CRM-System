namespace CRM.Infrastructure.Persistence.Configurations;

using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DonorConfiguration : IEntityTypeConfiguration<Donor>
{
    public void Configure(EntityTypeBuilder<Donor> builder)
    {
        builder.ToTable("donors");
        builder.ToTable(t => t.HasCheckConstraint("chk_donors_tax_number", "income_tax_number NOT LIKE '4%'"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => x.Status).HasDatabaseName("idx_donors_status");
        builder.HasIndex(x => x.RelationshipManagerId).HasDatabaseName("idx_donors_relationship_manager");
        builder.HasIndex(x => x.FollowUpDate).HasDatabaseName("idx_donors_follow_up_date");
        builder.HasIndex(x => x.CompanyName).HasDatabaseName("idx_donors_company_name");
        builder.HasIndex(x => x.FoodspaceCompanyId).HasDatabaseName("idx_donors_foodspace_id").HasFilter("foodspace_company_id IS NOT NULL");

        builder.Property(x => x.CompanyName).HasColumnName("company_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.CompanyTypeId).HasColumnName("company_type_id");
        builder.Property(x => x.Website).HasColumnName("website").HasMaxLength(500);
        builder.Property(x => x.RegisteredCompanyName).HasColumnName("registered_company_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.TradingName).HasColumnName("trading_name").HasMaxLength(255);
        builder.Property(x => x.EntityTypeId).HasColumnName("entity_type_id");
        builder.Property(x => x.CompanyRegistrationNumber).HasColumnName("company_registration_number").HasMaxLength(100);
        builder.Property(x => x.IncomeTaxNumber).HasColumnName("income_tax_number").HasMaxLength(100);
        builder.Property(x => x.DonationFrequencyId).HasColumnName("donation_frequency_id");
        builder.Property(x => x.BbbeeStatusId).HasColumnName("bbbee_status_id");
        builder.Property(x => x.CollectionAddress).HasColumnName("collection_address");
        builder.Property(x => x.OperationsLogisticsDetails).HasColumnName("operations_logistics_details");
        builder.Property(x => x.AdditionalInformation).HasColumnName("additional_information");
        builder.Property(x => x.RelationshipManagerId).HasColumnName("relationship_manager_id");

        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.SubmissionSource).HasColumnName("submission_source").HasConversion<string>().HasMaxLength(20).IsRequired();
        
        builder.Property(x => x.MarketingConsent).HasColumnName("marketing_consent");
        builder.Property(x => x.MarketingConsentDate).HasColumnName("marketing_consent_date");
        builder.Property(x => x.ImpactReportingPreferences).HasColumnName("impact_reporting_preferences");
        builder.Property(x => x.FollowUpDate).HasColumnName("follow_up_date");
        builder.Property(x => x.FoodspaceCompanyId).HasColumnName("foodspace_company_id").HasMaxLength(100);
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");

        // FKs
        builder.HasOne(x => x.CompanyType).WithMany().HasForeignKey(x => x.CompanyTypeId).OnDelete(DeleteBehavior.Restrict).IsRequired();
        builder.HasOne(x => x.EntityType).WithMany().HasForeignKey(x => x.EntityTypeId).OnDelete(DeleteBehavior.Restrict).IsRequired();
        builder.HasOne(x => x.DonationFrequency).WithMany().HasForeignKey(x => x.DonationFrequencyId).OnDelete(DeleteBehavior.Restrict).IsRequired();
        builder.HasOne(x => x.BbbeeStatus).WithMany().HasForeignKey(x => x.BbbeeStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RelationshipManager).WithMany().HasForeignKey(x => x.RelationshipManagerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict).IsRequired();
    }
}
