using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Models;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AdminService(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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

                // Notify all active users about the new policy
                var users = await _context.Users.Where(u => u.Status == "Active" && u.Email != null).ToListAsync();
                foreach (var user in users)
                {
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        var subject = $"New Policy: {policyData.PolicyTitle}";
                        var body = $@"
                            <h2>A new policy has been added</h2>
                            <p><strong>Title:</strong> {policyData.PolicyTitle}</p>
                            <p><strong>Category:</strong> {policyData.Category}</p>
                            <p>Please log in to the portal to review it.</p>";
                        
                        // Fire and forget to avoid blocking
                        _ = _emailService.SendEmailAsync(user.Email, subject, body);
                    }
                }

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
                UploadedDate = policy.DateUploaded,
                IsArchived = policy.IsArchived,
                ArchivedDate = policy.ArchivedDate
            };
        }

        public async Task<PolicyDetailData?> GetPolicyDetailAsync(int policyId)
        {
            var policy = await _context.Policies
                .Include(p => p.UploadedByUser)
                .Include(p => p.PolicyAssignments)
                .Include(p => p.SupplierPolicies)
                .FirstOrDefaultAsync(p => p.PolicyId == policyId);

            if (policy == null) return null;

            return new PolicyDetailData
            {
                PolicyId = policy.PolicyId,
                PolicyTitle = policy.PolicyTitle,
                Category = policy.Category ?? string.Empty,
                Description = policy.Description ?? string.Empty,
                FilePath = policy.FilePath,
                DateUploaded = policy.DateUploaded,
                UploadedBy = policy.UploadedByUser != null
                    ? $"{policy.UploadedByUser.FirstName} {policy.UploadedByUser.LastName}"
                    : "System",
                IsArchived = policy.IsArchived,
                ArchivedDate = policy.ArchivedDate,
                AssignedEmployees = policy.PolicyAssignments.Count,
                AssignedSuppliers = policy.SupplierPolicies.Count
            };
        }

        public async Task<bool> ArchivePolicyAsync(int policyId)
        {
            try
            {
                var policy = await _context.Policies.FindAsync(policyId);
                if (policy == null) return false;

                policy.IsArchived = true;
                policy.ArchivedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RestorePolicyAsync(int policyId)
        {
            try
            {
                var policy = await _context.Policies.FindAsync(policyId);
                if (policy == null) return false;

                policy.IsArchived = false;
                policy.ArchivedDate = null;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
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
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Create Supplier Record
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

                // 2. Create User Record for Login
                // Check if user already exists
                var existingUser = await _context.Users.AnyAsync(u => u.Email == supplierData.Email);
                if (!existingUser && !string.IsNullOrEmpty(supplierData.Password))
                {
                    var user = new User
                    {
                        FirstName = supplierData.ContactPersonFirstName,
                        LastName = supplierData.ContactPersonLastName,
                        Email = supplierData.Email,
                        Password = PasswordHasher.HashPassword(supplierData.Password), // Hash the password
                        Role = "Supplier",
                        Status = "Active"
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> UpdateSupplierAsync(SupplierData supplierData)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var supplier = await _context.Suppliers.FindAsync(supplierData.SupplierId);
                if (supplier == null) return false;

                var oldEmail = supplier.Email;

                supplier.SupplierName = supplierData.SupplierName;
                supplier.ContactPersonFirstName = supplierData.ContactPersonFirstName;
                supplier.ContactPersonLastName = supplierData.ContactPersonLastName;
                supplier.Email = supplierData.Email;
                supplier.ContactPersonNumber = supplierData.ContactPersonNumber;
                supplier.Address = supplierData.Address;
                supplier.Status = supplierData.Status;

                await _context.SaveChangesAsync();

                // Keep Supplier login in sync with Supplier record.
                // Passwords are stored in Users table (not Suppliers).
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == supplierData.Email);
                if (user == null && !string.IsNullOrWhiteSpace(oldEmail))
                {
                    user = await _context.Users.FirstOrDefaultAsync(u => u.Email == oldEmail);
                }

                // If there's a password provided, ensure a Supplier user exists.
                if (user == null)
                {
                    if (!string.IsNullOrWhiteSpace(supplierData.Password))
                    {
                        user = new User
                        {
                            FirstName = supplierData.ContactPersonFirstName,
                            LastName = supplierData.ContactPersonLastName,
                            Email = supplierData.Email,
                            Password = PasswordHasher.HashPassword(supplierData.Password),
                            Role = "Supplier",
                            Status = supplierData.Status
                        };
                        _context.Users.Add(user);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    user.FirstName = supplierData.ContactPersonFirstName;
                    user.LastName = supplierData.ContactPersonLastName;
                    user.Email = supplierData.Email;
                    user.Role = "Supplier";
                    user.Status = supplierData.Status;

                    if (!string.IsNullOrWhiteSpace(supplierData.Password))
                    {
                        user.Password = PasswordHasher.HashPassword(supplierData.Password);
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
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

        public async Task<SupplierData?> GetSupplierByIdAsync(int supplierId)
        {
            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null) return null;

            return new SupplierData
            {
                SupplierId = supplier.SupplierId,
                SupplierName = supplier.SupplierName ?? string.Empty,
                ContactPersonFirstName = supplier.ContactPersonFirstName ?? string.Empty,
                ContactPersonLastName = supplier.ContactPersonLastName ?? string.Empty,
                Email = supplier.Email ?? string.Empty,
                ContactPersonNumber = supplier.ContactPersonNumber ?? string.Empty,
                Address = supplier.Address ?? string.Empty,
                Status = supplier.Status ?? "Active"
                // Password is not returned for security
            };
        }

        // Procurement operations
        public async Task<bool> CreateProcurementAsync(ProcurementData procurementData)
        {
            try
            {
                var isCompliant = await IsSupplierCompliantForPolicyAsync(procurementData.SupplierId, procurementData.PolicyId);
                if (!isCompliant) return false;

                var supplierItem = await _context.SupplierItems
                    .FirstOrDefaultAsync(si => si.SupplierItemId == procurementData.SupplierItemId && si.SupplierId == procurementData.SupplierId);
                if (supplierItem == null) return false;
                if (!string.Equals(supplierItem.Status, "Available", StringComparison.OrdinalIgnoreCase)) return false;
                if (procurementData.Quantity <= 0 || supplierItem.QuantityAvailable < procurementData.Quantity) return false;

                supplierItem.QuantityAvailable -= procurementData.Quantity;
                if (supplierItem.QuantityAvailable <= 0)
                {
                    supplierItem.QuantityAvailable = 0;
                    supplierItem.Status = "OutOfStock";
                }
                supplierItem.LastUpdated = DateTime.UtcNow;

                var procurement = new Models.Procurement
                {
                    SupplierId = procurementData.SupplierId,
                    ItemName = supplierItem.ItemName,
                    Category = supplierItem.Category,
                    Quantity = procurementData.Quantity,
                    PurchaseDate = DateTime.UtcNow,
                    RelatedPolicyId = procurementData.PolicyId,
                    Status = ProcurementStatuses.Submitted,
                    SupplierResponseDeadline = DateTime.UtcNow.AddDays(7)
                };

                _context.Procurements.Add(procurement);
                await _context.SaveChangesAsync();

                _context.ProcurementStatusHistory.Add(new ProcurementStatusHistory
                {
                    ProcurementId = procurement.ProcurementId,
                    FromStatus = ProcurementStatuses.Draft,
                    ToStatus = ProcurementStatuses.Submitted,
                    ChangedAt = DateTime.UtcNow,
                    ChangedByUserId = null,
                    Reason = "Created by Admin"
                });

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
                if (!string.Equals(procurement.Status, ProcurementStatuses.Draft, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(procurement.Status, ProcurementStatuses.Submitted, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var isCompliant = await IsSupplierCompliantForPolicyAsync(procurementData.SupplierId, procurementData.PolicyId);
                if (!isCompliant) return false;

                procurement.SupplierId = procurementData.SupplierId;
                procurement.ItemName = procurementData.ItemName;
                procurement.Category = procurementData.Category;
                procurement.Quantity = procurementData.Quantity;
                procurement.RelatedPolicyId = procurementData.PolicyId;
                procurement.PurchaseDate = procurement.PurchaseDate ?? DateTime.UtcNow;
                procurement.SupplierResponseDeadline = procurement.SupplierResponseDeadline ?? DateTime.UtcNow.AddDays(7);
                procurement.Status = procurement.Status ?? ProcurementStatuses.Submitted;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<SupplierItemData>> GetSupplierItemsAsync(int supplierId)
        {
            return await _context.SupplierItems
                .Where(si => si.SupplierId == supplierId)
                .OrderBy(si => si.ItemName)
                .Select(si => new SupplierItemData
                {
                    SupplierItemId = si.SupplierItemId,
                    SupplierId = si.SupplierId,
                    ItemName = si.ItemName,
                    Category = si.Category ?? string.Empty,
                    QuantityAvailable = si.QuantityAvailable,
                    Status = si.Status
                })
                .ToListAsync();
        }

        public async Task<bool> UpsertSupplierItemAsync(int supplierId, SupplierItemData itemData)
        {
            try
            {
                var normalizedName = (itemData.ItemName ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(normalizedName)) return false;

                var item = await _context.SupplierItems
                    .FirstOrDefaultAsync(si => si.SupplierItemId == itemData.SupplierItemId && si.SupplierId == supplierId);

                if (item == null)
                {
                    item = await _context.SupplierItems
                        .FirstOrDefaultAsync(si => si.SupplierId == supplierId && si.ItemName == normalizedName);
                }

                if (item == null)
                {
                    item = new SupplierItem
                    {
                        SupplierId = supplierId,
                        ItemName = normalizedName,
                        Category = itemData.Category,
                        QuantityAvailable = Math.Max(0, itemData.QuantityAvailable),
                        Status = Math.Max(0, itemData.QuantityAvailable) > 0 ? "Available" : "OutOfStock",
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.SupplierItems.Add(item);
                }
                else
                {
                    item.ItemName = normalizedName;
                    item.Category = itemData.Category;
                    item.QuantityAvailable = Math.Max(0, itemData.QuantityAvailable);
                    item.Status = Math.Max(0, itemData.QuantityAvailable) > 0 ? "Available" : "OutOfStock";
                    item.LastUpdated = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SupplierRespondToProcurementAsync(SupplierProcurementActionData actionData)
        {
            try
            {
                var procurement = await _context.Procurements
                    .FirstOrDefaultAsync(p => p.ProcurementId == actionData.ProcurementId && p.SupplierId == actionData.SupplierId);
                if (procurement == null) return false;
                if (!string.Equals(procurement.Status, ProcurementStatuses.Submitted, StringComparison.OrdinalIgnoreCase)) return false;

                var previousStatus = procurement.Status;
                procurement.SupplierResponseDate = DateTime.UtcNow;

                if (!actionData.Approve)
                {
                    if (string.IsNullOrWhiteSpace(actionData.RejectionReason)) return false;
                    procurement.Status = ProcurementStatuses.SupplierRejected;
                    procurement.RejectionReason = actionData.RejectionReason.Trim();
                    procurement.SupplierCommitShipDate = null;
                }
                else
                {
                    if (actionData.SupplierCommitShipDate == null) return false;
                    procurement.Status = ProcurementStatuses.SupplierApproved;
                    procurement.SupplierCommitShipDate = actionData.SupplierCommitShipDate.Value.Date;
                    procurement.RejectionReason = null;
                }

                _context.ProcurementStatusHistory.Add(new ProcurementStatusHistory
                {
                    ProcurementId = procurement.ProcurementId,
                    FromStatus = previousStatus,
                    ToStatus = procurement.Status,
                    ChangedAt = DateTime.UtcNow,
                    ChangedByUserId = actionData.ChangedByUserId,
                    Reason = actionData.Approve ? "Supplier approved request" : actionData.RejectionReason
                });

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> IsSupplierCompliantForPolicyAsync(int supplierId, int? policyId)
        {
            if (!policyId.HasValue)
            {
                // If no policy is linked, keep previous strict behavior.
                var statuses = await _context.SupplierPolicies
                    .Where(sp => sp.SupplierId == supplierId)
                    .Select(sp => sp.ComplianceStatus)
                    .ToListAsync();

                if (!statuses.Any()) return false;
                return statuses.All(status => string.Equals(status, "Compliant", StringComparison.OrdinalIgnoreCase));
            }

            return await _context.SupplierPolicies.AnyAsync(sp =>
                sp.SupplierId == supplierId &&
                sp.PolicyId == policyId.Value &&
                sp.ComplianceStatus == "Compliant");
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

        public async Task<PolicyAssignmentStatusData> GetPolicyAssignmentStatusAsync(int policyId)
        {
            var assignedEmployeeIds = await _context.PolicyAssignments
                .Where(pa => pa.PolicyId == policyId)
                .Select(pa => pa.UserId)
                .Distinct()
                .ToListAsync();

            var assignedSupplierIds = await _context.SupplierPolicies
                .Where(sp => sp.PolicyId == policyId)
                .Select(sp => sp.SupplierId)
                .Distinct()
                .ToListAsync();

            return new PolicyAssignmentStatusData
            {
                AssignedEmployeeIds = assignedEmployeeIds,
                AssignedSupplierIds = assignedSupplierIds
            };
        }

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
