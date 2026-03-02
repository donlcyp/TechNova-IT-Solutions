using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    public class AdminProcurementController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ApplicationDbContext _context;

        public AdminProcurementController(IAdminService adminService, ApplicationDbContext context)
        {
            _adminService = adminService;
            _context = context;
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

        [HttpGet]
        public async Task<IActionResult> GetSupplierItems(int supplierId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (supplierId <= 0) return BadRequest(new { success = false, message = "Invalid supplier id" });

            // Branch Admin can only view supplier items for suppliers in their branch or global suppliers
            if (!IsSuperAdmin())
            {
                var callerBranchId = GetCallerBranchId();
                if (callerBranchId.HasValue)
                {
                    var supplierBranchId = await _context.Suppliers
                        .Where(s => s.SupplierId == supplierId)
                        .Select(s => (int?)s.BranchId)
                        .FirstOrDefaultAsync();

                    if (supplierBranchId.HasValue && supplierBranchId != callerBranchId)
                        return Unauthorized(new { success = false, message = "You can only view supplier items within your branch." });
                }
            }

            var items = await _adminService.GetSupplierItemsAsync(supplierId);
            return Ok(new { success = true, items });
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplierCompliantPolicies(int supplierId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (supplierId <= 0) return BadRequest(new { success = false, message = "Invalid supplier id" });

            // Branch Admin can only view compliant policies for suppliers in their branch or global suppliers
            if (!IsSuperAdmin())
            {
                var callerBranchId = GetCallerBranchId();
                if (callerBranchId.HasValue)
                {
                    var supplierBranchId = await _context.Suppliers
                        .Where(s => s.SupplierId == supplierId)
                        .Select(s => (int?)s.BranchId)
                        .FirstOrDefaultAsync();

                    if (supplierBranchId.HasValue && supplierBranchId != callerBranchId)
                        return Unauthorized(new { success = false, message = "You can only view supplier data within your branch." });
                }
            }

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

        [HttpGet]
        public async Task<IActionResult> GetSupplierCompliantPoliciesByItemCategory(int supplierId, int supplierItemId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });

            var policies = await _context.Policies
                .Where(p => !p.IsArchived && p.ReviewStatus == "Approved")
                .Select(p => new
                {
                    Id = p.PolicyId,
                    Title = p.PolicyTitle,
                    Category = p.Category
                })
                .OrderBy(p => p.Title)
                .ToListAsync();

            return Ok(new { success = true, policies });
        }

        [HttpPost]
        public async Task<IActionResult> CreateProcurement([FromBody] ProcurementData procurementData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (procurementData == null) return BadRequest(new { success = false, message = "Invalid procurement data" });

            // Branch Admins automatically stamp their branch; SuperAdmin keeps null (company-wide)
            if (!IsSuperAdmin())
            {
                var callerBranchId = GetCallerBranchId();
                procurementData.BranchId = callerBranchId;
            }

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

            return BadRequest(new { success = false, message = "Failed to create procurement due to validation, stock updates, or exchange rate lookup." });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProcurement([FromBody] ProcurementData procurementData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (procurementData == null) return BadRequest(new { success = false, message = "Invalid procurement data" });

            if (!procurementData.SupplierItemId.HasValue || procurementData.SupplierItemId.Value <= 0)
                return BadRequest(new { success = false, message = "Please select an available supplier item." });

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

        [HttpPost]
        public async Task<IActionResult> MarkDeliveryArrived(int procurementId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (procurementId <= 0) return BadRequest(new { success = false, message = "Invalid procurement id" });

            var result = await _adminService.MarkProcurementDeliveredAsync(procurementId, GetCurrentUserId());
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Marked delivery arrived for procurement ID: {procurementId}", "Procurement");
                return Ok(new { success = true, message = "Delivery marked as arrived." });
            }

            return BadRequest(new { success = false, message = "Only supplier-approved or late records can be marked as delivery arrived." });
        }
    }
}
