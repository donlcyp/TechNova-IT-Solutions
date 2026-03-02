namespace TechNova_IT_Solutions.Pages.BranchAdmin
{
    public class SupplierManagementModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SupplierManagementModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
        public string BranchDisplayName { get; set; } = string.Empty;
        public int? CallerBranchId { get; set; }

        public int TotalSuppliers { get; set; }
        public int ActiveSuppliers { get; set; }
        public int CompliantSuppliers { get; set; }

        public List<TechNova_IT_Solutions.Pages.SupplierItem> Suppliers { get; set; } = new();
        public List<TechNova_IT_Solutions.Pages.SupplierPolicyItem> ActivePolicies { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToPage("/Account/Login");

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            // Supplier management is company-level (SystemAdmin) — BranchAdmin can only select suppliers in Procurement
            if (userRole == RoleNames.BranchAdmin)
                return RedirectToPage("/BranchAdmin/Dashboard");
            if (userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee) return RedirectToPage("/Employee/Dashboard");
                if (userRole == RoleNames.SystemAdmin) return RedirectToPage("/SystemAdmin/Dashboard");
                if (userRole == RoleNames.ChiefComplianceManager || userRole == RoleNames.ComplianceManager) return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? "admin@technova.com";
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Administrator";
            IsSuperAdmin = userRole == RoleNames.SuperAdmin;

            var branchIdStr = HttpContext.Session.GetString(SessionKeys.BranchId);
            if (int.TryParse(branchIdStr, out var branchId))
            {
                CallerBranchId = branchId;
                BranchDisplayName = HttpContext.Session.GetString(SessionKeys.BranchName) ?? string.Empty;
            }

            IQueryable<TechNova_IT_Solutions.Models.Supplier> supplierQuery = _context.Suppliers.Include(s => s.SupplierPolicies);
            if (!IsSuperAdmin && CallerBranchId.HasValue)
            {
                supplierQuery = supplierQuery.Where(s => s.BranchId == CallerBranchId || s.BranchId == null);
            }

            var baseIds = await supplierQuery.Select(s => s.SupplierId).ToListAsync();
            TotalSuppliers = baseIds.Count;
            ActiveSuppliers = await supplierQuery.CountAsync(s => s.Status == "Active");
            CompliantSuppliers = await _context.SupplierPolicies
                .Where(sp => baseIds.Contains(sp.SupplierId) && sp.ComplianceStatus == "Compliant")
                .Select(sp => sp.SupplierId)
                .Distinct()
                .CountAsync();

            Suppliers = await supplierQuery
                .Include(s => s.Branch)
                .OrderBy(s => s.SupplierName)
                .Select(s => new TechNova_IT_Solutions.Pages.SupplierItem
                {
                    RawSupplierId = s.SupplierId,
                    SupplierId = "SUP-" + s.SupplierId.ToString("D3"),
                    SupplierName = s.SupplierName ?? string.Empty,
                    ContactFirstName = s.ContactPersonFirstName ?? string.Empty,
                    ContactLastName = s.ContactPersonLastName ?? string.Empty,
                    Email = s.Email ?? string.Empty,
                    ContactNumber = s.ContactPersonNumber ?? string.Empty,
                    Address = s.Address ?? string.Empty,
                    Status = s.Status ?? "Active",
                    IsGlobal = s.BranchId == null,
                    BranchLabel = s.BranchId == null ? "Main" : (s.Branch != null ? s.Branch.BranchName : "Branch #" + s.BranchId),
                    ComplianceStatus = s.SupplierPolicies.Any(sp => sp.ComplianceStatus == "Compliant")
                        ? "Compliant"
                        : s.SupplierPolicies.Any(sp => sp.ComplianceStatus == "Non-Compliant")
                            ? "Not Compliant"
                            : "Pending"
                })
                .ToListAsync();

            ActivePolicies = await _context.Policies
                .OrderBy(p => p.PolicyTitle)
                .Select(p => new TechNova_IT_Solutions.Pages.SupplierPolicyItem
                {
                    PolicyId = "POL-" + p.PolicyId.ToString("D3"),
                    Title = p.PolicyTitle ?? string.Empty
                })
                .ToListAsync();

            return Page();
        }
    }
}
