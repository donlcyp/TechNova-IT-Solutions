
namespace TechNova_IT_Solutions.Pages.ComplianceManager
{
    public class PolicyArchivesModel : PageModel
    {
        private readonly IComplianceManagerService _complianceService;
        private readonly ApplicationDbContext _context;

        public PolicyArchivesModel(IComplianceManagerService complianceService, ApplicationDbContext context)
        {
            _complianceService = complianceService;
            _context = context;
        }

        public int TotalArchived { get; set; }
        public int ArchivedThisMonth { get; set; }
        public int TotalCategories { get; set; }
        public List<ArchivedPolicyRecord> ArchivedPolicies { get; set; } = new();
        public int ExternalImportsCount { get; set; }
        public List<ExternalPolicyArchiveRow> ExternalPolicyImports { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CategoryFilter { get; set; }

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
                if (userRole == RoleNames.Employee)
                {
                    return RedirectToPage("/Employee/Dashboard");
                }
                return RedirectToPage("/Account/Login");
            }

            // Get archive data from service
            var archiveData = await _complianceService.GetPolicyArchivesAsync(SearchTerm, CategoryFilter);

            TotalArchived = archiveData.TotalArchived;
            ArchivedThisMonth = archiveData.ArchivedThisMonth;
            TotalCategories = archiveData.TotalCategories;
            ArchivedPolicies = archiveData.ArchivedPolicies;
            ExternalPolicyImports = await _context.ExternalPolicyImports
                .AsNoTracking()
                .OrderByDescending(i => i.ImportedAt)
                .Take(50)
                .Select(i => new ExternalPolicyArchiveRow
                {
                    ImportId = i.ImportId,
                    PolicyTitle = i.PolicyTitle,
                    Category = i.Category ?? "General",
                    SourceApi = i.SourceApi,
                    DocumentNumber = i.DocumentNumber,
                    ReviewStatus = i.ReviewStatus,
                    ImportedAt = i.ImportedAt,
                    ReviewedAt = i.ReviewedAt
                })
                .ToListAsync();
            ExternalImportsCount = ExternalPolicyImports.Count;

            return Page();
        }

        public class ExternalPolicyArchiveRow
        {
            public int ImportId { get; set; }
            public string PolicyTitle { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string SourceApi { get; set; } = string.Empty;
            public string? DocumentNumber { get; set; }
            public string ReviewStatus { get; set; } = string.Empty;
            public DateTime ImportedAt { get; set; }
            public DateTime? ReviewedAt { get; set; }
        }
    }
}





