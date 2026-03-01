using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class SplitManagerNameAddManagerEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "managerEmail",
                table: "Branches",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "managerFirstName",
                table: "Branches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "managerLastName",
                table: "Branches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Migrate existing managerName data: first word → FirstName, rest → LastName
            migrationBuilder.Sql(@"
                UPDATE [Branches]
                SET [managerFirstName] = CASE
                        WHEN CHARINDEX(' ', LTRIM(RTRIM([managerName]))) > 0
                            THEN LEFT(LTRIM(RTRIM([managerName])), CHARINDEX(' ', LTRIM(RTRIM([managerName]))) - 1)
                        ELSE LTRIM(RTRIM([managerName]))
                    END,
                    [managerLastName] = CASE
                        WHEN CHARINDEX(' ', LTRIM(RTRIM([managerName]))) > 0
                            THEN LTRIM(SUBSTRING(LTRIM(RTRIM([managerName])), CHARINDEX(' ', LTRIM(RTRIM([managerName]))) + 1, LEN([managerName])))
                        ELSE NULL
                    END
                WHERE [managerName] IS NOT NULL AND LTRIM(RTRIM([managerName])) <> '';
            ");

            migrationBuilder.DropColumn(
                name: "managerName",
                table: "Branches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "managerEmail",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "managerFirstName",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "managerLastName",
                table: "Branches");

            migrationBuilder.AddColumn<string>(
                name: "managerName",
                table: "Branches",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }
    }
}
