namespace TechNova_IT_Solutions.Pages.BranchAdmin
{
    public class ComplianceMonitoringModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ComplianceMonitoringModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public int TotalPoliciesAssigned { get; set; }
        public int EmployeesCompliant { get; set; }
        public int EmployeesNotCompliant { get; set; }
        public int SuppliersCompliant { get; set; }

        public List<TechNova_IT_Solutions.Pages.EmployeeComplianceItem> EmployeeCompliance { get; set; } = new();
        public List<TechNova_IT_Solutions.Pages.SupplierComplianceItem> SupplierCompliance { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToPage("/Account/Login");

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            // Compliance monitoring belongs to compliance roles — BranchAdmin does not monitor compliance
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

            int? callerBranchId = null;
            if (userRole == RoleNames.BranchAdmin)
            {
                var branchIdStr = HttpContext.Session.GetString(SessionKeys.BranchId);
                if (!string.IsNullOrEmpty(branchIdStr) && int.TryParse(branchIdStr, out var bid))
                    callerBranchId = bid;
            }

            bool scoped = callerBranchId.HasValue;

            TotalPoliciesAssigned = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .Where(pa => !scoped || (pa.User != null && pa.User.BranchId == callerBranchId))
                .CountAsync();

            EmployeesCompliant = await _context.ComplianceStatuses
                .Include(cs => cs.PolicyAssignment!.User)
                .Where(cs => cs.Status == "Acknowledged")
                .Where(cs => !scoped || (cs.PolicyAssignment != null && cs.PolicyAssignment.User != null && cs.PolicyAssignment.User.BranchId == callerBranchId))
                .CountAsync();

            EmployeesNotCompliant = await _context.ComplianceStatuses
                .Include(cs => cs.PolicyAssignment!.User)
                .Where(cs => cs.Status == "Pending" || cs.Status == "Overdue")
                .Where(cs => !scoped || (cs.PolicyAssignment != null && cs.PolicyAssignment.User != null && cs.PolicyAssignment.User.BranchId == callerBranchId))
                .CountAsync();

            SuppliersCompliant = await _context.SupplierPolicies
                .Include(sp => sp.Supplier)
                .Where(sp => sp.ComplianceStatus == "Compliant")
                .Where(sp => !scoped || sp.Supplier == null || sp.Supplier.BranchId == callerBranchId || sp.Supplier.BranchId == null)
                .Select(sp => sp.SupplierId)
                .Distinct()
                .CountAsync();

            var employeeData = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .Include(pa => pa.Policy)
                .Include(pa => pa.ComplianceStatus)
                .Where(pa => pa.User != null && pa.User.Role == RoleNames.Employee)
                .Where(pa => !scoped || (pa.User != null && pa.User.BranchId == callerBranchId))
                .ToListAsync();

            EmployeeCompliance = employeeData
                .Select(pa => new TechNova_IT_Solutions.Pages.EmployeeComplianceItem
                {
                    Name = pa.User != null ? $"{pa.User.FirstName} {pa.User.LastName}" : "Unknown",
                    AssignedPolicy = pa.Policy != null ? pa.Policy.PolicyTitle : "Unknown",
                    DateAssigned = pa.AssignedDate ?? DateTime.Now,
                    ComplianceStatus = pa.ComplianceStatus != null
                        ? (pa.ComplianceStatus.Status == "Acknowledged" ? "Compliant" : "Not Compliant")
                        : "Not Compliant",
                    AcknowledgedDate = pa.ComplianceStatus != null ? pa.ComplianceStatus.AcknowledgedDate : null
                })
                .OrderBy(x => x.Name)
                .ToList();

            SupplierCompliance = await _context.SupplierPolicies
                .Include(sp => sp.Supplier)
                .Include(sp => sp.Policy)
                .Where(sp => !scoped || sp.Supplier == null || sp.Supplier.BranchId == callerBranchId || sp.Supplier.BranchId == null)
                .OrderBy(sp => sp.Supplier.SupplierName)
                .Select(sp => new TechNova_IT_Solutions.Pages.SupplierComplianceItem
                {
                    Name = sp.Supplier != null ? sp.Supplier.SupplierName : "Unknown",
                    AssignedPolicy = sp.Policy != null ? sp.Policy.PolicyTitle : "Unknown",
                    ComplianceStatus = sp.ComplianceStatus ?? "Pending",
                    DateAssigned = sp.AssignedDate ?? DateTime.Now
                })
                .ToListAsync();

            return Page();
        }
    }
}
