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

            // The bare minimum non-generated statement this schema change needs to
            // be applicable at all: reference_number defaults to '' above so
            // existing rows satisfy the incoming NOT NULL constraint, but the
            // unique index right after this would fail immediately if two or more
            // pre-existing donors were both left at ''. This gives every
            // pre-existing row a distinct placeholder derived from its own
            // (already-unique) id, so the index below can be created. It does
            // NOT attempt the "nice" DON-{year}-NNNNN format or touch the
            // donor_reference_seq_* sequences — that data backfill lives in
            // DonorReferenceNumberBackfillSeeder (CRM.Infrastructure/Persistence/
            // Seeders), run as ordinary idempotent app code at startup instead of
            // migration SQL, so it stays testable/regenerable/reviewable like the
            // rest of the seeders rather than living in a migration's Up(). On a
            // fresh database (no existing donors) this UPDATE matches zero rows.
            migrationBuilder.Sql(
                "UPDATE donors SET reference_number = 'DON-LEGACY-' || substr(md5(id::text), 1, 8) " +
                "WHERE reference_number = '';");

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
