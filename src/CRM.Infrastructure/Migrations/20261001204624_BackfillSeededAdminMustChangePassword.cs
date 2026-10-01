using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillSeededAdminMustChangePassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Databases seeded before must_change_password existed still have the bootstrap admin
            // on its original password; AdminUserSeeder never touches an existing admin, so flag it
            // here. Before this change the seeder always used this exact address. Ordinary users
            // keep the column default (false).
            migrationBuilder.Sql(
                "UPDATE users SET must_change_password = TRUE WHERE lower(email) = 'admin@crm.local';");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
