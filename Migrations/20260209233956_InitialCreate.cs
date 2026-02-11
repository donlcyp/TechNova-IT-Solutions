using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    supplierID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    supplier_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    contact_person_fname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    contact_person_lname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    contact_person_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.supplierID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    userID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    firstname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    lastname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.userID);
                });

            migrationBuilder.CreateTable(
                name: "Audit_Logs",
                columns: table => new
                {
                    logID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userID = table.Column<int>(type: "int", nullable: true),
                    action = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    logdate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audit_Logs", x => x.logID);
                    table.ForeignKey(
                        name: "FK_Audit_Logs_Users_userID",
                        column: x => x.userID,
                        principalTable: "Users",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Policies",
                columns: table => new
                {
                    policyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    policy_title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    file_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    uploaded_by = table.Column<int>(type: "int", nullable: true),
                    date_uploaded = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policies", x => x.policyID);
                    table.ForeignKey(
                        name: "FK_Policies_Users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "Users",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Policy_Assignments",
                columns: table => new
                {
                    assignmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    policyID = table.Column<int>(type: "int", nullable: false),
                    userID = table.Column<int>(type: "int", nullable: false),
                    assigned_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policy_Assignments", x => x.assignmentID);
                    table.ForeignKey(
                        name: "FK_Policy_Assignments_Policies_policyID",
                        column: x => x.policyID,
                        principalTable: "Policies",
                        principalColumn: "policyID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Policy_Assignments_Users_userID",
                        column: x => x.userID,
                        principalTable: "Users",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Procurement",
                columns: table => new
                {
                    procurementID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    item_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    quantity = table.Column<int>(type: "int", nullable: true),
                    supplierID = table.Column<int>(type: "int", nullable: true),
                    related_policyID = table.Column<int>(type: "int", nullable: true),
                    purchase_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Procurement", x => x.procurementID);
                    table.ForeignKey(
                        name: "FK_Procurement_Policies_related_policyID",
                        column: x => x.related_policyID,
                        principalTable: "Policies",
                        principalColumn: "policyID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Procurement_Suppliers_supplierID",
                        column: x => x.supplierID,
                        principalTable: "Suppliers",
                        principalColumn: "supplierID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Supplier_Policies",
                columns: table => new
                {
                    supplier_policiesID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    supplierID = table.Column<int>(type: "int", nullable: false),
                    policyID = table.Column<int>(type: "int", nullable: false),
                    assigned_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    compliance_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Policies", x => x.supplier_policiesID);
                    table.ForeignKey(
                        name: "FK_Supplier_Policies_Policies_policyID",
                        column: x => x.policyID,
                        principalTable: "Policies",
                        principalColumn: "policyID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Supplier_Policies_Suppliers_supplierID",
                        column: x => x.supplierID,
                        principalTable: "Suppliers",
                        principalColumn: "supplierID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Compliance_Status",
                columns: table => new
                {
                    complianceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    assignmentID = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    acknowledged_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compliance_Status", x => x.complianceID);
                    table.ForeignKey(
                        name: "FK_Compliance_Status_Policy_Assignments_assignmentID",
                        column: x => x.assignmentID,
                        principalTable: "Policy_Assignments",
                        principalColumn: "assignmentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "userID", "email", "firstname", "lastname", "password", "role", "status" },
                values: new object[] { 1, "admin@technova.com", "System", "Administrator", "Admin@123", "Admin", "Active" });

            migrationBuilder.CreateIndex(
                name: "IX_Audit_Logs_userID",
                table: "Audit_Logs",
                column: "userID");

            migrationBuilder.CreateIndex(
                name: "IX_Compliance_Status_assignmentID",
                table: "Compliance_Status",
                column: "assignmentID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Policies_uploaded_by",
                table: "Policies",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "IX_Policy_Assignments_policyID_userID",
                table: "Policy_Assignments",
                columns: new[] { "policyID", "userID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Policy_Assignments_userID",
                table: "Policy_Assignments",
                column: "userID");

            migrationBuilder.CreateIndex(
                name: "IX_Procurement_related_policyID",
                table: "Procurement",
                column: "related_policyID");

            migrationBuilder.CreateIndex(
                name: "IX_Procurement_supplierID",
                table: "Procurement",
                column: "supplierID");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Policies_policyID",
                table: "Supplier_Policies",
                column: "policyID");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Policies_supplierID_policyID",
                table: "Supplier_Policies",
                columns: new[] { "supplierID", "policyID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_email",
                table: "Users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Audit_Logs");

            migrationBuilder.DropTable(
                name: "Compliance_Status");

            migrationBuilder.DropTable(
                name: "Procurement");

            migrationBuilder.DropTable(
                name: "Supplier_Policies");

            migrationBuilder.DropTable(
                name: "Policy_Assignments");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Policies");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
