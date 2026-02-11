using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Models;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _context;

        public EmployeeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<EmployeeDashboardData> GetEmployeeDashboardDataAsync(int userId)
        {
            var data = new EmployeeDashboardData();

            // Get assigned policies count
            data.AssignedPolicies = await _context.PolicyAssignments
                .Where(pa => pa.UserId == userId)
                .CountAsync();

            // Get pending policies (where compliance status is pending or not acknowledged)
            var pendingAssignments = await _context.PolicyAssignments
                .Include(pa => pa.ComplianceStatus)
                .Where(pa => pa.UserId == userId)
                .ToListAsync();
            
            data.PendingPolicies = pendingAssignments
                .Count(pa => pa.ComplianceStatus == null || pa.ComplianceStatus.Status == "Pending");

            // Get acknowledged policies
            data.AcknowledgedPolicies = pendingAssignments
                .Count(pa => pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged");

            // Calculate compliance rate
            if (data.AssignedPolicies > 0)
            {
                data.ComplianceRate = (int)((double)data.AcknowledgedPolicies / data.AssignedPolicies * 100);
            }

            return data;
        }

        public async Task<EmployeeComplianceStatusData> GetEmployeeComplianceStatusAsync(int userId)
        {
            var data = new EmployeeComplianceStatusData();

            // Get assigned policies count and related data
            var assignments = await _context.PolicyAssignments
                .Include(pa => pa.ComplianceStatus)
                .Where(pa => pa.UserId == userId)
                .ToListAsync();

            data.TotalPolicies = assignments.Count;

            // Get acknowledged policies
            data.AcknowledgedPolicies = assignments
                .Count(pa => pa.ComplianceStatus != null && pa.ComplianceStatus.Status == "Acknowledged");

            // Get pending policies
            data.PendingPolicies = assignments
                .Count(pa => pa.ComplianceStatus == null || pa.ComplianceStatus.Status == "Pending");

            // Calculate compliance score
            if (data.TotalPolicies > 0)
            {
                data.ComplianceScore = (data.AcknowledgedPolicies * 100) / data.TotalPolicies;
            }

            data.LastUpdated = DateTime.Now.ToString("MMM d, yyyy");

            return data;
        }

        public async Task<List<AssignedPolicyData>> GetAssignedPoliciesAsync(int userId)
        {
            return await _context.PolicyAssignments
                .Where(pa => pa.UserId == userId)
                .Include(pa => pa.Policy)
                .Include(pa => pa.ComplianceStatus)
                .Select(pa => new AssignedPolicyData
                {
                    PolicyId = pa.Policy != null ? pa.Policy.PolicyId : 0,
                    PolicyTitle = pa.Policy != null ? pa.Policy.PolicyTitle : "Unknown",
                    Category = pa.Policy != null ? (pa.Policy.Category ?? "General") : "General",
                    FilePath = pa.Policy != null ? pa.Policy.FilePath : null,
                    Description = pa.Policy != null ? pa.Policy.Description : null,
                    DateAssigned = pa.AssignedDate ?? DateTime.Now,
                    Status = pa.ComplianceStatus != null ? pa.ComplianceStatus.Status ?? "Pending" : "Pending",
                    AcknowledgedDate = pa.ComplianceStatus != null ? pa.ComplianceStatus.AcknowledgedDate : null
                })
                .OrderByDescending(p => p.DateAssigned)
                .ToListAsync();
        }

        public async Task<bool> AcknowledgePolicyAsync(int userId, int policyId)
        {
            try
            {
                // Find the assignment
                var assignment = await _context.PolicyAssignments
                    .Include(pa => pa.ComplianceStatus)
                    .FirstOrDefaultAsync(pa => pa.UserId == userId && pa.PolicyId == policyId);

                if (assignment == null) return false;

                if (assignment.ComplianceStatus != null)
                {
                    // Update existing compliance status
                    assignment.ComplianceStatus.Status = "Acknowledged";
                    assignment.ComplianceStatus.AcknowledgedDate = DateTime.Now;
                }
                else
                {
                    // Create new compliance status
                    var complianceStatus = new Models.ComplianceStatus
                    {
                        AssignmentId = assignment.AssignmentId,
                        Status = "Acknowledged",
                        AcknowledgedDate = DateTime.Now
                    };
                    _context.ComplianceStatuses.Add(complianceStatus);
                }

                // Log the acknowledgment
                var auditLog = new Models.AuditLog
                {
                    UserId = userId,
                    Action = $"Acknowledged policy: {policyId}",
                    Module = "Compliance",
                    LogDate = DateTime.Now
                };
                _context.AuditLogs.Add(auditLog);

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
