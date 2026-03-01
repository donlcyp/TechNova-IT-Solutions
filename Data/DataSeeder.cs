using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Models;

namespace TechNova_IT_Solutions.Data
{
    /// <summary>
    /// Seeds the database with initial/sample data. Idempotent: only adds data when tables are empty.
    /// </summary>
    public static class DataSeeder
    {
        /// <summary>
        /// Default password for all seeded users (except Admin, which is in migration).
        /// </summary>
        public const string SeededUserPassword = "Admin@123";

        public static async Task SeedAsync(ApplicationDbContext context)
        {
            if (context == null) return;

            await SeedUsersAsync(context).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);

            await SeedPoliciesAsync(context).ConfigureAwait(false);
            await SeedSuppliersAsync(context).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);

            await SeedPolicyAssignmentsAsync(context).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);

            await SeedComplianceStatusesAsync(context).ConfigureAwait(false);
            await SeedSupplierPoliciesAsync(context).ConfigureAwait(false);
            await SeedSupplierItemsAsync(context).ConfigureAwait(false);
            await SeedProcurementsAsync(context).ConfigureAwait(false);
            await SeedAuditLogsAsync(context).ConfigureAwait(false);

            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        private static async Task SeedUsersAsync(ApplicationDbContext context)
        {
            var hasSeedUsers = await context.Users.CountAsync().ConfigureAwait(false) > 1;

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(SeededUserPassword);

            if (!await context.Users.AnyAsync(u => u.Email == "superadmin@technova.com").ConfigureAwait(false))
            {
                context.Users.Add(new User
                {
                    FirstName = "Super",
                    LastName = "Administrator",
                    Email = "superadmin@technova.com",
                    Password = hashedPassword,
                    Role = "SuperAdmin",
                    Status = "Active"
                });
            }

            if (hasSeedUsers)
                return;

            context.Users.Add(new User
            {
                FirstName = "Jane",
                LastName = "Compliance",
                Email = "compliance@technova.com",
                Password = hashedPassword,
                Role = "ComplianceManager",
                Status = "Active"
            });
            context.Users.Add(new User
            {
                FirstName = "John",
                LastName = "Employee",
                Email = "employee@technova.com",
                Password = hashedPassword,
                Role = "Employee",
                Status = "Active"
            });
        }

        private static async Task SeedPoliciesAsync(ApplicationDbContext context)
        {
            if (await context.Policies.AnyAsync().ConfigureAwait(false))
                return;

            var adminId = await context.Users.Where(u => u.Role == "Admin").Select(u => u.UserId).FirstOrDefaultAsync().ConfigureAwait(false);
            var uploadedBy = adminId > 0 ? adminId : (int?)null;
            var date = DateTime.UtcNow;

            context.Policies.Add(new Policy
            {
                PolicyTitle = "Information Security Policy",
                Description = "Guidelines for protecting company and customer data.",
                Category = "Security",
                UploadedBy = uploadedBy,
                DateUploaded = date.AddDays(-30)
            });
            context.Policies.Add(new Policy
            {
                PolicyTitle = "IT Supply Chain Security",
                Description = "Requirements for third-party software and hardware suppliers.",
                Category = "Compliance",
                UploadedBy = uploadedBy,
                DateUploaded = date.AddDays(-20)
            });
            context.Policies.Add(new Policy
            {
                PolicyTitle = "Acceptable Use Policy",
                Description = "Acceptable use of IT systems and resources.",
                Category = "HR",
                UploadedBy = uploadedBy,
                DateUploaded = date.AddDays(-10)
            });
        }

        private static async Task SeedSuppliersAsync(ApplicationDbContext context)
        {
            if (await context.Suppliers.AnyAsync().ConfigureAwait(false))
                return;

            context.Suppliers.Add(new Supplier
            {
                SupplierName = "SecureTech Solutions",
                ContactPersonFirstName = "Alice",
                ContactPersonLastName = "Smith",
                Email = "alice@securetech.com",
                ContactPersonNumber = "+1-555-0100",
                Address = "123 Tech Park, Austin TX",
                Status = "Active"
            });
            context.Suppliers.Add(new Supplier
            {
                SupplierName = "Global IT Supplies Inc",
                ContactPersonFirstName = "Bob",
                ContactPersonLastName = "Jones",
                Email = "bob@globalitsupplies.com",
                ContactPersonNumber = "+1-555-0200",
                Address = "456 Commerce Dr, Boston MA",
                Status = "Active"
            });
        }

        private static async Task SeedPolicyAssignmentsAsync(ApplicationDbContext context)
        {
            if (await context.PolicyAssignments.AnyAsync().ConfigureAwait(false))
                return;

            var complianceUserId = await context.Users.Where(u => u.Email == "compliance@technova.com").Select(u => u.UserId).FirstOrDefaultAsync().ConfigureAwait(false);
            var employeeUserId = await context.Users.Where(u => u.Email == "employee@technova.com").Select(u => u.UserId).FirstOrDefaultAsync().ConfigureAwait(false);
            var policyIds = await context.Policies.OrderBy(p => p.PolicyId).Select(p => p.PolicyId).Take(3).ToListAsync().ConfigureAwait(false);
            if (policyIds.Count < 3 || complianceUserId == 0 || employeeUserId == 0) return;

            var assigned = DateTime.UtcNow;
            context.PolicyAssignments.Add(new PolicyAssignment { PolicyId = policyIds[0], UserId = complianceUserId, AssignedDate = assigned.AddDays(-15) });
            context.PolicyAssignments.Add(new PolicyAssignment { PolicyId = policyIds[1], UserId = complianceUserId, AssignedDate = assigned.AddDays(-15) });
            context.PolicyAssignments.Add(new PolicyAssignment { PolicyId = policyIds[0], UserId = employeeUserId, AssignedDate = assigned.AddDays(-5) });
            context.PolicyAssignments.Add(new PolicyAssignment { PolicyId = policyIds[2], UserId = employeeUserId, AssignedDate = assigned.AddDays(-5) });
        }

        private static async Task SeedComplianceStatusesAsync(ApplicationDbContext context)
        {
            if (await context.ComplianceStatuses.AnyAsync().ConfigureAwait(false))
                return;

            var assignmentIds = await context.PolicyAssignments.OrderBy(a => a.AssignmentId).Select(a => a.AssignmentId).ToListAsync().ConfigureAwait(false);
            if (assignmentIds.Count < 4) return;

            context.ComplianceStatuses.Add(new ComplianceStatus { AssignmentId = assignmentIds[0], Status = "Acknowledged", AcknowledgedDate = DateTime.UtcNow.AddDays(-10) });
            context.ComplianceStatuses.Add(new ComplianceStatus { AssignmentId = assignmentIds[1], Status = "Acknowledged", AcknowledgedDate = DateTime.UtcNow.AddDays(-8) });
            context.ComplianceStatuses.Add(new ComplianceStatus { AssignmentId = assignmentIds[2], Status = "Pending", AcknowledgedDate = null });
            context.ComplianceStatuses.Add(new ComplianceStatus { AssignmentId = assignmentIds[3], Status = "Pending", AcknowledgedDate = null });
        }

        private static async Task SeedSupplierPoliciesAsync(ApplicationDbContext context)
        {
            if (await context.SupplierPolicies.AnyAsync().ConfigureAwait(false))
                return;

            var supplierIds = await context.Suppliers.OrderBy(s => s.SupplierId).Select(s => s.SupplierId).Take(2).ToListAsync().ConfigureAwait(false);
            var policyIds = await context.Policies.OrderBy(p => p.PolicyId).Select(p => p.PolicyId).Take(2).ToListAsync().ConfigureAwait(false);
            if (supplierIds.Count < 2 || policyIds.Count < 2) return;

            var assigned = DateTime.UtcNow;
            context.SupplierPolicies.Add(new SupplierPolicy { SupplierId = supplierIds[0], PolicyId = policyIds[0], AssignedDate = assigned.AddDays(-25), ComplianceStatus = "Compliant" });
            context.SupplierPolicies.Add(new SupplierPolicy { SupplierId = supplierIds[0], PolicyId = policyIds[1], AssignedDate = assigned.AddDays(-25), ComplianceStatus = "Pending" });
            context.SupplierPolicies.Add(new SupplierPolicy { SupplierId = supplierIds[1], PolicyId = policyIds[0], AssignedDate = assigned.AddDays(-18), ComplianceStatus = "Compliant" });
            context.SupplierPolicies.Add(new SupplierPolicy { SupplierId = supplierIds[1], PolicyId = policyIds[1], AssignedDate = assigned.AddDays(-18), ComplianceStatus = "Non-Compliant" });
        }

        private static async Task SeedSupplierItemsAsync(ApplicationDbContext context)
        {
            if (await context.SupplierItems.AnyAsync().ConfigureAwait(false))
                return;

            var supplierIds = await context.Suppliers.OrderBy(s => s.SupplierId).Select(s => s.SupplierId).Take(2).ToListAsync().ConfigureAwait(false);
            if (supplierIds.Count < 2) return;

            context.SupplierItems.Add(new SupplierItem
            {
                SupplierId = supplierIds[0],
                ItemName = "Laptop Workstation",
                Category = "Hardware",
                QuantityAvailable = 25,
                Status = "Available",
                LastUpdated = DateTime.UtcNow
            });
            context.SupplierItems.Add(new SupplierItem
            {
                SupplierId = supplierIds[0],
                ItemName = "Security Software License",
                Category = "Software",
                QuantityAvailable = 10,
                Status = "Available",
                LastUpdated = DateTime.UtcNow
            });
            context.SupplierItems.Add(new SupplierItem
            {
                SupplierId = supplierIds[1],
                ItemName = "Network Switches",
                Category = "Hardware",
                QuantityAvailable = 8,
                Status = "Available",
                LastUpdated = DateTime.UtcNow
            });
        }

        private static async Task SeedProcurementsAsync(ApplicationDbContext context)
        {
            if (await context.Procurements.AnyAsync().ConfigureAwait(false))
                return;

            var supplierIds = await context.Suppliers.OrderBy(s => s.SupplierId).Select(s => s.SupplierId).Take(2).ToListAsync().ConfigureAwait(false);
            var policyIds = await context.Policies.OrderBy(p => p.PolicyId).Select(p => p.PolicyId).Take(2).ToListAsync().ConfigureAwait(false);
            if (supplierIds.Count < 2 || policyIds.Count < 2) return;

            context.Procurements.Add(new Procurement
            {
                ItemName = "Laptop Workstation",
                Category = "Hardware",
                Quantity = 10,
                SupplierId = supplierIds[0],
                RelatedPolicyId = policyIds[0],
                PurchaseDate = DateTime.UtcNow.AddDays(-14),
                Status = ProcurementStatuses.SupplierApproved,
                SupplierResponseDate = DateTime.UtcNow.AddDays(-13),
                SupplierResponseDeadline = DateTime.UtcNow.AddDays(-7),
                SupplierCommitShipDate = DateTime.UtcNow.AddDays(-10)
            });
            context.Procurements.Add(new Procurement
            {
                ItemName = "Security Software License",
                Category = "Software",
                Quantity = 1,
                SupplierId = supplierIds[0],
                RelatedPolicyId = policyIds[1],
                PurchaseDate = DateTime.UtcNow.AddDays(-7),
                Status = ProcurementStatuses.Submitted,
                SupplierResponseDeadline = DateTime.UtcNow
            });
            context.Procurements.Add(new Procurement
            {
                ItemName = "Network Switches",
                Category = "Hardware",
                Quantity = 5,
                SupplierId = supplierIds[1],
                RelatedPolicyId = policyIds[0],
                PurchaseDate = DateTime.UtcNow.AddDays(-3),
                Status = ProcurementStatuses.SupplierRejected,
                SupplierResponseDate = DateTime.UtcNow.AddDays(-2),
                SupplierResponseDeadline = DateTime.UtcNow.AddDays(4),
                RejectionReason = "Insufficient warehouse stock for this batch size"
            });
        }

        private static async Task SeedAuditLogsAsync(ApplicationDbContext context)
        {
            if (await context.AuditLogs.AnyAsync().ConfigureAwait(false))
                return;

            var adminId = await context.Users.Where(u => u.Role == "Admin").Select(u => u.UserId).FirstOrDefaultAsync().ConfigureAwait(false);
            var complianceUserId = await context.Users.Where(u => u.Email == "compliance@technova.com").Select(u => u.UserId).FirstOrDefaultAsync().ConfigureAwait(false);

            context.AuditLogs.Add(new AuditLog { UserId = adminId > 0 ? adminId : null, Action = "Login", Module = "Account", LogDate = DateTime.UtcNow.AddHours(-2) });
            context.AuditLogs.Add(new AuditLog { UserId = adminId > 0 ? adminId : null, Action = "Upload Policy", Module = "Policies", LogDate = DateTime.UtcNow.AddDays(-1) });
            context.AuditLogs.Add(new AuditLog { UserId = complianceUserId > 0 ? complianceUserId : null, Action = "View Compliance Report", Module = "Compliance", LogDate = DateTime.UtcNow.AddHours(-5) });
        }
    }
}
