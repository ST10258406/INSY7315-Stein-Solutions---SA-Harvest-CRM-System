using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Tests.Persistence;

/// <summary>A saved donor (plus the lookups and creator it needs) for the vetting FKs to point at.</summary>
internal static class VettingTestData
{
    public static async Task<Donor> AddDonorAsync(CrmDbContext context)
    {
        await LookupSeeder.SeedAsync(context);

        var creator = new User
        {
            Id = Guid.NewGuid(),
            Email = $"vetting-{Guid.NewGuid()}@example.com",
            FirstName = "Vetting",
            LastName = "Fixture",
            PasswordHash = "hash"
        };
        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            CompanyName = $"Vetted Co {Guid.NewGuid()}",
            ReferenceNumber = $"T-{Guid.NewGuid():N}"[..20],
            RegisteredCompanyName = "Vetted Co (Pty) Ltd",
            CompanyTypeId = await context.LookupCompanyTypes.Select(x => x.Id).FirstAsync(),
            EntityTypeId = await context.LookupEntityTypes.Select(x => x.Id).FirstAsync(),
            DonationFrequencyId = await context.LookupDonationFrequencies.Select(x => x.Id).FirstAsync(),
            Status = DonorStatus.PendingReview,
            SubmissionSource = SubmissionSource.ManualCapture,
            CreatedByUser = creator
        };
        context.Donors.Add(donor);
        await context.SaveChangesAsync();
        return donor;
    }
}
