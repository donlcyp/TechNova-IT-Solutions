
namespace TechNova_IT_Solutions.Pages.ComplianceManager
{
    public class ViolationManagementModel : PageModel
    {
        public int OpenViolations { get; set; }
        public int UnderReviewViolations { get; set; }
        public int EscalatedViolations { get; set; }
        public int ResolvedViolations { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToPage("/Account/Login");

            var role = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (role != RoleNames.ComplianceManager && !RoleNames.IsAdminRole(role))
                return RedirectToPage("/Account/Login");

            var branchIdStr = HttpContext.Session.GetString(SessionKeys.BranchId);
            int? branchId = int.TryParse(branchIdStr, out var bid) ? bid : null;
            // Branch CM is always scoped to their branch

            // Summary stats are rendered server-side; list is loaded via AJAX client-side
            using var scope = HttpContext.RequestServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

            IQueryable<Models.ComplianceViolation> q = context.ComplianceViolations;

            if (branchId.HasValue)
            {
                q = q.Where(v =>
                    (v.PolicyAssignment != null && v.PolicyAssignment.User != null && v.PolicyAssignment.User.BranchId == branchId) ||
                    (v.SupplierPolicy != null && v.SupplierPolicy.Supplier != null && (v.SupplierPolicy.Supplier.BranchId == branchId || v.SupplierPolicy.Supplier.BranchId == null)));
            }

            OpenViolations = await q.CountAsync(v => v.Status == "Open");
            UnderReviewViolations = await q.CountAsync(v => v.Status == "UnderReview");
            EscalatedViolations = await q.CountAsync(v => v.Status == "Escalated");
            ResolvedViolations = await q.CountAsync(v => v.Status == "Resolved");

            return Page();
        }
    }
}
