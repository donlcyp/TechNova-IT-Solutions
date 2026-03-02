
namespace TechNova_IT_Solutions.Pages.ChiefComplianceManager
{
    public class PolicyReviewModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IAdminService _adminService;

        public PolicyReviewModel(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IAdminService adminService)
        {
            _context = context;
            _environment = environment;
            _adminService = adminService;
        }

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CategoryFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public List<PolicyReviewItem> Policies { get; set; } = new();
        public List<string> AvailableCategories { get; set; } = new();

        // Summary counts
        public int PendingReviewCount { get; set; }
        public int PendingUpdateCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int ArchivedCount { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToPage("/Account/Login");

            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.ChiefComplianceManager && userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee) return RedirectToPage("/Employee/Dashboard");
                return RedirectToPage("/Account/Login");
            }

            var allPolicies = await _context.Policies
                .Include(p => p.UploadedByUser)
                .Include(p => p.Branch)
                .Include(p => p.ReviewedByUser)
                .OrderByDescending(p => p.DateUploaded)
                .Select(p => new PolicyReviewItem
                {
                    PolicyId = p.PolicyId,
                    Title = p.PolicyTitle,
                    Category = p.Category ?? "General",
                    Description = p.Description ?? string.Empty,
                    UploadedBy = p.UploadedByUser != null
                        ? $"{p.UploadedByUser.FirstName} {p.UploadedByUser.LastName}"
                        : "System",
                    BranchName = p.Branch != null ? p.Branch.BranchName : "Company-wide",
                    DateUploaded = p.DateUploaded ?? DateTime.Now,
                    ReviewStatus = p.ReviewStatus ?? "Approved",
                    FileName = p.FilePath ?? string.Empty,
                    IsArchived = p.IsArchived,
                    ReviewedBy = p.ReviewedByUser != null
                        ? $"{p.ReviewedByUser.FirstName} {p.ReviewedByUser.LastName}"
                        : null,
                    ReviewedAt = p.ReviewedAt,
                    ReviewNotes = p.ReviewNotes,
                    // Pending-update fields
                    PendingTitle = p.PendingTitle,
                    PendingCategory = p.PendingCategory,
                    PendingDescription = p.PendingDescription,
                    PendingFilePath = p.PendingFilePath,
                    PendingUpdatedAt = p.PendingUpdatedAt,
                    HasPendingUpdate = p.ReviewStatus == "PendingUpdate"
                })
                .ToListAsync();

            // Decode HTML entities stored in legacy data (e.g. "&amp;" → "&")
            foreach (var item in allPolicies)
            {
                item.Category = System.Net.WebUtility.HtmlDecode(item.Category);
                if (item.PendingCategory != null)
                    item.PendingCategory = System.Net.WebUtility.HtmlDecode(item.PendingCategory);
            }

            // Summary counts
            PendingReviewCount = allPolicies.Count(p => p.ReviewStatus == "PendingReview");
            PendingUpdateCount = allPolicies.Count(p => p.ReviewStatus == "PendingUpdate");
            ApprovedCount = allPolicies.Count(p => p.ReviewStatus == "Approved");
            RejectedCount = allPolicies.Count(p => p.ReviewStatus == "Rejected");
            ArchivedCount = allPolicies.Count(p => p.ReviewStatus == "Archived");

            AvailableCategories = allPolicies
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct().OrderBy(c => c).ToList();

            // Apply filters
            var filtered = allPolicies.AsEnumerable();

            if (!string.IsNullOrEmpty(SearchQuery))
                filtered = filtered.Where(p => p.Title.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(CategoryFilter) && CategoryFilter != "All")
                filtered = filtered.Where(p => p.Category == CategoryFilter);

            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "All")
                filtered = filtered.Where(p => p.ReviewStatus == StatusFilter);

            Policies = filtered.ToList();
            return Page();
        }

        // ── Approve a PendingReview policy ──────────────────────
        public async Task<IActionResult> OnPostApprove(int policyId, string? reviewNotes)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return RedirectToPage("/Account/Login");

            var policy = await _context.Policies.FindAsync(policyId);
            if (policy == null) { TempData["ErrorMessage"] = "Policy not found."; return RedirectToPage(); }

            bool ok;
            if (policy.ReviewStatus == "PendingUpdate")
            {
                ok = await _adminService.ApproveUpdateAsync(policyId, userId.Value, reviewNotes);
                TempData["SuccessMessage"] = ok ? "Policy update approved. Employees must re-acknowledge compliance." : "Failed to approve update.";
            }
            else
            {
                ok = await _adminService.ApprovePolicyAsync(policyId, userId.Value, reviewNotes);
                TempData["SuccessMessage"] = ok ? "Policy approved successfully! It is now available for assignment." : "Failed to approve policy.";
            }

            if (!ok) TempData["ErrorMessage"] = TempData["SuccessMessage"]?.ToString()?.Contains("Failed") == true ? TempData["SuccessMessage"]?.ToString() : null;
            return RedirectToPage();
        }

        // ── Reject a PendingReview or PendingUpdate policy ──────
        public async Task<IActionResult> OnPostReject(int policyId, string? reviewNotes)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return RedirectToPage("/Account/Login");

            var ok = await _adminService.RejectPolicyAsync(policyId, userId.Value, reviewNotes);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Policy has been rejected. The branch has been notified."
                : "Failed to reject policy.";
            return RedirectToPage();
        }

        private int? GetCurrentUserId()
        {
            var s = HttpContext.Session.GetString(SessionKeys.UserId);
            return int.TryParse(s, out var id) ? id : null;
        }
    }

    public class PolicyReviewItem
    {
        public int PolicyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public DateTime DateUploaded { get; set; }
        public string ReviewStatus { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        // Pending update
        public string? PendingTitle { get; set; }
        public string? PendingCategory { get; set; }
        public string? PendingDescription { get; set; }
        public string? PendingFilePath { get; set; }
        public DateTime? PendingUpdatedAt { get; set; }
        public bool HasPendingUpdate { get; set; }
    }
}





