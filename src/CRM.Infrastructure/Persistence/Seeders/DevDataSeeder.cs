namespace CRM.Infrastructure.Persistence.Seeders;

using CRM.Domain.Entities;
using CRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Local/dev-only convenience data — donors, contacts, tasks, approvals, and a
/// couple of interaction logs — so the UI has something to show right after
/// `docker compose up` instead of an empty dashboard. Only ever called for the
/// Development environment (see DatabaseSeeder.SeedAsync); never runs against
/// Staging/Production. Skipped entirely if any donor already exists, so it
/// never overwrites or duplicates data a developer has since created through
/// the app themselves.
/// </summary>
public static class DevDataSeeder
{
    public static async Task SeedAsync(CrmDbContext context)
    {
        if (await context.Donors.AnyAsync())
        {
            return;
        }

        var adminUser = await context.Users.SingleAsync(u => u.Email == "admin@crm.local");

        var companyTypeIds = await context.LookupCompanyTypes.ToDictionaryAsync(x => x.Name, x => x.Id);
        var entityTypeIds = await context.LookupEntityTypes.ToDictionaryAsync(x => x.Name, x => x.Id);
        var frequencyIds = await context.LookupDonationFrequencies.ToDictionaryAsync(x => x.Name, x => x.Id);
        var donationTypeIds = await context.LookupDonationTypes.ToDictionaryAsync(x => x.Name, x => x.Id);
        var regionIds = await context.LookupOperationalRegions.ToDictionaryAsync(x => x.Code, x => x.Id);
        var provinceIds = await context.LookupProvinces.ToDictionaryAsync(x => x.Code, x => x.Id);
        var bbbeeIds = await context.LookupBbbeeStatuses.ToDictionaryAsync(x => x.Name, x => x.Id);

        var now = DateTime.UtcNow;

        var donors = new[]
        {
            BuildDonor(
                companyName: "Pick n Pay Foundation", registeredName: "Pick n Pay Retailers (Pty) Ltd",
                companyType: "Retailer", entityType: "Public Company", frequency: "Monthly",
                status: DonorStatus.Active, source: SubmissionSource.ManualCapture,
                bbbeeStatus: "Level 2", taxNumber: "9871234567",
                donationTypes: ["Dry Goods", "Non Food"], regions: ["JHB", "CPT"],
                province: "GP", relationshipManagerId: adminUser.Id, createdByUserId: adminUser.Id,
                contactName: "Nomsa Dlamini", contactEmail: "nomsa.dlamini@pnp.example.co.za",
                companyTypeIds, entityTypeIds, frequencyIds, donationTypeIds, regionIds, provinceIds, bbbeeIds),

            BuildDonor(
                companyName: "Fruits & Roots Farming", registeredName: "Fruits & Roots Farming CC",
                companyType: "Farmer", entityType: "Close Corporation", frequency: "Weekly",
                status: DonorStatus.PendingReview, source: SubmissionSource.PublicForm,
                bbbeeStatus: null, taxNumber: "8221349876",
                donationTypes: ["Fruit", "Vegetables"], regions: ["EC"],
                province: "EC", relationshipManagerId: null, createdByUserId: adminUser.Id,
                contactName: "Johan van der Merwe", contactEmail: "johan@fruitsandroots.example.co.za",
                companyTypeIds, entityTypeIds, frequencyIds, donationTypeIds, regionIds, provinceIds, bbbeeIds),

            BuildDonor(
                companyName: "Golden Grain Mills", registeredName: "Golden Grain Milling (Pty) Ltd",
                companyType: "Mill", entityType: "Private Company", frequency: "Seasonal",
                status: DonorStatus.Lapsed, source: SubmissionSource.ManualCapture,
                bbbeeStatus: "Level 4", taxNumber: "7734561298",
                donationTypes: ["Dry Goods"], regions: ["MPU", "LIM"],
                province: "MP", relationshipManagerId: adminUser.Id, createdByUserId: adminUser.Id,
                contactName: "Sipho Nkosi", contactEmail: "sipho@goldengrain.example.co.za",
                companyTypeIds, entityTypeIds, frequencyIds, donationTypeIds, regionIds, provinceIds, bbbeeIds),

            BuildDonor(
                companyName: "Ocean Fresh Seafood", registeredName: "Ocean Fresh Seafood Distributors CC",
                companyType: "Distributor", entityType: "Close Corporation", frequency: "Ad Hoc",
                status: DonorStatus.Rejected, source: SubmissionSource.PublicForm,
                bbbeeStatus: null, taxNumber: "6612349087",
                donationTypes: ["Meat"], regions: ["CPT"],
                province: "WC", relationshipManagerId: null, createdByUserId: adminUser.Id,
                contactName: "Lindiwe Khumalo", contactEmail: "lindiwe@oceanfresh.example.co.za",
                companyTypeIds, entityTypeIds, frequencyIds, donationTypeIds, regionIds, provinceIds, bbbeeIds),

            BuildDonor(
                companyName: "Sunrise Bakery Co", registeredName: "Sunrise Bakery Co (Pty) Ltd",
                companyType: "Prepared Food", entityType: "Private Company", frequency: "Once-off",
                status: DonorStatus.Active, source: SubmissionSource.ManualCapture,
                bbbeeStatus: "Level 1", taxNumber: "5590123478",
                donationTypes: ["Bakery", "Prepared Food"], regions: ["JHB"],
                province: "GP", relationshipManagerId: adminUser.Id, createdByUserId: adminUser.Id,
                contactName: "Anél Botha", contactEmail: "anel@sunrisebakery.example.co.za",
                companyTypeIds, entityTypeIds, frequencyIds, donationTypeIds, regionIds, provinceIds, bbbeeIds),
        };

        context.Donors.AddRange(donors);
        await context.SaveChangesAsync();

        var active = donors[0];
        var pending = donors[1];
        var lapsed = donors[2];
        var rejected = donors[3];
        var active2 = donors[4];

        context.DonorTasks.AddRange(
            new DonorTask
            {
                Id = Guid.NewGuid(),
                DonorId = active.Id,
                Title = "Confirm December collection schedule",
                Description = "Check whether the festive-season collection slots still work for them.",
                DueDate = now.AddDays(5),
                IsCompleted = false,
                AssignedToUserId = adminUser.Id,
                CreatedByUserId = adminUser.Id,
            },
            new DonorTask
            {
                Id = Guid.NewGuid(),
                DonorId = pending.Id,
                Title = "Review submission documents",
                Description = "New public-form submission — verify company registration and BBBEE docs before approval.",
                DueDate = now.AddDays(2),
                IsCompleted = false,
                AssignedToUserId = adminUser.Id,
                CreatedByUserId = adminUser.Id,
            },
            new DonorTask
            {
                Id = Guid.NewGuid(),
                DonorId = lapsed.Id,
                Title = "Re-engage after quiet quarter",
                Description = "No donations logged in 3 months — call to check if they're still able to donate.",
                DueDate = now.AddDays(-4), // overdue on purpose, to exercise the follow-up UI
                IsCompleted = false,
                AssignedToUserId = adminUser.Id,
                CreatedByUserId = adminUser.Id,
            },
            new DonorTask
            {
                Id = Guid.NewGuid(),
                DonorId = active2.Id,
                Title = "Send Q3 impact report",
                Description = "They've opted into impact reporting — send the Q3 summary.",
                DueDate = now.AddDays(-1),
                IsCompleted = true,
                CompletedAt = now.AddDays(-1),
                AssignedToUserId = adminUser.Id,
                CreatedByUserId = adminUser.Id,
                CompletedByUserId = adminUser.Id,
            });

        context.DonorApprovals.AddRange(
            new DonorApproval
            {
                Id = Guid.NewGuid(),
                DonorId = pending.Id,
                RequestedByUserId = adminUser.Id,
                Status = ApprovalStatus.Pending,
            },
            new DonorApproval
            {
                Id = Guid.NewGuid(),
                DonorId = active.Id,
                RequestedByUserId = adminUser.Id,
                ReviewedByUserId = adminUser.Id,
                Status = ApprovalStatus.Approved,
                ReviewedAt = now.AddDays(-30),
            },
            new DonorApproval
            {
                Id = Guid.NewGuid(),
                DonorId = rejected.Id,
                RequestedByUserId = adminUser.Id,
                ReviewedByUserId = adminUser.Id,
                Status = ApprovalStatus.Rejected,
                RejectionReason = "Could not verify company registration number — resubmit with valid CIPC documents.",
                ReviewedAt = now.AddDays(-10),
            });

        context.InteractionLogs.AddRange(
            new InteractionLog
            {
                Id = Guid.NewGuid(),
                CreatedAt = now.AddDays(-20),
                DonorId = active.Id,
                CreatedByUserId = adminUser.Id,
                InteractionType = InteractionType.Call,
                Subject = "Monthly check-in",
                Body = "Confirmed collection volumes for the month, no issues raised.",
            },
            new InteractionLog
            {
                Id = Guid.NewGuid(),
                CreatedAt = now.AddDays(-1),
                DonorId = pending.Id,
                CreatedByUserId = adminUser.Id,
                InteractionType = InteractionType.FormSubmission,
                Subject = "Public form submission received",
                Body = "Donor submitted their onboarding form via the public site.",
            });

        await context.SaveChangesAsync();
    }

