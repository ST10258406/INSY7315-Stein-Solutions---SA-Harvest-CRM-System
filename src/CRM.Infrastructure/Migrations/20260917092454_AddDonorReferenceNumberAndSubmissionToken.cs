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
            // reference_number is added NOT NULL with a per-row computed
            // DEFAULT so every pre-existing donor row gets a distinct,
            // non-colliding placeholder as part of this single ALTER TABLE —
            // Postgres rewrites the table and evaluates a volatile DEFAULT
            // expression separately for each existing row (it cannot reference
            // another column such as id, so gen_random_uuid() stands in for
            // "give me something unique per row" instead). The AlterColumn
            // right after this drops that default again so it only ever
            // applied to the rows that existed at migration time — every donor
            // created afterward must supply a real value (there is deliberately
            // no lingering fallback default for new inserts to silently rely
            // on). Both calls are ordinary generated-shape MigrationBuilder
            // operations; nothing here is a hand-written migrationBuilder.Sql()
            // statement. The placeholder itself ("DON-LEGACY-{8 hex chars}") is
            // not the final "DON-{year}-NNNNN" format — DonorReferenceNumberBackfillSeeder
            // (CRM.Infrastructure/Persistence/Seeders) finishes that as ordinary,
            // idempotent, testable application code at startup instead of migration
            // SQL. On a fresh database (no existing donors) the DEFAULT never
            // actually executes for any row.
            migrationBuilder.AddColumn<string>(
                name: "reference_number",
                table: "donors",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "('DON-LEGACY-' || substr(replace(gen_random_uuid()::text, '-', ''), 1, 8))");

            migrationBuilder.AlterColumn<string>(
                name: "reference_number",
                table: "donors",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValueSql: "('DON-LEGACY-' || substr(replace(gen_random_uuid()::text, '-', ''), 1, 8))");

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
