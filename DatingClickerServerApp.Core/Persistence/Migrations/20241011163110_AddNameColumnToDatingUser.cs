using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingClickerServerApp.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddNameColumnToDatingUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "DatingUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                @"
                    UPDATE ""DatingUsers""
                    SET ""Name"" = ""JsonData""->>'name'
                    WHERE ""JsonData"" IS NOT NULL
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "DatingUsers");
        }
    }
}
