using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyArchiveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "archived_date",
                table: "Policies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_archived",
                table: "Policies",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "archived_date",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "is_archived",
                table: "Policies");
        }
    }
}
