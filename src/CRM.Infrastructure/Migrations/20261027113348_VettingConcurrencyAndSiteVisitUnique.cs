using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VettingConcurrencyAndSiteVisitUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vetting_submissions_site_visit_id",
                table: "vetting_submissions");

            // xmin is a Postgres system column that already exists on every table; the model only maps it.

            migrationBuilder.CreateIndex(
                name: "idx_vetting_submissions_site_visit",
                table: "vetting_submissions",
                column: "site_visit_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_vetting_submissions_site_visit",
                table: "vetting_submissions");

            migrationBuilder.CreateIndex(
                name: "IX_vetting_submissions_site_visit_id",
                table: "vetting_submissions",
                column: "site_visit_id");
        }
    }
}
