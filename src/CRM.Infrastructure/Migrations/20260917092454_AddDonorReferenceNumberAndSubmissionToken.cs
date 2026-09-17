using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDonorReferenceNumberAndSubmissionToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "reference_number",
                table: "donors",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "submission_token",
                table: "donors",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "submission_token_expires_at",
                table: "donors",
                type: "timestamp with time zone",
                nullable: true);

            // Data backfill, not a schema change: reference_number defaults to ''
            // above so existing rows satisfy the NOT NULL constraint being added,
            // but the unique index right after this would fail immediately if two
            // or more pre-existing donors were both left at ''. This assigns each
            // existing donor a real "DON-{year}-{5-digit sequence}" number (ordered
            // by created_at, per year), then creates/advances that year's sequence
            // to continue right after the highest backfilled number — so the next
            // donor created via IDonorRepository.GetNextReferenceNumberAsync can't
            // collide with one just assigned here. On a fresh database (no existing
            // donors) this is a no-op.
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    r RECORD;
                    seq_name TEXT;
                BEGIN
                    WITH numbered AS (
                        SELECT id,
                               EXTRACT(YEAR FROM created_at)::int AS yr,
                               ROW_NUMBER() OVER (PARTITION BY EXTRACT(YEAR FROM created_at) ORDER BY created_at) AS seq
                        FROM donors
                        WHERE reference_number = ''
                    )
                    UPDATE donors d
                    SET reference_number = 'DON-' || n.yr || '-' || LPAD(n.seq::text, 5, '0')
                    FROM numbered n
                    WHERE d.id = n.id;

                    FOR r IN
                        SELECT EXTRACT(YEAR FROM created_at)::int AS yr, COUNT(*) AS cnt
                        FROM donors
                        GROUP BY EXTRACT(YEAR FROM created_at)
                    LOOP
                        seq_name := 'donor_reference_seq_' || r.yr;
                        EXECUTE format('CREATE SEQUENCE IF NOT EXISTS %I START 1', seq_name);
                        EXECUTE format('SELECT setval(%L, %s)', seq_name, r.cnt);
                    END LOOP;
                END $$;
            ");

            migrationBuilder.CreateIndex(
                name: "idx_donors_reference_number",
                table: "donors",
                column: "reference_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_donors_submission_token",
                table: "donors",
                column: "submission_token",
                unique: true,
                filter: "submission_token IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_donors_reference_number",
                table: "donors");

            migrationBuilder.DropIndex(
                name: "idx_donors_submission_token",
                table: "donors");

            migrationBuilder.DropColumn(
                name: "reference_number",
                table: "donors");

            migrationBuilder.DropColumn(
                name: "submission_token",
                table: "donors");

            migrationBuilder.DropColumn(
                name: "submission_token_expires_at",
                table: "donors");
        }
    }
}
