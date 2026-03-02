using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class SplitAdminRolesAddSupplierContractSeverity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Data migration: split "Admin" role ──
            // Admins WITH a branchId become "BranchAdmin"; without become "SystemAdmin"
            migrationBuilder.Sql(
                "UPDATE Users SET role = 'BranchAdmin' WHERE role = 'Admin' AND branchId IS NOT NULL");
            migrationBuilder.Sql(
                "UPDATE Users SET role = 'SystemAdmin' WHERE role = 'Admin' AND branchId IS NULL");

            // ── Make all suppliers global (remove branch_id) ──
            migrationBuilder.Sql(
                "UPDATE Suppliers SET branch_id = NULL WHERE branch_id IS NOT NULL");

            migrationBuilder.AddColumn<string>(
                name: "severity_level",
                table: "compliance_violations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Supplier_Contracts",
                columns: table => new
                {
                    contractID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    supplierID = table.Column<int>(type: "int", nullable: false),
                    linked_policyID = table.Column<int>(type: "int", nullable: true),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    risk_level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    approved_by = table.Column<int>(type: "int", nullable: true),
                    approval_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Contracts", x => x.contractID);
                    table.ForeignKey(
                        name: "FK_Supplier_Contracts_Policies_linked_policyID",
                        column: x => x.linked_policyID,
                        principalTable: "Policies",
                        principalColumn: "policyID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Supplier_Contracts_Suppliers_supplierID",
                        column: x => x.supplierID,
                        principalTable: "Suppliers",
                        principalColumn: "supplierID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Supplier_Contracts_Users_approved_by",
                        column: x => x.approved_by,
                        principalTable: "Users",
                        principalColumn: "userID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Contracts_approved_by",
                table: "Supplier_Contracts",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Contracts_linked_policyID",
                table: "Supplier_Contracts",
                column: "linked_policyID");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Contracts_supplierID",
                table: "Supplier_Contracts",
                column: "supplierID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Supplier_Contracts");

            migrationBuilder.DropColumn(
                name: "severity_level",
                table: "compliance_violations");

            // ── Reverse role split: merge back to "Admin" ──
            migrationBuilder.Sql(
                "UPDATE Users SET role = 'Admin' WHERE role IN ('SystemAdmin', 'BranchAdmin')");
        }
    }
}
