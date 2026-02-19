using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Models;

namespace TechNova_IT_Solutions.Pages.SuperAdmin
{
    public class PolicyManagementModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PolicyManagementModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Policy> Policies { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(this, new[] { RoleNames.SuperAdmin });
            if (denied != null) return denied;

            Policies = await _context.Policies
                .OrderByDescending(p => p.DateUploaded)
                .Take(100)
                .ToListAsync();

            return Page();
        }
    }
}



