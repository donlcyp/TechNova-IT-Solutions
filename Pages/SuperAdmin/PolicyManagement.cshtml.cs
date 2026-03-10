using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Models;
using TechNova_IT_Solutions.Services;

namespace TechNova_IT_Solutions.Pages.SuperAdmin
{
    public class PolicyManagementModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPolicyReferenceApiService _policyReferenceApiService;

        public PolicyManagementModel(
            ApplicationDbContext context,
            IPolicyReferenceApiService policyReferenceApiService)
        {
            _context = context;
            _policyReferenceApiService = policyReferenceApiService;
        }

        // Stats
        public int TotalPolicies { get; set; }
        public int ActivePolicies { get; set; }
        public int ArchivedPolicies { get; set; }

        // Tabs data
        public List<Policy> Policies { get; set; } = new();
        public List<ComplianceTableRow> ComplianceRows { get; set; } = new();
        public List<ExternalPolicyData> ExternalPolicies { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(
                this,
                new[] { RoleNames.SuperAdmin },
                new Dictionary<string, string>
                {
                    [RoleNames.ChiefComplianceManager] = "/ComplianceManager/ComplianceDashboard",
                    [RoleNames.ComplianceManager] = "/ComplianceManager/ComplianceDashboard",
                    [RoleNames.Employee] = "/Employee/Dashboard",
                    [RoleNames.Supplier] = "/Supplier/Dashboard"
                });
            if (denied != null) return denied;

            TotalPolicies = await _context.Policies.CountAsync();
            ActivePolicies = await _context.Policies.CountAsync(p => !p.IsArchived);
            ArchivedPolicies = await _context.Policies.CountAsync(p => p.IsArchived);

            Policies = await _context.Policies
                .OrderByDescending(p => p.DateUploaded)
                .Take(100)
                .ToListAsync();

            ComplianceRows = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .Include(pa => pa.Policy)
                .Include(pa => pa.ComplianceStatus)
                .OrderByDescending(pa => pa.AssignedDate)
                .Take(100)
                .Select(pa => new ComplianceTableRow
                {
                    Assignee = pa.User.FirstName + " " + pa.User.LastName,
                    PolicyTitle = pa.Policy.PolicyTitle,
                    ComplianceStatus = pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged"
                        ? "Compliant"
                        : "Not Compliant",
                    DateAssigned = pa.AssignedDate
                })
                .ToListAsync();

            var externalPolicies = await _policyReferenceApiService.GetAllPoliciesAsync();
            ExternalPolicies = externalPolicies
                .OrderByDescending(p => p.DateUploaded ?? DateTime.MinValue)
                .Take(50)
                .ToList();

            return Page();
        }

        public class ComplianceTableRow
        {
            public string Assignee { get; set; } = string.Empty;
            public string PolicyTitle { get; set; } = string.Empty;
            public string ComplianceStatus { get; set; } = string.Empty;
            public DateTime? DateAssigned { get; set; }
        }
    }
}



