using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Data;

namespace TechNova_IT_Solutions.Pages.ComplianceManager
{
    public class CMPolicyManagementModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CMPolicyManagementModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
        public int? CallerBranchId { get; set; }
        public string BranchDisplayName { get; set; } = string.Empty;

        // Summary
        public int TotalPolicies { get; set; }
        public int ApprovedPolicies { get; set; }
        public int PendingReviewPolicies { get; set; }
        public int PendingUpdatePolicies { get; set; }
        public int RejectedPolicies { get; set; }
        public int ArchivedPolicies { get; set; }

        // Lists
        public List<CMPolicyItem> Policies { get; set; } = new();
        public List<CMPolicyEmployee> Employees { get; set; } = new();
        public List<CMPolicySupplier> Suppliers { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToPage("/Account/Login");

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.ComplianceManager && !RoleNames.IsAdminRole(userRole))
            {
                if (userRole == RoleNames.Employee) return RedirectToPage("/Employee/Dashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? "";
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Compliance Manager";
            IsSuperAdmin = false; // Branch CM cannot manage global policies

            var branchIdStr = HttpContext.Session.GetString(SessionKeys.BranchId);
            if (int.TryParse(branchIdStr, out var branchId))
            {
                CallerBranchId = branchId;
                BranchDisplayName = HttpContext.Session.GetString(SessionKeys.BranchName) ?? string.Empty;
            }

            // Only show branch-scoped + company-wide policies
            var policyQuery = _context.Policies
                .Where(p => p.BranchId == null || p.BranchId == CallerBranchId);

            var allPolicies = await policyQuery.ToListAsync();
            TotalPolicies = allPolicies.Count;
            ApprovedPolicies = allPolicies.Count(p => p.ReviewStatus == "Approved" && !p.IsArchived);
            PendingReviewPolicies = allPolicies.Count(p => p.ReviewStatus == "PendingReview");
            PendingUpdatePolicies = allPolicies.Count(p => p.ReviewStatus == "PendingUpdate");
            RejectedPolicies = allPolicies.Count(p => p.ReviewStatus == "Rejected");
            ArchivedPolicies = allPolicies.Count(p => p.ReviewStatus == "Archived" || p.IsArchived);

            Policies = await _context.Policies
                .Where(p => p.BranchId == null || p.BranchId == CallerBranchId)
                .Include(p => p.UploadedByUser)
                .OrderByDescending(p => p.DateUploaded)
                .Select(p => new CMPolicyItem
                {
                    PolicyId = "POL-" + p.PolicyId.ToString("D3"),
                    RawId = p.PolicyId,
                    Title = p.PolicyTitle ?? string.Empty,
                    Category = p.Category ?? string.Empty,
                    Description = p.Description ?? string.Empty,
                    Status = p.IsArchived ? "Archived" : "Active",
                    ReviewStatus = p.ReviewStatus ?? "Approved",
                    IsArchived = p.IsArchived,
                    IsBranchPolicy = p.BranchId != null,
                    DateUploaded = p.DateUploaded ?? DateTime.Now,
                    UploadedBy = p.UploadedByUser != null
                        ? $"{p.UploadedByUser.FirstName} {p.UploadedByUser.LastName}"
                        : "System",
                    FilePath = p.FilePath
                })
                .ToListAsync();

            // Employees for assignment (scoped)
            var employeeQuery = _context.Users
                .Where(u => u.Role == RoleNames.Employee && u.Status == "Active");
            if (!IsSuperAdmin && CallerBranchId.HasValue)
                employeeQuery = employeeQuery.Where(u => u.BranchId == CallerBranchId);

            Employees = await employeeQuery
                .OrderBy(u => u.FirstName)
                .Select(u => new CMPolicyEmployee { Id = u.UserId, Name = $"{u.FirstName} {u.LastName}" })
                .ToListAsync();

            // Suppliers for assignment (scoped)
            var supplierQuery = _context.Suppliers.Where(s => s.Status == "Active");
            if (!IsSuperAdmin && CallerBranchId.HasValue)
                supplierQuery = supplierQuery.Where(s => s.BranchId == CallerBranchId || s.BranchId == null);

            Suppliers = await supplierQuery
                .OrderBy(s => s.SupplierName)
                .Select(s => new CMPolicySupplier { Id = s.SupplierId, Name = s.SupplierName ?? string.Empty })
                .ToListAsync();

            return Page();
        }
    }

    public class CMPolicyItem
    {
        public string PolicyId { get; set; } = string.Empty;
        public int RawId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ReviewStatus { get; set; } = "Approved";
        public bool IsArchived { get; set; }
        public bool IsBranchPolicy { get; set; }
        public DateTime DateUploaded { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public string? FilePath { get; set; }
    }

    public class CMPolicyEmployee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CMPolicySupplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
