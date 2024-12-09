using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingClickerServerApp.Common.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatingUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ExternalId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    Age = table.Column<int>(type: "integer", nullable: true),
                    HasChildren = table.Column<bool>(type: "boolean", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    PreviewUrl = table.Column<string>(type: "text", nullable: false),
                    About = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Interests = table.Column<string>(type: "text", nullable: false),
                    CityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    JsonData = table.Column<JsonElement>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatingUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatingUserActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    DatingUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatingUserActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatingUserActions_DatingUsers_DatingUserId",
                        column: x => x.DatingUserId,
                        principalTable: "DatingUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatingUserActions_DatingUserId",
                table: "DatingUserActions",
                column: "DatingUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DatingUsers_ExternalId",
                table: "DatingUsers",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatingUserActions");

            migrationBuilder.DropTable(
                name: "DatingUsers");
        }
    }
}
