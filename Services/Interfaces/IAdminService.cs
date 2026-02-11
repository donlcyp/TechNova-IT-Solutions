namespace TechNova_IT_Solutions.Services.Interfaces
{
    public interface IAdminService
    {
        Task<AdminDashboardData> GetDashboardDataAsync();
        
        // Policy operations
        Task<bool> CreatePolicyAsync(PolicyData policyData);
        Task<bool> UpdatePolicyAsync(PolicyData policyData);
        Task<bool> DeletePolicyAsync(int policyId);
        
        // Policy assignment
        Task<bool> AssignPolicyToEmployeesAsync(int policyId, List<int> employeeIds);
        Task<bool> AssignPolicyToSuppliersAsync(int policyId, List<int> supplierIds);
        
        // Supplier operations
        Task<bool> CreateSupplierAsync(SupplierData supplierData);
        Task<bool> UpdateSupplierAsync(SupplierData supplierData);
        Task<bool> DeleteSupplierAsync(int supplierId);
        
        // Procurement operations
        Task<bool> CreateProcurementAsync(ProcurementData procurementData);
        Task<bool> UpdateProcurementAsync(ProcurementData procurementData);
        Task<bool> DeleteProcurementAsync(int procurementId);

        // Audit logging
        Task LogActivityAsync(int? userId, string action, string module);

        // Policy file retrieval
        Task<PolicyData?> GetPolicyByIdAsync(int policyId);
    }

    public class AdminDashboardData
    {
        public int TotalUsers { get; set; }
        public int ActivePolicies { get; set; }
        public int PendingCompliance { get; set; }
        public int TotalSuppliers { get; set; }
        public int RecentProcurements { get; set; }
        public int AuditLogsToday { get; set; }
        public int CompliancePercentage { get; set; }
        public List<PolicyItem> RecentPolicies { get; set; } = new();
        public List<ProcurementItem> RecentProcurementsData { get; set; } = new();
        public List<ActivityItem> RecentActivities { get; set; } = new();
    }

    public class PolicyItem
    {
        public string Name { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
    }

    public class ProcurementItem
    {
        public string Supplier { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string LinkedPolicy { get; set; } = string.Empty;
    }

    public class ActivityItem
    {
        public string IconClass { get; set; } = string.Empty;
        public string IconSvg { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
    }

    public class PolicyData
    {
        public int PolicyId { get; set; }
        public string PolicyTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? FilePath { get; set; } = string.Empty;
        public DateTime? UploadedDate { get; set; }
    }

    public class SupplierData
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string ContactPersonFirstName { get; set; } = string.Empty;
        public string ContactPersonLastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactPersonNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class ProcurementData
    {
        public int ProcurementId { get; set; }
        public int SupplierId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime ProcurementDate { get; set; }
        public int? PolicyId { get; set; }
    }

    public class PolicyAssignmentRequest
    {
        public int PolicyId { get; set; }
        public List<int> EmployeeIds { get; set; } = new();
        public List<int> SupplierIds { get; set; } = new();
    }
}
