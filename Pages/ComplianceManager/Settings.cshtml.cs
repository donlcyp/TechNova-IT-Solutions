
namespace TechNova_IT_Solutions.Pages.ComplianceManager
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

        public async Task<IActionResult> OnGet()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only ComplianceManager and Admin can access
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.ComplianceManager && userRole != RoleNames.Admin && userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee)
                {
                    return RedirectToPage("/Employee/Dashboard");
                }
                return RedirectToPage("/Account/Login");
            }

            // Load current user from database
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                Profile.FullName = $"{user.FirstName} {user.LastName}".Trim();
                Profile.Email = user.Email ?? "";
                Profile.Phone = "";
                Profile.Role = user.Role ?? RoleNames.ComplianceManager;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfile()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                var nameParts = (Profile.FullName ?? "").Split(' ', 2);
                user.FirstName = nameParts.Length > 0 ? nameParts[0] : "";
                user.LastName = nameParts.Length > 1 ? nameParts[1] : "";
                user.Email = Profile.Email;
                // Phone not stored in User model
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString(SessionKeys.UserName, Profile.FullName ?? "");
                HttpContext.Session.SetString(SessionKeys.UserEmail, Profile.Email ?? "");
            }

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePassword()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Password.NewPassword != Password.ConfirmPassword)
            {
                ModelState.AddModelError("Password.ConfirmPassword", "Passwords do not match.");
                return Page();
            }

            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return Page();
            }

            // Verify current password
            if (!PasswordHasher.VerifyPassword(Password.CurrentPassword, user.Password))
            {
                ModelState.AddModelError("Password.CurrentPassword", "Current password is incorrect.");
                return Page();
            }

            // Hash and save new password
            user.Password = PasswordHasher.HashPassword(Password.NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToPage();
        }
    }

    public class ProfileSettings
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class PasswordChange
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}





