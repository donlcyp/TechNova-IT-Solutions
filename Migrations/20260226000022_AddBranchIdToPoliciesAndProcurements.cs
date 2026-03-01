using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchIdToPoliciesAndProcurements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "branch_id",
                table: "Procurement",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "branch_id",
                table: "Policies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Procurement_branch_id",
                table: "Procurement",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_branch_id",
                table: "Policies",
                column: "branch_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Policies_Branches_branch_id",
                table: "Policies",
                column: "branch_id",
                principalTable: "Branches",
                principalColumn: "branchId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Procurement_Branches_branch_id",
                table: "Procurement",
                column: "branch_id",
                principalTable: "Branches",
                principalColumn: "branchId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Policies_Branches_branch_id",
                table: "Policies");

            migrationBuilder.DropForeignKey(
                name: "FK_Procurement_Branches_branch_id",
                table: "Procurement");

            migrationBuilder.DropIndex(
                name: "IX_Procurement_branch_id",
                table: "Procurement");

            migrationBuilder.DropIndex(
                name: "IX_Policies_branch_id",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "Procurement");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "Policies");
        }
    }
}
