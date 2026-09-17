namespace CRM.Infrastructure.Persistence.Seeders;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Finishes the reference_number backfill that migration
/// AddDonorReferenceNumberAndSubmissionToken's Up() deliberately leaves half
/// done: that migration only assigns pre-existing donor rows a unique
/// "DON-LEGACY-{8 hex chars}" placeholder (the bare minimum needed for its
/// unique index to be creatable at all), and does not touch the
/// donor_reference_seq_* sequences. This finishes the job as ordinary,
/// idempotent, testable application code instead of hand-written PL/pgSQL
/// embedded in a migration — see that migration's remarks for why.
///
/// Reassigns each placeholder row a real "DON-{year}-{5-digit sequence}" number
/// (grouped and ordered by the donor's own CreatedAt, per year, so a backfilled
/// number reads like a genuine historical reference rather than one issued
/// today), then advances that year's real Postgres sequence past the highest
/// number just assigned — so the next donor created via
/// IDonorRepository.GetNextReferenceNumberAsync can never collide with one
/// assigned here.
///
/// Idempotent: only rows still carrying the "DON-LEGACY-" placeholder are
/// touched, so — like the other seeders — this is safe to run on every startup.
/// On a fresh database (no pre-existing donors) this is a no-op.
/// </summary>
public static class DonorReferenceNumberBackfillSeeder
{
    private const string LegacyPrefix = "DON-LEGACY-";

    public static async Task SeedAsync(CrmDbContext context)
    {
        var pending = await context.Donors
            .Where(d => d.ReferenceNumber.StartsWith(LegacyPrefix))
            .OrderBy(d => d.CreatedAt)
            .ToListAsync();

        if (pending.Count == 0)
            return;

        foreach (var yearGroup in pending.GroupBy(d => d.CreatedAt.Year).OrderBy(g => g.Key))
        {
            var year = yearGroup.Key;
            var ordered = yearGroup.OrderBy(d => d.CreatedAt).ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i].ReferenceNumber = $"DON-{year}-{(i + 1):D5}";
            }

            // The InMemory provider (CRM.API.Tests) can't execute raw SQL — same
            // constraint as IDonorRepository.GetNextReferenceNumberAsync/
            // ClaimBySubmissionTokenAsync. Skipping sequence sync there is safe:
            // those tests never call GetNextReferenceNumberAsync against
            // pre-seeded legacy donors in the same run.
            if (context.Database.IsNpgsql())
            {
                var sequenceName = $"donor_reference_seq_{year}";

                // Identifiers can't be bound SQL parameters in any dialect, so this
                // one statement needs raw interpolation — safe here because
                // sequenceName is built only from an int (the donor's own
                // CreatedAt.Year), never user input. Mirrors
                // GetNextReferenceNumberAsync's identical lazy-create pattern.
#pragma warning disable EF1002
                await context.Database.ExecuteSqlRawAsync(
                    $"CREATE SEQUENCE IF NOT EXISTS {sequenceName} START 1");
#pragma warning restore EF1002

                // setval()'s first argument is a regclass, which (like nextval())
                // accepts a plain text value, so — unlike CREATE SEQUENCE above —
                // this can go through proper parameterization instead of raw
                // interpolation.
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT setval({sequenceName}, {ordered.Count})");
            }
        }

        await context.SaveChangesAsync();
    }
}
