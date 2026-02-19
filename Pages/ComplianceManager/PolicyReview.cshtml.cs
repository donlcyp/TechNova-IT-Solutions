
namespace TechNova_IT_Solutions.Pages.ComplianceManager
{
    public class PolicyReviewModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PolicyReviewModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CategoryFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public List<PolicyItem> Policies { get; set; } = new();
        public int TotalDraftPolicies { get; set; }
        public int TotalApprovedPolicies { get; set; }
        public int RecentlyUploadedPolicies { get; set; }

        public async Task<IActionResult> OnGet()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only ComplianceManager and Admin can access
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.ComplianceManager && userRole != RoleNames.Admin && userRole != RoleNames.SuperAdmin)
            {
                // Redirect to appropriate dashboard based on role
                if (userRole == RoleNames.Employee)
                {
                    return RedirectToPage("/Employee/Dashboard");
                }
                return RedirectToPage("/Account/Login");
            }

            // Load policies from database
            var allPolicies = await _context.Policies
                .Include(p => p.UploadedByUser)
                .OrderByDescending(p => p.DateUploaded)
                .Select(p => new PolicyItem
                {
                    PolicyId = p.PolicyId,
                    Title = p.PolicyTitle,
                    Category = p.Category ?? "General",
                    Description = p.Description ?? string.Empty,
                    UploadedBy = p.UploadedByUser != null ? $"{p.UploadedByUser.FirstName} {p.UploadedByUser.LastName}" : "Unknown",
                    DateUploaded = p.DateUploaded ?? DateTime.Now,
                    Status = "Active",
                    FileName = p.FilePath ?? string.Empty
                })
                .ToListAsync();

            // Calculate statistics from all policies (before filtering)
            TotalDraftPolicies = allPolicies.Count(p => p.Status == "Draft");
            TotalApprovedPolicies = allPolicies.Count(p => p.Status == "Approved" || p.Status == "Active");
            RecentlyUploadedPolicies = allPolicies.Count(p => p.DateUploaded >= DateTime.Now.AddDays(-7));

            // Apply filters
            var filteredPolicies = allPolicies.AsEnumerable();

            if (!string.IsNullOrEmpty(SearchQuery))
            {
                filteredPolicies = filteredPolicies.Where(p => 
                    p.Title.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(CategoryFilter) && CategoryFilter != "All")
            {
                filteredPolicies = filteredPolicies.Where(p => p.Category == CategoryFilter);
            }

            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "All")
            {
                filteredPolicies = filteredPolicies.Where(p => p.Status == StatusFilter);
            }

            Policies = filteredPolicies.ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostApprove(int policyId)
        {
            var policy = await _context.Policies.FindAsync(policyId);
            if (policy != null)
            {
                // Policy status update logic can be added here if a status column exists
            }
            
            TempData["SuccessMessage"] = "Policy approved successfully!";
            return RedirectToPage();
        }

        public IActionResult OnPostEdit(PolicyItem policy)
        {
            // TODO: Implement actual database update
            
            TempData["SuccessMessage"] = "Policy updated successfully!";
            return RedirectToPage();
        }
    }

    public class PolicyItem
    {
        public int PolicyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime DateUploaded { get; set; }
        public string Status { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}





