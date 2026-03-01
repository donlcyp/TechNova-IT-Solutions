using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Models;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly ExternalApisConfiguration _externalApiConfiguration;
        private readonly ILogger<AdminService> _logger;

        public AdminService(
            ApplicationDbContext context,
            IEmailService emailService,
            IExchangeRateService exchangeRateService,
            Microsoft.Extensions.Options.IOptions<ExternalApisConfiguration> externalApiConfiguration,
            ILogger<AdminService> logger)
        {
            _context = context;
            _emailService = emailService;
            _exchangeRateService = exchangeRateService;
            _externalApiConfiguration = externalApiConfiguration.Value;
            _logger = logger;
        }

        public async Task<AdminDashboardData> GetDashboardDataAsync(int? branchId = null)
        {
            var data = new AdminDashboardData();
            bool scoped = branchId.HasValue;

            // Get statistics — scoped to branch when branchId is provided
            data.TotalUsers = scoped
                ? await _context.Users.Where(u => u.BranchId == branchId).CountAsync()
                : await _context.Users.CountAsync();

            data.ActivePolicies = scoped
                ? await _context.Policies.Where(p => !p.IsArchived && (p.BranchId == branchId || p.BranchId == null)).CountAsync()
                : await _context.Policies.Where(p => !p.IsArchived).CountAsync();

            data.TotalSuppliers = scoped
                ? await _context.Suppliers.Where(s => s.Status == "Active" && (s.BranchId == branchId || s.BranchId == null)).CountAsync()
                : await _context.Suppliers.Where(s => s.Status == "Active").CountAsync();

            // Pending compliance — scoped via policy assignments for this branch's employees
            var pendingQuery = _context.ComplianceStatuses.Where(cs => cs.Status == "Pending");
            if (scoped)
                pendingQuery = pendingQuery.Where(cs => cs.PolicyAssignment.User.BranchId == branchId);
            data.PendingCompliance = await pendingQuery.CountAsync();

            // Recent procurements count (last 30 days)
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var procQuery = _context.Procurements.Where(p => p.PurchaseDate >= thirtyDaysAgo);
            if (scoped)
                procQuery = procQuery.Where(p => p.BranchId == branchId || p.BranchId == null);
            data.RecentProcurements = await procQuery.CountAsync();

            // Audit logs today
            var today = DateTime.Today;
            var auditQuery = _context.AuditLogs.Where(al => al.LogDate.Date == today);
            if (scoped)
                auditQuery = auditQuery.Where(al => al.UserId != null &&
                    _context.Users.Any(u => u.UserId == al.UserId && u.BranchId == branchId));
            data.AuditLogsToday = await auditQuery.CountAsync();

            // Compliance percentage
            var totalAssignments = scoped
                ? await _context.PolicyAssignments
                    .Where(pa => pa.User.BranchId == branchId)
                    .CountAsync()
                : await _context.PolicyAssignments.CountAsync();

            var acknowledgedQuery = _context.ComplianceStatuses.Where(cs => cs.Status == "Acknowledged");
            if (scoped)
                acknowledgedQuery = acknowledgedQuery.Where(cs => cs.PolicyAssignment.User.BranchId == branchId);
            var acknowledgedCount = await acknowledgedQuery.CountAsync();

            if (totalAssignments > 0)
                data.CompliancePercentage = (int)((double)acknowledgedCount / totalAssignments * 100);

            // Recent Policies — scoped to branch (show company-wide + branch-specific)
            var recentPoliciesQuery = _context.Policies.AsQueryable();
            if (scoped)
                recentPoliciesQuery = recentPoliciesQuery.Where(p => p.BranchId == branchId || p.BranchId == null);
            data.RecentPolicies = await recentPoliciesQuery
                .OrderByDescending(p => p.DateUploaded)
                .Take(5)
                .Select(p => new PolicyItem
                {
                    Name = p.PolicyTitle,
                    AssignedDate = p.DateUploaded ?? DateTime.Now
                })
                .ToListAsync();

            // Recent Procurements
            var recentProcQuery = _context.Procurements
                .Include(p => p.Supplier)
                .Include(p => p.RelatedPolicy)
                .OrderByDescending(p => p.PurchaseDate);

            data.RecentProcurementsData = await (scoped
                ? recentProcQuery.Where(p => p.BranchId == branchId || p.BranchId == null)
                : recentProcQuery)
                .Take(5)
                .Select(p => new ProcurementItem
                {
                    Supplier = p.Supplier != null ? p.Supplier.SupplierName : "N/A",
                    Item = p.ItemName ?? "N/A",
                    Date = p.PurchaseDate ?? DateTime.Now,
                    LinkedPolicy = p.RelatedPolicy != null ? p.RelatedPolicy.PolicyTitle : "General"
                })
                .ToListAsync();

            // Recent Activities
            var recentAuditQuery = _context.AuditLogs
                .Include(al => al.User)
                .OrderByDescending(al => al.LogDate);

            data.RecentActivities = await (scoped
                ? recentAuditQuery.Where(al => al.UserId != null &&
                    _context.Users.Any(u => u.UserId == al.UserId && u.BranchId == branchId))
                : recentAuditQuery)
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
                    DateUploaded = policyData.UploadedDate ?? DateTime.Now,
                    BranchId = policyData.BranchId  // null = company-wide, non-null = branch-specific
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create policy {PolicyTitle}", policyData.PolicyTitle);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update policy {PolicyId}", policyData.PolicyId);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive policy {PolicyId}", policyId);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore policy {PolicyId}", policyId);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete policy {PolicyId}", policyId);
                return false;
            }
        }

        // Supplier operations
        public async Task<SupplierOperationResult> CreateSupplierAsync(SupplierData supplierData)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cleanedData = NormalizeSupplierData(supplierData);
                var validation = ValidateSupplierData(cleanedData);
                if (!validation.Success)
                {
                    return validation;
                }

                var normalizedEmail = cleanedData.Email.Trim().ToLowerInvariant();
                var supplierEmail = cleanedData.Email.Trim();

                var supplierEmailExists = await _context.Suppliers
                    .AnyAsync(s => s.Email != null && s.Email.Trim().ToLower() == normalizedEmail);
                if (supplierEmailExists)
                {
                    return new SupplierOperationResult { Success = false, Message = "A supplier account with this email already exists." };
                }

                var userEmailExists = await _context.Users
                    .AnyAsync(u => u.Email != null && u.Email.Trim().ToLower() == normalizedEmail);
                if (userEmailExists)
                {
                    return new SupplierOperationResult { Success = false, Message = "A user account with this email already exists." };
                }

                // 1. Create Supplier Record
                var supplier = new Models.Supplier
                {
                    SupplierName = cleanedData.SupplierName,
                    ContactPersonFirstName = cleanedData.ContactPersonFirstName,
                    ContactPersonLastName = cleanedData.ContactPersonLastName,
                    Email = supplierEmail,
                    ContactPersonNumber = cleanedData.ContactPersonNumber,
                    Address = cleanedData.Address,
                    Status = cleanedData.Status,
                    BranchId = cleanedData.BranchId   // null = global/enterprise supplier
                };

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                // 2. Create User Record for Login
                // Check if user already exists
                var existingUser = await _context.Users.AnyAsync(u =>
                    u.Email != null && u.Email.Trim().ToLower() == normalizedEmail);
                if (!existingUser && !string.IsNullOrWhiteSpace(cleanedData.Password))
                {
                    var user = new User
                    {
                        FirstName = cleanedData.ContactPersonFirstName,
                        LastName = cleanedData.ContactPersonLastName,
                        Email = supplierEmail,
                        Password = PasswordHasher.HashPassword(cleanedData.Password), // Hash the password
                        Role = "Supplier",
                        Status = "Active",
                        BranchId = cleanedData.BranchId  // inherit branch from supplier record
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return new SupplierOperationResult { Success = true, Message = "Supplier created successfully." };
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _logger.LogWarning(ex, "Duplicate email prevented supplier creation for {Email}", supplierData.Email);
                await transaction.RollbackAsync();
                return new SupplierOperationResult { Success = false, Message = "Email already exists in another account." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create supplier for email {Email}", supplierData.Email);
                await transaction.RollbackAsync();
                return new SupplierOperationResult { Success = false, Message = "Failed to create supplier. Check data format and field lengths." };
            }
        }

        public async Task<SupplierOperationResult> UpdateSupplierAsync(SupplierData supplierData)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cleanedData = NormalizeSupplierData(supplierData);
                cleanedData.SupplierId = supplierData.SupplierId;

                var validation = ValidateSupplierData(cleanedData);
                if (!validation.Success)
                {
                    return validation;
                }

                var normalizedEmail = cleanedData.Email.Trim().ToLowerInvariant();
                var supplierEmail = cleanedData.Email.Trim();

                var supplierEmailExists = await _context.Suppliers
                    .AnyAsync(s => s.SupplierId != cleanedData.SupplierId && s.Email != null && s.Email.Trim().ToLower() == normalizedEmail);
                if (supplierEmailExists)
                {
                    return new SupplierOperationResult { Success = false, Message = "Another supplier account already uses this email." };
                }

                var supplier = await _context.Suppliers.FindAsync(cleanedData.SupplierId);
                if (supplier == null)
                {
                    return new SupplierOperationResult { Success = false, Message = "Supplier not found." };
                }
                if (string.Equals(supplier.Status, "Terminated", StringComparison.OrdinalIgnoreCase))
                {
                    return new SupplierOperationResult { Success = false, Message = "Terminated suppliers cannot be edited. Use Restore first." };
                }

                var oldEmail = supplier.Email;
                var oldEmailNormalized = (oldEmail ?? string.Empty).Trim().ToLowerInvariant();
                var userEmailExists = await _context.Users
                    .AnyAsync(u => u.Email != null &&
                                   u.Email.Trim().ToLower() == normalizedEmail &&
                                   u.Email.Trim().ToLower() != oldEmailNormalized);
                if (userEmailExists)
                {
                    return new SupplierOperationResult { Success = false, Message = "A user account with this email already exists." };
                }

                supplier.SupplierName = cleanedData.SupplierName;
                supplier.ContactPersonFirstName = cleanedData.ContactPersonFirstName;
                supplier.ContactPersonLastName = cleanedData.ContactPersonLastName;
                supplier.Email = supplierEmail;
                supplier.ContactPersonNumber = cleanedData.ContactPersonNumber;
                supplier.Address = cleanedData.Address;
                supplier.Status = cleanedData.Status;
                if (!string.Equals(supplier.Status, "Terminated", StringComparison.OrdinalIgnoreCase))
                {
                    supplier.TerminationReason = null;
                    supplier.TerminatedAt = null;
                    supplier.TerminatedByUserId = null;
                }

                await _context.SaveChangesAsync();

                // Keep Supplier login in sync with Supplier record.
                // Passwords are stored in Users table (not Suppliers).
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.Trim().ToLower() == normalizedEmail);
                if (user == null && !string.IsNullOrWhiteSpace(oldEmail))
                {
                    var oldEmailKey = oldEmail.Trim().ToLowerInvariant();
                    user = await _context.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.Trim().ToLower() == oldEmailKey);
                }

                // If there's a password provided, ensure a Supplier user exists.
                if (user == null)
                {
                    if (!string.IsNullOrWhiteSpace(cleanedData.Password))
                    {
                        user = new User
                        {
                            FirstName = cleanedData.ContactPersonFirstName,
                            LastName = cleanedData.ContactPersonLastName,
                            Email = supplierEmail,
                            Password = PasswordHasher.HashPassword(cleanedData.Password),
                            Role = "Supplier",
                            Status = cleanedData.Status,
                            BranchId = cleanedData.BranchId  // inherit branch from supplier record
                        };
                        _context.Users.Add(user);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    user.FirstName = cleanedData.ContactPersonFirstName;
                    user.LastName = cleanedData.ContactPersonLastName;
                    user.Email = supplierEmail;
                    user.Role = "Supplier";
                    user.Status = cleanedData.Status;
                    user.BranchId = cleanedData.BranchId;  // keep BranchId in sync

                    if (!string.IsNullOrWhiteSpace(cleanedData.Password))
                    {
                        user.Password = PasswordHasher.HashPassword(cleanedData.Password);
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return new SupplierOperationResult { Success = true, Message = "Supplier updated successfully." };
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _logger.LogWarning(ex, "Duplicate email prevented supplier update for {SupplierId}", supplierData.SupplierId);
                await transaction.RollbackAsync();
                return new SupplierOperationResult { Success = false, Message = "Email already exists in another account." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update supplier {SupplierId}", supplierData.SupplierId);
                await transaction.RollbackAsync();
                return new SupplierOperationResult { Success = false, Message = "Failed to update supplier. Check data format and field lengths." };
            }
        }

        private static SupplierData NormalizeSupplierData(SupplierData supplierData)
        {
            return new SupplierData
            {
                SupplierId = supplierData.SupplierId,
                SupplierName = (supplierData.SupplierName ?? string.Empty).Trim(),
                ContactPersonFirstName = (supplierData.ContactPersonFirstName ?? string.Empty).Trim(),
                ContactPersonLastName = (supplierData.ContactPersonLastName ?? string.Empty).Trim(),
                Email = (supplierData.Email ?? string.Empty).Trim(),
                ContactPersonNumber = (supplierData.ContactPersonNumber ?? string.Empty).Trim(),
                Address = (supplierData.Address ?? string.Empty).Trim(),
                Status = string.IsNullOrWhiteSpace(supplierData.Status) ? "Active" : supplierData.Status.Trim(),
                Password = string.IsNullOrWhiteSpace(supplierData.Password) ? null : supplierData.Password.Trim(),
                BranchId = supplierData.BranchId
            };
        }

        private static SupplierOperationResult ValidateSupplierData(SupplierData supplierData)
        {
            if (string.IsNullOrWhiteSpace(supplierData.SupplierName))
            {
                return new SupplierOperationResult { Success = false, Message = "Supplier name is required." };
            }

            if (supplierData.SupplierName.Length > 255)
            {
                return new SupplierOperationResult { Success = false, Message = "Supplier name must be 255 characters or fewer." };
            }

            if (string.IsNullOrWhiteSpace(supplierData.ContactPersonFirstName) || string.IsNullOrWhiteSpace(supplierData.ContactPersonLastName))
            {
                return new SupplierOperationResult { Success = false, Message = "Contact first name and last name are required." };
            }

            if (supplierData.ContactPersonFirstName.Length > 100 || supplierData.ContactPersonLastName.Length > 100)
            {
                return new SupplierOperationResult { Success = false, Message = "Contact names must be 100 characters or fewer." };
            }

            if (string.IsNullOrWhiteSpace(supplierData.Email))
            {
                return new SupplierOperationResult { Success = false, Message = "Email is required." };
            }

            if (supplierData.Email.Length > 255)
            {
                return new SupplierOperationResult { Success = false, Message = "Email must be 255 characters or fewer." };
            }

            try
            {
                _ = new System.Net.Mail.MailAddress(supplierData.Email);
            }
            catch
            {
                return new SupplierOperationResult { Success = false, Message = "Please enter a valid email address." };
            }

            if (string.IsNullOrWhiteSpace(supplierData.ContactPersonNumber))
            {
                return new SupplierOperationResult { Success = false, Message = "Contact number is required." };
            }

            if (supplierData.ContactPersonNumber.Length > 20)
            {
                return new SupplierOperationResult { Success = false, Message = "Contact number must be 20 characters or fewer." };
            }

            if (string.IsNullOrWhiteSpace(supplierData.Address))
            {
                return new SupplierOperationResult { Success = false, Message = "Address is required." };
            }

            if (supplierData.Address.Length > 500)
            {
                return new SupplierOperationResult { Success = false, Message = "Address must be 500 characters or fewer." };
            }

            if (supplierData.Status.Length > 20)
            {
                return new SupplierOperationResult { Success = false, Message = "Status must be 20 characters or fewer." };
            }

            return new SupplierOperationResult { Success = true };
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx)
            {
                return sqlEx.Number == 2601 || sqlEx.Number == 2627;
            }

            return false;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete supplier {SupplierId}", supplierId);
                return false;
            }
        }

        public async Task<bool> TerminateSupplierAsync(SupplierTerminationData terminationData, int? changedByUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (terminationData.SupplierId <= 0) return false;
                var reason = (terminationData.Reason ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(reason)) return false;

                var supplier = await _context.Suppliers.FindAsync(terminationData.SupplierId);
                if (supplier == null) return false;
                if (string.Equals(supplier.Status, "Terminated", StringComparison.OrdinalIgnoreCase)) return false;

                supplier.Status = "Terminated";
                supplier.TerminationReason = reason;
                supplier.TerminatedAt = DateTime.UtcNow;
                supplier.TerminatedByUserId = changedByUserId;

                if (!string.IsNullOrWhiteSpace(supplier.Email))
                {
                    var normalizedEmail = supplier.Email.Trim().ToLowerInvariant();
                    var linkedUsers = await _context.Users
                        .Where(u => u.Email != null &&
                                    u.Email.ToLower() == normalizedEmail &&
                                    u.Role == "Supplier")
                        .ToListAsync();
                    foreach (var linkedUser in linkedUsers)
                    {
                        linkedUser.Status = "Inactive";
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (!string.IsNullOrWhiteSpace(supplier.Email))
                {
                    try
                    {
                        var subject = $"Supplier Contract Terminated - {supplier.SupplierName}";
                        var body = $@"
                            <h3>Contract Termination Notice</h3>
                            <p>Dear {supplier.ContactPersonFirstName ?? supplier.SupplierName},</p>
                            <p>Your supplier contract with TechNova has been terminated effective {DateTime.UtcNow:MMM dd, yyyy}.</p>
                            <p><strong>Reason:</strong> {reason}</p>
                            <p>If you believe this is an error, please contact TechNova administration.</p>";
                        _ = _emailService.SendEmailAsync(supplier.Email, subject, body);
                    }
                    catch (Exception mailEx)
                    {
                        _logger.LogWarning(mailEx, "Termination email failed for supplier {SupplierId}", supplier.SupplierId);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to terminate supplier {SupplierId}", terminationData.SupplierId);
                return false;
            }
        }

        public async Task<bool> RestoreSupplierAsync(int supplierId, int? changedByUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (supplierId <= 0) return false;
                var supplier = await _context.Suppliers.FindAsync(supplierId);
                if (supplier == null) return false;
                if (!string.Equals(supplier.Status, "Terminated", StringComparison.OrdinalIgnoreCase)) return false;

                supplier.Status = "Active";
                supplier.TerminationReason = null;
                supplier.TerminatedAt = null;
                supplier.TerminatedByUserId = null;

                if (!string.IsNullOrWhiteSpace(supplier.Email))
                {
                    var normalizedEmail = supplier.Email.Trim().ToLowerInvariant();
                    var linkedUsers = await _context.Users
                        .Where(u => u.Email != null &&
                                    u.Email.ToLower() == normalizedEmail &&
                                    u.Role == "Supplier")
                        .ToListAsync();
                    foreach (var linkedUser in linkedUsers)
                    {
                        linkedUser.Status = "Active";
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to restore supplier {SupplierId} by user {ChangedBy}", supplierId, changedByUserId);
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
                Status = supplier.Status ?? "Active",
                TerminationReason = supplier.TerminationReason,
                TerminatedAt = supplier.TerminatedAt,
                TerminatedByUserId = supplier.TerminatedByUserId,
                BranchId = supplier.BranchId
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

                var normalizedCurrency = (supplierItem.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
                if (normalizedCurrency.Length != 3) return false;
                if (supplierItem.UnitPrice <= 0) return false;

                var baseCurrency = _externalApiConfiguration.ExchangeRateApi.BaseCurrency.Trim().ToUpperInvariant();
                var exchangeResult = await _exchangeRateService.GetRateAsync(normalizedCurrency, baseCurrency);
                if (!exchangeResult.Success) return false;

                var originalAmount = decimal.Round(supplierItem.UnitPrice * procurementData.Quantity, 2);
                var convertedAmount = decimal.Round(originalAmount * exchangeResult.Rate, 2);

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
                    CurrencyCode = normalizedCurrency,
                    OriginalAmount = originalAmount,
                    ExchangeRate = exchangeResult.Rate,
                    ConvertedAmount = convertedAmount,
                    ConversionTimestamp = exchangeResult.RetrievedAtUtc,
                    Status = ProcurementStatuses.Submitted,
                    SupplierResponseDeadline = DateTime.UtcNow.AddDays(7),
                    RevisedDeliveryDate = null,
                    DelayReason = null,
                    BranchId = procurementData.BranchId  // null = company-wide, non-null = branch-specific
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create procurement for supplier {SupplierId}", procurementData.SupplierId);
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
                if (!procurementData.SupplierItemId.HasValue) return false;
                if (procurementData.Quantity <= 0) return false;

                var supplierItem = await _context.SupplierItems
                    .FirstOrDefaultAsync(si => si.SupplierItemId == procurementData.SupplierItemId && si.SupplierId == procurementData.SupplierId);
                if (supplierItem == null) return false;
                if (!string.Equals(supplierItem.Status, "Available", StringComparison.OrdinalIgnoreCase)) return false;

                var previousReservedQty = Math.Max(0, procurement.Quantity ?? 0);
                var previousReservedItem = await FindSupplierItemForProcurementReservationAsync(procurement);
                var sameReservedItem = previousReservedItem?.SupplierItemId == supplierItem.SupplierItemId;
                var effectiveAvailability = supplierItem.QuantityAvailable + (sameReservedItem ? previousReservedQty : 0);
                if (effectiveAvailability < procurementData.Quantity) return false;

                procurement.SupplierId = procurementData.SupplierId;
                procurement.ItemName = supplierItem.ItemName;
                procurement.Category = supplierItem.Category;
                procurement.Quantity = procurementData.Quantity;
                procurement.RelatedPolicyId = procurementData.PolicyId;
                procurement.PurchaseDate = procurement.PurchaseDate ?? DateTime.UtcNow;

                var normalizedCurrency = (supplierItem.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
                if (normalizedCurrency.Length != 3) return false;
                if (supplierItem.UnitPrice <= 0) return false;

                var baseCurrency = _externalApiConfiguration.ExchangeRateApi.BaseCurrency.Trim().ToUpperInvariant();
                var exchangeResult = await _exchangeRateService.GetRateAsync(normalizedCurrency, baseCurrency);
                if (!exchangeResult.Success) return false;

                var originalAmount = decimal.Round(supplierItem.UnitPrice * procurementData.Quantity, 2);

                if (previousReservedItem != null && previousReservedQty > 0)
                {
                    previousReservedItem.QuantityAvailable += previousReservedQty;
                    ApplySupplierItemStockStatus(previousReservedItem);
                    previousReservedItem.LastUpdated = DateTime.UtcNow;
                }

                supplierItem.QuantityAvailable -= procurementData.Quantity;
                if (supplierItem.QuantityAvailable < 0) supplierItem.QuantityAvailable = 0;
                ApplySupplierItemStockStatus(supplierItem);
                supplierItem.LastUpdated = DateTime.UtcNow;

                procurement.CurrencyCode = normalizedCurrency;
                procurement.OriginalAmount = originalAmount;
                procurement.ExchangeRate = exchangeResult.Rate;
                procurement.ConvertedAmount = decimal.Round(originalAmount * exchangeResult.Rate, 2);
                procurement.ConversionTimestamp = exchangeResult.RetrievedAtUtc;
                procurement.SupplierResponseDeadline = procurement.SupplierResponseDeadline ?? DateTime.UtcNow.AddDays(7);
                procurement.Status = procurement.Status ?? ProcurementStatuses.Submitted;
                procurement.RevisedDeliveryDate = procurementData.RevisedDeliveryDate;
                procurement.DelayReason = procurementData.DelayReason;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update procurement {ProcurementId}", procurementData.ProcurementId);
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
                    UnitPrice = si.UnitPrice,
                    CurrencyCode = si.CurrencyCode,
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
                var normalizedCurrency = (itemData.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
                if (normalizedCurrency.Length != 3) return false;
                if (itemData.UnitPrice <= 0) return false;

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
                        UnitPrice = decimal.Round(itemData.UnitPrice, 2),
                        CurrencyCode = normalizedCurrency,
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
                    item.UnitPrice = decimal.Round(itemData.UnitPrice, 2);
                    item.CurrencyCode = normalizedCurrency;
                    item.QuantityAvailable = Math.Max(0, itemData.QuantityAvailable);
                    item.Status = Math.Max(0, itemData.QuantityAvailable) > 0 ? "Available" : "OutOfStock";
                    item.LastUpdated = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert supplier item for supplier {SupplierId}", supplierId);
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

                    var reservedItem = await FindSupplierItemForProcurementReservationAsync(procurement);
                    var reservedQty = Math.Max(0, procurement.Quantity ?? 0);
                    if (reservedItem != null && reservedQty > 0)
                    {
                        reservedItem.QuantityAvailable += reservedQty;
                        ApplySupplierItemStockStatus(reservedItem);
                        reservedItem.LastUpdated = DateTime.UtcNow;
                    }
                }
                else
                {
                    if (actionData.SupplierCommitShipDate == null) return false;
                    procurement.Status = ProcurementStatuses.SupplierApproved;
                    procurement.SupplierCommitShipDate = actionData.SupplierCommitShipDate.Value.Date;
                    procurement.RevisedDeliveryDate = null;
                    procurement.DelayReason = null;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed supplier response for procurement {ProcurementId}", actionData.ProcurementId);
                return false;
            }
        }

        public async Task<bool> SupplierReportDelayAsync(SupplierProcurementActionData actionData)
        {
            try
            {
                var procurement = await _context.Procurements
                    .FirstOrDefaultAsync(p => p.ProcurementId == actionData.ProcurementId && p.SupplierId == actionData.SupplierId);
                if (procurement == null) return false;

                if (!string.Equals(procurement.Status, ProcurementStatuses.SupplierApproved, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(procurement.Status, ProcurementStatuses.Late, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(actionData.DelayReason)) return false;
                if (!actionData.RevisedDeliveryDate.HasValue) return false;

                var revisedDate = actionData.RevisedDeliveryDate.Value.Date;
                if (revisedDate < DateTime.UtcNow.Date) return false;
                if (procurement.SupplierCommitShipDate.HasValue &&
                    revisedDate <= procurement.SupplierCommitShipDate.Value.Date)
                {
                    return false;
                }

                var previousStatus = procurement.Status;
                procurement.Status = ProcurementStatuses.Late;
                procurement.DelayReason = actionData.DelayReason.Trim();
                procurement.RevisedDeliveryDate = revisedDate;

                if (!string.Equals(previousStatus, ProcurementStatuses.Late, StringComparison.OrdinalIgnoreCase))
                {
                    _context.ProcurementStatusHistory.Add(new ProcurementStatusHistory
                    {
                        ProcurementId = procurement.ProcurementId,
                        FromStatus = previousStatus,
                        ToStatus = ProcurementStatuses.Late,
                        ChangedAt = DateTime.UtcNow,
                        ChangedByUserId = actionData.ChangedByUserId,
                        Reason = procurement.DelayReason
                    });

                    await NotifyLateEscalationAsync(procurement, procurement.DelayReason);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed delay report for procurement {ProcurementId}", actionData.ProcurementId);
                return false;
            }
        }

        private static void ApplySupplierItemStockStatus(SupplierItem item)
        {
            item.Status = item.QuantityAvailable > 0 ? "Available" : "OutOfStock";
        }

        private async Task<SupplierItem?> FindSupplierItemForProcurementReservationAsync(Models.Procurement procurement)
        {
            if (!procurement.SupplierId.HasValue || string.IsNullOrWhiteSpace(procurement.ItemName))
            {
                return null;
            }

            return await _context.SupplierItems
                .Where(si => si.SupplierId == procurement.SupplierId.Value && si.ItemName == procurement.ItemName)
                .OrderByDescending(si => si.SupplierItemId)
                .FirstOrDefaultAsync();
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

                if (!string.Equals(procurement.Status, ProcurementStatuses.Received, StringComparison.OrdinalIgnoreCase))
                {
                    var reservedItem = await FindSupplierItemForProcurementReservationAsync(procurement);
                    var reservedQty = Math.Max(0, procurement.Quantity ?? 0);
                    if (reservedItem != null && reservedQty > 0)
                    {
                        reservedItem.QuantityAvailable += reservedQty;
                        ApplySupplierItemStockStatus(reservedItem);
                        reservedItem.LastUpdated = DateTime.UtcNow;
                    }
                }

                _context.Procurements.Remove(procurement);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete procurement {ProcurementId}", procurementId);
                return false;
            }
        }

        public async Task<bool> MarkProcurementDeliveredAsync(int procurementId, int? changedByUserId)
        {
            try
            {
                var procurement = await _context.Procurements.FindAsync(procurementId);
                if (procurement == null) return false;
                if (!string.Equals(procurement.Status, ProcurementStatuses.SupplierApproved, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(procurement.Status, ProcurementStatuses.Late, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var previousStatus = procurement.Status;
                procurement.Status = ProcurementStatuses.Received;
                procurement.ReceivedDate = DateTime.UtcNow;

                _context.ProcurementStatusHistory.Add(new ProcurementStatusHistory
                {
                    ProcurementId = procurement.ProcurementId,
                    FromStatus = previousStatus,
                    ToStatus = ProcurementStatuses.Received,
                    ChangedAt = DateTime.UtcNow,
                    ChangedByUserId = changedByUserId,
                    Reason = "Delivery marked as arrived by Admin"
                });

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark procurement {ProcurementId} as delivered", procurementId);
                return false;
            }
        }

        public async Task<int> SyncLateProcurementsAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var candidates = await _context.Procurements
                    .Where(p => p.Status == ProcurementStatuses.SupplierApproved || p.Status == ProcurementStatuses.Late)
                    .ToListAsync();

                var markedCount = 0;

                foreach (var procurement in candidates)
                {
                    // 7-day window starts on Delivery Begin date (Day 1), so deadline is +6 days.
                    var expectedArrival = procurement.RevisedDeliveryDate?.Date ??
                                          (procurement.SupplierCommitShipDate?.Date.AddDays(6));
                    if (!expectedArrival.HasValue)
                    {
                        continue;
                    }

                    if (expectedArrival.Value < today &&
                        string.Equals(procurement.Status, ProcurementStatuses.SupplierApproved, StringComparison.OrdinalIgnoreCase))
                    {
                        var previousStatus = procurement.Status;
                        procurement.Status = ProcurementStatuses.Late;
                        markedCount++;

                        _context.ProcurementStatusHistory.Add(new ProcurementStatusHistory
                        {
                            ProcurementId = procurement.ProcurementId,
                            FromStatus = previousStatus,
                            ToStatus = ProcurementStatuses.Late,
                            ChangedAt = DateTime.UtcNow,
                            ChangedByUserId = null,
                            Reason = "Auto-flagged late: planned delivery date has passed."
                        });

                        await NotifyLateEscalationAsync(procurement, "Auto-flagged late by system.");
                    }
                    else if (expectedArrival.Value >= today &&
                             string.Equals(procurement.Status, ProcurementStatuses.Late, StringComparison.OrdinalIgnoreCase))
                    {
                        // Keep UI and workflow in sync with the current SLA window.
                        procurement.Status = ProcurementStatuses.SupplierApproved;
                        markedCount++;

                        _context.ProcurementStatusHistory.Add(new ProcurementStatusHistory
                        {
                            ProcurementId = procurement.ProcurementId,
                            FromStatus = ProcurementStatuses.Late,
                            ToStatus = ProcurementStatuses.SupplierApproved,
                            ChangedAt = DateTime.UtcNow,
                            ChangedByUserId = null,
                            Reason = "Auto-corrected: request is still within the allowed delivery window."
                        });
                    }
                }

                if (markedCount == 0) return 0;

                await _context.SaveChangesAsync();
                return markedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync late procurements");
                return 0;
            }
        }

        private async Task NotifyLateEscalationAsync(Models.Procurement procurement, string reason)
        {
            try
            {
                var recipients = new List<string>();

                var supplierEmail = await _context.Suppliers
                    .Where(s => s.SupplierId == procurement.SupplierId)
                    .Select(s => s.Email)
                    .FirstOrDefaultAsync();
                if (!string.IsNullOrWhiteSpace(supplierEmail))
                {
                    recipients.Add(supplierEmail);
                }

                var staffEmails = await _context.Users
                    .Where(u => (u.Role == "Admin" || u.Role == "ChiefComplianceManager" || u.Role == "ComplianceManager" || u.Role == "SuperAdmin") &&
                                u.Status == "Active" &&
                                u.Email != null)
                    .Select(u => u.Email!)
                    .Distinct()
                    .ToListAsync();
                recipients.AddRange(staffEmails);

                recipients = recipients
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!recipients.Any()) return;

                var subject = $"Late Delivery Escalation: PROC-{procurement.ProcurementId:D3}";
                var body = $@"
                    <h3>Late Delivery Alert</h3>
                    <p><strong>Procurement ID:</strong> PROC-{procurement.ProcurementId:D3}</p>
                    <p><strong>Item:</strong> {procurement.ItemName}</p>
                    <p><strong>Status:</strong> {procurement.Status}</p>
                    <p><strong>Delivery Begin:</strong> {(procurement.SupplierCommitShipDate?.ToString("MMM dd, yyyy") ?? "N/A")}</p>
                    <p><strong>Revised Delivery:</strong> {(procurement.RevisedDeliveryDate?.ToString("MMM dd, yyyy") ?? "N/A")}</p>
                    <p><strong>Reason:</strong> {reason}</p>";

                foreach (var email in recipients)
                {
                    _ = _emailService.SendEmailAsync(email, subject, body);
                }
            }
            catch
            {
                // Notification failure must not block core transaction.
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
                    var isActiveSupplier = await _context.Suppliers
                        .AnyAsync(s => s.SupplierId == supId && s.Status == "Active");
                    if (!isActiveSupplier) continue;

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
