using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class ClearAllDataExceptAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete data from all tables except Users
            // Order matters due to foreign key constraints
            
            migrationBuilder.Sql("DELETE FROM Compliance_Status");
            migrationBuilder.Sql("DELETE FROM Policy_Assignments");
            migrationBuilder.Sql("DELETE FROM Supplier_Policies");
            migrationBuilder.Sql("DELETE FROM Procurement");
            migrationBuilder.Sql("DELETE FROM Audit_Logs");
            migrationBuilder.Sql("DELETE FROM Suppliers");
            migrationBuilder.Sql("DELETE FROM Policies");
            
            // Reset identity seeds for cleared tables
            migrationBuilder.Sql("DBCC CHECKIDENT ('Compliance_Status', RESEED, 0)");
            migrationBuilder.Sql("DBCC CHECKIDENT ('Policy_Assignments', RESEED, 0)");
            migrationBuilder.Sql("DBCC CHECKIDENT ('Supplier_Policies', RESEED, 0)");
            migrationBuilder.Sql("DBCC CHECKIDENT ('Procurement', RESEED, 0)");
            migrationBuilder.Sql("DBCC CHECKIDENT ('Audit_Logs', RESEED, 0)");
            migrationBuilder.Sql("DBCC CHECKIDENT ('Suppliers', RESEED, 0)");
            migrationBuilder.Sql("DBCC CHECKIDENT ('Policies', RESEED, 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This cannot be reversed - data deletion is permanent
            // Consider creating a backup before running this migration
        }
    }
}
