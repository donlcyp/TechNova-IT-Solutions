using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.SystemAdmin
{
    public class DashboardModel : PageModel
    {
        private readonly IBranchService _branchService;
        private readonly ApplicationDbContext _context;
        private readonly IAdminService _adminService;

        public DashboardModel(IBranchService branchService, ApplicationDbContext context, IAdminService adminService)
        {
            _branchService = branchService;
            _context = context;
            _adminService = adminService;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        // Branch Stats
        public int TotalBranches { get; set; }
        public int ActiveBranches { get; set; }
        public int InactiveBranches { get; set; }
        public int UnassignedBranches { get; set; }
        public int TotalBranchAdmins { get; set; }
        public int TotalSuppliers { get; set; }

        // Branch list for overview table
        public List<BranchData> Branches { get; set; } = new();

        // Recent activities
        public List<ActivityItem> RecentActivities { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToPage("/Account/Login");

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.SystemAdmin && userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.BranchAdmin) return RedirectToPage("/BranchAdmin/Dashboard");
                if (userRole == RoleNames.Employee)    return RedirectToPage("/Employee/Dashboard");
                if (userRole == RoleNames.ChiefComplianceManager || userRole == RoleNames.ComplianceManager)
                    return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? string.Empty;
            UserName  = HttpContext.Session.GetString(SessionKeys.UserName)  ?? "System Administrator";

            // Branch stats
            Branches         = await _branchService.GetAllBranchesAsync();
            TotalBranches    = Branches.Count;
            ActiveBranches   = Branches.Count(b => b.Status == "Active");
            InactiveBranches = Branches.Count(b => b.Status != "Active");
            UnassignedBranches = Branches.Count(b => b.AssignedAdminId == null);

            TotalBranchAdmins = await _context.Users
                .CountAsync(u => u.Role == RoleNames.BranchAdmin && u.Status == "Active");

            TotalSuppliers = await _context.Suppliers.CountAsync();

            // Recent activities
            var dashboardData = await _adminService.GetDashboardDataAsync(null);
            RecentActivities = dashboardData.RecentActivities;

            return Page();
        }
    }
}
