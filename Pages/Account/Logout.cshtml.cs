using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Models;

namespace TechNova_IT_Solutions.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LogoutModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGet()
        {
            // Log the logout action
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId))
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = "User Logout",
                    Module = "Authentication",
                    LogDate = DateTime.Now
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }

            // Clear session data
            HttpContext.Session.Clear();
            TempData.Clear();
            
            return RedirectToPage("/Account/Login");
        }
    }
}



