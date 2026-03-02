namespace TechNova_IT_Solutions.Services.Interfaces
{
    public interface IEmployeeService
    {
        Task<EmployeeDashboardData> GetEmployeeDashboardDataAsync(int userId);
        Task<EmployeeComplianceStatusData> GetEmployeeComplianceStatusAsync(int userId);
        Task<List<AssignedPolicyData>> GetAssignedPoliciesAsync(int userId);
        Task<bool> AcknowledgePolicyAsync(int userId, int policyId);
        Task<int> GetPendingPoliciesCountAsync(int userId);
        Task<int> GetActiveViolationsCountAsync(int userId);
    }

    public class EmployeeDashboardData
    {
        public int AssignedPolicies { get; set; }
        public int PendingPolicies { get; set; }
        public int ComplianceRate { get; set; }
        public int AcknowledgedPolicies { get; set; }
    }

    public class EmployeeComplianceStatusData
    {
        public int ComplianceScore { get; set; }
        public int TotalPolicies { get; set; }
        public int AcknowledgedPolicies { get; set; }
        public int PendingPolicies { get; set; }
        public string LastUpdated { get; set; } = string.Empty;
    }

    public class AssignedPolicyData
    {
        public int PolicyId { get; set; }
        public string PolicyTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? Description { get; set; }
        public DateTime DateAssigned { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? AcknowledgedDate { get; set; }
    }
}
