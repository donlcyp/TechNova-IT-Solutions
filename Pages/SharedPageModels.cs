namespace TechNova_IT_Solutions.Pages
{
    // Shared view-model types used by SystemAdmin, BranchAdmin, and other Razor Page models.

    public class AuditLogEntry
    {
        public string LogId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ActionPerformed { get; set; } = string.Empty;
        public string FullDescription { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public string IpAddress { get; set; } = string.Empty;
    }

    public class EmployeeComplianceItem
    {
        public string Name { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public DateTime DateAssigned { get; set; }
        public string ComplianceStatus { get; set; } = string.Empty;
        public DateTime? AcknowledgedDate { get; set; }
    }

    public class SupplierComplianceItem
    {
        public string Name { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = string.Empty;
        public DateTime DateAssigned { get; set; }
    }

    public class PolicyMgmtItem
    {
        public string PolicyId { get; set; } = string.Empty;
        public int RawId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
        public DateTime DateUploaded { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public string? FilePath { get; set; }
    }

    public class PolicyEmployee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PolicySupplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProcurementRecord
    {
        public string ProcurementId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int? PolicyId { get; set; }
        public string LinkedPolicy { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal? OriginalAmount { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? ConvertedAmount { get; set; }
        public DateTime? DeliveryBegin { get; set; }
        public DateTime? RevisedDeliveryDate { get; set; }
        public string? DelayReason { get; set; }
        public string WorkflowStatus { get; set; } = string.Empty;
        public DateTime? SupplierResponseDeadline { get; set; }
        public DateTime? PossibleArrival { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public string DeliveryStatus { get; set; } = string.Empty;
        public bool CanEdit { get; set; }
        public bool CanMarkDeliveryArrived { get; set; }
        public bool CanDelete { get; set; }
        public string BranchName { get; set; } = string.Empty;
    }

    public class SupplierReference
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PolicyReference
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class SupplierItemReference
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public string? Name { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; }
        public string? CurrencyCode { get; set; }
        public int? QuantityAvailable { get; set; }
        public string? Status { get; set; }
    }

    public class ComplianceReportItem
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = string.Empty;
        public DateTime? AssignedDate { get; set; }
        public DateTime? AcknowledgedDate { get; set; }
    }

    public class SupplierReportItem
    {
        public string SupplierName { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = string.Empty;
        public DateTime? AssignedDate { get; set; }
    }

    public class ProcurementReportItem
    {
        public string ProcurementId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string LinkedPolicy { get; set; } = string.Empty;
        public DateTime? PurchaseDate { get; set; }
        public int Quantity { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
    }

    public class SupplierItem
    {
        public int RawSupplierId { get; set; }
        public string SupplierId { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string ContactFirstName { get; set; } = string.Empty;
        public string ContactLastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsGlobal { get; set; }
        public string BranchLabel { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = string.Empty;
        public string? TerminationReason { get; set; }
        public DateTime? TerminatedAt { get; set; }
    }

    public class SupplierPolicyItem
    {
        public int RawPolicyId { get; set; }
        public string PolicyId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}
