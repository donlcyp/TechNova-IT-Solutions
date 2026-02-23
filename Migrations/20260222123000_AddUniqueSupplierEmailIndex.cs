using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TechNova_IT_Solutions.Data;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260222123000_AddUniqueSupplierEmailIndex")]
    /// <inheritdoc />
    public partial class AddUniqueSupplierEmailIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_email",
                table: "Suppliers",
                column: "email",
                unique: true,
                filter: "[email] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suppliers_email",
                table: "Suppliers");
        }
    }
}
