using TechNova_IT_Solutions.Models;

namespace TechNova_IT_Solutions.Services.Interfaces
{
    public interface IComplianceManagerService
    {
        Task<ComplianceDashboardData> GetComplianceDashboardDataAsync(int? branchId = null);
        Task<EmployeeComplianceReportData> GetEmployeeComplianceReportAsync(int? branchId = null);
        Task<AuditTrailData> GetAuditTrailDataAsync(int? branchId = null);
        Task<ComplianceReportsData> GetComplianceReportsDataAsync(int? branchId = null);
        Task<List<ExternalPolicyData>> GetExternalPolicyReferencesAsync(string category);
        Task<PolicyArchivesData> GetPolicyArchivesAsync(string? searchTerm, string? categoryFilter, int? branchId = null);
    }

    public class ComplianceDashboardData
    {
        public int TotalPoliciesAssigned { get; set; }
        public int EmployeesCompliant { get; set; }
        public int EmployeesNotCompliant { get; set; }
        public int SuppliersCompliant { get; set; }
        public int TotalArchivedPolicies { get; set; }
        public List<EmployeeComplianceData> EmployeeCompliance { get; set; } = new();
        public List<SupplierComplianceData> SupplierCompliance { get; set; } = new();
        public List<RecentlyAssignedData> RecentlyAssigned { get; set; } = new();
        public List<ArchivedPolicyRecord> RecentlyArchivedPolicies { get; set; } = new();
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
        public int TotalPoliciesAssigned { get; set; }
        public int PoliciesCompliant { get; set; }
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
        public string UserEmail { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ActionPerformed { get; set; } = string.Empty;
        public string FullDescription { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string DateTime { get; set; } = string.Empty;
        public string ExactTimestamp { get; set; } = string.Empty;
        public string IpAddress { get; set; } = "N/A";
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

    public class PolicyArchivesData
    {
        public int TotalArchived { get; set; }
        public int ArchivedThisMonth { get; set; }
        public int TotalCategories { get; set; }
        public List<ArchivedPolicyRecord> ArchivedPolicies { get; set; } = new();
    }

    public class ArchivedPolicyRecord
    {
        public int PolicyId { get; set; }
        public string PolicyTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ArchivedDate { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public string DateUploaded { get; set; } = string.Empty;
    }
}
