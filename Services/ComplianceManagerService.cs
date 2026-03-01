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

        public async Task<ComplianceDashboardData> GetComplianceDashboardDataAsync(int? branchId = null)
        {
            var data = new ComplianceDashboardData();
            bool scoped = branchId.HasValue;

            // Get total policies assigned
            data.TotalPoliciesAssigned = scoped
                ? await _context.PolicyAssignments
                    .Include(pa => pa.User)
                    .Where(pa => pa.User.BranchId == branchId)
                    .CountAsync()
                : await _context.PolicyAssignments.CountAsync();

            // Get employees compliant count
            var assignmentsWithStatus = await _context.PolicyAssignments
                .Include(pa => pa.ComplianceStatus)
                .Include(pa => pa.User)
                .Where(pa => !scoped || pa.User.BranchId == branchId)
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
            data.SuppliersCompliant = scoped
                ? await _context.Suppliers
                    .Where(s => s.Status == "Active" && (s.BranchId == branchId || s.BranchId == null))
                    .CountAsync()
                : await _context.Suppliers
                    .Where(s => s.Status == "Active")
                    .CountAsync();

            // Get employee compliance data
            data.EmployeeCompliance = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .Include(pa => pa.Policy)
                .Include(pa => pa.ComplianceStatus)
                .Where(pa => !scoped || pa.User.BranchId == branchId)
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

            // Get supplier compliance data — real data from SupplierPolicies
            var supplierQuery = _context.Suppliers
                .Include(s => s.SupplierPolicies)
                .ThenInclude(sp => sp.Policy)
                .Where(s => s.Status == "Active" && (!scoped || s.BranchId == branchId || s.BranchId == null))
                .Take(10);

            data.SupplierCompliance = (await supplierQuery.ToListAsync())
                .Select(s =>
                {
                    var policies = s.SupplierPolicies.ToList();
                    var totalAssigned = policies.Count;
                    var compliantCount = policies.Count(sp =>
                        sp.ComplianceStatus != null &&
                        sp.ComplianceStatus.Equals("Compliant", StringComparison.OrdinalIgnoreCase));
                    var latestAssignment = policies
                        .Where(sp => sp.AssignedDate.HasValue)
                        .OrderByDescending(sp => sp.AssignedDate)
                        .FirstOrDefault();

                    return new SupplierComplianceData
                    {
                        SupplierName = s.SupplierName,
                        Industry = s.Address ?? "N/A",
                        AssignedPolicy = policies.Any()
                            ? policies.First().Policy!.PolicyTitle
                            : "No Policies Assigned",
                        ComplianceStatus = totalAssigned == 0 ? "No Policies"
                            : compliantCount == totalAssigned ? "Compliant"
                            : "Not Compliant",
                        VerifiedDate = latestAssignment?.AssignedDate?.ToString("yyyy-MM-dd") ?? "N/A",
                        TotalPoliciesAssigned = totalAssigned,
                        PoliciesCompliant = compliantCount
                    };
                })
                .ToList();

            // Get recently assigned policies
            data.RecentlyAssigned = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .Include(pa => pa.Policy)
                .Where(pa => !scoped || pa.User.BranchId == branchId)
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

            // Get total archived policies count
            data.TotalArchivedPolicies = await _context.Policies
                .Where(p => p.IsArchived)
                .CountAsync();

            // Get recently archived policies (latest 8)
            data.RecentlyArchivedPolicies = await _context.Policies
                .Include(p => p.UploadedByUser)
                .Where(p => p.IsArchived)
                .OrderByDescending(p => p.ArchivedDate)
                .Take(8)
                .Select(p => new ArchivedPolicyRecord
                {
                    PolicyId = p.PolicyId,
                    PolicyTitle = p.PolicyTitle,
                    Category = p.Category ?? "Uncategorized",
                    Description = p.Description ?? "No description available",
                    ArchivedDate = p.ArchivedDate.HasValue
                        ? p.ArchivedDate.Value.ToString("yyyy-MM-dd")
                        : "Unknown",
                    UploadedBy = p.UploadedByUser != null
                        ? $"{p.UploadedByUser.FirstName} {p.UploadedByUser.LastName}"
                        : "Unknown",
                    DateUploaded = p.DateUploaded.HasValue
                        ? p.DateUploaded.Value.ToString("yyyy-MM-dd")
                        : "Unknown"
                })
                .ToListAsync();

            return data;
        }

        public async Task<EmployeeComplianceReportData> GetEmployeeComplianceReportAsync(int? branchId = null)
        {
            var data = new EmployeeComplianceReportData();
            bool scoped = branchId.HasValue;

            // Get all assignments with related data
            var allAssignments = await _context.PolicyAssignments
                .Include(pa => pa.ComplianceStatus)
                .Include(pa => pa.User)
                .Where(pa => !scoped || pa.User.BranchId == branchId)
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
                .Where(cs => cs.AcknowledgedDate.HasValue && cs.AcknowledgedDate.Value >= sevenDaysAgo
                    && (!scoped || cs.PolicyAssignment.User.BranchId == branchId))
                .CountAsync();

            // Get employee records
            var employeeAssignments = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .Include(pa => pa.Policy)
                .Include(pa => pa.ComplianceStatus)
                .Where(pa => !scoped || pa.User.BranchId == branchId)
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

        public async Task<AuditTrailData> GetAuditTrailDataAsync(int? branchId = null)
        {
            var data = new AuditTrailData();
            bool scoped = branchId.HasValue;

            // Get total policy actions
            data.TotalPolicyActions = scoped
                ? await _context.AuditLogs
                    .Where(al => al.Module == "Policy" && al.UserId != null &&
                        _context.Users.Any(u => u.UserId == al.UserId && u.BranchId == branchId))
                    .CountAsync()
                : await _context.AuditLogs.Where(al => al.Module == "Policy").CountAsync();

            // Get total compliance actions
            data.TotalComplianceActions = scoped
                ? await _context.AuditLogs
                    .Where(al => al.Module == "Compliance" && al.UserId != null &&
                        _context.Users.Any(u => u.UserId == al.UserId && u.BranchId == branchId))
                    .CountAsync()
                : await _context.AuditLogs.Where(al => al.Module == "Compliance").CountAsync();

            // Get activities today
            var today = DateTime.Today;
            data.ActivitiesToday = scoped
                ? await _context.AuditLogs
                    .Where(al => al.LogDate.Date == today && al.UserId != null &&
                        _context.Users.Any(u => u.UserId == al.UserId && u.BranchId == branchId))
                    .CountAsync()
                : await _context.AuditLogs.Where(al => al.LogDate.Date == today).CountAsync();

            // Get audit logs
            var auditQuery = _context.AuditLogs
                .Include(al => al.User)
                .OrderByDescending(al => al.LogDate);

            data.AuditLogs = await (scoped
                ? auditQuery.Where(al => al.UserId != null && al.User!.BranchId == branchId)
                : auditQuery)
                .Take(50)
                .Select(al => new Interfaces.AuditLogRecord
                {
                    LogId = al.LogId,
                    UserName = al.User != null ? $"{al.User.FirstName} {al.User.LastName}" : "System",
                    UserEmail = al.User != null ? al.User.Email : "system@technova.com",
                    Role = al.User != null ? (al.User.Role ?? "Unknown") : "System",
                    ActionPerformed = al.Action ?? "Unknown Action",
                    FullDescription = al.Action ?? "No additional details available",
                    Module = al.Module ?? "General",
                    DateTime = al.LogDate.ToString("yyyy-MM-dd HH:mm"),
                    ExactTimestamp = al.LogDate.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    IpAddress = "N/A"
                })
                .ToListAsync();

            return data;
        }

        public async Task<ComplianceReportsData> GetComplianceReportsDataAsync(int? branchId = null)
        {
            var data = new ComplianceReportsData();
            bool scoped = branchId.HasValue;

            // Get total policies
            data.TotalPolicies = await _context.Policies.CountAsync();

            // Get total employees (scoped)
            data.TotalEmployees = scoped
                ? await _context.Users.Where(u => u.Role == "Employee" && u.BranchId == branchId).CountAsync()
                : await _context.Users.Where(u => u.Role == "Employee").CountAsync();

            // Load assignments with compliance status (scoped)
            var allAssignments = await _context.PolicyAssignments
                .Include(pa => pa.ComplianceStatus)
                .Include(pa => pa.User)
                .Where(pa => !scoped || pa.User.BranchId == branchId)
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
                .Include(pa => pa.User)
                .Where(pa => !scoped || pa.User.BranchId == branchId)
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

        public async Task<PolicyArchivesData> GetPolicyArchivesAsync(string? searchTerm, string? categoryFilter, int? branchId = null)
        {
            var data = new PolicyArchivesData();
            bool scoped = branchId.HasValue;

            // Base query: only archived policies, optionally branch-scoped via Policy.BranchId
            var archivedQuery = _context.Policies
                .Include(p => p.UploadedByUser)
                .Where(p => p.IsArchived)
                .Where(p => !scoped || p.BranchId == branchId || p.BranchId == null);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                archivedQuery = archivedQuery.Where(p =>
                    p.PolicyTitle.ToLower().Contains(term) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)) ||
                    (p.Category != null && p.Category.ToLower().Contains(term)));
            }

            // Apply category filter
            if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != "all")
            {
                archivedQuery = archivedQuery.Where(p => p.Category == categoryFilter);
            }

            // Summary stats (before filtering for cards) — scoped
            var allArchived = _context.Policies
                .Include(p => p.UploadedByUser)
                .Where(p => p.IsArchived)
                .Where(p => !scoped || p.BranchId == branchId || p.BranchId == null);
            data.TotalArchived = await allArchived.CountAsync();

            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            data.ArchivedThisMonth = await allArchived
                .Where(p => p.ArchivedDate.HasValue && p.ArchivedDate.Value >= startOfMonth)
                .CountAsync();

            data.TotalCategories = await allArchived
                .Where(p => p.Category != null)
                .Select(p => p.Category)
                .Distinct()
                .CountAsync();

            // Get filtered archived policies
            data.ArchivedPolicies = await archivedQuery
                .OrderByDescending(p => p.ArchivedDate)
                .Select(p => new ArchivedPolicyRecord
                {
                    PolicyId = p.PolicyId,
                    PolicyTitle = p.PolicyTitle,
                    Category = p.Category ?? "Uncategorized",
                    Description = p.Description ?? "No description available",
                    ArchivedDate = p.ArchivedDate.HasValue
                        ? p.ArchivedDate.Value.ToString("yyyy-MM-dd")
                        : "Unknown",
                    UploadedBy = p.UploadedByUser != null
                        ? $"{p.UploadedByUser.FirstName} {p.UploadedByUser.LastName}"
                        : "Unknown",
                    DateUploaded = p.DateUploaded.HasValue
                        ? p.DateUploaded.Value.ToString("yyyy-MM-dd")
                        : "Unknown"
                })
                .ToListAsync();

            return data;
        }
    }
}
