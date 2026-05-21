using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Models;
using System.Security.Cryptography;
using System.Text;

namespace TechNova_IT_Solutions.Data
{
    /// <summary>
    /// Seeds the database with initial/sample data. Idempotent: only adds data when tables are empty.
    /// </summary>
    public static class DataSeeder
    {
        /// <summary>
        /// DEPRECATED: This constant is no longer used. Passwords are now generated randomly in development.
        /// </summary>
        [Obsolete("This constant is deprecated. Passwords are now generated randomly for security.")]
        public const string SeededUserPassword = "DEPRECATED_DO_NOT_USE";

        public static async Task SeedAsync(ApplicationDbContext context, IHostEnvironment environment, ILogger logger)
        {
            if (context == null) return;

            await SeedBranchesAsync(context).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);

            await SeedUsersAsync(context, environment, logger).ConfigureAwait(false);
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

        /// <summary>
        /// Generates a cryptographically secure random password
        /// </summary>
        private static string GenerateRandomStrongPassword()
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";
            const string allChars = uppercase + lowercase + digits + special;

            var password = new StringBuilder();
            
            // Ensure at least one character from each required category
            password.Append(uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)]);
            password.Append(lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)]);
            password.Append(digits[RandomNumberGenerator.GetInt32(digits.Length)]);
            password.Append(special[RandomNumberGenerator.GetInt32(special.Length)]);

            // Fill the rest with random characters (total length 16)
            for (int i = 4; i < 16; i++)
            {
                password.Append(allChars[RandomNumberGenerator.GetInt32(allChars.Length)]);
            }

            // Shuffle the password to avoid predictable patterns
            return new string(password.ToString().OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
        }

        private static async Task SeedUsersAsync(ApplicationDbContext context, IHostEnvironment environment, ILogger logger)
        {
            var hasSeedUsers = await context.Users.CountAsync().ConfigureAwait(false) > 1;
            var branchIds = await context.Branches
                .OrderBy(b => b.BranchId)
                .Select(b => b.BranchId)
                .ToListAsync()
                .ConfigureAwait(false);
            var primaryBranchId = branchIds.Count > 0 ? (int?)branchIds[0] : null;
            var secondaryBranchId = branchIds.Count > 1 ? (int?)branchIds[1] : primaryBranchId;
            var tertiaryBranchId = branchIds.Count > 2 ? (int?)branchIds[2] : primaryBranchId;

            // Check if we're in production environment
            bool isProduction = environment.IsProduction();

            string hashedPassword;
            string actualPassword;

            if (isProduction)
            {
                // In production, do NOT seed users with default passwords
                logger.LogInformation("Production environment detected. Skipping user seeding to prevent default password usage.");
                
                // Only ensure super admin and system admin exist if they don't already
                // These should be created through migrations or manual setup with strong passwords
                return;
            }
            else
            {
                // In development/staging, generate random strong passwords
                actualPassword = GenerateRandomStrongPassword();
                hashedPassword = BCrypt.Net.BCrypt.HashPassword(actualPassword);
                
                logger.LogWarning("===========================================");
                logger.LogWarning("DEVELOPMENT ENVIRONMENT - SEEDING USERS");
                logger.LogWarning("Generated password for seeded users: {Password}", actualPassword);
                logger.LogWarning("All seeded users MUST change password on first login");
                logger.LogWarning("===========================================");
            }

            if (!await context.Users.AnyAsync(u => u.Email == "superadmin@technova.com").ConfigureAwait(false))
            {
                context.Users.Add(new User
                {
                    FirstName = "Super",
                    LastName = "Administrator",
                    Email = "superadmin@technova.com",
                    Password = hashedPassword,
                    Role = "SuperAdmin",
                    Status = "Active",
                    MustChangePassword = true
                });
            }

            if (!await context.Users.AnyAsync(u => u.Email == "sysadmin@technova.com").ConfigureAwait(false))
            {
                context.Users.Add(new User
                {
                    FirstName = "System",
                    LastName = "Administrator",
                    Email = "sysadmin@technova.com",
                    Password = hashedPassword,
                    Role = "SystemAdmin",
                    Status = "Active",
                    MustChangePassword = true
                });
            }

            if (hasSeedUsers)
                return;

            context.Users.Add(new User
            {
                FirstName = "Ava",
                LastName = "Branch",
                Email = "branchadmin@technova.com",
                Password = hashedPassword,
                Role = RoleNames.BranchAdmin,
                Status = "Active",
                MustChangePassword = true,
                BranchId = primaryBranchId
            });
            context.Users.Add(new User
            {
                FirstName = "Chris",
                LastName = "Chief",
                Email = "ccm@technova.com",
                Password = hashedPassword,
                Role = RoleNames.ChiefComplianceManager,
                Status = "Active",
                MustChangePassword = true
            });
            context.Users.Add(new User
            {
                FirstName = "Jane",
                LastName = "Compliance",
                Email = "compliance@technova.com",
                Password = hashedPassword,
                Role = "ComplianceManager",
                Status = "Active",
                MustChangePassword = true,
                BranchId = primaryBranchId
            });
            context.Users.Add(new User
            {
                FirstName = "John",
                LastName = "Employee",
                Email = "employee@technova.com",
                Password = hashedPassword,
                Role = "Employee",
                Status = "Active",
                MustChangePassword = true,
                BranchId = primaryBranchId
            });
            context.Users.Add(new User
            {
                FirstName = "Sam",
                LastName = "Supplier",
                Email = "supplier@technova.com",
                Password = hashedPassword,
                Role = RoleNames.Supplier,
                Status = "Active",
                MustChangePassword = true
            });
            context.Users.Add(new User
            {
                FirstName = "Liam",
                LastName = "Cebu",
                Email = "branchadmin.cebu@technova.com",
                Password = hashedPassword,
                Role = RoleNames.BranchAdmin,
                Status = "Active",
                MustChangePassword = true,
                BranchId = secondaryBranchId
            });
            context.Users.Add(new User
            {
                FirstName = "Maya",
                LastName = "Manila",
                Email = "branchadmin.manila@technova.com",
                Password = hashedPassword,
                Role = RoleNames.BranchAdmin,
                Status = "Active",
                MustChangePassword = true,
                BranchId = tertiaryBranchId
            });
            context.Users.Add(new User
            {
                FirstName = "Ella",
                LastName = "Cebu",
                Email = "cebu.compliance@technova.com",
                Password = hashedPassword,
                Role = RoleNames.ComplianceManager,
                Status = "Active",
                MustChangePassword = true,
                BranchId = secondaryBranchId
            });
            context.Users.Add(new User
            {
                FirstName = "Diego",
                LastName = "Manila",
                Email = "manila.compliance@technova.com",
                Password = hashedPassword,
                Role = RoleNames.ComplianceManager,
                Status = "Active",
                MustChangePassword = true,
                BranchId = tertiaryBranchId
            });
            context.Users.Add(new User
            {
                FirstName = "Riley",
                LastName = "Employee",
                Email = "employee2@technova.com",
                Password = hashedPassword,
                Role = RoleNames.Employee,
                Status = "Active",
                MustChangePassword = true,
                BranchId = primaryBranchId
            });
            context.Users.Add(new User
            {
                FirstName = "Noel",
                LastName = "Employee",
                Email = "employee3@technova.com",
                Password = hashedPassword,
                Role = RoleNames.Employee,
                Status = "Active",
                MustChangePassword = true,
                BranchId = secondaryBranchId
            });
            context.Users.Add(new User
            {
                FirstName = "Sasha",
                LastName = "Employee",
                Email = "employee4@technova.com",
                Password = hashedPassword,
                Role = RoleNames.Employee,
                Status = "Active",
                MustChangePassword = true,
                BranchId = tertiaryBranchId
            });
            context.Users.Add(new User
            {
                FirstName = "Luna",
                LastName = "Supplier",
                Email = "supplier2@technova.com",
                Password = hashedPassword,
                Role = RoleNames.Supplier,
                Status = "Active",
                MustChangePassword = true
            });
            context.Users.Add(new User
            {
                FirstName = "Kai",
                LastName = "Supplier",
                Email = "supplier3@technova.com",
                Password = hashedPassword,
                Role = RoleNames.Supplier,
                Status = "Active",
                MustChangePassword = true
            });
        }

        private static async Task SeedBranchesAsync(ApplicationDbContext context)
        {
            if (await context.Branches.AnyAsync().ConfigureAwait(false))
                return;

            context.Branches.Add(new Branch
            {
                BranchName = "Main Branch",
                Address = "100 Tech Avenue",
                City = "Davao City",
                Region = "Davao Region",
                Phone = "+63-82-555-0100",
                Email = "mainbranch@technova.com",
                ManagerFirstName = "Mia",
                ManagerLastName = "Reyes",
                ManagerEmail = "m.reyes@technova.com",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-120),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            });
            context.Branches.Add(new Branch
            {
                BranchName = "Cebu Branch",
                Address = "200 Harbor Road",
                City = "Cebu City",
                Region = "Central Visayas",
                Phone = "+63-32-555-0200",
                Email = "cebu@technova.com",
                ManagerFirstName = "Noah",
                ManagerLastName = "Santos",
                ManagerEmail = "n.santos@technova.com",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-90),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            });
            context.Branches.Add(new Branch
            {
                BranchName = "Manila Branch",
                Address = "300 Ayala Avenue",
                City = "Makati",
                Region = "NCR",
                Phone = "+63-2-555-0300",
                Email = "manila@technova.com",
                ManagerFirstName = "Iris",
                ManagerLastName = "Valdez",
                ManagerEmail = "i.valdez@technova.com",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-75),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            });
        }

        private static async Task SeedPoliciesAsync(ApplicationDbContext context)
        {
            if (await context.Policies.AnyAsync().ConfigureAwait(false))
                return;

            var adminId = await context.Users.Where(u => u.Role == RoleNames.SystemAdmin || u.Role == RoleNames.BranchAdmin).Select(u => u.UserId).FirstOrDefaultAsync().ConfigureAwait(false);
            var uploadedBy = adminId > 0 ? adminId : (int?)null;
            var branchIds = await context.Branches.OrderBy(b => b.BranchId).Select(b => b.BranchId).ToListAsync().ConfigureAwait(false);
            var primaryBranchId = branchIds.Count > 0 ? (int?)branchIds[0] : null;
            var secondaryBranchId = branchIds.Count > 1 ? (int?)branchIds[1] : null;
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
            context.Policies.Add(new Policy
            {
                PolicyTitle = "Incident Response Plan",
                Description = "Steps for reporting, triage, and recovery from security incidents.",
                Category = "Security",
                UploadedBy = uploadedBy,
                DateUploaded = date.AddDays(-22)
            });
            context.Policies.Add(new Policy
            {
                PolicyTitle = "Access Control Policy",
                Description = "Role-based access guidelines for internal systems.",
                Category = "Security",
                UploadedBy = uploadedBy,
                DateUploaded = date.AddDays(-18)
            });
            context.Policies.Add(new Policy
            {
                PolicyTitle = "Vendor Risk Management Policy",
                Description = "Vendor onboarding, assessment, and monitoring requirements.",
                Category = "Compliance",
                UploadedBy = uploadedBy,
                DateUploaded = date.AddDays(-16)
            });
            context.Policies.Add(new Policy
            {
                PolicyTitle = "Data Retention and Disposal Policy",
                Description = "Retention periods and secure disposal procedures for data.",
                Category = "Compliance",
                UploadedBy = uploadedBy,
                DateUploaded = date.AddDays(-12)
            });
            context.Policies.Add(new Policy
            {
                PolicyTitle = "Remote Work Security Policy",
                Description = "Security requirements for offsite and remote work.",
                Category = "Security",
                UploadedBy = uploadedBy,
                DateUploaded = date.AddDays(-9),
                BranchId = primaryBranchId
            });
            context.Policies.Add(new Policy
            {
                PolicyTitle = "Business Continuity Policy",
                Description = "Business continuity planning and operational resilience.",
                Category = "Operations",
                UploadedBy = uploadedBy,
                DateUploaded = date.AddDays(-7),
                BranchId = secondaryBranchId
            });
        }

        private static async Task SeedSuppliersAsync(ApplicationDbContext context)
        {
            if (await context.Suppliers.AnyAsync().ConfigureAwait(false))
                return;

            var branchIds = await context.Branches.OrderBy(b => b.BranchId).Select(b => b.BranchId).ToListAsync().ConfigureAwait(false);
            var primaryBranchId = branchIds.Count > 0 ? (int?)branchIds[0] : null;
            var secondaryBranchId = branchIds.Count > 1 ? (int?)branchIds[1] : null;

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
                Status = "Active",
                BranchId = primaryBranchId
            });
            context.Suppliers.Add(new Supplier
            {
                SupplierName = "Pacific Hardware Co",
                ContactPersonFirstName = "Leah",
                ContactPersonLastName = "Tan",
                Email = "leah@pacifichardware.com",
                ContactPersonNumber = "+63-2-555-0400",
                Address = "12 Bayfront Ave, Manila",
                Status = "Active",
                BranchId = secondaryBranchId
            });
            context.Suppliers.Add(new Supplier
            {
                SupplierName = "CloudShield Services",
                ContactPersonFirstName = "Marco",
                ContactPersonLastName = "Dela Cruz",
                Email = "marco@cloudshield.io",
                ContactPersonNumber = "+63-82-555-0500",
                Address = "88 Cloud Park, Davao",
                Status = "Active",
                BranchId = primaryBranchId
            });
            context.Suppliers.Add(new Supplier
            {
                SupplierName = "Metro Office Systems",
                ContactPersonFirstName = "Priya",
                ContactPersonLastName = "Singh",
                Email = "priya@metrooffice.com",
                ContactPersonNumber = "+63-32-555-0600",
                Address = "77 Gateway Blvd, Cebu",
                Status = "Active",
                BranchId = secondaryBranchId
            });
        }

        private static async Task SeedPolicyAssignmentsAsync(ApplicationDbContext context)
        {
            if (await context.PolicyAssignments.AnyAsync().ConfigureAwait(false))
                return;

            var complianceUserId = await context.Users.Where(u => u.Email == "compliance@technova.com").Select(u => u.UserId).FirstOrDefaultAsync().ConfigureAwait(false);
            var cebuComplianceUserId = await context.Users.Where(u => u.Email == "cebu.compliance@technova.com").Select(u => u.UserId).FirstOrDefaultAsync().ConfigureAwait(false);
            var manilaComplianceUserId = await context.Users.Where(u => u.Email == "manila.compliance@technova.com").Select(u => u.UserId).FirstOrDefaultAsync().ConfigureAwait(false);
            var employeeUserId = await context.Users.Where(u => u.Email == "employee@technova.com").Select(u => u.UserId).FirstOrDefaultAsync().ConfigureAwait(false);
            var employee2UserId = await context.Users.Where(u => u.Email == "employee2@technova.com").Select(u => u.UserId).FirstOrDefaultAsync().ConfigureAwait(false);
            var employee3UserId = await context.Users.Where(u => u.Email == "employee3@technova.com").Select(u => u.UserId).FirstOrDefaultAsync().ConfigureAwait(false);
            var employee4UserId = await context.Users.Where(u => u.Email == "employee4@technova.com").Select(u => u.UserId).FirstOrDefaultAsync().ConfigureAwait(false);
            var policyIds = await context.Policies.OrderBy(p => p.PolicyId).Select(p => p.PolicyId).Take(8).ToListAsync().ConfigureAwait(false);
            if (policyIds.Count < 2) return;

            var userIds = new List<int>
            {
                complianceUserId,
                cebuComplianceUserId,
                manilaComplianceUserId,
                employeeUserId,
                employee2UserId,
                employee3UserId,
                employee4UserId
            }.Where(id => id > 0).ToList();

            if (userIds.Count == 0) return;

            var assigned = DateTime.UtcNow;
            for (int i = 0; i < userIds.Count; i++)
            {
                var policyA = policyIds[i % policyIds.Count];
                var policyB = policyIds[(i + 1) % policyIds.Count];

                context.PolicyAssignments.Add(new PolicyAssignment
                {
                    PolicyId = policyA,
                    UserId = userIds[i],
                    AssignedDate = assigned.AddDays(-(6 + i))
                });
                context.PolicyAssignments.Add(new PolicyAssignment
                {
                    PolicyId = policyB,
                    UserId = userIds[i],
                    AssignedDate = assigned.AddDays(-(10 + i))
                });
            }
        }

        private static async Task SeedComplianceStatusesAsync(ApplicationDbContext context)
        {
            if (await context.ComplianceStatuses.AnyAsync().ConfigureAwait(false))
                return;

            var assignmentIds = await context.PolicyAssignments.OrderBy(a => a.AssignmentId).Select(a => a.AssignmentId).ToListAsync().ConfigureAwait(false);
            if (assignmentIds.Count == 0) return;

            var statuses = new[] { "Acknowledged", "Pending", "Overdue" };
            var now = DateTime.UtcNow;

            for (int i = 0; i < assignmentIds.Count; i++)
            {
                var status = statuses[i % statuses.Length];
                context.ComplianceStatuses.Add(new ComplianceStatus
                {
                    AssignmentId = assignmentIds[i],
                    Status = status,
                    AcknowledgedDate = status == "Acknowledged" ? now.AddDays(-(4 + i)) : null
                });
            }
        }

        private static async Task SeedSupplierPoliciesAsync(ApplicationDbContext context)
        {
            if (await context.SupplierPolicies.AnyAsync().ConfigureAwait(false))
                return;

            var supplierIds = await context.Suppliers.OrderBy(s => s.SupplierId).Select(s => s.SupplierId).Take(5).ToListAsync().ConfigureAwait(false);
            var policyIds = await context.Policies.OrderBy(p => p.PolicyId).Select(p => p.PolicyId).Take(4).ToListAsync().ConfigureAwait(false);
            if (supplierIds.Count == 0 || policyIds.Count < 2) return;

            var assigned = DateTime.UtcNow;
            var statuses = new[] { "Compliant", "Pending", "Non-Compliant" };

            for (int i = 0; i < supplierIds.Count; i++)
            {
                var policyA = policyIds[i % policyIds.Count];
                var policyB = policyIds[(i + 1) % policyIds.Count];

                context.SupplierPolicies.Add(new SupplierPolicy
                {
                    SupplierId = supplierIds[i],
                    PolicyId = policyA,
                    AssignedDate = assigned.AddDays(-(20 + i)),
                    ComplianceStatus = statuses[i % statuses.Length]
                });
                context.SupplierPolicies.Add(new SupplierPolicy
                {
                    SupplierId = supplierIds[i],
                    PolicyId = policyB,
                    AssignedDate = assigned.AddDays(-(14 + i)),
                    ComplianceStatus = statuses[(i + 1) % statuses.Length]
                });
            }
        }

        private static async Task SeedSupplierItemsAsync(ApplicationDbContext context)
        {
            if (await context.SupplierItems.AnyAsync().ConfigureAwait(false))
                return;

            var supplierIds = await context.Suppliers.OrderBy(s => s.SupplierId).Select(s => s.SupplierId).Take(5).ToListAsync().ConfigureAwait(false);
            if (supplierIds.Count == 0) return;

            for (int i = 0; i < supplierIds.Count; i++)
            {
                context.SupplierItems.Add(new SupplierItem
                {
                    SupplierId = supplierIds[i],
                    ItemName = "Laptop Workstation",
                    Category = "Hardware",
                    QuantityAvailable = 25 - i,
                    UnitPrice = 52000 + (i * 1500),
                    CurrencyCode = "PHP",
                    Status = "Available",
                    LastUpdated = DateTime.UtcNow.AddDays(-2)
                });
                context.SupplierItems.Add(new SupplierItem
                {
                    SupplierId = supplierIds[i],
                    ItemName = "Security Software License",
                    Category = "Software",
                    QuantityAvailable = 12 + i,
                    UnitPrice = 18000 + (i * 750),
                    CurrencyCode = "PHP",
                    Status = "Available",
                    LastUpdated = DateTime.UtcNow.AddDays(-1)
                });
            }

            if (supplierIds.Count > 1)
            {
                context.SupplierItems.Add(new SupplierItem
                {
                    SupplierId = supplierIds[1],
                    ItemName = "Network Switches",
                    Category = "Hardware",
                    QuantityAvailable = 8,
                    UnitPrice = 32000,
                    CurrencyCode = "PHP",
                    Status = "Available",
                    LastUpdated = DateTime.UtcNow
                });
            }
            if (supplierIds.Count > 2)
            {
                context.SupplierItems.Add(new SupplierItem
                {
                    SupplierId = supplierIds[2],
                    ItemName = "Backup Storage Array",
                    Category = "Hardware",
                    QuantityAvailable = 4,
                    UnitPrice = 185000,
                    CurrencyCode = "PHP",
                    Status = "Available",
                    LastUpdated = DateTime.UtcNow
                });
            }
        }

        private static async Task SeedProcurementsAsync(ApplicationDbContext context)
        {
            if (await context.Procurements.AnyAsync().ConfigureAwait(false))
                return;

            var supplierIds = await context.Suppliers.OrderBy(s => s.SupplierId).Select(s => s.SupplierId).Take(5).ToListAsync().ConfigureAwait(false);
            var policyIds = await context.Policies.OrderBy(p => p.PolicyId).Select(p => p.PolicyId).Take(4).ToListAsync().ConfigureAwait(false);
            if (supplierIds.Count == 0 || policyIds.Count < 2) return;

            context.Procurements.Add(new Procurement
            {
                ItemName = "Laptop Workstation",
                Category = "Hardware",
                Quantity = 10,
                SupplierId = supplierIds[0],
                RelatedPolicyId = policyIds[0],
                PurchaseDate = DateTime.UtcNow.AddDays(-14),
                CurrencyCode = "PHP",
                OriginalAmount = 520000,
                ExchangeRate = 1,
                ConvertedAmount = 520000,
                ConversionTimestamp = DateTime.UtcNow.AddDays(-14),
                Status = ProcurementStatuses.SupplierApproved,
                SupplierResponseDate = DateTime.UtcNow.AddDays(-13),
                SupplierResponseDeadline = DateTime.UtcNow.AddDays(-7),
                SupplierCommitShipDate = DateTime.UtcNow.AddDays(-10)
            });
            context.Procurements.Add(new Procurement
            {
                ItemName = "Security Software License",
                Category = "Software",
                Quantity = 2,
                SupplierId = supplierIds[0],
                RelatedPolicyId = policyIds[1],
                PurchaseDate = DateTime.UtcNow.AddDays(-7),
                CurrencyCode = "PHP",
                OriginalAmount = 36000,
                ExchangeRate = 1,
                ConvertedAmount = 36000,
                ConversionTimestamp = DateTime.UtcNow.AddDays(-7),
                Status = ProcurementStatuses.Submitted,
                SupplierResponseDeadline = DateTime.UtcNow.AddDays(1)
            });
            if (supplierIds.Count > 1)
            {
                context.Procurements.Add(new Procurement
                {
                    ItemName = "Network Switches",
                    Category = "Hardware",
                    Quantity = 5,
                    SupplierId = supplierIds[1],
                    RelatedPolicyId = policyIds[0],
                    PurchaseDate = DateTime.UtcNow.AddDays(-5),
                    CurrencyCode = "PHP",
                    OriginalAmount = 160000,
                    ExchangeRate = 1,
                    ConvertedAmount = 160000,
                    ConversionTimestamp = DateTime.UtcNow.AddDays(-5),
                    Status = ProcurementStatuses.SupplierRejected,
                    SupplierResponseDate = DateTime.UtcNow.AddDays(-4),
                    SupplierResponseDeadline = DateTime.UtcNow.AddDays(2),
                    RejectionReason = "Insufficient warehouse stock for this batch size"
                });
            }
            if (supplierIds.Count > 2)
            {
                context.Procurements.Add(new Procurement
                {
                    ItemName = "Backup Storage Array",
                    Category = "Hardware",
                    Quantity = 2,
                    SupplierId = supplierIds[2],
                    RelatedPolicyId = policyIds[2],
                    PurchaseDate = DateTime.UtcNow.AddDays(-20),
                    CurrencyCode = "PHP",
                    OriginalAmount = 370000,
                    ExchangeRate = 1,
                    ConvertedAmount = 370000,
                    ConversionTimestamp = DateTime.UtcNow.AddDays(-20),
                    Status = ProcurementStatuses.Shipped,
                    SupplierResponseDate = DateTime.UtcNow.AddDays(-18),
                    SupplierResponseDeadline = DateTime.UtcNow.AddDays(-15),
                    SupplierCommitShipDate = DateTime.UtcNow.AddDays(-16),
                    ShipmentDate = DateTime.UtcNow.AddDays(-12)
                });
            }
            if (supplierIds.Count > 3)
            {
                context.Procurements.Add(new Procurement
                {
                    ItemName = "Endpoint Protection Suite",
                    Category = "Software",
                    Quantity = 25,
                    SupplierId = supplierIds[3],
                    RelatedPolicyId = policyIds[3],
                    PurchaseDate = DateTime.UtcNow.AddDays(-28),
                    CurrencyCode = "PHP",
                    OriginalAmount = 225000,
                    ExchangeRate = 1,
                    ConvertedAmount = 225000,
                    ConversionTimestamp = DateTime.UtcNow.AddDays(-28),
                    Status = ProcurementStatuses.Received,
                    SupplierResponseDate = DateTime.UtcNow.AddDays(-26),
                    SupplierResponseDeadline = DateTime.UtcNow.AddDays(-22),
                    SupplierCommitShipDate = DateTime.UtcNow.AddDays(-23),
                    ShipmentDate = DateTime.UtcNow.AddDays(-21),
                    ReceivedDate = DateTime.UtcNow.AddDays(-18)
                });
            }
            if (supplierIds.Count > 4)
            {
                context.Procurements.Add(new Procurement
                {
                    ItemName = "Office Network Upgrade",
                    Category = "Hardware",
                    Quantity = 1,
                    SupplierId = supplierIds[4],
                    RelatedPolicyId = policyIds[0],
                    PurchaseDate = DateTime.UtcNow.AddDays(-35),
                    CurrencyCode = "PHP",
                    OriginalAmount = 980000,
                    ExchangeRate = 1,
                    ConvertedAmount = 980000,
                    ConversionTimestamp = DateTime.UtcNow.AddDays(-35),
                    Status = ProcurementStatuses.Late,
                    SupplierResponseDate = DateTime.UtcNow.AddDays(-33),
                    SupplierResponseDeadline = DateTime.UtcNow.AddDays(-25),
                    SupplierCommitShipDate = DateTime.UtcNow.AddDays(-28),
                    DelayReason = "Awaiting customs clearance"
                });
            }
        }

        private static async Task SeedAuditLogsAsync(ApplicationDbContext context)
        {
            if (await context.AuditLogs.AnyAsync().ConfigureAwait(false))
                return;

            var adminId = await context.Users.Where(u => u.Role == RoleNames.SystemAdmin || u.Role == RoleNames.BranchAdmin).Select(u => u.UserId).FirstOrDefaultAsync().ConfigureAwait(false);
            var complianceUserId = await context.Users.Where(u => u.Email == "compliance@technova.com").Select(u => u.UserId).FirstOrDefaultAsync().ConfigureAwait(false);

            context.AuditLogs.Add(new AuditLog { UserId = adminId > 0 ? adminId : null, Action = "Login", Module = "Account", LogDate = DateTime.UtcNow.AddHours(-2) });
            context.AuditLogs.Add(new AuditLog { UserId = adminId > 0 ? adminId : null, Action = "Upload Policy", Module = "Policies", LogDate = DateTime.UtcNow.AddDays(-1) });
            context.AuditLogs.Add(new AuditLog { UserId = complianceUserId > 0 ? complianceUserId : null, Action = "View Compliance Report", Module = "Compliance", LogDate = DateTime.UtcNow.AddHours(-5) });
        }
    }
}
