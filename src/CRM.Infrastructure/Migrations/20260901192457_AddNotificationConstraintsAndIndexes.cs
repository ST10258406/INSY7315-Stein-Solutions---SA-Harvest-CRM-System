using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationConstraintsAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "notifications");

            migrationBuilder.RenameIndex(
                name: "IX_notifications_user_id",
                table: "notifications",
                newName: "idx_notifications_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_created_at",
                table: "notifications",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_notifications_unread",
                table: "notifications",
                column: "is_read",
                filter: "is_read = false");

            migrationBuilder.AddCheckConstraint(
                name: "chk_notifications_type",
                table: "notifications",
                sql: "notification_type IN ('FollowUpReminder', 'NewDonorPendingReview', 'TaskDue', 'TaskAssigned', 'DonorApproved', 'DonorRejected')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_notifications_created_at",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "idx_notifications_unread",
                table: "notifications");

            migrationBuilder.DropCheckConstraint(
                name: "chk_notifications_type",
                table: "notifications");

            migrationBuilder.RenameIndex(
                name: "idx_notifications_user_id",
                table: "notifications",
                newName: "IX_notifications_user_id");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
