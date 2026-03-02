namespace TechNova_IT_Solutions.Pages.SystemAdmin
{
    public class PolicyManagementModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PolicyManagementModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
        public int? CallerBranchId { get; set; }
        public string BranchDisplayName { get; set; } = string.Empty;

        public int TotalPolicies { get; set; }
        public int ActivePolicies { get; set; }
        public int ArchivedPolicies { get; set; }
        public int RecentlyUploaded { get; set; }

        public List<TechNova_IT_Solutions.Pages.PolicyMgmtItem> Policies { get; set; } = new();
        public List<TechNova_IT_Solutions.Pages.PolicyEmployee> Employees { get; set; } = new();
        public List<TechNova_IT_Solutions.Pages.PolicySupplier> Suppliers { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToPage("/Account/Login");

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            // Policy management belongs to compliance roles — redirect SystemAdmin
            if (userRole == RoleNames.SystemAdmin)
                return RedirectToPage("/SystemAdmin/Dashboard");
            if (userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee) return RedirectToPage("/Employee/Dashboard");
                if (userRole == RoleNames.BranchAdmin) return RedirectToPage("/BranchAdmin/Dashboard");
                if (userRole == RoleNames.ChiefComplianceManager || userRole == RoleNames.ComplianceManager) return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? "admin@technova.com";
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Administrator";
            IsSuperAdmin = userRole == RoleNames.SuperAdmin;

            // System Admin sees all policies system-wide
            TotalPolicies = await _context.Policies.CountAsync();
            ActivePolicies = await _context.Policies.Where(p => !p.IsArchived).CountAsync();
            ArchivedPolicies = await _context.Policies.Where(p => p.IsArchived).CountAsync();

            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            RecentlyUploaded = await _context.Policies
                .Where(p => p.DateUploaded >= thirtyDaysAgo)
                .CountAsync();

            Policies = await _context.Policies
                .Include(p => p.UploadedByUser)
                .OrderByDescending(p => p.DateUploaded)
                .Select(p => new TechNova_IT_Solutions.Pages.PolicyMgmtItem
                {
                    PolicyId = "POL-" + p.PolicyId.ToString("D3"),
                    RawId = p.PolicyId,
                    Title = p.PolicyTitle ?? string.Empty,
                    Category = p.Category ?? string.Empty,
                    Description = p.Description ?? string.Empty,
                    Status = p.IsArchived ? "Archived" : "Active",
                    IsArchived = p.IsArchived,
                    DateUploaded = p.DateUploaded ?? DateTime.Now,
                    UploadedBy = p.UploadedByUser != null
                        ? $"{p.UploadedByUser.FirstName} {p.UploadedByUser.LastName}"
                        : "System",
                    FilePath = p.FilePath
                })
                .ToListAsync();

            // System Admin sees all employees
            Employees = await _context.Users
                .Where(u => u.Role == RoleNames.Employee && u.Status == "Active")
                .OrderBy(u => u.FirstName)
                .Select(u => new TechNova_IT_Solutions.Pages.PolicyEmployee
                {
                    Id = u.UserId,
                    Name = $"{u.FirstName} {u.LastName}"
                })
                .ToListAsync();

            // System Admin sees all suppliers
            Suppliers = await _context.Suppliers
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.SupplierName)
                .Select(s => new TechNova_IT_Solutions.Pages.PolicySupplier
                {
                    Id = s.SupplierId,
                    Name = s.SupplierName ?? string.Empty
                })
                .ToListAsync();

            return Page();
        }
    }
}
