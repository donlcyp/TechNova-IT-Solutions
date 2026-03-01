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

        private bool IsSuperAdmin()
        {
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            return userRole == RoleNames.SuperAdmin;
        }

        private int? GetCurrentUserId()
        {
            var s = HttpContext.Session.GetString(SessionKeys.UserId);
            return int.TryParse(s, out var id) ? id : null;
        }

        private int? GetCallerBranchId()
        {
            var s = HttpContext.Session.GetString(SessionKeys.BranchId);
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

            // Branch Admins can only assign to employees in their own branch and accessible suppliers
            if (!IsSuperAdmin())
            {
                var callerBranchId = GetCallerBranchId();
                if (callerBranchId.HasValue)
                {
                    if (request.EmployeeIds.Any())
                    {
                        var hasOutOfBranchEmployees = await _context.Users
                            .Where(u => request.EmployeeIds.Contains(u.UserId) && u.BranchId != callerBranchId)
                            .AnyAsync();
                        if (hasOutOfBranchEmployees)
                            return BadRequest(new { success = false, message = "You can only assign policies to employees within your branch." });
                    }

                    if (request.SupplierIds.Any())
                    {
                        var hasInvalidSuppliers = await _context.Suppliers
                            .Where(s => request.SupplierIds.Contains(s.SupplierId)
                                && s.BranchId != callerBranchId
                                && s.BranchId != null)
                            .AnyAsync();
                        if (hasInvalidSuppliers)
                            return BadRequest(new { success = false, message = "You can only assign policies to your branch's suppliers or global (Main) suppliers." });
                    }
                }
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
