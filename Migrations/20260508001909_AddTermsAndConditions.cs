using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class AddTermsAndConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "terms_accepted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "terms_accepted_date",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "terms_accepted",
                table: "Suppliers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "terms_accepted_date",
                table: "Suppliers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    tokenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userId = table.Column<int>(type: "int", nullable: false),
                    token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    expiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    usedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    isUsed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.tokenId);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_userId",
                        column: x => x.userId,
                        principalTable: "Users",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_userId",
                table: "PasswordResetTokens",
                column: "userId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropColumn(
                name: "terms_accepted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "terms_accepted_date",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "terms_accepted",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "terms_accepted_date",
                table: "Suppliers");
        }
    }
}
