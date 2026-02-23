using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class FixExternalPolicyImportUserFkDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_External_Policy_Imports_Users_imported_by_user_id",
                table: "External_Policy_Imports");

            migrationBuilder.DropForeignKey(
                name: "FK_External_Policy_Imports_Users_reviewed_by_user_id",
                table: "External_Policy_Imports");

            migrationBuilder.AddForeignKey(
                name: "FK_External_Policy_Imports_Users_imported_by_user_id",
                table: "External_Policy_Imports",
                column: "imported_by_user_id",
                principalTable: "Users",
                principalColumn: "userID");

            migrationBuilder.AddForeignKey(
                name: "FK_External_Policy_Imports_Users_reviewed_by_user_id",
                table: "External_Policy_Imports",
                column: "reviewed_by_user_id",
                principalTable: "Users",
                principalColumn: "userID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_External_Policy_Imports_Users_imported_by_user_id",
                table: "External_Policy_Imports");

            migrationBuilder.DropForeignKey(
                name: "FK_External_Policy_Imports_Users_reviewed_by_user_id",
                table: "External_Policy_Imports");

            migrationBuilder.AddForeignKey(
                name: "FK_External_Policy_Imports_Users_imported_by_user_id",
                table: "External_Policy_Imports",
                column: "imported_by_user_id",
                principalTable: "Users",
                principalColumn: "userID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_External_Policy_Imports_Users_reviewed_by_user_id",
                table: "External_Policy_Imports",
                column: "reviewed_by_user_id",
                principalTable: "Users",
                principalColumn: "userID",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
