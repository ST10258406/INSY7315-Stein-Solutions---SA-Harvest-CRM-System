using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lookup_bbbee_statuses",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_bbbee_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lookup_company_types",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_company_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lookup_donation_frequencies",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_donation_frequencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lookup_donation_types",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_donation_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lookup_entity_types",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_entity_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lookup_operational_regions",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_operational_regions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lookup_provinces",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_provinces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "donors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    company_type_id = table.Column<short>(type: "smallint", nullable: false),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    registered_company_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    trading_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    entity_type_id = table.Column<short>(type: "smallint", nullable: false),
                    company_registration_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    income_tax_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    donation_frequency_id = table.Column<short>(type: "smallint", nullable: false),
                    bbbee_status_id = table.Column<short>(type: "smallint", nullable: true),
                    collection_address = table.Column<string>(type: "text", nullable: true),
                    operations_logistics_details = table.Column<string>(type: "text", nullable: true),
                    additional_information = table.Column<string>(type: "text", nullable: true),
                    relationship_manager_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    submission_source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    marketing_consent = table.Column<bool>(type: "boolean", nullable: false),
                    marketing_consent_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    impact_reporting_preferences = table.Column<string>(type: "text", nullable: true),
                    follow_up_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    foodspace_company_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donors", x => x.id);
                    table.CheckConstraint("chk_donors_tax_number", "income_tax_number NOT LIKE '4%'");
                    table.ForeignKey(
                        name: "FK_donors_lookup_bbbee_statuses_bbbee_status_id",
                        column: x => x.bbbee_status_id,
                        principalTable: "lookup_bbbee_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_donors_lookup_company_types_company_type_id",
                        column: x => x.company_type_id,
                        principalTable: "lookup_company_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_donors_lookup_donation_frequencies_donation_frequency_id",
                        column: x => x.donation_frequency_id,
                        principalTable: "lookup_donation_frequencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_donors_lookup_entity_types_entity_type_id",
                        column: x => x.entity_type_id,
                        principalTable: "lookup_entity_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_donors_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_donors_users_relationship_manager_id",
                        column: x => x.relationship_manager_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notification_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    related_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    related_entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assigned_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_roles_users_assigned_by_user_id",
                        column: x => x.assigned_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "donor_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    donor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donor_approvals", x => x.id);
                    table.ForeignKey(
                        name: "FK_donor_approvals_donors_donor_id",
                        column: x => x.donor_id,
                        principalTable: "donors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_donor_approvals_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_donor_approvals_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "donor_contacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    donor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    job_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donor_contacts", x => x.id);
                    table.ForeignKey(
                        name: "FK_donor_contacts_donors_donor_id",
                        column: x => x.donor_id,
                        principalTable: "donors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "donor_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    donor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    blob_storage_path = table.Column<string>(type: "text", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donor_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_donor_documents_donors_donor_id",
                        column: x => x.donor_id,
                        principalTable: "donors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_donor_documents_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "donor_donation_types",
                columns: table => new
                {
                    donor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    donation_type_id = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donor_donation_types", x => new { x.donor_id, x.donation_type_id });
                    table.ForeignKey(
                        name: "FK_donor_donation_types_donors_donor_id",
                        column: x => x.donor_id,
                        principalTable: "donors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_donor_donation_types_lookup_donation_types_donation_type_id",
                        column: x => x.donation_type_id,
                        principalTable: "lookup_donation_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "donor_legal_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    donor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    street_name_number = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    suburb = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    province_id = table.Column<short>(type: "smallint", nullable: false),
                    postal_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donor_legal_addresses", x => x.id);
                    table.ForeignKey(
                        name: "FK_donor_legal_addresses_donors_donor_id",
                        column: x => x.donor_id,
                        principalTable: "donors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_donor_legal_addresses_lookup_provinces_province_id",
                        column: x => x.province_id,
                        principalTable: "lookup_provinces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "donor_operational_regions",
                columns: table => new
                {
                    donor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operational_region_id = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donor_operational_regions", x => new { x.donor_id, x.operational_region_id });
                    table.ForeignKey(
                        name: "FK_donor_operational_regions_donors_donor_id",
                        column: x => x.donor_id,
                        principalTable: "donors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_donor_operational_regions_lookup_operational_regions_operat~",
                        column: x => x.operational_region_id,
                        principalTable: "lookup_operational_regions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "donor_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    donor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donor_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_donor_tasks_donors_donor_id",
                        column: x => x.donor_id,
                        principalTable: "donors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_donor_tasks_users_assigned_to_user_id",
                        column: x => x.assigned_to_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_donor_tasks_users_completed_by_user_id",
                        column: x => x.completed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_donor_tasks_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "interaction_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    donor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    interaction_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    body = table.Column<string>(type: "text", nullable: false),
                    email_attachment_url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interaction_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_interaction_logs_donors_donor_id",
                        column: x => x.donor_id,
                        principalTable: "donors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_interaction_logs_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "lookup_bbbee_statuses",
                columns: new[] { "id", "description", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { (short)1, null, true, "Level 1", (short)1 },
                    { (short)2, null, true, "Level 2", (short)2 },
                    { (short)3, null, true, "Level 3", (short)3 },
                    { (short)4, null, true, "Level 4", (short)4 },
                    { (short)5, null, true, "Level 5", (short)5 },
                    { (short)6, null, true, "Level 6", (short)6 },
                    { (short)7, null, true, "Level 7", (short)7 },
                    { (short)8, null, true, "Level 8", (short)8 },
                    { (short)9, null, true, "Exempt Micro Enterprise", (short)9 },
                    { (short)10, null, true, "Non-Compliant", (short)10 },
                    { (short)11, null, true, "Not Applicable", (short)11 }
                });

            migrationBuilder.InsertData(
                table: "lookup_company_types",
                columns: new[] { "id", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { (short)1, true, "Manufacturer", (short)1 },
                    { (short)2, true, "Farmer", (short)2 },
                    { (short)3, true, "Distributor", (short)3 },
                    { (short)4, true, "Retailer", (short)4 },
                    { (short)5, true, "Packhouse", (short)5 },
                    { (short)6, true, "Financial", (short)6 },
                    { (short)7, true, "Market", (short)7 },
                    { (short)8, true, "Prepared Food", (short)8 },
                    { (short)9, true, "Butcher", (short)9 },
                    { (short)10, true, "Dairy", (short)10 },
                    { (short)11, true, "Broker", (short)11 },
                    { (short)12, true, "Mill", (short)12 },
                    { (short)13, true, "Cold Storage", (short)13 },
                    { (short)14, true, "Packaging", (short)14 },
                    { (short)15, true, "Other", (short)15 }
                });

            migrationBuilder.InsertData(
                table: "lookup_donation_frequencies",
                columns: new[] { "id", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { (short)1, true, "Ad Hoc", (short)1 },
                    { (short)2, true, "Once-off", (short)2 },
                    { (short)3, true, "Weekly", (short)3 },
                    { (short)4, true, "Monthly", (short)4 },
                    { (short)5, true, "Seasonal", (short)5 }
                });

            migrationBuilder.InsertData(
                table: "lookup_donation_types",
                columns: new[] { "id", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { (short)1, true, "Bakery", (short)1 },
                    { (short)2, true, "Beverages", (short)2 },
                    { (short)3, true, "Dairy", (short)3 },
                    { (short)4, true, "Dry Goods", (short)4 },
                    { (short)5, true, "Financial", (short)5 },
                    { (short)6, true, "Fruit", (short)6 },
                    { (short)7, true, "Meat", (short)7 },
                    { (short)8, true, "Non Food", (short)8 },
                    { (short)9, true, "Prepared Food", (short)9 },
                    { (short)10, true, "Vegetables", (short)10 },
                    { (short)11, true, "Other", (short)11 }
                });

            migrationBuilder.InsertData(
                table: "lookup_entity_types",
                columns: new[] { "id", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { (short)1, true, "Private Company", (short)1 },
                    { (short)2, true, "Public Company", (short)2 },
                    { (short)3, true, "Close Corporation", (short)3 },
                    { (short)4, true, "Trust", (short)4 },
                    { (short)5, true, "Non Profit", (short)5 },
                    { (short)6, true, "Association", (short)6 }
                });

            migrationBuilder.InsertData(
                table: "lookup_operational_regions",
                columns: new[] { "id", "code", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { (short)1, "JHB", true, "Johannesburg", (short)1 },
                    { (short)2, "CPT", true, "Cape Town", (short)2 },
                    { (short)3, "KZN", true, "KwaZulu-Natal", (short)3 },
                    { (short)4, "EC", true, "Eastern Cape", (short)4 },
                    { (short)5, "BFN", true, "Bloemfontein", (short)5 },
                    { (short)6, "MPU", true, "Mpumalanga", (short)6 },
                    { (short)7, "LIM", true, "Limpopo", (short)7 }
                });

            migrationBuilder.InsertData(
                table: "lookup_provinces",
                columns: new[] { "id", "code", "is_active", "name", "sort_order" },
                values: new object[,]
                {
                    { (short)1, "EC", true, "Eastern Cape", (short)1 },
                    { (short)2, "FS", true, "Free State", (short)2 },
                    { (short)3, "GP", true, "Gauteng", (short)3 },
                    { (short)4, "KZN", true, "KwaZulu-Natal", (short)4 },
                    { (short)5, "LP", true, "Limpopo", (short)5 },
                    { (short)6, "MP", true, "Mpumalanga", (short)6 },
                    { (short)7, "NC", true, "Northern Cape", (short)7 },
                    { (short)8, "NW", true, "North West", (short)8 },
                    { (short)9, "WC", true, "Western Cape", (short)9 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_donor_approvals_donor_id",
                table: "donor_approvals",
                column: "donor_id");

            migrationBuilder.CreateIndex(
                name: "IX_donor_approvals_requested_by_user_id",
                table: "donor_approvals",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_donor_approvals_reviewed_by_user_id",
                table: "donor_approvals",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_donor_contacts_donor_id_contact_type",
                table: "donor_contacts",
                columns: new[] { "donor_id", "contact_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_donor_documents_donor_id",
                table: "donor_documents",
                column: "donor_id");

            migrationBuilder.CreateIndex(
                name: "IX_donor_documents_uploaded_by_user_id",
                table: "donor_documents",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_donor_donation_types_donation_type_id",
                table: "donor_donation_types",
                column: "donation_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_donor_legal_addresses_donor_id",
                table: "donor_legal_addresses",
                column: "donor_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_donor_legal_addresses_province_id",
                table: "donor_legal_addresses",
                column: "province_id");

            migrationBuilder.CreateIndex(
                name: "IX_donor_operational_regions_operational_region_id",
                table: "donor_operational_regions",
                column: "operational_region_id");

            migrationBuilder.CreateIndex(
                name: "IX_donor_tasks_assigned_to_user_id",
                table: "donor_tasks",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_donor_tasks_completed_by_user_id",
                table: "donor_tasks",
                column: "completed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_donor_tasks_created_by_user_id",
                table: "donor_tasks",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_donor_tasks_donor_id",
                table: "donor_tasks",
                column: "donor_id");

            migrationBuilder.CreateIndex(
                name: "idx_donors_company_name",
                table: "donors",
                column: "company_name");

            migrationBuilder.CreateIndex(
                name: "idx_donors_follow_up_date",
                table: "donors",
                column: "follow_up_date");

            migrationBuilder.CreateIndex(
                name: "idx_donors_foodspace_id",
                table: "donors",
                column: "foodspace_company_id",
                filter: "foodspace_company_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_donors_relationship_manager",
                table: "donors",
                column: "relationship_manager_id");

            migrationBuilder.CreateIndex(
                name: "idx_donors_status",
                table: "donors",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_donors_bbbee_status_id",
                table: "donors",
                column: "bbbee_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_donors_company_type_id",
                table: "donors",
                column: "company_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_donors_created_by_user_id",
                table: "donors",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_donors_donation_frequency_id",
                table: "donors",
                column: "donation_frequency_id");

            migrationBuilder.CreateIndex(
                name: "IX_donors_entity_type_id",
                table: "donors",
                column: "entity_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_interaction_logs_created_by_user_id",
                table: "interaction_logs",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_interaction_logs_donor_id",
                table: "interaction_logs",
                column: "donor_id");

            migrationBuilder.CreateIndex(
                name: "IX_lookup_bbbee_statuses_name",
                table: "lookup_bbbee_statuses",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_company_types_name",
                table: "lookup_company_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_donation_frequencies_name",
                table: "lookup_donation_frequencies",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_donation_types_name",
                table: "lookup_donation_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_entity_types_name",
                table: "lookup_entity_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_operational_regions_code",
                table: "lookup_operational_regions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_operational_regions_name",
                table: "lookup_operational_regions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_provinces_code",
                table: "lookup_provinces",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_provinces_name",
                table: "lookup_provinces",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_assigned_by_user_id",
                table: "user_roles",
                column: "assigned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "donor_approvals");

            migrationBuilder.DropTable(
                name: "donor_contacts");

            migrationBuilder.DropTable(
                name: "donor_documents");

            migrationBuilder.DropTable(
                name: "donor_donation_types");

            migrationBuilder.DropTable(
                name: "donor_legal_addresses");

            migrationBuilder.DropTable(
                name: "donor_operational_regions");

            migrationBuilder.DropTable(
                name: "donor_tasks");

            migrationBuilder.DropTable(
                name: "interaction_logs");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "lookup_donation_types");

            migrationBuilder.DropTable(
                name: "lookup_provinces");

            migrationBuilder.DropTable(
                name: "lookup_operational_regions");

            migrationBuilder.DropTable(
                name: "donors");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "lookup_bbbee_statuses");

            migrationBuilder.DropTable(
                name: "lookup_company_types");

            migrationBuilder.DropTable(
                name: "lookup_donation_frequencies");

            migrationBuilder.DropTable(
                name: "lookup_entity_types");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
