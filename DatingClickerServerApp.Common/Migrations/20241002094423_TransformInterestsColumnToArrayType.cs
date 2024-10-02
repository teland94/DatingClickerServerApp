using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingClickerServerApp.Common.Migrations
{
    /// <inheritdoc />
    public partial class TransformInterestsColumnToArrayType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 0: Ensure TempInterests column exists
            migrationBuilder.AddColumn<string[]>(
                name: "TempInterests",
                table: "DatingUsers",
                type: "text[]",
                nullable: true);

            // Step 1: Check if the table DatingUsers has any data
            migrationBuilder.Sql(
                @"
                    DO $$
                    BEGIN
                        IF EXISTS (SELECT 1 FROM ""DatingUsers"" LIMIT 1) THEN
                            -- Step 2: Copy data from Interests to TempInterests, converting comma-separated string to array
                            UPDATE ""DatingUsers""
                            SET ""TempInterests"" = string_to_array(""Interests"", ',')
                            WHERE ""Interests"" IS NOT NULL;

                            -- Step 3: Drop the Interests column
                            ALTER TABLE ""DatingUsers"" DROP COLUMN ""Interests"";

                            -- Step 4: Rename TempInterests to Interests
                            ALTER TABLE ""DatingUsers"" RENAME COLUMN ""TempInterests"" TO ""Interests"";

                            -- Step 5: Update the configuration if needed
                            ALTER TABLE ""DatingUsers"" ALTER COLUMN ""Interests"" SET DATA TYPE text[];
                        ELSE
                            -- Create the Interests column directly as an array
                            ALTER TABLE ""DatingUsers"" ADD COLUMN ""Interests"" text[] NULL;
                        END IF;
                    END
                $$;
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step to revert the migration if needed
            migrationBuilder.Sql(
                @"
                    DO $$
                    BEGIN
                        -- Step 1: Rename Interests to TempInterests
                        ALTER TABLE ""DatingUsers"" RENAME COLUMN ""Interests"" TO ""TempInterests"";

                        -- Step 2: Add Interests column as text
                        ALTER TABLE ""DatingUsers"" ADD COLUMN ""Interests"" text NOT NULL DEFAULT '';

                        -- Step 3: Copy data from TempInterests to Interests, converting array to comma-separated string
                        UPDATE ""DatingUsers""
                        SET ""Interests"" = array_to_string(""TempInterests"", ',')
                        WHERE ""TempInterests"" IS NOT NULL;

                        -- Step 4: Drop the TempInterests column
                        ALTER TABLE ""DatingUsers"" DROP COLUMN ""TempInterests"";
                    END
                $$;
                ");
        }
    }
}
