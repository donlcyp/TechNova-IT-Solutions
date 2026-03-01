using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Pages.Supplier
{
    public class SettingsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthenticationService _authService;

        public SettingsModel(ApplicationDbContext context, IAuthenticationService authService)
        {
            _context = context;
            _authService = authService;
        }

        [BindProperty]
        public string CurrentPassword { get; set; } = string.Empty;

        [BindProperty]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public string SupplierName { get; set; } = string.Empty;
        public string SupplierEmail { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(this, new[] { RoleNames.Supplier }, fallbackPage: "/Supplier/Login");
            if (denied != null) return denied;

            await LoadProfileAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(this, new[] { RoleNames.Supplier }, fallbackPage: "/Supplier/Login");
            if (denied != null) return denied;

            await LoadProfileAsync();

            if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ErrorMessage = "All password fields are required.";
                return Page();
            }

            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "New password and confirmation do not match.";
                return Page();
            }

            if (NewPassword.Length < 8)
            {
                ErrorMessage = "New password must be at least 8 characters.";
                return Page();
            }

            var userEmail = HttpContext.Session.GetString(SessionKeys.UserEmail);
            if (string.IsNullOrWhiteSpace(userEmail))
                return RedirectToPage("/Supplier/Login");

            // Verify current password via authentication service
            var authResult = await _authService.AuthenticateUserAsync(userEmail, CurrentPassword);
            if (!authResult.Success)
            {
                ErrorMessage = "Current password is incorrect.";
                return Page();
            }

            // Update password
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                ErrorMessage = "User account not found.";
                return Page();
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            await _context.SaveChangesAsync();

            SuccessMessage = "Password changed successfully.";
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            return Page();
        }

        private async Task LoadProfileAsync()
        {
            var userEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? string.Empty;
            SupplierEmail = userEmail;

            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Email == userEmail);
            SupplierName = supplier != null
                ? $"{supplier.ContactPersonFirstName} {supplier.ContactPersonLastName}".Trim()
                : HttpContext.Session.GetString(SessionKeys.UserName) ?? string.Empty;
        }
    }
}
