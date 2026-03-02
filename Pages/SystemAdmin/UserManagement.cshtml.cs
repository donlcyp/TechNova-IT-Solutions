namespace TechNova_IT_Solutions.Pages.SystemAdmin
{
    public class UserManagementModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IBranchService _branchService;

        public UserManagementModel(IUserService userService, IBranchService branchService)
        {
            _userService = userService;
            _branchService = branchService;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? BranchDisplayName { get; set; }
        public bool IsSuperAdmin { get; set; }

        public List<UserData> Users { get; set; } = new();
        public List<BranchData> Branches { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToPage("/Account/Login");

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.SystemAdmin && userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee) return RedirectToPage("/Employee/Dashboard");
                if (userRole == RoleNames.BranchAdmin) return RedirectToPage("/BranchAdmin/Dashboard");
                if (userRole == RoleNames.ChiefComplianceManager || userRole == RoleNames.ComplianceManager) return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? "admin@technova.com";
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Administrator";
            IsSuperAdmin = userRole == RoleNames.SuperAdmin;

            var allUsers = await _userService.GetAllUsersAsync();

            if (IsSuperAdmin)
            {
                Users = allUsers;
            }
            else
            {
                // System Admin sees all users system-wide
                Users = allUsers;
            }

            // Load branches for BranchAdmin assignment
            Branches = await _branchService.GetAllBranchesAsync();

            return Page();
        }
    }
}
