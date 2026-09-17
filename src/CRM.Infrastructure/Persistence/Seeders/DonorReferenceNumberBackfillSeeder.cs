namespace CRM.Infrastructure.Persistence.Seeders;

using System.Text.RegularExpressions;
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
/// today). Numbering for each year continues after whatever the HIGHEST
/// already-issued number for that year is — not just "how many legacy rows are
/// in this batch" — because a real "DON-{year}-NNNNN" number can already exist
/// for that year (an earlier real submission, an import, or a prior partial
/// run of this seeder) even while other rows for the same year are still
/// legacy placeholders. Numbering purely from the batch size would either
/// collide with one of those existing numbers (unique index violation) or,
/// worse, leave the sequence set BELOW a number that's already in use. The
/// sequence itself is only ever advanced via GREATEST(current, target), never
/// set backwards, for the same reason.
///
/// Idempotent: only rows still carrying the "DON-LEGACY-" placeholder are
/// touched, so — like the other seeders — this is safe to run on every startup.
/// On a fresh database (no pre-existing donors) this is a no-op.
/// </summary>
public static class DonorReferenceNumberBackfillSeeder
{
    private const string LegacyPrefix = "DON-LEGACY-";

    private static readonly Regex RealReferenceNumberPattern =
        new(@"^DON-(?<year>\d{4})-(?<sequence>\d{5})$", RegexOptions.Compiled);

    public static async Task SeedAsync(CrmDbContext context)
    {
        var pending = await context.Donors
            .Where(d => d.ReferenceNumber.StartsWith(LegacyPrefix))
            .OrderBy(d => d.CreatedAt)
            .ToListAsync();

        if (pending.Count == 0)
            return;

        // Every already-real (non-legacy) reference number in the table, so the
        // numbers this pass assigns — and the sequence value it leaves behind —
        // can never collide with or regress behind one that already exists for
        // the same year, from any source.
        var existingRealNumbers = await context.Donors
            .Where(d => !d.ReferenceNumber.StartsWith(LegacyPrefix))
            .Select(d => d.ReferenceNumber)
            .ToListAsync();

        var maxSequenceByYear = existingRealNumbers
            .Select(n => RealReferenceNumberPattern.Match(n))
            .Where(m => m.Success)
            .GroupBy(m => int.Parse(m.Groups["year"].Value))
            .ToDictionary(g => g.Key, g => g.Max(m => int.Parse(m.Groups["sequence"].Value)));

        foreach (var yearGroup in pending.GroupBy(d => d.CreatedAt.Year).OrderBy(g => g.Key))
        {
            var year = yearGroup.Key;
            var ordered = yearGroup.OrderBy(d => d.CreatedAt).ToList();

            var nextSequence = maxSequenceByYear.GetValueOrDefault(year, 0) + 1;
            foreach (var donor in ordered)
            {
                donor.ReferenceNumber = $"DON-{year}-{nextSequence:D5}";
                nextSequence++;
            }
            var highestAssigned = nextSequence - 1;

            // The InMemory provider (CRM.API.Tests) can't execute raw SQL — same
            // constraint as IDonorRepository.GetNextReferenceNumberAsync/
            // ClaimBySubmissionTokenAsync. Skipping sequence sync there is safe:
            // those tests never call GetNextReferenceNumberAsync against
            // pre-seeded legacy donors in the same run.
            if (context.Database.IsNpgsql())
            {
                var sequenceName = $"donor_reference_seq_{year}";

                // Identifiers can't be bound SQL parameters in any dialect, so
                // this needs raw interpolation — safe here because sequenceName
                // is built only from an int (the donor's own CreatedAt.Year),
                // never user input. Mirrors GetNextReferenceNumberAsync's
                // identical lazy-create pattern.
#pragma warning disable EF1002
                await context.Database.ExecuteSqlRawAsync(
                    $"CREATE SEQUENCE IF NOT EXISTS {sequenceName} START 1");

                // GREATEST(last_value, highestAssigned) — never move the
                // sequence backward. If real submissions already advanced it
                // past highestAssigned (e.g. this year's sequence already
                // issued numbers ahead of what a small legacy batch computes),
                // setting it down to highestAssigned would let a future
                // nextval() reissue an already-used number. last_value reads
                // the sequence's own current position directly and works even
                // if nextval() was never called in this session (unlike
                // currval()). highestAssigned is computed only from ints
                // above, so raw interpolation for both values here is safe —
                // same justification as CREATE SEQUENCE above.
                await context.Database.ExecuteSqlRawAsync(
                    $"SELECT setval('{sequenceName}', GREATEST((SELECT last_value FROM {sequenceName}), {highestAssigned}))");
#pragma warning restore EF1002
            }
        }

        await context.SaveChangesAsync();
    }
}
