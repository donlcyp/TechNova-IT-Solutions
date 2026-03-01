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
                name: "Branches",
                columns: table => new
                {
                    branchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branchName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    city = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    managerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.branchId);
                });

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
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    termination_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    terminated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    terminated_by_user_id = table.Column<int>(type: "int", nullable: true),
                    branch_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.supplierID);
                    table.ForeignKey(
                        name: "FK_Suppliers_Branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "Branches",
                        principalColumn: "branchId",
                        onDelete: ReferentialAction.SetNull);
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
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    branchId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.userID);
                    table.ForeignKey(
                        name: "FK_Users_Branches_branchId",
                        column: x => x.branchId,
                        principalTable: "Branches",
                        principalColumn: "branchId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Supplier_Items",
                columns: table => new
                {
                    supplier_itemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    supplierID = table.Column<int>(type: "int", nullable: false),
                    item_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    unit_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    currency_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    quantity_available = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    last_updated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Items", x => x.supplier_itemID);
                    table.ForeignKey(
                        name: "FK_Supplier_Items_Suppliers_supplierID",
                        column: x => x.supplierID,
                        principalTable: "Suppliers",
                        principalColumn: "supplierID",
                        onDelete: ReferentialAction.Cascade);
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
                    date_uploaded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_archived = table.Column<bool>(type: "bit", nullable: false),
                    archived_date = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    purchase_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    currency_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    original_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    exchange_rate = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    converted_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    conversion_timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    supplier_response_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    supplier_response_deadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    supplier_commit_ship_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    revised_delivery_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    delay_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    shipment_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    received_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    rejection_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
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

            migrationBuilder.CreateTable(
                name: "Procurement_Status_History",
                columns: table => new
                {
                    historyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    procurementID = table.Column<int>(type: "int", nullable: false),
                    from_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    to_status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    changed_by_userID = table.Column<int>(type: "int", nullable: true),
                    changed_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Procurement_Status_History", x => x.historyID);
                    table.ForeignKey(
                        name: "FK_Procurement_Status_History_Procurement_procurementID",
                        column: x => x.procurementID,
                        principalTable: "Procurement",
                        principalColumn: "procurementID",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_Procurement_Status_History_procurementID",
                table: "Procurement_Status_History",
                column: "procurementID");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Items_supplierID_item_name",
                table: "Supplier_Items",
                columns: new[] { "supplierID", "item_name" },
                unique: true);

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
                name: "IX_Suppliers_branch_id",
                table: "Suppliers",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_email",
                table: "Suppliers",
                column: "email",
                unique: true,
                filter: "[email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_branchId",
                table: "Users",
                column: "branchId");

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
                name: "External_Policy_Imports");

            migrationBuilder.DropTable(
                name: "Procurement_Status_History");

            migrationBuilder.DropTable(
                name: "Supplier_Items");

            migrationBuilder.DropTable(
                name: "Supplier_Policies");

            migrationBuilder.DropTable(
                name: "Policy_Assignments");

            migrationBuilder.DropTable(
                name: "Procurement");

            migrationBuilder.DropTable(
                name: "Policies");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Branches");
        }
    }
}
