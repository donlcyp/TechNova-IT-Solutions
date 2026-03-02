namespace TechNova_IT_Solutions.Pages.BranchAdmin
{
    public class UserManagementModel : PageModel
    {
        private readonly IUserService _userService;

        public UserManagementModel(IUserService userService)
        {
            _userService = userService;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? BranchDisplayName { get; set; }
        public bool IsSuperAdmin { get; set; }

        public List<UserData> Users { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToPage("/Account/Login");

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.BranchAdmin && userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee) return RedirectToPage("/Employee/Dashboard");
                if (userRole == RoleNames.SystemAdmin) return RedirectToPage("/SystemAdmin/Dashboard");
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
                // Branch Admin sees Employees and Compliance Managers in their own branch
                var branchIdStr = HttpContext.Session.GetString(SessionKeys.BranchId);
                if (int.TryParse(branchIdStr, out int branchId))
                {
                    var branchRoles = new[] { RoleNames.Employee, RoleNames.ComplianceManager };
                    Users = allUsers
                        .Where(u => u.BranchId == branchId && branchRoles.Contains(u.Role))
                        .ToList();
                    BranchDisplayName = allUsers.FirstOrDefault(u => u.BranchId == branchId)?.BranchName
                        ?? HttpContext.Session.GetString(SessionKeys.BranchName);
                }
                else
                {
                    Users = new List<UserData>();
                }
            }

            return Page();
        }
    }
}
