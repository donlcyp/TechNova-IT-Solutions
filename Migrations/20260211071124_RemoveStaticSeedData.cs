using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStaticSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Audit_Logs",
                keyColumn: "logID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Audit_Logs",
                keyColumn: "logID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Audit_Logs",
                keyColumn: "logID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Audit_Logs",
                keyColumn: "logID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Audit_Logs",
                keyColumn: "logID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Audit_Logs",
                keyColumn: "logID",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Audit_Logs",
                keyColumn: "logID",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Audit_Logs",
                keyColumn: "logID",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Audit_Logs",
                keyColumn: "logID",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Audit_Logs",
                keyColumn: "logID",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Compliance_Status",
                keyColumn: "complianceID",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Procurement",
                keyColumn: "procurementID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Procurement",
                keyColumn: "procurementID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Procurement",
                keyColumn: "procurementID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Procurement",
                keyColumn: "procurementID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Procurement",
                keyColumn: "procurementID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Supplier_Policies",
                keyColumn: "supplier_policiesID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Supplier_Policies",
                keyColumn: "supplier_policiesID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Supplier_Policies",
                keyColumn: "supplier_policiesID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Supplier_Policies",
                keyColumn: "supplier_policiesID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Supplier_Policies",
                keyColumn: "supplier_policiesID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Policies",
                keyColumn: "policyID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Policy_Assignments",
                keyColumn: "assignmentID",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "supplierID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "supplierID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "supplierID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "supplierID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Policies",
                keyColumn: "policyID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Policies",
                keyColumn: "policyID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Policies",
                keyColumn: "policyID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Policies",
                keyColumn: "policyID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Policies",
                keyColumn: "policyID",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Audit_Logs",
                columns: new[] { "logID", "action", "logdate", "module", "userID" },
                values: new object[,]
                {
                    { 1, "Login", new DateTime(2026, 2, 10, 8, 30, 0, 0, DateTimeKind.Unspecified), "Authentication", 1 },
                    { 3, "Create Policy", new DateTime(2026, 2, 10, 9, 15, 0, 0, DateTimeKind.Unspecified), "Policy Management", 1 },
                    { 10, "Approve Procurement", new DateTime(2026, 2, 10, 13, 0, 0, 0, DateTimeKind.Unspecified), "Procurement", 1 }
                });

            migrationBuilder.InsertData(
                table: "Policies",
                columns: new[] { "policyID", "category", "date_uploaded", "description", "file_path", "policy_title", "uploaded_by" },
                values: new object[,]
                {
                    { 1, "Data Privacy", new DateTime(2025, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Comprehensive policy covering GDPR and data protection requirements for handling customer and employee data.", "/policies/data-privacy-policy.pdf", "Data Privacy and Protection Policy", 1 },
                    { 2, "Security", new DateTime(2025, 1, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Guidelines for maintaining information security, including password management, access controls, and incident reporting.", "/policies/info-security-policy.pdf", "Information Security Policy", 1 },
                    { 5, "Procurement", new DateTime(2025, 1, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Standards and expectations for supplier behavior, including ethical practices and compliance requirements.", "/policies/supplier-code-conduct.pdf", "Supplier Code of Conduct", 1 }
                });

            migrationBuilder.InsertData(
                table: "Suppliers",
                columns: new[] { "supplierID", "address", "contact_person_fname", "contact_person_lname", "contact_person_number", "email", "status", "supplier_name" },
                values: new object[,]
                {
                    { 1, "123 Tech Boulevard, Silicon Valley, CA 94025", "Robert", "Anderson", "+1-555-0101", "robert.anderson@cloudtech.com", "Active", "CloudTech Solutions Inc" },
                    { 2, "456 Security Street, Boston, MA 02101", "Jennifer", "Lee", "+1-555-0102", "jennifer.lee@securedata.com", "Active", "SecureData Corp" },
                    { 3, "789 Enterprise Drive, Austin, TX 78701", "David", "Kumar", "+1-555-0103", "david.kumar@globalit.com", "Active", "Global IT Services Ltd" },
                    { 4, "321 Hardware Lane, Seattle, WA 98101", "Maria", "Santos", "+1-555-0104", "maria.santos@techhardware.com", "Inactive", "TechHardware Supplies" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "userID", "email", "firstname", "lastname", "password", "role", "status" },
                values: new object[,]
                {
                    { 2, "sarah.johnson@technova.com", "Sarah", "Johnson", "Compliance@123", "ComplianceManager", "Active" },
                    { 3, "michael.chen@technova.com", "Michael", "Chen", "Compliance@123", "ComplianceManager", "Active" },
                    { 4, "emma.williams@technova.com", "Emma", "Williams", "Employee@123", "Employee", "Active" },
                    { 5, "james.brown@technova.com", "James", "Brown", "Employee@123", "Employee", "Active" },
                    { 6, "olivia.martinez@technova.com", "Olivia", "Martinez", "Employee@123", "Employee", "Active" },
                    { 7, "william.davis@technova.com", "William", "Davis", "Employee@123", "Employee", "Active" },
                    { 8, "sophia.garcia@technova.com", "Sophia", "Garcia", "Employee@123", "Employee", "Active" },
                    { 9, "liam.rodriguez@technova.com", "Liam", "Rodriguez", "Employee@123", "Employee", "Inactive" }
                });

            migrationBuilder.InsertData(
                table: "Audit_Logs",
                columns: new[] { "logID", "action", "logdate", "module", "userID" },
                values: new object[,]
                {
                    { 2, "Login", new DateTime(2026, 2, 10, 8, 45, 0, 0, DateTimeKind.Unspecified), "Authentication", 2 },
                    { 4, "Login", new DateTime(2026, 2, 10, 9, 30, 0, 0, DateTimeKind.Unspecified), "Authentication", 4 },
                    { 5, "Login", new DateTime(2026, 2, 10, 9, 35, 0, 0, DateTimeKind.Unspecified), "Authentication", 5 },
                    { 6, "Acknowledge Policy", new DateTime(2026, 2, 10, 10, 0, 0, 0, DateTimeKind.Unspecified), "Compliance", 4 },
                    { 7, "Assign Policy", new DateTime(2026, 2, 10, 10, 30, 0, 0, DateTimeKind.Unspecified), "Policy Management", 2 },
                    { 8, "Login", new DateTime(2026, 2, 10, 11, 0, 0, 0, DateTimeKind.Unspecified), "Authentication", 3 },
                    { 9, "Review Compliance", new DateTime(2026, 2, 10, 11, 30, 0, 0, DateTimeKind.Unspecified), "Supplier Management", 3 }
                });

            migrationBuilder.InsertData(
                table: "Policies",
                columns: new[] { "policyID", "category", "date_uploaded", "description", "file_path", "policy_title", "uploaded_by" },
                values: new object[,]
                {
                    { 3, "HR", new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Professional conduct standards and ethical guidelines for all TechNova employees.", "/policies/code-of-conduct.pdf", "Code of Conduct", 2 },
                    { 4, "HR", new DateTime(2025, 2, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "Guidelines and requirements for employees working remotely, including security and communication protocols.", "/policies/remote-work-policy.pdf", "Remote Work Policy", 2 },
                    { 6, "Operations", new DateTime(2025, 2, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Framework for ensuring business operations continue during disruptions and emergencies.", "/policies/business-continuity-policy.pdf", "Business Continuity Policy", 3 }
                });

            migrationBuilder.InsertData(
                table: "Policy_Assignments",
                columns: new[] { "assignmentID", "assigned_date", "policyID", "userID" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 4 },
                    { 2, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 4 },
                    { 4, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 5 },
                    { 5, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 5 },
                    { 8, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 6 },
                    { 9, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 6 },
                    { 11, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 7 },
                    { 13, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 8 },
                    { 14, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 8 }
                });

            migrationBuilder.InsertData(
                table: "Procurement",
                columns: new[] { "procurementID", "category", "item_name", "purchase_date", "quantity", "related_policyID", "supplierID" },
                values: new object[,]
                {
                    { 1, "Cloud Services", "Cloud Infrastructure Services - Annual Subscription", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 5, 1 },
                    { 2, "Software", "Cybersecurity Software Licenses", new DateTime(2026, 1, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), 100, 2, 2 },
                    { 3, "Hardware", "Laptop Computers - Dell XPS 15", new DateTime(2026, 2, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 25, null, 4 },
                    { 5, "Services", "IT Support Services - Quarterly Contract", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, null, 3 }
                });

            migrationBuilder.InsertData(
                table: "Supplier_Policies",
                columns: new[] { "supplier_policiesID", "assigned_date", "compliance_status", "policyID", "supplierID" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Compliant", 5, 1 },
                    { 2, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Compliant", 5, 2 },
                    { 3, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Compliant", 1, 2 },
                    { 4, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Pending", 5, 3 },
                    { 5, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Non-Compliant", 5, 4 }
                });

            migrationBuilder.InsertData(
                table: "Compliance_Status",
                columns: new[] { "complianceID", "acknowledged_date", "assignmentID", "status" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 2, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Acknowledged" },
                    { 2, new DateTime(2025, 2, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Acknowledged" },
                    { 4, new DateTime(2025, 2, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Acknowledged" },
                    { 5, new DateTime(2025, 2, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Acknowledged" },
                    { 8, new DateTime(2025, 2, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Acknowledged" },
                    { 9, null, 9, "Overdue" },
                    { 11, new DateTime(2025, 2, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), 11, "Acknowledged" },
                    { 13, new DateTime(2025, 2, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 13, "Acknowledged" },
                    { 14, new DateTime(2025, 2, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 14, "Acknowledged" }
                });

            migrationBuilder.InsertData(
                table: "Policy_Assignments",
                columns: new[] { "assignmentID", "assigned_date", "policyID", "userID" },
                values: new object[,]
                {
                    { 3, new DateTime(2025, 2, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, 4 },
                    { 6, new DateTime(2025, 2, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, 5 },
                    { 7, new DateTime(2026, 2, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, 5 },
                    { 10, new DateTime(2026, 2, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, 6 },
                    { 12, new DateTime(2025, 2, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, 7 },
                    { 15, new DateTime(2025, 2, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, 8 },
                    { 16, new DateTime(2026, 2, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, 8 }
                });

            migrationBuilder.InsertData(
                table: "Procurement",
                columns: new[] { "procurementID", "category", "item_name", "purchase_date", "quantity", "related_policyID", "supplierID" },
                values: new object[] { 4, "Cloud Services", "Data Backup and Recovery Services", new DateTime(2026, 2, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 6, 1 });

            migrationBuilder.InsertData(
                table: "Compliance_Status",
                columns: new[] { "complianceID", "acknowledged_date", "assignmentID", "status" },
                values: new object[,]
                {
                    { 3, null, 3, "Pending" },
                    { 6, new DateTime(2025, 2, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Acknowledged" },
                    { 7, null, 7, "Pending" },
                    { 10, null, 10, "Pending" },
                    { 12, null, 12, "Pending" },
                    { 15, new DateTime(2025, 2, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 15, "Acknowledged" },
                    { 16, null, 16, "Pending" }
                });
        }
    }
}
