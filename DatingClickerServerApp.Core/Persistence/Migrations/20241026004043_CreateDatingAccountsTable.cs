using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingClickerServerApp.Common.Migrations
{
    /// <inheritdoc />
    public partial class CreateDatingAccountsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DatingAccountId",
                table: "DatingUserActions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DatingAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AppUserId = table.Column<string>(type: "text", nullable: false),
                    AppName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    JsonAuthData = table.Column<string>(type: "text", nullable: false),
                    JsonProfileData = table.Column<JsonElement>(type: "jsonb", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatingAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatingUserActions_DatingAccountId",
                table: "DatingUserActions",
                column: "DatingAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_DatingUserActions_DatingAccounts_DatingAccountId",
                table: "DatingUserActions",
                column: "DatingAccountId",
                principalTable: "DatingAccounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatingUserActions_DatingAccounts_DatingAccountId",
                table: "DatingUserActions");

            migrationBuilder.DropTable(
                name: "DatingAccounts");

            migrationBuilder.DropIndex(
                name: "IX_DatingUserActions_DatingAccountId",
                table: "DatingUserActions");

            migrationBuilder.DropColumn(
                name: "DatingAccountId",
                table: "DatingUserActions");
        }
    }
}
