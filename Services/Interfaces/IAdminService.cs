namespace TechNova_IT_Solutions.Services.Interfaces
{
    public interface IAdminService
    {
        Task<AdminDashboardData> GetDashboardDataAsync(int? branchId = null);
        
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
        Task<SupplierOperationResult> CreateSupplierAsync(SupplierData supplierData);
        Task<SupplierOperationResult> UpdateSupplierAsync(SupplierData supplierData);
        Task<bool> DeleteSupplierAsync(int supplierId);
        Task<bool> TerminateSupplierAsync(SupplierTerminationData terminationData, int? changedByUserId);
        Task<bool> RestoreSupplierAsync(int supplierId, int? changedByUserId);
        Task<SupplierData?> GetSupplierByIdAsync(int supplierId);
        
        // Procurement operations
        Task<bool> CreateProcurementAsync(ProcurementData procurementData);
        Task<bool> UpdateProcurementAsync(ProcurementData procurementData);
        Task<bool> DeleteProcurementAsync(int procurementId);
        Task<bool> MarkProcurementDeliveredAsync(int procurementId, int? changedByUserId);
        Task<int> SyncLateProcurementsAsync();
        Task<List<SupplierItemData>> GetSupplierItemsAsync(int supplierId);
        Task<bool> UpsertSupplierItemAsync(int supplierId, SupplierItemData itemData);
        Task<bool> SupplierRespondToProcurementAsync(SupplierProcurementActionData actionData);
        Task<bool> SupplierReportDelayAsync(SupplierProcurementActionData actionData);

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
        /// <summary>null = company-wide policy; non-null = branch-specific.</summary>
        public int? BranchId { get; set; }
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
        public string? TerminationReason { get; set; }
        public DateTime? TerminatedAt { get; set; }
        public int? TerminatedByUserId { get; set; }
        /// <summary>null = enterprise/global supplier; non-null = owned by a specific branch.</summary>
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
    }

    public class SupplierOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SupplierTerminationData
    {
        public int SupplierId { get; set; }
        public string Reason { get; set; } = string.Empty;
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
        public string CurrencyCode { get; set; } = "PHP";
        public decimal OriginalAmount { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? ConvertedAmount { get; set; }
        public DateTime? ConversionTimestamp { get; set; }
        public string? Status { get; set; }
        public DateTime? SupplierResponseDeadline { get; set; }
        public DateTime? SupplierCommitShipDate { get; set; }
        public DateTime? RevisedDeliveryDate { get; set; }
        public string? DelayReason { get; set; }
        public string? RejectionReason { get; set; }
        /// <summary>null = company-wide procurement; non-null = branch-specific.</summary>
        public int? BranchId { get; set; }
    }

    public class SupplierItemData
    {
        public int SupplierItemId { get; set; }
        public int SupplierId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string CurrencyCode { get; set; } = "PHP";
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
        public DateTime? RevisedDeliveryDate { get; set; }
        public string? DelayReason { get; set; }
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
