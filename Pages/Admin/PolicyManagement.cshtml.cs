using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Data;

namespace TechNova_IT_Solutions.Pages
{
    public class PolicyManagementModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PolicyManagementModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        // Summary Data
        public int TotalPolicies { get; set; }
        public int ActivePolicies { get; set; }
        public int RecentlyUploaded { get; set; }

        // Policy List
        public List<PolicyMgmtItem> Policies { get; set; } = new();

        // For Assignment
        public List<PolicyEmployee> Employees { get; set; } = new();
        public List<PolicySupplier> Suppliers { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            // Check authentication
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only Admin can access
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                if (userRole == "Employee") return RedirectToPage("/Employee/Dashboard");
                if (userRole == "ComplianceManager") return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString("UserEmail") ?? "admin@technova.com";
            UserName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            // Calculate summary statistics
            TotalPolicies = await _context.Policies.CountAsync();
            ActivePolicies = TotalPolicies; // All policies are considered active by default
            
            // Count recently uploaded (last 30 days)
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            RecentlyUploaded = await _context.Policies
                .Where(p => p.DateUploaded >= thirtyDaysAgo)
                .CountAsync();

            // Fetch policies from database
            Policies = await _context.Policies
                .Include(p => p.UploadedByUser)
                .OrderByDescending(p => p.DateUploaded)
                .Select(p => new PolicyMgmtItem
                {
                    PolicyId = "POL-" + p.PolicyId.ToString("D3"),
                    Title = p.PolicyTitle ?? string.Empty,
                    Category = p.Category ?? string.Empty,
                    Status = "Active", // Default status
                    DateUploaded = p.DateUploaded ?? DateTime.Now,
                    UploadedBy = p.UploadedByUser != null 
                        ? $"{p.UploadedByUser.FirstName} {p.UploadedByUser.LastName}" 
                        : "System"
                })
                .ToListAsync();

            // Fetch employees for policy assignment
            Employees = await _context.Users
                .Where(u => u.Role == "Employee" && u.Status == "Active")
                .OrderBy(u => u.FirstName)
                .Select(u => new PolicyEmployee
                {
                    Id = u.UserId,
                    Name = $"{u.FirstName} {u.LastName}"
                })
                .ToListAsync();

            // Fetch suppliers for policy assignment
            Suppliers = await _context.Suppliers
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.SupplierName)
                .Select(s => new PolicySupplier
                {
                    Id = s.SupplierId,
                    Name = s.SupplierName ?? string.Empty
                })
                .ToListAsync();

            return Page();
        }
    }

    public class PolicyMgmtItem
    {
        public string PolicyId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DateUploaded { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
    }

    public class PolicyEmployee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PolicySupplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
