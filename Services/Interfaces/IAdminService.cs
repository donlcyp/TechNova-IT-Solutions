namespace TechNova_IT_Solutions.Services.Interfaces
{
    public interface IAdminService
    {
        Task<AdminDashboardData> GetDashboardDataAsync();
        
        // Policy operations
        Task<bool> CreatePolicyAsync(PolicyData policyData);
        Task<bool> UpdatePolicyAsync(PolicyData policyData);
        Task<bool> DeletePolicyAsync(int policyId);
        Task<bool> ArchivePolicyAsync(int policyId);
        Task<bool> RestorePolicyAsync(int policyId);
        Task<PolicyDetailData?> GetPolicyDetailAsync(int policyId);
        
        // Policy assignment
        Task<bool> AssignPolicyToEmployeesAsync(int policyId, List<int> employeeIds);
        Task<bool> AssignPolicyToSuppliersAsync(int policyId, List<int> supplierIds);
        Task<PolicyAssignmentStatusData> GetPolicyAssignmentStatusAsync(int policyId);
        
        // Supplier operations
        Task<bool> CreateSupplierAsync(SupplierData supplierData);
        Task<bool> UpdateSupplierAsync(SupplierData supplierData);
        Task<bool> DeleteSupplierAsync(int supplierId);
        Task<SupplierData?> GetSupplierByIdAsync(int supplierId);
        
        // Procurement operations
        Task<bool> CreateProcurementAsync(ProcurementData procurementData);
        Task<bool> UpdateProcurementAsync(ProcurementData procurementData);
        Task<bool> DeleteProcurementAsync(int procurementId);
        Task<List<SupplierItemData>> GetSupplierItemsAsync(int supplierId);
        Task<bool> UpsertSupplierItemAsync(int supplierId, SupplierItemData itemData);
        Task<bool> SupplierRespondToProcurementAsync(SupplierProcurementActionData actionData);

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
        public bool IsArchived { get; set; }
        public DateTime? ArchivedDate { get; set; }
    }

    public class PolicyDetailData
    {
        public int PolicyId { get; set; }
        public string PolicyTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public DateTime? DateUploaded { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
        public DateTime? ArchivedDate { get; set; }
        public int AssignedEmployees { get; set; }
        public int AssignedSuppliers { get; set; }
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
        public string? Password { get; set; } // Optional: Only used when creating a new supplier
    }

    public class ProcurementData
    {
        public int ProcurementId { get; set; }
        public int SupplierId { get; set; }
        public int? SupplierItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime ProcurementDate { get; set; }
        public int? PolicyId { get; set; }
        public string? Status { get; set; }
        public DateTime? SupplierResponseDeadline { get; set; }
        public DateTime? SupplierCommitShipDate { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class SupplierItemData
    {
        public int SupplierItemId { get; set; }
        public int SupplierId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int QuantityAvailable { get; set; }
        public string Status { get; set; } = "Available";
    }

    public class SupplierProcurementActionData
    {
        public int ProcurementId { get; set; }
        public int SupplierId { get; set; }
        public bool Approve { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? SupplierCommitShipDate { get; set; }
        public int? ChangedByUserId { get; set; }
    }

    public class PolicyAssignmentRequest
    {
        public int PolicyId { get; set; }
        public List<int> PolicyIds { get; set; } = new();
        public List<int> EmployeeIds { get; set; } = new();
        public List<int> SupplierIds { get; set; } = new();
    }

    public class PolicyAssignmentStatusData
    {
        public List<int> AssignedEmployeeIds { get; set; } = new();
        public List<int> AssignedSupplierIds { get; set; } = new();
    }
}
