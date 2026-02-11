using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Models;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardData> GetDashboardDataAsync()
        {
            var data = new AdminDashboardData();

            // Get statistics
            data.TotalUsers = await _context.Users.CountAsync();
            data.ActivePolicies = await _context.Policies.CountAsync();
            data.TotalSuppliers = await _context.Suppliers.Where(s => s.Status == "Active").CountAsync();
            
            // Get pending compliance count
            data.PendingCompliance = await _context.ComplianceStatuses
                .Where(cs => cs.Status == "Pending")
                .CountAsync();

            // Get recent procurements count (last 30 days)
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            data.RecentProcurements = await _context.Procurements
                .Where(p => p.PurchaseDate >= thirtyDaysAgo)
                .CountAsync();

            // Get audit logs today
            var today = DateTime.Today;
            data.AuditLogsToday = await _context.AuditLogs
                .Where(al => al.LogDate.Date == today)
                .CountAsync();

            // Calculate compliance percentage
            var totalAssignments = await _context.PolicyAssignments.CountAsync();
            var acknowledgedCount = await _context.ComplianceStatuses
                .Where(cs => cs.Status == "Acknowledged")
                .CountAsync();
            
            if (totalAssignments > 0)
            {
                data.CompliancePercentage = (int)((double)acknowledgedCount / totalAssignments * 100);
            }

            // Recent Policies
            data.RecentPolicies = await _context.Policies
                .OrderByDescending(p => p.DateUploaded)
                .Take(5)
                .Select(p => new PolicyItem
                {
                    Name = p.PolicyTitle,
                    AssignedDate = p.DateUploaded ?? DateTime.Now
                })
                .ToListAsync();

            // Recent Procurements
            data.RecentProcurementsData = await _context.Procurements
                .Include(p => p.Supplier)
                .Include(p => p.RelatedPolicy)
                .OrderByDescending(p => p.PurchaseDate)
                .Take(5)
                .Select(p => new ProcurementItem
                {
                    Supplier = p.Supplier != null ? p.Supplier.SupplierName : "N/A",
                    Item = p.ItemName ?? "N/A",
                    Date = p.PurchaseDate ?? DateTime.Now,
                    LinkedPolicy = p.RelatedPolicy != null ? p.RelatedPolicy.PolicyTitle : "General"
                })
                .ToListAsync();

            // Recent Activities from Audit Logs
            data.RecentActivities = await _context.AuditLogs
                .Include(al => al.User)
                .OrderByDescending(al => al.LogDate)
                .Take(6)
                .Select(al => new ActivityItem
                {
                    IconClass = al.Module == "Authentication" ? "user" : (al.Module == "Policy" ? "policy" : "procurement"),
                    IconSvg = GetIconForModule(al.Module),
                    Title = al.Action ?? "Unknown Action",
                    Description = al.User != null ? $"{al.Action} by {al.User.FirstName} {al.User.LastName}" : (al.Action ?? "Unknown Action"),
                    Time = GetTimeAgo(al.LogDate)
                })
                .ToListAsync();

            return data;
        }

        private static string GetIconForModule(string? module)
        {
            return module switch
            {
                "Authentication" => "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"></path><circle cx=\"8.5\" cy=\"7\" r=\"4\"></circle></svg>",
                "Policy" => "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z\"></path><polyline points=\"14 2 14 8 20 8\"></polyline></svg>",
                _ => "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><rect x=\"1\" y=\"4\" width=\"22\" height=\"16\" rx=\"2\" ry=\"2\"></rect><line x1=\"1\" y1=\"10\" x2=\"23\" y2=\"10\"></line></svg>"
            };
        }

        private static string GetTimeAgo(DateTime dateTime)
        {
            var span = DateTime.Now - dateTime;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} mins ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hour{((int)span.TotalHours > 1 ? "s" : "")} ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} day{((int)span.TotalDays > 1 ? "s" : "")} ago";
            return dateTime.ToString("MMM dd, yyyy");
        }

        // Policy operations
        public async Task<bool> CreatePolicyAsync(PolicyData policyData)
        {
            try
            {
                var policy = new Models.Policy
                {
                    PolicyTitle = policyData.PolicyTitle,
                    Category = policyData.Category,
                    Description = policyData.Description,
                    FilePath = policyData.FilePath,
                    DateUploaded = policyData.UploadedDate ?? DateTime.Now
                };

                _context.Policies.Add(policy);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdatePolicyAsync(PolicyData policyData)
        {
            try
            {
                var policy = await _context.Policies.FindAsync(policyData.PolicyId);
                if (policy == null) return false;

                policy.PolicyTitle = policyData.PolicyTitle;
                policy.Category = policyData.Category;
                policy.Description = policyData.Description;
                // Only update file path if a new file was provided
                if (policyData.FilePath != null)
                    policy.FilePath = policyData.FilePath;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<PolicyData?> GetPolicyByIdAsync(int policyId)
        {
            var policy = await _context.Policies.FindAsync(policyId);
            if (policy == null) return null;

            return new PolicyData
            {
                PolicyId = policy.PolicyId,
                PolicyTitle = policy.PolicyTitle,
                Category = policy.Category ?? string.Empty,
                Description = policy.Description ?? string.Empty,
                FilePath = policy.FilePath,
                UploadedDate = policy.DateUploaded
            };
        }

        public async Task<bool> DeletePolicyAsync(int policyId)
        {
            try
            {
                var policy = await _context.Policies.FindAsync(policyId);
                if (policy == null) return false;

                _context.Policies.Remove(policy);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Supplier operations
        public async Task<bool> CreateSupplierAsync(SupplierData supplierData)
        {
            try
            {
                var supplier = new Models.Supplier
                {
                    SupplierName = supplierData.SupplierName,
                    ContactPersonFirstName = supplierData.ContactPersonFirstName,
                    ContactPersonLastName = supplierData.ContactPersonLastName,
                    Email = supplierData.Email,
                    ContactPersonNumber = supplierData.ContactPersonNumber,
                    Address = supplierData.Address,
                    Status = supplierData.Status
                };

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateSupplierAsync(SupplierData supplierData)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(supplierData.SupplierId);
                if (supplier == null) return false;

                supplier.SupplierName = supplierData.SupplierName;
                supplier.ContactPersonFirstName = supplierData.ContactPersonFirstName;
                supplier.ContactPersonLastName = supplierData.ContactPersonLastName;
                supplier.Email = supplierData.Email;
                supplier.ContactPersonNumber = supplierData.ContactPersonNumber;
                supplier.Address = supplierData.Address;
                supplier.Status = supplierData.Status;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteSupplierAsync(int supplierId)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(supplierId);
                if (supplier == null) return false;

                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Procurement operations
        public async Task<bool> CreateProcurementAsync(ProcurementData procurementData)
        {
            try
            {
                var procurement = new Models.Procurement
                {
                    SupplierId = procurementData.SupplierId,
                    ItemName = procurementData.ItemName,
                    Category = procurementData.Category,
                    Quantity = procurementData.Quantity,
                    PurchaseDate = procurementData.ProcurementDate,
                    RelatedPolicyId = procurementData.PolicyId
                };

                _context.Procurements.Add(procurement);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateProcurementAsync(ProcurementData procurementData)
        {
            try
            {
                var procurement = await _context.Procurements.FindAsync(procurementData.ProcurementId);
                if (procurement == null) return false;

                procurement.SupplierId = procurementData.SupplierId;
                procurement.ItemName = procurementData.ItemName;
                procurement.Category = procurementData.Category;
                procurement.Quantity = procurementData.Quantity;
                procurement.PurchaseDate = procurementData.ProcurementDate;
                procurement.RelatedPolicyId = procurementData.PolicyId;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteProcurementAsync(int procurementId)
        {
            try
            {
                var procurement = await _context.Procurements.FindAsync(procurementId);
                if (procurement == null) return false;

                _context.Procurements.Remove(procurement);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ── Policy Assignment ──────────────────────────────────────

        public async Task<bool> AssignPolicyToEmployeesAsync(int policyId, List<int> employeeIds)
        {
            try
            {
                foreach (var empId in employeeIds)
                {
                    // Skip if already assigned
                    var exists = await _context.PolicyAssignments
                        .AnyAsync(pa => pa.PolicyId == policyId && pa.UserId == empId);
                    if (exists) continue;

                    var assignment = new PolicyAssignment
                    {
                        PolicyId = policyId,
                        UserId = empId,
                        AssignedDate = DateTime.Now
                    };
                    _context.PolicyAssignments.Add(assignment);
                    await _context.SaveChangesAsync();

                    // Create a Pending compliance status for the new assignment
                    var complianceStatus = new ComplianceStatus
                    {
                        AssignmentId = assignment.AssignmentId,
                        Status = "Pending"
                    };
                    _context.ComplianceStatuses.Add(complianceStatus);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AssignPolicyToSuppliersAsync(int policyId, List<int> supplierIds)
        {
            try
            {
                foreach (var supId in supplierIds)
                {
                    var exists = await _context.SupplierPolicies
                        .AnyAsync(sp => sp.PolicyId == policyId && sp.SupplierId == supId);
                    if (exists) continue;

                    var supplierPolicy = new SupplierPolicy
                    {
                        PolicyId = policyId,
                        SupplierId = supId,
                        AssignedDate = DateTime.Now,
                        ComplianceStatus = "Pending"
                    };
                    _context.SupplierPolicies.Add(supplierPolicy);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ── Audit Logging ──────────────────────────────────────────

        public async Task LogActivityAsync(int? userId, string action, string module)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Module = module,
                    LogDate = DateTime.Now
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Silently fail — audit logging should not break operations
            }
        }
    }
}
