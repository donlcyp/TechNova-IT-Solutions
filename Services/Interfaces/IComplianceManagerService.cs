using TechNova_IT_Solutions.Models;

namespace TechNova_IT_Solutions.Services.Interfaces
{
    public interface IComplianceManagerService
    {
        Task<ComplianceDashboardData> GetComplianceDashboardDataAsync();
        Task<EmployeeComplianceReportData> GetEmployeeComplianceReportAsync();
        Task<AuditTrailData> GetAuditTrailDataAsync();
        Task<ComplianceReportsData> GetComplianceReportsDataAsync();
        Task<List<ExternalPolicyData>> GetExternalPolicyReferencesAsync(string category);
    }

    public class ComplianceDashboardData
    {
        public int TotalPoliciesAssigned { get; set; }
        public int EmployeesCompliant { get; set; }
        public int EmployeesNotCompliant { get; set; }
        public int SuppliersCompliant { get; set; }
        public List<EmployeeComplianceData> EmployeeCompliance { get; set; } = new();
        public List<SupplierComplianceData> SupplierCompliance { get; set; } = new();
        public List<RecentlyAssignedData> RecentlyAssigned { get; set; } = new();
    }

    public class EmployeeComplianceData
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = string.Empty;
        public string AcknowledgedDate { get; set; } = string.Empty;
    }

    public class SupplierComplianceData
    {
        public string SupplierName { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = string.Empty;
        public string VerifiedDate { get; set; } = string.Empty;
    }

    public class RecentlyAssignedData
    {
        public string PolicyName { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string DateAssigned { get; set; } = string.Empty;
    }

    public class EmployeeComplianceReportData
    {
        public int TotalEmployeesAssigned { get; set; }
        public int EmployeesCompliant { get; set; }
        public int EmployeesNotCompliant { get; set; }
        public int RecentlyAcknowledged { get; set; }
        public List<EmployeeRecord> EmployeeRecords { get; set; } = new();
        public List<EmployeeDetail> EmployeeDetails { get; set; } = new();
    }

    public class EmployeeRecord
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public string DateAssigned { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = string.Empty;
        public string AcknowledgedDate { get; set; } = string.Empty;
    }

    public class EmployeeDetail
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string JoinedDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<EmployeePolicyInfo> Policies { get; set; } = new();
    }

    public class EmployeePolicyInfo
    {
        public string PolicyName { get; set; } = string.Empty;
        public string DateAssigned { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AcknowledgedDate { get; set; } = string.Empty;
    }

    public class AuditTrailData
    {
        public int TotalPolicyActions { get; set; }
        public int TotalComplianceActions { get; set; }
        public int ActivitiesToday { get; set; }
        public List<AuditLogRecord> AuditLogs { get; set; } = new();
    }

    public class AuditLogRecord
    {
        public int LogId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ActionPerformed { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string DateTime { get; set; } = string.Empty;
    }

    public class ComplianceReportsData
    {
        public int TotalPolicies { get; set; }
        public int TotalEmployees { get; set; }
        public int CompliantEmployees { get; set; }
        public int NonCompliantEmployees { get; set; }
        public double ComplianceRate { get; set; }
        public List<PolicyComplianceData> PolicyCompliance { get; set; } = new();
    }

    public class PolicyComplianceData
    {
        public string PolicyName { get; set; } = string.Empty;
        public int TotalAssigned { get; set; }
        public int Compliant { get; set; }
        public int NonCompliant { get; set; }
        public double ComplianceRate { get; set; }
    }
}
