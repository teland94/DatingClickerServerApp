using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingClickerServerApp.Common.Migrations
{
    /// <inheritdoc />
    public partial class CreateBlacklistedDatingUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlacklistedDatingUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlacklistedDatingUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlacklistedDatingUsers_DatingUsers_Id",
                        column: x => x.Id,
                        principalTable: "DatingUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlacklistedDatingUsers");
        }
    }
}
