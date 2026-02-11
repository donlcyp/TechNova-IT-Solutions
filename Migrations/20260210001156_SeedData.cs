using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "userID", "email", "firstname", "lastname", "password", "role", "status" },
                values: new object[,]
                {
                    { 1, "admin@technova.com", "System", "Administrator", "Admin@123", "Admin", "Active" },
                    { 2, "sarah.johnson@technova.com", "Sarah", "Johnson", "Compliance@123", "ComplianceManager", "Active" },
                    { 3, "michael.chen@technova.com", "Michael", "Chen", "Compliance@123", "ComplianceManager", "Active" },
                    { 4, "emma.williams@technova.com", "Emma", "Williams", "Employee@123", "Employee", "Active" },
                    { 5, "james.brown@technova.com", "James", "Brown", "Employee@123", "Employee", "Active" },
                    { 6, "olivia.martinez@technova.com", "Olivia", "Martinez", "Employee@123", "Employee", "Active" },
                    { 7, "william.davis@technova.com", "William", "Davis", "Employee@123", "Employee", "Active" },
                    { 8, "sophia.garcia@technova.com", "Sophia", "Garcia", "Employee@123", "Employee", "Active" },
                    { 9, "liam.rodriguez@technova.com", "Liam", "Rodriguez", "Employee@123", "Employee", "Inactive" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 9);
        }
    }
}
