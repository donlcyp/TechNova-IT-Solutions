
namespace TechNova_IT_Solutions.Pages.ComplianceManager
{
    public class PolicyArchivesModel : PageModel
    {
        private readonly IComplianceManagerService _complianceService;

        public PolicyArchivesModel(IComplianceManagerService complianceService)
        {
            _complianceService = complianceService;
        }

        public int TotalArchived { get; set; }
        public int ArchivedThisMonth { get; set; }
        public int TotalCategories { get; set; }
        public List<ArchivedPolicyRecord> ArchivedPolicies { get; set; } = new();

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

            return Page();
        }
    }
}





