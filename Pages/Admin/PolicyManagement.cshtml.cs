
namespace TechNova_IT_Solutions.Pages
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

        // Summary Data
        public int TotalPolicies { get; set; }
        public int ActivePolicies { get; set; }
        public int ArchivedPolicies { get; set; }
        public int RecentlyUploaded { get; set; }

        // Policy List
        public List<PolicyMgmtItem> Policies { get; set; } = new();

        // For Assignment
        public List<PolicyEmployee> Employees { get; set; } = new();
        public List<PolicySupplier> Suppliers { get; set; } = new();

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
            if (!RoleNames.IsAdminRole(userRole) && userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee) return RedirectToPage("/Employee/Dashboard");
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

            // Calculate summary statistics
            TotalPolicies = await _context.Policies.CountAsync();
            ActivePolicies = await _context.Policies.Where(p => !p.IsArchived).CountAsync();
            ArchivedPolicies = await _context.Policies.Where(p => p.IsArchived).CountAsync();
            
            // Count recently uploaded (last 30 days)
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            RecentlyUploaded = await _context.Policies
                .Where(p => p.DateUploaded >= thirtyDaysAgo)
                .CountAsync();

            // Fetch policies from database
            Policies = await _context.Policies
                .Include(p => p.UploadedByUser)
                .OrderByDescending(p => p.DateUploaded)
                .Select(p => new PolicyMgmtItem
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

            // Fetch employees for policy assignment — scoped to caller's branch
            var employeeQuery = _context.Users
                .Where(u => u.Role == RoleNames.Employee && u.Status == "Active");
            if (!IsSuperAdmin && CallerBranchId.HasValue)
            {
                employeeQuery = employeeQuery.Where(u => u.BranchId == CallerBranchId);
            }
            Employees = await employeeQuery
                .OrderBy(u => u.FirstName)
                .Select(u => new PolicyEmployee
                {
                    Id = u.UserId,
                    Name = $"{u.FirstName} {u.LastName}"
                })
                .ToListAsync();

            // Fetch suppliers for policy assignment — Branch Admin sees own branch + global (null), SuperAdmin sees all
            var supplierQuery = _context.Suppliers.Where(s => s.Status == "Active");
            if (!IsSuperAdmin && CallerBranchId.HasValue)
            {
                supplierQuery = supplierQuery.Where(s => s.BranchId == CallerBranchId || s.BranchId == null);
            }
            Suppliers = await supplierQuery
                .OrderBy(s => s.SupplierName)
                .Select(s => new PolicySupplier
                {
                    Id = s.SupplierId,
                    Name = s.SupplierName ?? string.Empty
                })
                .ToListAsync();

            return Page();
        }
    }

    public class PolicyMgmtItem
    {
        public string PolicyId { get; set; } = string.Empty;
        public int RawId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
        public DateTime DateUploaded { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public string? FilePath { get; set; }
    }

    public class PolicyEmployee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PolicySupplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}





