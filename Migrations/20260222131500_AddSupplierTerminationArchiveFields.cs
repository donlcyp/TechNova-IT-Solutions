using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TechNova_IT_Solutions.Data;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260222131500_AddSupplierTerminationArchiveFields")]
    /// <inheritdoc />
    public partial class AddSupplierTerminationArchiveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "terminated_at",
                table: "Suppliers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "terminated_by_user_id",
                table: "Suppliers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "termination_reason",
                table: "Suppliers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "terminated_at",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "terminated_by_user_id",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "termination_reason",
                table: "Suppliers");
        }
    }
}
