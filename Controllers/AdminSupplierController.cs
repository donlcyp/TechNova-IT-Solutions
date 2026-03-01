using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    public class AdminSupplierController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ApplicationDbContext _context;

        public AdminSupplierController(IAdminService adminService, ApplicationDbContext context)
        {
            _adminService = adminService;
            _context = context;
        }

        private bool IsAdmin()
        {
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            return userRole == RoleNames.Admin || userRole == RoleNames.SuperAdmin;
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
            if (IsSuperAdmin()) return true;
            var callerBranchId = GetCallerBranchId();
            return callerBranchId.HasValue && supplierBranchId == callerBranchId;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSupplier([FromBody] SupplierData supplierData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (supplierData == null) return BadRequest(new { success = false, message = "Invalid supplier data" });

            // Branch Admins automatically stamp their branch; SuperAdmin can supply an explicit BranchId or null (global)
            if (!IsSuperAdmin())
            {
                var callerBranchId = GetCallerBranchId();
                if (!callerBranchId.HasValue)
                    return BadRequest(new { success = false, message = "You have no branch assigned. A Super Admin must assign you to a branch first." });
                supplierData.BranchId = callerBranchId;
            }

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

            var result = await _adminService.CreateSupplierAsync(supplierData);
            if (result.Success)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Created supplier: {supplierData.SupplierName}", "Supplier");
                return Ok(new { success = true, message = string.IsNullOrWhiteSpace(result.Message) ? "Supplier created successfully" : result.Message });
            }

            return BadRequest(new { success = false, message = string.IsNullOrWhiteSpace(result.Message) ? "Failed to create supplier" : result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSupplier([FromBody] SupplierData supplierData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
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
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });

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
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
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
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
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

        [HttpGet]
        public async Task<IActionResult> GetSupplier(int supplierId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });

            var supplier = await _adminService.GetSupplierByIdAsync(supplierId);
            if (supplier != null)
            {
                return Ok(new { success = true, supplier });
            }

            return NotFound(new { success = false, message = "Supplier not found" });
        }
    }
}
