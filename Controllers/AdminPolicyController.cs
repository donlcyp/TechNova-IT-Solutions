using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    public class AdminPolicyController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;

        public AdminPolicyController(
            IAdminService adminService,
            IEmailService emailService,
            IWebHostEnvironment environment,
            ApplicationDbContext context)
        {
            _adminService = adminService;
            _emailService = emailService;
            _environment = environment;
            _context = context;
        }

        private bool IsAdmin()
        {
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            return userRole == RoleNames.Admin || userRole == RoleNames.SuperAdmin;
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

        [HttpPost]
        public async Task<IActionResult> UploadPolicy(IFormFile? policyFile, [FromForm] string policyTitle, [FromForm] string category, [FromForm] string description, [FromForm] string status, [FromForm] int? policyId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (string.IsNullOrWhiteSpace(policyTitle)) return BadRequest(new { success = false, message = "Policy title is required" });

            string filePath = string.Empty;

            if (policyFile != null && policyFile.Length > 0)
            {
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "policies");
                Directory.CreateDirectory(uploadsDir);

                var safeFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(policyFile.FileName)}";
                var fullPath = Path.Combine(uploadsDir, safeFileName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
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
                if (string.IsNullOrEmpty(filePath))
                {
                    policyData.FilePath = null!;
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

        [HttpGet]
        public IActionResult DownloadPolicyFile(int policyId)
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            var policy = _adminService.GetPolicyByIdAsync(policyId).Result;
            if (policy == null || string.IsNullOrEmpty(policy.FilePath))
            {
                return NotFound(new { message = "File not found" });
            }

            var fullPath = Path.Combine(_environment.WebRootPath, policy.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound(new { message = "File not found on server" });
            }

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
            {
                return Unauthorized(new { success = false, message = "Not authenticated" });
            }

            var detail = await _adminService.GetPolicyDetailAsync(policyId);
            if (detail == null)
            {
                return NotFound(new { success = false, message = "Policy not found" });
            }

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

            var success = true;
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
                await _adminService.LogActivityAsync(
                    GetCurrentUserId(),
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
    }
}
