using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;

        public AdminController(IAdminService adminService, IUserService userService, IEmailService emailService, IWebHostEnvironment environment, ApplicationDbContext context)
        {
            _adminService = adminService;
            _userService = userService;
            _emailService = emailService;
            _environment = environment;
            _context = context;
        }

        private bool IsAdmin()
        {
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            return userRole == RoleNames.Admin || userRole == RoleNames.SuperAdmin;
        }

        private bool IsCurrentUserSuperAdmin()
        {
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            return string.Equals(userRole, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPrivilegedAdminRole(string? role)
        {
            return string.Equals(role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IActionResult> Dashboard()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var data = await _adminService.GetDashboardDataAsync();
            return View(data);
        }

        public async Task<IActionResult> UserManagement()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserData userData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (userData == null)
            {
                return BadRequest(new { success = false, message = "Invalid user data" });
            }
            if (!IsCurrentUserSuperAdmin() && IsPrivilegedAdminRole(userData.Role))
            {
                return BadRequest(new { success = false, message = "System Administrator cannot create Admin or Super Admin accounts." });
            }

            var result = await _userService.CreateUserAsync(userData);

            if (result.Success)
            {
                var message = "User created successfully.";
                if (result.EmailAttempted)
                {
                    message = result.EmailSent
                        ? "User created successfully. Account email was sent."
                        : $"User created successfully, but account email failed: {result.EmailError ?? "Unknown error"}";
                }

                return Ok(new
                {
                    success = true,
                    message,
                    emailAttempted = result.EmailAttempted,
                    emailSent = result.EmailSent,
                    emailError = result.EmailError
                });
            }
            
            return BadRequest(new { success = false, message = "Failed to create user" });
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(int userId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var user = await _userService.GetUserByIdAsync(userId);
            
            if (user != null)
            {
                return Ok(new { success = true, user = user });
            }
            
            return NotFound(new { success = false, message = "User not found" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser([FromBody] UserData userData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (userData == null)
            {
                return BadRequest(new { success = false, message = "Invalid user data" });
            }
            if (!IsCurrentUserSuperAdmin() && IsPrivilegedAdminRole(userData.Role))
            {
                return BadRequest(new { success = false, message = "System Administrator cannot assign Admin or Super Admin roles." });
            }

            var result = await _userService.UpdateUserAsync(userData);
            
            if (result)
            {
                return Ok(new { success = true, message = "User updated successfully" });
            }
            
            return BadRequest(new { success = false, message = "Failed to update user" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var result = await _userService.DeleteUserAsync(userId);
            
            if (result)
            {
                return Ok(new { success = true, message = "User deleted successfully" });
            }
            
            return BadRequest(new { success = false, message = "Failed to delete user" });
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound(new { success = false, message = "User not found" });
            if (string.Equals(user.Role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Super Admin accounts are protected and cannot be deactivated." });
            }

            var result = await _userService.DeactivateUserAsync(userId);
            
            if (result)
            {
                return Ok(new { success = true, message = "User deactivated successfully" });
            }
            
            return BadRequest(new { success = false, message = "Failed to deactivate user" });
        }

        [HttpPost]
        public async Task<IActionResult> ReactivateUser(int userId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound(new { success = false, message = "User not found" });
            if (string.Equals(user.Role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Super Admin accounts are protected and cannot be reactivated via this action." });
            }

            var result = await _userService.ReactivateUserAsync(userId);
            
            if (result)
            {
                return Ok(new { success = true, message = "User reactivated successfully" });
            }
            
            return BadRequest(new { success = false, message = "Failed to reactivate user" });
        }

        public IActionResult PolicyManagement()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        public IActionResult SupplierManagement()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        public IActionResult ComplianceMonitoring()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        public IActionResult AuditLogs()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        public IActionResult Reports()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        public IActionResult Procurement()
        {
            var denied = RoleAccess.RequireRoleOrAccessDenied(this, RoleNames.Admin, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            return View();
        }

        private int? GetCurrentUserId()
        {
            var s = HttpContext.Session.GetString(SessionKeys.UserId);
            return int.TryParse(s, out var id) ? id : null;
        }

        private sealed class EmailNotificationSummary
        {
            public int Recipients { get; set; }
            public int SentCount { get; set; }
            public int FailedCount { get; set; }
            public List<string> FailedRecipients { get; set; } = new();
        }

        private async Task<EmailNotificationSummary> NotifyEmployeesPolicyAssignedAsync(IEnumerable<int> policyIds, IEnumerable<int> employeeIds)
        {
            var summary = new EmailNotificationSummary();
            var employeeIdList = employeeIds.Distinct().ToList();
            var policyIdList = policyIds.Distinct().ToList();
            if (!employeeIdList.Any() || !policyIdList.Any()) return summary;

            var employees = await _context.Users
                .Where(u => employeeIdList.Contains(u.UserId) &&
                            u.Role == RoleNames.Employee &&
                            !string.IsNullOrEmpty(u.Email))
                .ToListAsync();

            var policies = await _context.Policies
                .Where(p => policyIdList.Contains(p.PolicyId))
                .Select(p => new { p.PolicyId, p.PolicyTitle })
                .ToListAsync();

            var policyText = string.Join(", ", policies.Select(p => p.PolicyTitle));
            summary.Recipients = employees.Count;
            foreach (var employee in employees)
            {
                var subject = "New Policy Assignment";
                var body = $@"
                    <h2>New policy assigned</h2>
                    <p>Hello {employee.FirstName},</p>
                    <p>The following policy/policies were assigned to you:</p>
                    <p><strong>{policyText}</strong></p>
                    <p>Please log in to your TechNova account and acknowledge them.</p>";

                var emailResult = await _emailService.SendEmailAsync(employee.Email!, subject, body);
                if (emailResult.Success)
                {
                    summary.SentCount++;
                }
                else
                {
                    summary.FailedCount++;
                    summary.FailedRecipients.Add(employee.Email!);
                }
            }

            return summary;
        }

        private async Task<EmailNotificationSummary> NotifyEmployeesPolicyUpdatedAsync(int policyId, string policyTitle)
        {
            var summary = new EmailNotificationSummary();
            var employeeIds = await _context.PolicyAssignments
                .Where(pa => pa.PolicyId == policyId)
                .Select(pa => pa.UserId)
                .Distinct()
                .ToListAsync();

            if (!employeeIds.Any()) return summary;

            var employees = await _context.Users
                .Where(u => employeeIds.Contains(u.UserId) &&
                            u.Role == RoleNames.Employee &&
                            !string.IsNullOrEmpty(u.Email))
                .ToListAsync();

            summary.Recipients = employees.Count;
            foreach (var employee in employees)
            {
                var subject = "Assigned Policy Updated";
                var body = $@"
                    <h2>Policy update notice</h2>
                    <p>Hello {employee.FirstName},</p>
                    <p>Your assigned policy <strong>{policyTitle}</strong> has been updated.</p>
                    <p>Please review the updated policy in your TechNova account.</p>";

                var emailResult = await _emailService.SendEmailAsync(employee.Email!, subject, body);
                if (emailResult.Success)
                {
                    summary.SentCount++;
                }
                else
                {
                    summary.FailedCount++;
                    summary.FailedRecipients.Add(employee.Email!);
                }
            }

            return summary;
        }

        // Policy Management POST actions
        [HttpPost]
        public async Task<IActionResult> CreatePolicy([FromBody] PolicyData policyData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (policyData == null) return BadRequest(new { success = false, message = "Invalid policy data" });

            policyData.UploadedDate ??= DateTime.Now;
            var result = await _adminService.CreatePolicyAsync(policyData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Created policy: {policyData.PolicyTitle}", "Policy");
                return Ok(new { success = true, message = "Policy created successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to create policy" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePolicy([FromBody] PolicyData policyData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (policyData == null) return BadRequest(new { success = false, message = "Invalid policy data" });

            var result = await _adminService.UpdatePolicyAsync(policyData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Updated policy: {policyData.PolicyTitle}", "Policy");
                var emailSummary = new EmailNotificationSummary();
                if (policyData.PolicyId > 0)
                {
                    emailSummary = await NotifyEmployeesPolicyUpdatedAsync(policyData.PolicyId, policyData.PolicyTitle);
                }

                return Ok(new
                {
                    success = true,
                    message = $"Policy updated successfully. Email sent: {emailSummary.SentCount}, failed: {emailSummary.FailedCount}.",
                    emailRecipients = emailSummary.Recipients,
                    emailSent = emailSummary.SentCount,
                    emailFailed = emailSummary.FailedCount,
                    failedRecipients = emailSummary.FailedRecipients
                });
            }
            return BadRequest(new { success = false, message = "Failed to update policy" });
        }

        /// <summary>
        /// Upload a policy with an attached file (PDF, DOCX, etc.).
        /// Accepts multipart/form-data.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UploadPolicy(IFormFile? policyFile, [FromForm] string policyTitle, [FromForm] string category, [FromForm] string description, [FromForm] string status, [FromForm] int? policyId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (string.IsNullOrWhiteSpace(policyTitle)) return BadRequest(new { success = false, message = "Policy title is required" });

            string filePath = string.Empty;

            // Save uploaded file
            if (policyFile != null && policyFile.Length > 0)
            {
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "policies");
                Directory.CreateDirectory(uploadsDir);

                // Create unique filename: timestamp_originalname
                var safeFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(policyFile.FileName)}";
                var fullPath = Path.Combine(uploadsDir, safeFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await policyFile.CopyToAsync(stream);
                }

                filePath = $"/uploads/policies/{safeFileName}";
            }

            var policyData = new PolicyData
            {
                PolicyTitle = policyTitle,
                Category = category,
                Description = description,
                FilePath = filePath,
                UploadedDate = DateTime.Now
            };

            bool result;
            if (policyId.HasValue && policyId.Value > 0)
            {
                policyData.PolicyId = policyId.Value;
                // Keep existing file if no new file uploaded
                if (string.IsNullOrEmpty(filePath))
                {
                    policyData.FilePath = null!; // signal to keep existing
                }
                result = await _adminService.UpdatePolicyAsync(policyData);
                if (result)
                {
                    await _adminService.LogActivityAsync(GetCurrentUserId(), $"Updated policy with file: {policyTitle}", "Policy");
                }
            }
            else
            {
                result = await _adminService.CreatePolicyAsync(policyData);
                if (result) await _adminService.LogActivityAsync(GetCurrentUserId(), $"Created policy with file: {policyTitle}", "Policy");
            }

            if (result)
            {
                var emailSummary = new EmailNotificationSummary();
                if (policyId.HasValue && policyId.Value > 0)
                {
                    emailSummary = await NotifyEmployeesPolicyUpdatedAsync(policyData.PolicyId, policyTitle);
                }

                return Ok(new
                {
                    success = true,
                    message = $"Policy saved successfully. Email sent: {emailSummary.SentCount}, failed: {emailSummary.FailedCount}.",
                    emailRecipients = emailSummary.Recipients,
                    emailSent = emailSummary.SentCount,
                    emailFailed = emailSummary.FailedCount,
                    failedRecipients = emailSummary.FailedRecipients
                });
            }
            return BadRequest(new { success = false, message = "Failed to save policy" });
        }

        /// <summary>
        /// Serve a policy file for viewing/download.
        /// </summary>
        [HttpGet]
        public IActionResult DownloadPolicyFile(int policyId)
        {
            // Allow any authenticated user to download
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized();

            var policy = _adminService.GetPolicyByIdAsync(policyId).Result;
            if (policy == null || string.IsNullOrEmpty(policy.FilePath))
                return NotFound(new { message = "File not found" });

            var fullPath = Path.Combine(_environment.WebRootPath, policy.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { message = "File not found on server" });

            var ext = Path.GetExtension(fullPath).ToLower();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };

            var fileName = Path.GetFileName(fullPath);
            // For PDFs and images, allow inline viewing; for others, force download
            if (ext == ".pdf" || ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".txt")
            {
                Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileName}\"");
                return PhysicalFile(fullPath, contentType);
            }
            return PhysicalFile(fullPath, contentType, fileName);
        }

        [HttpPost]
        public async Task<IActionResult> DeletePolicy(int policyId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var result = await _adminService.DeletePolicyAsync(policyId);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Deleted policy ID: {policyId}", "Policy");
                return Ok(new { success = true, message = "Policy deleted successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to delete policy" });
        }

        [HttpGet]
        public async Task<IActionResult> GetPolicyDetail(int policyId)
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { success = false, message = "Not authenticated" });

            var detail = await _adminService.GetPolicyDetailAsync(policyId);
            if (detail == null)
                return NotFound(new { success = false, message = "Policy not found" });

            return Ok(new
            {
                success = true,
                policy = new
                {
                    detail.PolicyId,
                    detail.PolicyTitle,
                    detail.Category,
                    detail.Description,
                    detail.FilePath,
                    DateUploaded = detail.DateUploaded?.ToString("MMM dd, yyyy"),
                    detail.UploadedBy,
                    detail.IsArchived,
                    ArchivedDate = detail.ArchivedDate?.ToString("MMM dd, yyyy"),
                    detail.AssignedEmployees,
                    detail.AssignedSuppliers
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> ArchivePolicy(int policyId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var result = await _adminService.ArchivePolicyAsync(policyId);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Archived policy ID: {policyId}", "Policy");
                return Ok(new { success = true, message = "Policy archived successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to archive policy" });
        }

        [HttpPost]
        public async Task<IActionResult> RestorePolicy(int policyId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var result = await _adminService.RestorePolicyAsync(policyId);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Restored policy ID: {policyId}", "Policy");
                return Ok(new { success = true, message = "Policy restored successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to restore policy" });
        }

        [HttpPost]
        public async Task<IActionResult> AssignPolicy([FromBody] PolicyAssignmentRequest request)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (request == null) return BadRequest(new { success = false, message = "Invalid request" });

            var policyIds = request.PolicyIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
            if (!policyIds.Any() && request.PolicyId > 0)
            {
                policyIds.Add(request.PolicyId);
            }

            if (!policyIds.Any())
            {
                return BadRequest(new { success = false, message = "At least one policy must be selected." });
            }

            bool success = true;
            foreach (var policyId in policyIds)
            {
                if (request.EmployeeIds.Any())
                {
                    success &= await _adminService.AssignPolicyToEmployeesAsync(policyId, request.EmployeeIds);
                }
                if (request.SupplierIds.Any())
                {
                    success &= await _adminService.AssignPolicyToSuppliersAsync(policyId, request.SupplierIds);
                }
            }

            if (success)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(),
                    $"Assigned {policyIds.Count} policy(ies) to {request.EmployeeIds.Count} employee(s) and {request.SupplierIds.Count} supplier(s)",
                    "Policy");
                var emailSummary = new EmailNotificationSummary();
                if (request.EmployeeIds.Any())
                {
                    emailSummary = await NotifyEmployeesPolicyAssignedAsync(policyIds, request.EmployeeIds);
                }

                return Ok(new
                {
                    success = true,
                    message = $"Policy assignment completed successfully. Email sent: {emailSummary.SentCount}, failed: {emailSummary.FailedCount}.",
                    emailRecipients = emailSummary.Recipients,
                    emailSent = emailSummary.SentCount,
                    emailFailed = emailSummary.FailedCount,
                    failedRecipients = emailSummary.FailedRecipients
                });
            }
            return BadRequest(new { success = false, message = "Failed to assign policy" });
        }

        [HttpGet]
        public async Task<IActionResult> GetPolicyAssignmentStatus(int policyId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (policyId <= 0) return BadRequest(new { success = false, message = "Invalid policy id" });

            var status = await _adminService.GetPolicyAssignmentStatusAsync(policyId);
            return Ok(new
            {
                success = true,
                assignedEmployeeIds = status.AssignedEmployeeIds,
                assignedSupplierIds = status.AssignedSupplierIds
            });
        }

        // Supplier Management POST actions
        [HttpPost]
        public async Task<IActionResult> CreateSupplier([FromBody] SupplierData supplierData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (supplierData == null) return BadRequest(new { success = false, message = "Invalid supplier data" });

            var result = await _adminService.CreateSupplierAsync(supplierData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Created supplier: {supplierData.SupplierName}", "Supplier");
                return Ok(new { success = true, message = "Supplier created successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to create supplier" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSupplier([FromBody] SupplierData supplierData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (supplierData == null) return BadRequest(new { success = false, message = "Invalid supplier data" });

            var result = await _adminService.UpdateSupplierAsync(supplierData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Updated supplier: {supplierData.SupplierName}", "Supplier");
                return Ok(new { success = true, message = "Supplier updated successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to update supplier" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSupplier(int supplierId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var result = await _adminService.DeleteSupplierAsync(supplierId);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Deleted supplier ID: {supplierId}", "Supplier");
                return Ok(new { success = true, message = "Supplier deleted successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to delete supplier" });
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplier(int supplierId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var supplier = await _adminService.GetSupplierByIdAsync(supplierId);
            
            if (supplier != null)
            {
                return Ok(new { success = true, supplier = supplier });
            }
            
            return NotFound(new { success = false, message = "Supplier not found" });
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplierItems(int supplierId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (supplierId <= 0) return BadRequest(new { success = false, message = "Invalid supplier id" });

            var items = await _adminService.GetSupplierItemsAsync(supplierId);
            return Ok(new { success = true, items });
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplierCompliantPolicies(int supplierId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (supplierId <= 0) return BadRequest(new { success = false, message = "Invalid supplier id" });

            var policies = await _context.SupplierPolicies
                .Where(sp => sp.SupplierId == supplierId && sp.ComplianceStatus == "Compliant")
                .Select(sp => new
                {
                    Id = sp.Policy.PolicyId,
                    Title = sp.Policy.PolicyTitle
                })
                .Distinct()
                .OrderBy(p => p.Title)
                .ToListAsync();

            return Ok(new { success = true, policies });
        }

        // Procurement POST actions
        [HttpPost]
        public async Task<IActionResult> CreateProcurement([FromBody] ProcurementData procurementData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (procurementData == null) return BadRequest(new { success = false, message = "Invalid procurement data" });

            if (procurementData.SupplierId <= 0)
                return BadRequest(new { success = false, message = "Please select a supplier." });

            if (!procurementData.SupplierItemId.HasValue || procurementData.SupplierItemId.Value <= 0)
                return BadRequest(new { success = false, message = "Please select an available supplier item." });

            if (procurementData.Quantity <= 0)
                return BadRequest(new { success = false, message = "Quantity must be greater than zero." });

            if (!procurementData.PolicyId.HasValue || procurementData.PolicyId.Value <= 0)
                return BadRequest(new { success = false, message = "Please select a linked policy." });

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.SupplierId == procurementData.SupplierId);
            if (supplier == null)
                return BadRequest(new { success = false, message = "Selected supplier was not found." });

            if (!string.Equals(supplier.Status, "Active", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Selected supplier is not active." });

            var supplierItem = await _context.SupplierItems.FirstOrDefaultAsync(si =>
                si.SupplierItemId == procurementData.SupplierItemId.Value &&
                si.SupplierId == procurementData.SupplierId);

            if (supplierItem == null)
                return BadRequest(new { success = false, message = "Selected supplier item was not found." });

            if (!string.Equals(supplierItem.Status, "Available", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Selected supplier item is not available." });

            if (supplierItem.QuantityAvailable < procurementData.Quantity)
                return BadRequest(new { success = false, message = $"Only {supplierItem.QuantityAvailable} item(s) are available in stock." });

            var isCompliantForPolicy = await _context.SupplierPolicies.AnyAsync(sp =>
                sp.SupplierId == procurementData.SupplierId &&
                sp.PolicyId == procurementData.PolicyId.Value &&
                sp.ComplianceStatus == "Compliant");

            if (!isCompliantForPolicy)
                return BadRequest(new { success = false, message = "Supplier is not compliant with the selected linked policy." });

            var result = await _adminService.CreateProcurementAsync(procurementData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Created procurement: {procurementData.ItemName}", "Procurement");
                return Ok(new { success = true, message = "Procurement created successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to create procurement due to validation or stock updates." });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProcurement([FromBody] ProcurementData procurementData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (procurementData == null) return BadRequest(new { success = false, message = "Invalid procurement data" });

            var result = await _adminService.UpdateProcurementAsync(procurementData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Updated procurement: {procurementData.ItemName}", "Procurement");
                return Ok(new { success = true, message = "Procurement updated successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to update procurement" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProcurement(int procurementId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var result = await _adminService.DeleteProcurementAsync(procurementId);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Deleted procurement ID: {procurementId}", "Procurement");
                return Ok(new { success = true, message = "Procurement deleted successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to delete procurement" });
        }
    }
}

