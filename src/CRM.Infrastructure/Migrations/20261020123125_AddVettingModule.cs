using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVettingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vetting_officers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    initials = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    assigned_region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    workload_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vetting_officers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vetting_site_visits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    donor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    officer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: false),
                    scheduled_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    location_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    visit_notes = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    visit_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vetting_site_visits", x => x.id);
                    table.ForeignKey(
                        name: "FK_vetting_site_visits_donors_donor_id",
                        column: x => x.donor_id,
                        principalTable: "donors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vetting_site_visits_vetting_officers_officer_id",
                        column: x => x.officer_id,
                        principalTable: "vetting_officers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vetting_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_reference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    donor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    officer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_visit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    admin_decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    decision_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vetting_submissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_vetting_submissions_donors_donor_id",
                        column: x => x.donor_id,
                        principalTable: "donors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vetting_submissions_vetting_officers_officer_id",
                        column: x => x.officer_id,
                        principalTable: "vetting_officers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vetting_submissions_vetting_site_visits_site_visit_id",
                        column: x => x.site_visit_id,
                        principalTable: "vetting_site_visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vetting_assessment_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    contact_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    website = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    address_physical = table.Column<string>(type: "text", nullable: true),
                    address_detail = table.Column<string>(type: "text", nullable: true),
                    province = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    what3words = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    core_focus_areas = table.Column<string>(type: "jsonb", nullable: false),
                    target_population = table.Column<string>(type: "jsonb", nullable: false),
                    programmes_summary = table.Column<string>(type: "text", nullable: false),
                    distribution_channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_time_females = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    full_time_males = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    volunteers_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_registered_npo = table.Column<bool>(type: "boolean", nullable: false),
                    npo_certificate_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_registered_dsd = table.Column<bool>(type: "boolean", nullable: false),
                    pbo_certificate_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    beneficiary_races = table.Column<string>(type: "jsonb", nullable: false),
                    beneficiary_genders = table.Column<string>(type: "jsonb", nullable: false),
                    beneficiary_age_groups = table.Column<string>(type: "jsonb", nullable: false),
                    feeding_frequency = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    total_people_served = table.Column<int>(type: "integer", nullable: false),
                    females_served = table.Column<int>(type: "integer", nullable: false),
                    males_served = table.Column<int>(type: "integer", nullable: false),
                    african_served = table.Column<int>(type: "integer", nullable: false),
                    coloured_served = table.Column<int>(type: "integer", nullable: false),
                    indian_asian_served = table.Column<int>(type: "integer", nullable: false),
                    white_served = table.Column<int>(type: "integer", nullable: false),
                    sa_harvest_reliance = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    transport_capacity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    meals_provided = table.Column<string>(type: "jsonb", nullable: false),
                    feeding_days = table.Column<string>(type: "jsonb", nullable: false),
                    last_fed_date = table.Column<DateOnly>(type: "date", nullable: true),
                    food_storage_types = table.Column<string>(type: "jsonb", nullable: false),
                    kitchen_storage_photos_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    kitchen_clean = table.Column<bool>(type: "boolean", nullable: false),
                    water_access = table.Column<bool>(type: "boolean", nullable: false),
                    toilet_facilities = table.Column<bool>(type: "boolean", nullable: false),
                    pest_free = table.Column<bool>(type: "boolean", nullable: false),
                    infrastructure_checks = table.Column<string>(type: "jsonb", nullable: false),
                    delivery_access_ease = table.Column<bool>(type: "boolean", nullable: false),
                    parking_and_security = table.Column<bool>(type: "boolean", nullable: false),
                    police_station_proximity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    officer_comments = table.Column<string>(type: "text", nullable: true),
                    proposal_capabilities = table.Column<string>(type: "text", nullable: true),
                    digital_capabilities = table.Column<string>(type: "text", nullable: true),
                    facility_photos_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    sla_signed = table.Column<bool>(type: "boolean", nullable: false),
                    consent_form_signed = table.Column<bool>(type: "boolean", nullable: false),
                    policy_document_present = table.Column<bool>(type: "boolean", nullable: false),
                    certificates_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vetting_assessment_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_vetting_assessment_details_vetting_submissions_submission_id",
                        column: x => x.submission_id,
                        principalTable: "vetting_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vetting_evidence_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shot_index = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_captured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    file_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    blob_storage_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    captured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vetting_evidence_photos", x => x.id);
                    table.CheckConstraint("chk_vetting_evidence_photos_shot_index", "shot_index BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_vetting_evidence_photos_vetting_submissions_submission_id",
                        column: x => x.submission_id,
                        principalTable: "vetting_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vetting_flags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vetting_flags", x => x.id);
                    table.ForeignKey(
                        name: "FK_vetting_flags_vetting_submissions_submission_id",
                        column: x => x.submission_id,
                        principalTable: "vetting_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_vetting_assessment_details_submission",
                table: "vetting_assessment_details",
                column: "submission_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_vetting_evidence_photos_submission_shot",
                table: "vetting_evidence_photos",
                columns: new[] { "submission_id", "shot_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vetting_flags_submission_id",
                table: "vetting_flags",
                column: "submission_id");

            migrationBuilder.CreateIndex(
                name: "idx_vetting_site_visits_date_officer",
                table: "vetting_site_visits",
                columns: new[] { "scheduled_date", "officer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_vetting_site_visits_donor_id",
                table: "vetting_site_visits",
                column: "donor_id");

            migrationBuilder.CreateIndex(
                name: "IX_vetting_site_visits_officer_id",
                table: "vetting_site_visits",
                column: "officer_id");

            migrationBuilder.CreateIndex(
                name: "idx_vetting_submissions_approval_state",
                table: "vetting_submissions",
                column: "approval_state");

            migrationBuilder.CreateIndex(
                name: "idx_vetting_submissions_donor",
                table: "vetting_submissions",
                column: "donor_id");

            migrationBuilder.CreateIndex(
                name: "idx_vetting_submissions_form_reference",
                table: "vetting_submissions",
                column: "form_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_vetting_submissions_officer",
                table: "vetting_submissions",
                column: "officer_id");

            migrationBuilder.CreateIndex(
                name: "IX_vetting_submissions_site_visit_id",
                table: "vetting_submissions",
                column: "site_visit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vetting_assessment_details");

            migrationBuilder.DropTable(
                name: "vetting_evidence_photos");

            migrationBuilder.DropTable(
                name: "vetting_flags");

            migrationBuilder.DropTable(
                name: "vetting_submissions");

            migrationBuilder.DropTable(
                name: "vetting_site_visits");

            migrationBuilder.DropTable(
                name: "vetting_officers");
        }
    }
}
