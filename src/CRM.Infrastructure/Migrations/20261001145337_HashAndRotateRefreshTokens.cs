using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HashAndRotateRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "refresh_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GraceReplayedAt",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplacedByTokenHash",
                table: "refresh_tokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevokedAt",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_FamilyId",
                table: "refresh_tokens",
                column: "FamilyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_FamilyId",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "GraceReplayedAt",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "ReplacedByTokenHash",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "refresh_tokens");
        }
    }
}
