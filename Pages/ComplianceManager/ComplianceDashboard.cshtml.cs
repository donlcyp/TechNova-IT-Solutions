
namespace TechNova_IT_Solutions.Pages.ComplianceManager
{
    public class ComplianceDashboardModel : PageModel
    {
        private readonly IComplianceManagerService _complianceService;
        private readonly Data.ApplicationDbContext _context;

        public ComplianceDashboardModel(IComplianceManagerService complianceService, Data.ApplicationDbContext context)
        {
            _complianceService = complianceService;
            _context = context;
        }

        public int TotalPoliciesAssigned { get; set; }
        public int EmployeesCompliant { get; set; }
        public int EmployeesNotCompliant { get; set; }
        public int SuppliersCompliant { get; set; }
        public int TotalArchivedPolicies { get; set; }

        // Risk indicators
        public int OpenViolations { get; set; }
        public int EscalatedViolations { get; set; }
        public int SuspendedSuppliers { get; set; }

        public List<EmployeeComplianceData> EmployeeCompliance { get; set; } = new();
        public List<SupplierComplianceData> SupplierCompliance { get; set; } = new();
        public List<RecentlyAssignedData> RecentlyAssigned { get; set; } = new();
        public List<ArchivedPolicyRecord> RecentlyArchivedPolicies { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only ComplianceManager and Admin can access
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.ComplianceManager && userRole != RoleNames.Admin)
            {
                // Redirect to appropriate dashboard based on role
                if (userRole == RoleNames.Employee)
                {
                    return RedirectToPage("/Employee/Dashboard");
                }
                return RedirectToPage("/Account/Login");
            }

            // Extract branch scope
            int? callerBranchId = null;
            if (userRole == RoleNames.ComplianceManager || userRole == RoleNames.Admin)
            {
                var branchIdStr = HttpContext.Session.GetString(SessionKeys.BranchId);
                if (!string.IsNullOrEmpty(branchIdStr) && int.TryParse(branchIdStr, out var bid))
                    callerBranchId = bid;
            }

            // Get dashboard data from service
            var dashboardData = await _complianceService.GetComplianceDashboardDataAsync(callerBranchId);
            
            TotalPoliciesAssigned = dashboardData.TotalPoliciesAssigned;
            EmployeesCompliant = dashboardData.EmployeesCompliant;
            EmployeesNotCompliant = dashboardData.EmployeesNotCompliant;
            SuppliersCompliant = dashboardData.SuppliersCompliant;
            TotalArchivedPolicies = dashboardData.TotalArchivedPolicies;
            EmployeeCompliance = dashboardData.EmployeeCompliance;
            SupplierCompliance = dashboardData.SupplierCompliance;
            RecentlyAssigned = dashboardData.RecentlyAssigned;
            RecentlyArchivedPolicies = dashboardData.RecentlyArchivedPolicies;

            // Load risk indicators from violation table
            IQueryable<Models.ComplianceViolation> vq = _context.ComplianceViolations;
            if (callerBranchId.HasValue)
            {
                vq = vq.Where(v =>
                    (v.PolicyAssignment != null && v.PolicyAssignment.User != null && v.PolicyAssignment.User.BranchId == callerBranchId) ||
                    (v.SupplierPolicy != null && v.SupplierPolicy.Supplier != null && (v.SupplierPolicy.Supplier.BranchId == callerBranchId || v.SupplierPolicy.Supplier.BranchId == null)));
            }
            OpenViolations = await vq.CountAsync(v => v.Status == "Open" || v.Status == "UnderReview");
            EscalatedViolations = await vq.CountAsync(v => v.Status == "Escalated");

            IQueryable<Models.Supplier> sq = _context.Suppliers.Where(s => s.Status == "Suspended");
            if (callerBranchId.HasValue)
                sq = sq.Where(s => s.BranchId == callerBranchId || s.BranchId == null);
            SuspendedSuppliers = await sq.CountAsync();

            return Page();
        }
    }
}