    private static Donor BuildDonor(
        string companyName, string registeredName, string companyType, string entityType, string frequency,
        DonorStatus status, SubmissionSource source, string? bbbeeStatus, string taxNumber,
        string[] donationTypes, string[] regions, string province,
        Guid? relationshipManagerId, Guid createdByUserId, string contactName, string contactEmail,
        Dictionary<string, short> companyTypeIds, Dictionary<string, short> entityTypeIds,
        Dictionary<string, short> frequencyIds, Dictionary<string, short> donationTypeIds,
        Dictionary<string, short> regionIds, Dictionary<string, short> provinceIds, Dictionary<string, short> bbbeeIds)
    {
        var donorId = Guid.NewGuid();

        var donor = new Donor
        {
            Id = donorId,
            CompanyName = companyName,
            CompanyTypeId = companyTypeIds[companyType],
            RegisteredCompanyName = registeredName,
            EntityTypeId = entityTypeIds[entityType],
            IncomeTaxNumber = taxNumber,
            DonationFrequencyId = frequencyIds[frequency],
            BbbeeStatusId = bbbeeStatus is null ? null : bbbeeIds[bbbeeStatus],
            CollectionAddress = $"12 Industrial Road, {companyName}",
            RelationshipManagerId = relationshipManagerId,
            Status = status,
            SubmissionSource = source,
            MarketingConsent = true,
            CreatedByUserId = createdByUserId,
            LegalAddress = new DonorLegalAddress
            {
                Id = Guid.NewGuid(),
                DonorId = donorId,
                StreetAddress = "12 Industrial Road",
                Suburb = "Midrand",
                City = "Johannesburg",
                ProvinceId = provinceIds[province],
                PostalCode = "1685",
            },
        };

        donor.Contacts.Add(new DonorContact
        {
            Id = Guid.NewGuid(),
            DonorId = donorId,
            ContactType = ContactType.Primary,
            Name = contactName,
            Email = contactEmail,
            Phone = "011 555 0100",
        });

        foreach (var type in donationTypes)
        {
            donor.DonationTypes.Add(new DonorDonationType { DonorId = donorId, DonationTypeId = donationTypeIds[type] });
        }

        foreach (var region in regions)
        {
            donor.OperationalRegions.Add(new DonorOperationalRegion { DonorId = donorId, OperationalRegionId = regionIds[region] });
        }

        return donor;
    }
}
