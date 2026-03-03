using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Services;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    public class AdminSupplierController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AdminSupplierController(IAdminService adminService, ApplicationDbContext context, IEmailService emailService)
        {
            _adminService = adminService;
            _context = context;
            _emailService = emailService;
        }

        private bool IsAdmin()
        {
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            return RoleNames.IsAdminRole(userRole) || userRole == RoleNames.SuperAdmin;
        }

        private bool IsSuperAdmin()
        {
            return HttpContext.Session.GetString(SessionKeys.UserRole) == RoleNames.SuperAdmin;
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

        /// <summary>
        /// Returns true if the caller may modify this supplier.
        /// SuperAdmin: always allowed.
        /// Branch Admin: allowed only for suppliers they own (matching BranchId).
        /// Global suppliers (null BranchId) can only be modified by SuperAdmin.
        /// </summary>
        private bool CanModifySupplier(int? supplierBranchId)
        {
            if (IsSystemAdminOrHigher()) return true;
            var callerBranchId = GetCallerBranchId();
            return callerBranchId.HasValue && supplierBranchId == callerBranchId;
        }

        private bool IsSystemAdminOrHigher()
        {
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            return userRole == RoleNames.SystemAdmin || userRole == RoleNames.SuperAdmin;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSupplier([FromBody] SupplierData supplierData)
        {
            // Only SystemAdmin or SuperAdmin can create suppliers (global entities)
            if (!IsSystemAdminOrHigher())
                return Unauthorized(new { success = false, message = "Only System Admin or Super Admin can create suppliers." });
            if (supplierData == null) return BadRequest(new { success = false, message = "Invalid supplier data" });

            // Suppliers are global entities — BranchId is always null
            supplierData.BranchId = null;

            var normalizedEmail = (supplierData.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return BadRequest(new { success = false, message = "Email is required." });
            }

            var supplierEmailExists = await _context.Suppliers.AnyAsync(s => s.Email != null && s.Email.ToLower() == normalizedEmail);
            if (supplierEmailExists)
            {
                return BadRequest(new { success = false, message = "A supplier account with this email already exists." });
            }

            var userEmailExists = await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == normalizedEmail);
            if (userEmailExists)
            {
                return BadRequest(new { success = false, message = "A user account with this email already exists." });
            }

            // Always use default password for new suppliers
            const string defaultPassword = "Supplier@123";
            supplierData.Password = defaultPassword;

            var result = await _adminService.CreateSupplierAsync(supplierData);
            if (result.Success)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Created supplier: {supplierData.SupplierName}", "Supplier");

                // Send welcome email with account credentials
                var subject = "Welcome to TechNova IT Solutions - Supplier Account Created";
                var body = $@"
<div style='font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:0 auto;'>
    <div style='background:linear-gradient(135deg,#0f172a,#1e3a5f);padding:24px 32px;border-radius:12px 12px 0 0;'>
        <h1 style='color:#fff;margin:0;font-size:22px;'>Welcome to TechNova IT Solutions</h1>
    </div>
    <div style='background:#fff;padding:28px 32px;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 12px 12px;'>
        <p style='color:#1f2937;font-size:15px;line-height:1.6;'>Hello <strong>{supplierData.ContactPersonFirstName} {supplierData.ContactPersonLastName}</strong>,</p>
        <p style='color:#1f2937;font-size:15px;line-height:1.6;'>Your supplier account for <strong>{supplierData.SupplierName}</strong> has been created. Here are your login credentials:</p>
        <div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:16px 20px;margin:16px 0;'>
            <p style='margin:4px 0;font-size:14px;color:#334155;'><strong>Email:</strong> {supplierData.Email}</p>
            <p style='margin:4px 0;font-size:14px;color:#334155;'><strong>Password:</strong> {defaultPassword}</p>
        </div>
        <p style='color:#dc2626;font-size:13px;font-weight:600;'>⚠ For security, you will be required to change your password on first login.</p>
        <p style='color:#6b7280;font-size:13px;margin-top:20px;'>If you have any questions, please contact your TechNova administrator.</p>
    </div>
</div>";
                if (!string.IsNullOrWhiteSpace(supplierData.Email))
                    _ = _emailService.SendEmailAsync(supplierData.Email, subject, body);

                return Ok(new { success = true, message = string.IsNullOrWhiteSpace(result.Message) ? "Supplier created successfully. Notification email sent." : result.Message });
            }

            return BadRequest(new { success = false, message = string.IsNullOrWhiteSpace(result.Message) ? "Failed to create supplier" : result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSupplier([FromBody] SupplierData supplierData)
        {
            if (!IsSystemAdminOrHigher()) return Unauthorized(new { success = false, message = "Access denied" });
            if (supplierData == null) return BadRequest(new { success = false, message = "Invalid supplier data" });
            if (supplierData.SupplierId <= 0) return BadRequest(new { success = false, message = "Invalid supplier id." });

            var dbSupplier = await _context.Suppliers
                .Where(s => s.SupplierId == supplierData.SupplierId)
                .Select(s => new { s.Status, s.BranchId, s.Email })
                .FirstOrDefaultAsync();

            if (dbSupplier == null) return NotFound(new { success = false, message = "Supplier not found." });

            if (!CanModifySupplier(dbSupplier.BranchId))
                return Forbid();

            if (string.Equals(dbSupplier.Status, "Terminated", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Terminated suppliers cannot be edited. Use Restore first." });
            }

            var normalizedEmail = (supplierData.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return BadRequest(new { success = false, message = "Email is required." });
            }

            var supplierEmailExists = await _context.Suppliers
                .AnyAsync(s => s.SupplierId != supplierData.SupplierId && s.Email != null && s.Email.ToLower() == normalizedEmail);
            if (supplierEmailExists)
            {
                return BadRequest(new { success = false, message = "Another supplier account already uses this email." });
            }

            var currentSupplierEmailNormalized = (dbSupplier.Email ?? string.Empty).Trim().ToLowerInvariant();
            var userEmailExists = await _context.Users
                .AnyAsync(u => u.Email != null &&
                               u.Email.ToLower() == normalizedEmail &&
                               u.Email.ToLower() != currentSupplierEmailNormalized);
            if (userEmailExists)
            {
                return BadRequest(new { success = false, message = "A user account with this email already exists." });
            }

            // Preserve the original BranchId — it is set at creation time and not changed
            supplierData.BranchId = dbSupplier.BranchId;

            var result = await _adminService.UpdateSupplierAsync(supplierData);
            if (result.Success)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Updated supplier: {supplierData.SupplierName}", "Supplier");
                return Ok(new { success = true, message = string.IsNullOrWhiteSpace(result.Message) ? "Supplier updated successfully" : result.Message });
            }

            return BadRequest(new { success = false, message = string.IsNullOrWhiteSpace(result.Message) ? "Failed to update supplier" : result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSupplier(int supplierId)
        {
            if (!IsSystemAdminOrHigher()) return Unauthorized(new { success = false, message = "Access denied" });

            var supplierBranchId = await _context.Suppliers
                .Where(s => s.SupplierId == supplierId)
                .Select(s => (int?)s.BranchId)
                .FirstOrDefaultAsync();

            if (!CanModifySupplier(supplierBranchId))
                return Forbid();

            var result = await _adminService.DeleteSupplierAsync(supplierId);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Deleted supplier ID: {supplierId}", "Supplier");
                return Ok(new { success = true, message = "Supplier deleted successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to delete supplier" });
        }

        [HttpPost]
        public async Task<IActionResult> TerminateSupplier([FromBody] SupplierTerminationData terminationData)
        {
            if (!IsSystemAdminOrHigher()) return Unauthorized(new { success = false, message = "Access denied" });
            if (terminationData == null || terminationData.SupplierId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid termination request." });
            }
            if (string.IsNullOrWhiteSpace(terminationData.Reason))
            {
                return BadRequest(new { success = false, message = "Termination reason is required." });
            }

            var supplierBranchId = await _context.Suppliers
                .Where(s => s.SupplierId == terminationData.SupplierId)
                .Select(s => (int?)s.BranchId)
                .FirstOrDefaultAsync();

            if (!CanModifySupplier(supplierBranchId))
                return Forbid();

            var result = await _adminService.TerminateSupplierAsync(terminationData, GetCurrentUserId());
            if (!result)
            {
                return BadRequest(new { success = false, message = "Failed to terminate supplier contract." });
            }

            await _adminService.LogActivityAsync(
                GetCurrentUserId(),
                $"Terminated supplier contract (Supplier ID: {terminationData.SupplierId})",
                "Supplier");

            return Ok(new { success = true, message = "Supplier contract terminated and archived. Notification email sent." });
        }

        [HttpPost]
        public async Task<IActionResult> RestoreSupplier(int supplierId)
        {
            if (!IsSystemAdminOrHigher()) return Unauthorized(new { success = false, message = "Access denied" });
            if (supplierId <= 0) return BadRequest(new { success = false, message = "Invalid supplier id." });

            var supplierBranchId = await _context.Suppliers
                .Where(s => s.SupplierId == supplierId)
                .Select(s => (int?)s.BranchId)
                .FirstOrDefaultAsync();

            if (!CanModifySupplier(supplierBranchId))
                return Forbid();

            var result = await _adminService.RestoreSupplierAsync(supplierId, GetCurrentUserId());
            if (!result)
            {
                return BadRequest(new { success = false, message = "Failed to restore supplier account." });
            }

            await _adminService.LogActivityAsync(GetCurrentUserId(), $"Restored supplier account (Supplier ID: {supplierId})", "Supplier");
            return Ok(new { success = true, message = "Supplier restored successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!IsSystemAdminOrHigher()) return Unauthorized(new { success = false, message = "Access denied" });
            if (request == null || request.SupplierId <= 0)
                return BadRequest(new { success = false, message = "Invalid request." });

            var supplier = await _context.Suppliers.FindAsync(request.SupplierId);
            if (supplier == null) return NotFound(new { success = false, message = "Supplier not found." });

            if (!CanModifySupplier(supplier.BranchId))
                return Forbid();

            var normalizedEmail = (supplier.Email ?? string.Empty).Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email != null && u.Email.Trim().ToLower() == normalizedEmail && u.Role == "Supplier");

            if (user == null)
                return BadRequest(new { success = false, message = "No login account found for this supplier." });

            const string defaultPassword = "Supplier@123";
            user.Password = PasswordHasher.HashPassword(defaultPassword);
            user.MustChangePassword = true;
            await _context.SaveChangesAsync();

            await _adminService.LogActivityAsync(GetCurrentUserId(), $"Reset password for supplier: {supplier.SupplierName} (ID: {supplier.SupplierId})", "Supplier");

            // Send notification email
            var subject = "TechNova IT Solutions - Password Reset";
            var body = $@"
<div style='font-family:Segoe UI,Arial,sans-serif;max-width:600px;margin:0 auto;'>
    <div style='background:linear-gradient(135deg,#0f172a,#1e3a5f);padding:24px 32px;border-radius:12px 12px 0 0;'>
        <h1 style='color:#fff;margin:0;font-size:22px;'>Password Reset Notification</h1>
    </div>
    <div style='background:#fff;padding:28px 32px;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 12px 12px;'>
        <p style='color:#1f2937;font-size:15px;line-height:1.6;'>Hello <strong>{supplier.ContactPersonFirstName} {supplier.ContactPersonLastName}</strong>,</p>
        <p style='color:#1f2937;font-size:15px;line-height:1.6;'>Your password for the TechNova IT Solutions supplier portal has been reset by an administrator.</p>
        <div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:16px 20px;margin:16px 0;'>
            <p style='margin:4px 0;font-size:14px;color:#334155;'><strong>Email:</strong> {supplier.Email}</p>
            <p style='margin:4px 0;font-size:14px;color:#334155;'><strong>New Password:</strong> {defaultPassword}</p>
        </div>
        <p style='color:#dc2626;font-size:13px;font-weight:600;'>⚠ For security, you will be required to change your password on your next login.</p>
        <p style='color:#6b7280;font-size:13px;margin-top:20px;'>If you did not expect this reset, please contact your TechNova administrator immediately.</p>
    </div>
</div>";
            _ = _emailService.SendEmailAsync(supplier.Email!, subject, body);

            return Ok(new { success = true, message = "Password reset to default. The supplier has been notified via email." });
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplier(int supplierId)
        {
            if (!IsSystemAdminOrHigher()) return Unauthorized(new { success = false, message = "Access denied" });

            var supplier = await _adminService.GetSupplierByIdAsync(supplierId);
            if (supplier != null)
            {
                return Ok(new { success = true, supplier });
            }

            return NotFound(new { success = false, message = "Supplier not found" });
        }
    }

    public class ResetPasswordRequest
    {
        public int SupplierId { get; set; }
    }
}
