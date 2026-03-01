using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class AddComplianceViolations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "compliance_violations",
                columns: table => new
                {
                    violationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    assignment_id = table.Column<int>(type: "int", nullable: true),
                    supplier_policy_id = table.Column<int>(type: "int", nullable: true),
                    violation_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    resolution = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    raised_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    resolved_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    raised_by_user_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compliance_violations", x => x.violationID);
                    table.ForeignKey(
                        name: "FK_compliance_violations_Policy_Assignments_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "Policy_Assignments",
                        principalColumn: "assignmentID");
                    table.ForeignKey(
                        name: "FK_compliance_violations_Supplier_Policies_supplier_policy_id",
                        column: x => x.supplier_policy_id,
                        principalTable: "Supplier_Policies",
                        principalColumn: "supplier_policiesID");
                    table.ForeignKey(
                        name: "FK_compliance_violations_Users_raised_by_user_id",
                        column: x => x.raised_by_user_id,
                        principalTable: "Users",
                        principalColumn: "userID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_compliance_violations_assignment_id",
                table: "compliance_violations",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_compliance_violations_raised_by_user_id",
                table: "compliance_violations",
                column: "raised_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_compliance_violations_supplier_policy_id",
                table: "compliance_violations",
                column: "supplier_policy_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "compliance_violations");
        }
    }
}
