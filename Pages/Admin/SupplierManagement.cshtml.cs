
namespace TechNova_IT_Solutions.Pages
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

        // Summary Data
        public int TotalSuppliers { get; set; }
        public int ActiveSuppliers { get; set; }
        public int CompliantSuppliers { get; set; }

        // Supplier List
        public List<SupplierItem> Suppliers { get; set; } = new();

        // Active Policies for Assignment
        public List<SupplierPolicyItem> ActivePolicies { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            // Check authentication
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only Admin can access
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.Admin && userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee) return RedirectToPage("/Employee/Dashboard");
                if (userRole == RoleNames.ComplianceManager) return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? "admin@technova.com";
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Administrator";

            // Calculate summary statistics
            TotalSuppliers = await _context.Suppliers.CountAsync();
            ActiveSuppliers = await _context.Suppliers.Where(s => s.Status == "Active").CountAsync();
            CompliantSuppliers = await _context.SupplierPolicies
                .Where(sp => sp.ComplianceStatus == "Compliant")
                .Select(sp => sp.SupplierId)
                .Distinct()
                .CountAsync();

            // Fetch suppliers with their compliance status
            Suppliers = await _context.Suppliers
                .Include(s => s.SupplierPolicies)
                .OrderBy(s => s.SupplierName)
                .Select(s => new SupplierItem
                {
                    SupplierId = "SUP-" + s.SupplierId.ToString("D3"),
                    SupplierName = s.SupplierName ?? string.Empty,
                    ContactFirstName = s.ContactPersonFirstName ?? string.Empty,
                    ContactLastName = s.ContactPersonLastName ?? string.Empty,
                    Email = s.Email ?? string.Empty,
                    ContactNumber = s.ContactPersonNumber ?? string.Empty,
                    Address = s.Address ?? string.Empty,
                    Status = s.Status ?? "Active",
                    ComplianceStatus = s.SupplierPolicies.Any(sp => sp.ComplianceStatus == "Compliant") 
                        ? "Compliant" 
                        : s.SupplierPolicies.Any(sp => sp.ComplianceStatus == "Non-Compliant")
                            ? "Not Compliant"
                            : "Pending"
                })
                .ToListAsync();

            // Fetch active policies for assignment
            ActivePolicies = await _context.Policies
                .OrderBy(p => p.PolicyTitle)
                .Select(p => new SupplierPolicyItem
                {
                    PolicyId = "POL-" + p.PolicyId.ToString("D3"),
                    Title = p.PolicyTitle ?? string.Empty
                })
                .ToListAsync();

            return Page();
        }
    }

    public class SupplierItem
    {
        public string SupplierId { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string ContactFirstName { get; set; } = string.Empty;
        public string ContactLastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = string.Empty;
    }

    public class SupplierPolicyItem
    {
        public string PolicyId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}





