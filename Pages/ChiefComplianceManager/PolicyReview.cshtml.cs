
namespace TechNova_IT_Solutions.Pages.ChiefComplianceManager
{
    public class PolicyReviewModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PolicyReviewModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CategoryFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public List<PolicyItem> Policies { get; set; } = new();
        public List<string> AvailableCategories { get; set; } = new();
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

            // Check user role - only ChiefComplianceManager and SuperAdmin can access
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.ChiefComplianceManager && userRole != RoleNames.SuperAdmin)
            {
                // Redirect to appropriate dashboard based on role
                if (userRole == RoleNames.Employee)
                {
                    return RedirectToPage("/Employee/Dashboard");
                }
                return RedirectToPage("/Account/Login");
            }

            // Chief CM always sees all branches — no branch scoping
            int? callerBranchId = null;
            bool scoped = callerBranchId.HasValue;

            // Load policies from database
            var allPolicies = await _context.Policies
                .Include(p => p.UploadedByUser)
                .Where(p => !scoped || (p.UploadedByUser != null && p.UploadedByUser.BranchId == callerBranchId))
                .OrderByDescending(p => p.DateUploaded)
                .Select(p => new PolicyItem
                {
                    PolicyId = p.PolicyId,
                    Title = p.PolicyTitle,
                    Category = p.Category ?? "General",
                    Description = p.Description ?? string.Empty,
                    UploadedBy = p.UploadedByUser != null ? $"{p.UploadedByUser.FirstName} {p.UploadedByUser.LastName}" : "Unknown",
                    DateUploaded = p.DateUploaded ?? DateTime.Now,
                    Status = p.IsArchived ? "Archived" : "Active",
                    FileName = p.FilePath ?? string.Empty
                })
                .ToListAsync();

            // Calculate statistics from all policies (before filtering)
            TotalDraftPolicies = allPolicies.Count(p => p.Status == "Archived");
            TotalApprovedPolicies = allPolicies.Count(p => p.Status == "Active");
            RecentlyUploadedPolicies = allPolicies.Count(p => p.DateUploaded >= DateTime.Now.AddDays(-7));

            // Collect all distinct categories before filtering
            AvailableCategories = allPolicies
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

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
                // Un-archive / re-activate the policy if it was archived
                if (policy.IsArchived)
                {
                    policy.IsArchived = false;
                    policy.ArchivedDate = null;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Policy has been re-activated successfully!";
                }
                else
                {
                    TempData["SuccessMessage"] = "Policy is already active.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Policy not found.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEdit(PolicyItem policy, IFormFile? policyFile)
        {
            var existing = await _context.Policies.FindAsync(policy.PolicyId);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Policy not found.";
                return RedirectToPage();
            }

            existing.PolicyTitle = policy.Title;
            existing.Category = policy.Category;
            existing.Description = policy.Description;

            if (policyFile != null && policyFile.Length > 0)
            {
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "policies");
                Directory.CreateDirectory(uploadsDir);
                var safeFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(policyFile.FileName)}";
                var fullPath = Path.Combine(uploadsDir, safeFileName);
                await using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await policyFile.CopyToAsync(stream);
                }
                existing.FilePath = $"/uploads/policies/{safeFileName}";
            }

            await _context.SaveChangesAsync();
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





