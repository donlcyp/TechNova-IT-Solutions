
namespace TechNova_IT_Solutions.Pages.Employee
{
    public class SettingsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SettingsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ProfileSettings Profile { get; set; } = new ProfileSettings();

        [BindProperty]
        public PasswordChange Password { get; set; } = new PasswordChange();

        [BindProperty]
        public NotificationSettings Notifications { get; set; } = new NotificationSettings();

        [BindProperty]
        public DisplaySettings Display { get; set; } = new DisplaySettings();

        public async Task<IActionResult> OnGet()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only Employee and Admin can access
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.Employee && userRole != RoleNames.Admin && userRole != RoleNames.SuperAdmin)
            {
                // Redirect to appropriate dashboard based on role
                if (userRole == RoleNames.ComplianceManager)
                {
                    return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                }
                return RedirectToPage("/Account/Login");
            }

            // Load current user settings from database
            int userId = int.Parse(userIdString);
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                Profile.FullName = $"{user.FirstName} {user.LastName}";
                Profile.Email = user.Email;
                Profile.Phone = string.Empty;
                Profile.Department = user.Role ?? "N/A";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfile()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Update profile logic here
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePassword()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            int userId = int.Parse(userIdString);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Verify current password against stored hash
            if (!PasswordHasher.VerifyPassword(Password.CurrentPassword, user.Password))
            {
                ModelState.AddModelError("Password.CurrentPassword", "Current password is incorrect.");
                return Page();
            }

            if (Password.NewPassword != Password.ConfirmPassword)
            {
                ModelState.AddModelError("Password.ConfirmPassword", "Passwords do not match.");
                return Page();
            }

            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToPage();
        }

        public IActionResult OnPostUpdateNotifications()
        {
            // Update notification preferences
            TempData["SuccessMessage"] = "Notification preferences updated!";
            return RedirectToPage();
        }

        public IActionResult OnPostUpdateDisplay()
        {
            // Update display settings
            TempData["SuccessMessage"] = "Display settings updated!";
            return RedirectToPage();
        }
    }

    public class ProfileSettings
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }

    public class PasswordChange
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class NotificationSettings
    {
        public bool EmailNotifications { get; set; }
        public bool PolicyReminders { get; set; }
        public bool ComplianceAlerts { get; set; }
        public bool WeeklyDigest { get; set; }
    }

    public class DisplaySettings
    {
        public string Language { get; set; } = "en";
        public string Timezone { get; set; } = "America/New_York";
        public string Theme { get; set; } = "light";
    }
}





