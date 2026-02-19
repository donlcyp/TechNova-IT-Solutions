using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    public partial class ProcurementWorkflowEnhancements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "Procurement",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_date",
                table: "Procurement",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "shipment_date",
                table: "Procurement",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "Procurement",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<DateTime>(
                name: "supplier_commit_ship_date",
                table: "Procurement",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "supplier_response_date",
                table: "Procurement",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "supplier_response_deadline",
                table: "Procurement",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Supplier_Items",
                columns: table => new
                {
                    supplier_itemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    supplierID = table.Column<int>(type: "int", nullable: false),
                    item_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                name: "IX_Procurement_Status_History_procurementID",
                table: "Procurement_Status_History",
                column: "procurementID");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Items_supplierID_item_name",
                table: "Supplier_Items",
                columns: new[] { "supplierID", "item_name" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Procurement_Status_History");

            migrationBuilder.DropTable(
                name: "Supplier_Items");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "Procurement");

            migrationBuilder.DropColumn(
                name: "received_date",
                table: "Procurement");

            migrationBuilder.DropColumn(
                name: "shipment_date",
                table: "Procurement");

            migrationBuilder.DropColumn(
                name: "status",
                table: "Procurement");

            migrationBuilder.DropColumn(
                name: "supplier_commit_ship_date",
                table: "Procurement");

            migrationBuilder.DropColumn(
                name: "supplier_response_date",
                table: "Procurement");

            migrationBuilder.DropColumn(
                name: "supplier_response_deadline",
                table: "Procurement");
        }
    }
}
