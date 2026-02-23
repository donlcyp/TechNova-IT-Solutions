using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalPolicyImportWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "External_Policy_Imports",
                columns: table => new
                {
                    importID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    source_api = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    document_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    external_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    policy_title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    publication_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    review_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    imported_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    review_notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    imported_by_user_id = table.Column<int>(type: "int", nullable: true),
                    reviewed_by_user_id = table.Column<int>(type: "int", nullable: true),
                    approved_policy_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_External_Policy_Imports", x => x.importID);
                    table.ForeignKey(
                        name: "FK_External_Policy_Imports_Policies_approved_policy_id",
                        column: x => x.approved_policy_id,
                        principalTable: "Policies",
                        principalColumn: "policyID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_External_Policy_Imports_Users_imported_by_user_id",
                        column: x => x.imported_by_user_id,
                        principalTable: "Users",
                        principalColumn: "userID");
                    table.ForeignKey(
                        name: "FK_External_Policy_Imports_Users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "Users",
                        principalColumn: "userID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_External_Policy_Imports_approved_policy_id",
                table: "External_Policy_Imports",
                column: "approved_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_External_Policy_Imports_imported_by_user_id",
                table: "External_Policy_Imports",
                column: "imported_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_External_Policy_Imports_review_status",
                table: "External_Policy_Imports",
                column: "review_status");

            migrationBuilder.CreateIndex(
                name: "IX_External_Policy_Imports_reviewed_by_user_id",
                table: "External_Policy_Imports",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_External_Policy_Imports_source_api_document_number",
                table: "External_Policy_Imports",
                columns: new[] { "source_api", "document_number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "External_Policy_Imports");
        }
    }
}
