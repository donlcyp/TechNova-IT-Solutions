using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    public class BranchController : Controller
    {
        private readonly IBranchService _branchService;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly ILogger<BranchController> _logger;

        public BranchController(
            IBranchService branchService,
            IUserService userService,
            IEmailService emailService,
            ILogger<BranchController> logger)
        {
            _branchService = branchService;
            _userService   = userService;
            _emailService  = emailService;
            _logger        = logger;
        }

        // GET /Branch/GetAll
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var branches = await _branchService.GetAllBranchesAsync();
            return Ok(new { success = true, branches });
        }

        // GET /Branch/Get?branchId=1
        [HttpGet]
        public async Task<IActionResult> Get(int branchId)
        {
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var branch = await _branchService.GetBranchByIdAsync(branchId);
            if (branch == null)
                return NotFound(new { success = false, message = "Branch not found." });

            return Ok(new { success = true, branch });
        }

        // POST /Branch/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BranchData branchData)
        {
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            if (branchData == null || string.IsNullOrWhiteSpace(branchData.BranchName))
                return BadRequest(new { success = false, message = "Branch name is required." });

            if (string.IsNullOrWhiteSpace(branchData.Address))
                return BadRequest(new { success = false, message = "Address is required." });

            if (string.IsNullOrWhiteSpace(branchData.City))
                return BadRequest(new { success = false, message = "City is required." });

            var result = await _branchService.CreateBranchAsync(branchData);
            if (result)
            {
                // Send branch-online notification to the branch manager
                await SendBranchOnlineNotificationAsync(branchData);
                return Ok(new { success = true, message = "Branch created successfully." });
            }

            return BadRequest(new { success = false, message = "Failed to create branch." });
        }

        // POST /Branch/Update
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] BranchData branchData)
        {
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            if (branchData == null || branchData.BranchId <= 0)
                return BadRequest(new { success = false, message = "Invalid branch data." });

            if (string.IsNullOrWhiteSpace(branchData.BranchName))
                return BadRequest(new { success = false, message = "Branch name is required." });

            if (string.IsNullOrWhiteSpace(branchData.Address))
                return BadRequest(new { success = false, message = "Address is required." });

            if (string.IsNullOrWhiteSpace(branchData.City))
                return BadRequest(new { success = false, message = "City is required." });

            var result = await _branchService.UpdateBranchAsync(branchData);
            if (result)
                return Ok(new { success = true, message = "Branch updated successfully." });

            return BadRequest(new { success = false, message = "Failed to update branch." });
        }

        // POST /Branch/Deactivate?branchId=1
        [HttpPost]
        public async Task<IActionResult> Deactivate(int branchId)
        {
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin, RoleNames.SystemAdmin);
            if (denied != null) return denied;

            var result = await _branchService.DeactivateBranchAsync(branchId);
            if (result)
                return Ok(new { success = true, message = "Branch deactivated." });

            return BadRequest(new { success = false, message = "Failed to deactivate branch." });
        }

        // POST /Branch/Reactivate?branchId=1
        [HttpPost]
        public async Task<IActionResult> Reactivate(int branchId)
        {
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin, RoleNames.SystemAdmin);
            if (denied != null) return denied;

            var result = await _branchService.ReactivateBranchAsync(branchId);
            if (result)
            {
                // Send branch-online notification to the branch manager
                var branch = await _branchService.GetBranchByIdAsync(branchId);
                if (branch != null)
                    await SendBranchOnlineNotificationAsync(branch);

                return Ok(new { success = true, message = "Branch reactivated." });
            }

            return BadRequest(new { success = false, message = "Failed to reactivate branch." });
        }

        // POST /Branch/Delete?branchId=1
        [HttpPost]
        public async Task<IActionResult> Delete(int branchId)
        {
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin);
            if (denied != null) return denied;

            var result = await _branchService.DeleteBranchAsync(branchId);
            if (result)
                return Ok(new { success = true, message = "Branch deleted permanently." });

            return BadRequest(new { success = false, message = "Failed to delete branch." });
        }

        // GET /Branch/GetAvailableAdmins
        [HttpGet]
        public async Task<IActionResult> GetAvailableAdmins()
        {
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin, RoleNames.SystemAdmin);
            if (denied != null) return denied;

            var admins = await _branchService.GetAvailableAdminsAsync();
            return Ok(new { success = true, admins });
        }

        // POST /Branch/AssignAdmin
        [HttpPost]
        public async Task<IActionResult> AssignAdmin([FromBody] AssignAdminRequest req)
        {
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin, RoleNames.SystemAdmin);
            if (denied != null) return denied;

            if (req == null || req.BranchId <= 0 || req.AdminUserId <= 0)
                return BadRequest(new { success = false, message = "Invalid assignment data." });

            var result = await _branchService.AssignAdminToBranchAsync(req.BranchId, req.AdminUserId);
            if (result)
            {
                var branch = await _branchService.GetBranchByIdAsync(req.BranchId);
                var admin  = await _userService.GetUserByIdAsync(req.AdminUserId);

                // Notify the branch manager that an admin has been assigned
                if (branch != null)
                    await SendAdminAssignedNotificationAsync(branch);

                // Notify the admin about which branch they've been assigned to
                if (branch != null && admin != null)
                    await SendAdminBranchAssignmentNotificationAsync(admin, branch);

                return Ok(new { success = true, message = "Admin assigned to branch successfully." });
            }

            return BadRequest(new { success = false, message = "Failed to assign admin. Ensure the admin account and branch both exist." });
        }

        // POST /Branch/UnassignAdmin?branchId=1
        [HttpPost]
        public async Task<IActionResult> UnassignAdmin(int branchId)
        {
            var denied = RoleAccess.RequireRoleOrUnauthorized(this, RoleNames.SuperAdmin, RoleNames.SystemAdmin);
            if (denied != null) return denied;

            var result = await _branchService.UnassignAdminFromBranchAsync(branchId);
            if (result)
                return Ok(new { success = true, message = "Admin unassigned from branch." });

            return BadRequest(new { success = false, message = "No admin was assigned to this branch." });
        }

        // ── Private: notify the admin about their branch assignment ──────────
        private async Task SendAdminBranchAssignmentNotificationAsync(UserData admin, BranchData branch)
        {
            if (string.IsNullOrWhiteSpace(admin.Email))
            {
                _logger.LogInformation(
                    "Admin '{AdminName}' has no email — skipping branch assignment notification.",
                    admin.FullName);
                return;
            }

            var subject = $"TechNova — You Have Been Assigned to {branch.BranchName} Branch";

            var managerName = $"{branch.ManagerFirstName} {branch.ManagerLastName}".Trim();
            if (string.IsNullOrWhiteSpace(managerName)) managerName = "N/A";

            var body = $@"
<p style=""margin:0 0 12px;color:#1f2937;"">Dear <strong>{System.Net.WebUtility.HtmlEncode(admin.FullName)}</strong>,</p>
<p style=""margin:0 0 12px;color:#1f2937;"">
    You have been assigned as the <strong>System Administrator</strong> for the
    <strong>{System.Net.WebUtility.HtmlEncode(branch.BranchName)}</strong> branch on the TechNova IT Solutions platform.
</p>
<p style=""margin:0 0 6px;color:#1f2937;font-weight:600;"">Branch Details:</p>
<table style=""border-collapse:collapse;font-size:14px;color:#374151;margin-bottom:16px;"">
    <tr><td style=""padding:4px 12px 4px 0;font-weight:600;"">Branch</td><td style=""padding:4px 0;"">{System.Net.WebUtility.HtmlEncode(branch.BranchName)}</td></tr>
    <tr><td style=""padding:4px 12px 4px 0;font-weight:600;"">Location</td><td style=""padding:4px 0;"">{System.Net.WebUtility.HtmlEncode(branch.City)}{(string.IsNullOrWhiteSpace(branch.Region) ? "" : $", {System.Net.WebUtility.HtmlEncode(branch.Region)}")}</td></tr>
    <tr><td style=""padding:4px 12px 4px 0;font-weight:600;"">Address</td><td style=""padding:4px 0;"">{System.Net.WebUtility.HtmlEncode(branch.Address)}</td></tr>
    <tr><td style=""padding:4px 12px 4px 0;font-weight:600;"">Branch Manager</td><td style=""padding:4px 0;"">{System.Net.WebUtility.HtmlEncode(managerName)}</td></tr>
</table>
<p style=""margin:0 0 12px;color:#1f2937;"">
    As the branch administrator, you now have access to manage policies, procurement, suppliers, and users
    for the <strong>{System.Net.WebUtility.HtmlEncode(branch.BranchName)}</strong> branch.
</p>
<p style=""margin:0;color:#6b7280;font-size:13px;"">
    If you have any questions, please contact the Super Admin or the TechNova IT support team.
</p>";

            var emailResult = await _emailService.SendEmailAsync(admin.Email, subject, body);

            if (emailResult.Success)
            {
                _logger.LogInformation(
                    "Branch assignment notification sent to admin {AdminEmail} for branch '{BranchName}'.",
                    admin.Email, branch.BranchName);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send branch assignment notification to admin {AdminEmail}: {Error}",
                    admin.Email, emailResult.ErrorMessage);
            }
        }

        // ── Private: send admin-assigned email notification ──────────
        private async Task SendAdminAssignedNotificationAsync(BranchData branch)
        {
            if (string.IsNullOrWhiteSpace(branch.ManagerEmail))
            {
                _logger.LogInformation(
                    "Branch '{BranchName}' has no manager email — skipping admin-assigned notification.",
                    branch.BranchName);
                return;
            }

            var managerFirst = branch.ManagerFirstName ?? "Manager";
            var managerLast  = branch.ManagerLastName ?? "";
            var managerFull  = $"{managerFirst} {managerLast}".Trim();

            var adminName = branch.AssignedAdminName ?? "A new administrator";

            var subject = $"TechNova — Admin Assigned to {branch.BranchName} Branch";

            var body = $@"
<p style=""margin:0 0 12px;color:#1f2937;"">Dear <strong>{System.Net.WebUtility.HtmlEncode(managerFull)}</strong>,</p>
<p style=""margin:0 0 12px;color:#1f2937;"">
    This is to inform you that a <strong>System Administrator</strong> has been assigned to the
    <strong>{System.Net.WebUtility.HtmlEncode(branch.BranchName)}</strong> branch.
</p>
<p style=""margin:0 0 6px;color:#1f2937;font-weight:600;"">Assignment Details:</p>
<table style=""border-collapse:collapse;font-size:14px;color:#374151;margin-bottom:16px;"">
    <tr><td style=""padding:4px 12px 4px 0;font-weight:600;"">Admin Name</td><td style=""padding:4px 0;"">{System.Net.WebUtility.HtmlEncode(adminName)}</td></tr>
    <tr><td style=""padding:4px 12px 4px 0;font-weight:600;"">Branch</td><td style=""padding:4px 0;"">{System.Net.WebUtility.HtmlEncode(branch.BranchName)}</td></tr>
    <tr><td style=""padding:4px 12px 4px 0;font-weight:600;"">Location</td><td style=""padding:4px 0;"">{System.Net.WebUtility.HtmlEncode(branch.City)}{(string.IsNullOrWhiteSpace(branch.Region) ? "" : $", {System.Net.WebUtility.HtmlEncode(branch.Region)}")}</td></tr>
</table>
<p style=""margin:0 0 12px;color:#1f2937;"">
    The assigned administrator now has access to manage branch operations including policies, procurement, and user management
    for the <strong>{System.Net.WebUtility.HtmlEncode(branch.BranchName)}</strong> branch.
</p>
<p style=""margin:0;color:#6b7280;font-size:13px;"">
    If you have any questions or concerns regarding this assignment, please contact the Super Admin or TechNova IT support.
</p>";

            var emailResult = await _emailService.SendEmailAsync(branch.ManagerEmail, subject, body);

            if (emailResult.Success)
            {
                _logger.LogInformation(
                    "Admin-assigned notification sent to {ManagerEmail} for branch '{BranchName}'.",
                    branch.ManagerEmail, branch.BranchName);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send admin-assigned notification to {ManagerEmail}: {Error}",
                    branch.ManagerEmail, emailResult.ErrorMessage);
            }
        }

        // ── Private: send branch-online email notification ──────────
        private async Task SendBranchOnlineNotificationAsync(BranchData branch)
        {
            if (string.IsNullOrWhiteSpace(branch.ManagerEmail))
            {
                _logger.LogInformation(
                    "Branch '{BranchName}' has no manager email — skipping online notification.",
                    branch.BranchName);
                return;
            }

            var firstName = branch.ManagerFirstName ?? "Manager";
            var lastName  = branch.ManagerLastName ?? "";
            var fullName  = $"{firstName} {lastName}".Trim();

            var subject = $"TechNova — {branch.BranchName} Branch System Is Now Online";

            var body = $@"
<p style=""margin:0 0 12px;color:#1f2937;"">Dear <strong>{System.Net.WebUtility.HtmlEncode(fullName)}</strong>,</p>
<p style=""margin:0 0 12px;color:#1f2937;"">
    We are pleased to inform you that the <strong>{System.Net.WebUtility.HtmlEncode(branch.BranchName)}</strong> branch system 
    is now <span style=""color:#059669;font-weight:600;"">online</span> and fully operational on the TechNova IT Solutions platform.
</p>
<p style=""margin:0 0 6px;color:#1f2937;font-weight:600;"">Branch Details:</p>
<table style=""border-collapse:collapse;font-size:14px;color:#374151;margin-bottom:16px;"">
    <tr><td style=""padding:4px 12px 4px 0;font-weight:600;"">Branch</td><td style=""padding:4px 0;"">{System.Net.WebUtility.HtmlEncode(branch.BranchName)}</td></tr>
    <tr><td style=""padding:4px 12px 4px 0;font-weight:600;"">Location</td><td style=""padding:4px 0;"">{System.Net.WebUtility.HtmlEncode(branch.City)}{(string.IsNullOrWhiteSpace(branch.Region) ? "" : $", {System.Net.WebUtility.HtmlEncode(branch.Region)}")}</td></tr>
    <tr><td style=""padding:4px 12px 4px 0;font-weight:600;"">Address</td><td style=""padding:4px 0;"">{System.Net.WebUtility.HtmlEncode(branch.Address)}</td></tr>
</table>
<p style=""margin:0 0 12px;color:#1f2937;"">
    As Branch Manager, you can now access the system to manage policies, procurement, and compliance operations for your branch.
</p>
<p style=""margin:0;color:#6b7280;font-size:13px;"">
    If you have any questions, please contact the Super Admin or the TechNova IT support team.
</p>";

            var emailResult = await _emailService.SendEmailAsync(branch.ManagerEmail, subject, body);

            if (emailResult.Success)
            {
                _logger.LogInformation(
                    "Branch-online notification sent to {ManagerEmail} for branch '{BranchName}'.",
                    branch.ManagerEmail, branch.BranchName);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send branch-online notification to {ManagerEmail}: {Error}",
                    branch.ManagerEmail, emailResult.ErrorMessage);
            }
        }
    }

    public class AssignAdminRequest
    {
        public int BranchId { get; set; }
        public int AdminUserId { get; set; }
    }
}
