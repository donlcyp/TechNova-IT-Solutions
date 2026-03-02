using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pending_category",
                table: "Policies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_description",
                table: "Policies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_file_path",
                table: "Policies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_title",
                table: "Policies",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pending_updated_at",
                table: "Policies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pending_updated_by",
                table: "Policies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "review_notes",
                table: "Policies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "review_status",
                table: "Policies",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "Policies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reviewed_by",
                table: "Policies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Policies_reviewed_by",
                table: "Policies",
                column: "reviewed_by");

            migrationBuilder.AddForeignKey(
                name: "FK_Policies_Users_reviewed_by",
                table: "Policies",
                column: "reviewed_by",
                principalTable: "Users",
                principalColumn: "userID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Policies_Users_reviewed_by",
                table: "Policies");

            migrationBuilder.DropIndex(
                name: "IX_Policies_reviewed_by",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "pending_category",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "pending_description",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "pending_file_path",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "pending_title",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "pending_updated_at",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "pending_updated_by",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "review_notes",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "review_status",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "reviewed_by",
                table: "Policies");
        }
    }
}
