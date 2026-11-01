namespace CRM.Infrastructure.Persistence.Configurations.Vetting;

using CRM.Domain.Entities.Vetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class VettingAssessmentDetailsConfiguration : IEntityTypeConfiguration<VettingAssessmentDetails>
{
    public void Configure(EntityTypeBuilder<VettingAssessmentDetails> builder)
    {
        builder.ToTable("vetting_assessment_details");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        // The unique index is what makes the relationship one-to-one.
        builder.HasIndex(x => x.SubmissionId).IsUnique().HasDatabaseName("idx_vetting_assessment_details_submission");
        builder.Property(x => x.SubmissionId).HasColumnName("submission_id");
        builder.HasOne(x => x.Submission).WithOne(x => x.Details)
            .HasForeignKey<VettingAssessmentDetails>(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict).IsRequired();

        // 1 Organisation & Contact
        builder.Property(x => x.LegalName).HasColumnName("legal_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContactName).HasColumnName("contact_name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.ContactEmail).HasColumnName("contact_email").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Website).HasColumnName("website").HasMaxLength(150);

        // 2 Location
        builder.Property(x => x.AddressPhysical).HasColumnName("address_physical");
        builder.Property(x => x.AddressDetail).HasColumnName("address_detail");
        builder.Property(x => x.Province).HasColumnName("province").HasMaxLength(30).IsRequired();
        builder.Property(x => x.What3Words).HasColumnName("what3words").HasMaxLength(100).IsRequired();

        // 3 Programmes & Services
        builder.Property(x => x.CoreFocusAreas).HasColumnName("core_focus_areas").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.TargetPopulation).HasColumnName("target_population").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ProgrammesSummary).HasColumnName("programmes_summary").IsRequired();
        builder.Property(x => x.DistributionChannel).HasColumnName("distribution_channel").HasMaxLength(50).IsRequired();

        // 4 Staffing
        builder.Property(x => x.FullTimeFemales).HasColumnName("full_time_females").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.FullTimeMales).HasColumnName("full_time_males").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.VolunteersCount).HasColumnName("volunteers_count").IsRequired().HasDefaultValue(0);

        // 5 Registration
        builder.Property(x => x.IsRegisteredNpo).HasColumnName("is_registered_npo").IsRequired();
        builder.Property(x => x.NpoCertificatePath).HasColumnName("npo_certificate_path").HasMaxLength(255);
        builder.Property(x => x.IsRegisteredDsd).HasColumnName("is_registered_dsd").IsRequired();
        builder.Property(x => x.PboCertificatePath).HasColumnName("pbo_certificate_path").HasMaxLength(255);

        // 6 Demographics
        builder.Property(x => x.BeneficiaryRaces).HasColumnName("beneficiary_races").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.BeneficiaryGenders).HasColumnName("beneficiary_genders").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.BeneficiaryAgeGroups).HasColumnName("beneficiary_age_groups").HasColumnType("jsonb").IsRequired();

        // 7 Feeding Operation
        builder.Property(x => x.FeedingFrequency).HasColumnName("feeding_frequency").HasMaxLength(30).IsRequired();
        builder.Property(x => x.TotalPeopleServed).HasColumnName("total_people_served").IsRequired();
        builder.Property(x => x.FemalesServed).HasColumnName("females_served").IsRequired();
        builder.Property(x => x.MalesServed).HasColumnName("males_served").IsRequired();
        builder.Property(x => x.AfricanServed).HasColumnName("african_served").IsRequired();
        builder.Property(x => x.ColouredServed).HasColumnName("coloured_served").IsRequired();
        builder.Property(x => x.IndianAsianServed).HasColumnName("indian_asian_served").IsRequired();
        builder.Property(x => x.WhiteServed).HasColumnName("white_served").IsRequired();
        builder.Property(x => x.SaHarvestReliance).HasColumnName("sa_harvest_reliance").HasMaxLength(20).IsRequired();
        builder.Property(x => x.TransportCapacity).HasColumnName("transport_capacity").HasMaxLength(50).IsRequired();
        builder.Property(x => x.MealsProvided).HasColumnName("meals_provided").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.FeedingDays).HasColumnName("feeding_days").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.LastFedDate).HasColumnName("last_fed_date");

        // 8 Facilities & Hygiene
        builder.Property(x => x.FoodStorageTypes).HasColumnName("food_storage_types").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.KitchenStoragePhotosPath).HasColumnName("kitchen_storage_photos_path").HasMaxLength(255);
        builder.Property(x => x.KitchenClean).HasColumnName("kitchen_clean").IsRequired();
        builder.Property(x => x.WaterAccess).HasColumnName("water_access").IsRequired();
        builder.Property(x => x.ToiletFacilities).HasColumnName("toilet_facilities").IsRequired();
        builder.Property(x => x.PestFree).HasColumnName("pest_free").IsRequired();
        builder.Property(x => x.InfrastructureChecks).HasColumnName("infrastructure_checks").HasColumnType("jsonb").IsRequired();

        // 9 Access & Security
        builder.Property(x => x.DeliveryAccessEase).HasColumnName("delivery_access_ease").IsRequired();
        builder.Property(x => x.ParkingAndSecurity).HasColumnName("parking_and_security").IsRequired();
        builder.Property(x => x.PoliceStationProximity).HasColumnName("police_station_proximity").HasMaxLength(100);

        // 10 Capacity Notes
        builder.Property(x => x.OfficerComments).HasColumnName("officer_comments");
        builder.Property(x => x.ProposalCapabilities).HasColumnName("proposal_capabilities");
        builder.Property(x => x.DigitalCapabilities).HasColumnName("digital_capabilities");
        builder.Property(x => x.FacilityPhotosPath).HasColumnName("facility_photos_path").HasMaxLength(255);

        // 11 Documents
        builder.Property(x => x.SlaSigned).HasColumnName("sla_signed").IsRequired();
        builder.Property(x => x.ConsentFormSigned).HasColumnName("consent_form_signed").IsRequired();
        builder.Property(x => x.PolicyDocumentPresent).HasColumnName("policy_document_present").IsRequired();
        builder.Property(x => x.CertificatesPath).HasColumnName("certificates_path").HasMaxLength(255);
    }
}
