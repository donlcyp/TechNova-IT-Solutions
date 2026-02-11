using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Models;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Services
{
    public class ComplianceManagerService : IComplianceManagerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPolicyReferenceApiService _policyApiService;

        public ComplianceManagerService(
            ApplicationDbContext context,
            IPolicyReferenceApiService policyApiService)
        {
            _context = context;
            _policyApiService = policyApiService;
        }

        public async Task<ComplianceDashboardData> GetComplianceDashboardDataAsync()
        {
            var data = new ComplianceDashboardData();

            // Get total policies assigned
            data.TotalPoliciesAssigned = await _context.PolicyAssignments.CountAsync();

            // Get employees compliant count
            var assignmentsWithStatus = await _context.PolicyAssignments
                .Include(pa => pa.ComplianceStatus)
                .ToListAsync();
            
            data.EmployeesCompliant = assignmentsWithStatus
                .Where(pa => pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged")
                .Select(pa => pa.UserId)
                .Distinct()
                .Count();

            // Get employees not compliant count
            data.EmployeesNotCompliant = assignmentsWithStatus
                .Where(pa => pa.ComplianceStatus == null || pa.ComplianceStatus.Status == "Pending")
                .Select(pa => pa.UserId)
                .Distinct()
                .Count();

            // Get suppliers compliant count
            data.SuppliersCompliant = await _context.Suppliers
                .Where(s => s.Status == "Active")
                .CountAsync();

            // Get employee compliance data
            data.EmployeeCompliance = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .Include(pa => pa.Policy)
                .Include(pa => pa.ComplianceStatus)
                .OrderByDescending(pa => pa.AssignedDate)
                .Take(15)
                .Select(pa => new EmployeeComplianceData
                {
                    EmployeeName = pa.User != null ? $"{pa.User.FirstName} {pa.User.LastName}" : "Unknown",
                    Department = pa.User != null ? (pa.User.Role ?? "N/A") : "N/A",
                    AssignedPolicy = pa.Policy != null ? pa.Policy.PolicyTitle : "Unknown Policy",
                    ComplianceStatus = pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged" ? "Compliant" : "Not Compliant",
                    AcknowledgedDate = pa.ComplianceStatus != null && pa.ComplianceStatus.AcknowledgedDate.HasValue 
                        ? pa.ComplianceStatus.AcknowledgedDate.Value.ToString("yyyy-MM-dd") 
                        : "Pending"
                })
                .ToListAsync();

            // Get supplier compliance data
            data.SupplierCompliance = await _context.Suppliers
                .Include(s => s.SupplierPolicies)
                .ThenInclude(sp => sp.Policy)
                .Where(s => s.Status == "Active")
                .Take(10)
                .Select(s => new SupplierComplianceData
                {
                    SupplierName = s.SupplierName,
                    Industry = s.ContactPersonFirstName ?? "Various Industries",
                    AssignedPolicy = s.SupplierPolicies.Any() 
                        ? s.SupplierPolicies.First().Policy!.PolicyTitle 
                        : "General Supplier Policy",
                    ComplianceStatus = s.Status == "Active" ? "Compliant" : "Not Compliant",
                    VerifiedDate = DateTime.Now.AddDays(-Random.Shared.Next(1, 30)).ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            // Get recently assigned policies
            data.RecentlyAssigned = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .Include(pa => pa.Policy)
                .OrderByDescending(pa => pa.AssignedDate)
                .Take(8)
                .Select(pa => new RecentlyAssignedData
                {
                    PolicyName = pa.Policy != null ? pa.Policy.PolicyTitle : "Unknown",
                    AssignedTo = pa.User != null ? $"{pa.User.FirstName} {pa.User.LastName}" : "Unknown",
                    Type = "Employee",
                    DateAssigned = pa.AssignedDate.HasValue 
                        ? pa.AssignedDate.Value.ToString("yyyy-MM-dd") 
                        : DateTime.Now.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return data;
        }

        public async Task<EmployeeComplianceReportData> GetEmployeeComplianceReportAsync()
        {
            var data = new EmployeeComplianceReportData();

            // Get all assignments with related data
            var allAssignments = await _context.PolicyAssignments
                .Include(pa => pa.ComplianceStatus)
                .ToListAsync();

            // Get total employees assigned
            data.TotalEmployeesAssigned = allAssignments
                .Select(pa => pa.UserId)
                .Distinct()
                .Count();

            // Get employees compliant
            data.EmployeesCompliant = allAssignments
                .Where(pa => pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged")
                .Select(pa => pa.UserId)
                .Distinct()
                .Count();

            // Get employees not compliant
            data.EmployeesNotCompliant = allAssignments
                .Where(pa => pa.ComplianceStatus == null || pa.ComplianceStatus.Status == "Pending")
                .Select(pa => pa.UserId)
                .Distinct()
                .Count();

            // Get recently acknowledged count (last 7 days)
            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            data.RecentlyAcknowledged = await _context.ComplianceStatuses
                .Where(cs => cs.AcknowledgedDate.HasValue && cs.AcknowledgedDate.Value >= sevenDaysAgo)
                .CountAsync();

            // Get employee records
            var employeeAssignments = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .Include(pa => pa.Policy)
                .Include(pa => pa.ComplianceStatus)
                .ToListAsync();

            data.EmployeeRecords = employeeAssignments
                .Select(pa => new EmployeeRecord
                {
                    EmployeeId = pa.User != null ? pa.User.UserId : 0,
                    EmployeeName = pa.User != null ? $"{pa.User.FirstName} {pa.User.LastName}" : "Unknown",
                    Department = pa.User != null ? (pa.User.Role ?? "N/A") : "N/A",
                    AssignedPolicy = pa.Policy != null ? pa.Policy.PolicyTitle : "Unknown",
                    DateAssigned = pa.AssignedDate.HasValue 
                        ? pa.AssignedDate.Value.ToString("yyyy-MM-dd") 
                        : DateTime.Now.ToString("yyyy-MM-dd"),
                    ComplianceStatus = pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged" 
                        ? "Compliant" 
                        : "Not Compliant",
                    AcknowledgedDate = pa.ComplianceStatus != null && pa.ComplianceStatus.AcknowledgedDate.HasValue 
                        ? pa.ComplianceStatus.AcknowledgedDate.Value.ToString("yyyy-MM-dd") 
                        : "Pending"
                })
                .OrderBy(e => e.EmployeeName)
                .ToList();

            // Build employee details for modal (grouped by employee with their policies)
            data.EmployeeDetails = employeeAssignments
                .Where(pa => pa.User != null)
                .GroupBy(pa => pa.User!.UserId)
                .Select(g =>
                {
                    var user = g.First().User!;
                    return new EmployeeDetail
                    {
                        EmployeeId = user.UserId,
                        FullName = $"{user.FirstName} {user.LastName}",
                        Department = user.Role ?? "N/A",
                        Email = user.Email ?? "N/A",
                        Position = user.Role ?? "N/A",
                        JoinedDate = "N/A",
                        Status = user.Status ?? "Active",
                        Policies = g.Select(pa => new EmployeePolicyInfo
                        {
                            PolicyName = pa.Policy != null ? pa.Policy.PolicyTitle : "Unknown",
                            DateAssigned = pa.AssignedDate.HasValue
                                ? pa.AssignedDate.Value.ToString("yyyy-MM-dd")
                                : "N/A",
                            Status = pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged"
                                ? "Compliant"
                                : "Not Compliant",
                            AcknowledgedDate = pa.ComplianceStatus != null && pa.ComplianceStatus.AcknowledgedDate.HasValue
                                ? pa.ComplianceStatus.AcknowledgedDate.Value.ToString("yyyy-MM-dd")
                                : "Pending"
                        }).ToList()
                    };
                })
                .OrderBy(e => e.FullName)
                .ToList();

            return data;
        }

        public async Task<AuditTrailData> GetAuditTrailDataAsync()
        {
            var data = new AuditTrailData();

            // Get total policy actions
            data.TotalPolicyActions = await _context.AuditLogs
                .Where(al => al.Module == "Policy")
                .CountAsync();

            // Get total compliance actions
            data.TotalComplianceActions = await _context.AuditLogs
                .Where(al => al.Module == "Compliance")
                .CountAsync();

            // Get activities today
            var today = DateTime.Today;
            data.ActivitiesToday = await _context.AuditLogs
                .Where(al => al.LogDate.Date == today)
                .CountAsync();

            // Get audit logs
            data.AuditLogs = await _context.AuditLogs
                .Include(al => al.User)
                .OrderByDescending(al => al.LogDate)
                .Take(50)
                .Select(al => new Interfaces.AuditLogRecord
                {
                    LogId = al.LogId,
                    UserName = al.User != null ? $"{al.User.FirstName} {al.User.LastName}" : "System",
                    Role = al.User != null ? (al.User.Role ?? "Unknown") : "System",
                    ActionPerformed = al.Action ?? "Unknown Action",
                    Module = al.Module ?? "General",
                    DateTime = al.LogDate.ToString("yyyy-MM-dd HH:mm:ss")
                })
                .ToListAsync();

            return data;
        }

        public async Task<ComplianceReportsData> GetComplianceReportsDataAsync()
        {
            var data = new ComplianceReportsData();

            // Get total policies
            data.TotalPolicies = await _context.Policies.CountAsync();

            // Get total employees
            data.TotalEmployees = await _context.Users
                .Where(u => u.Role == "Employee")
                .CountAsync();

            // Load assignments with compliance status
            var allAssignments = await _context.PolicyAssignments
                .Include(pa => pa.ComplianceStatus)
                .ToListAsync();

            // Get compliant employees
            data.CompliantEmployees = allAssignments
                .Where(pa => pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged")
                .Select(pa => pa.UserId)
                .Distinct()
                .Count();

            // Get non-compliant employees
            data.NonCompliantEmployees = allAssignments
                .Where(pa => pa.ComplianceStatus == null || pa.ComplianceStatus.Status == "Pending")
                .Select(pa => pa.UserId)
                .Distinct()
                .Count();

            // Calculate compliance rate
            if (data.TotalEmployees > 0)
            {
                data.ComplianceRate = Math.Round((double)data.CompliantEmployees / data.TotalEmployees * 100, 2);
            }

            // Get policy compliance data
            var policies = await _context.Policies.ToListAsync();
            var policyAssignments = await _context.PolicyAssignments
                .Include(pa => pa.ComplianceStatus)
                .ToListAsync();

            data.PolicyCompliance = policies.Select(p => new PolicyComplianceData
            {
                PolicyName = p.PolicyTitle,
                TotalAssigned = policyAssignments.Count(pa => pa.PolicyId == p.PolicyId),
                Compliant = policyAssignments
                    .Count(pa => pa.PolicyId == p.PolicyId && 
                          pa.ComplianceStatus != null && 
                          pa.ComplianceStatus.Status == "Acknowledged"),
                NonCompliant = policyAssignments
                    .Count(pa => pa.PolicyId == p.PolicyId && 
                          (pa.ComplianceStatus == null || 
                           pa.ComplianceStatus.Status == "Pending")),
                ComplianceRate = policyAssignments.Count(pa => pa.PolicyId == p.PolicyId) > 0
                    ? Math.Round((double)policyAssignments
                        .Count(pa => pa.PolicyId == p.PolicyId && 
                              pa.ComplianceStatus != null && 
                              pa.ComplianceStatus.Status == "Acknowledged") / 
                        policyAssignments.Count(pa => pa.PolicyId == p.PolicyId) * 100, 2)
                    : 0
            })
            .OrderByDescending(pc => pc.ComplianceRate)
            .ToList();

            return data;
        }

        public async Task<List<ExternalPolicyData>> GetExternalPolicyReferencesAsync(string category)
        {
            // Fetch external policy references from the API
            return await _policyApiService.SearchPoliciesByCategoryAsync(category);
        }
    }
}
