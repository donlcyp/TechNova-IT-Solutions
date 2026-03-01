using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechNova_IT_Solutions.Models
{
    [Table("Procurement")]
    public class Procurement
    {
        [Key]
        [Column("procurementID")]
        public int ProcurementId { get; set; }

        [StringLength(255)]
        [Column("item_name")]
        public string? ItemName { get; set; }

        [StringLength(100)]
        [Column("category")]
        public string? Category { get; set; }

        [Column("quantity")]
        public int? Quantity { get; set; }

        [ForeignKey("Supplier")]
        [Column("supplierID")]
        public int? SupplierId { get; set; }

        [ForeignKey("RelatedPolicy")]
        [Column("related_policyID")]
        public int? RelatedPolicyId { get; set; }

        [Column("purchase_date")]
        public DateTime? PurchaseDate { get; set; }

        [StringLength(3)]
        [Column("currency_code")]
        public string CurrencyCode { get; set; } = "PHP";

        [Column("original_amount", TypeName = "decimal(18,2)")]
        public decimal OriginalAmount { get; set; }

        [Column("exchange_rate", TypeName = "decimal(18,6)")]
        public decimal ExchangeRate { get; set; }

        [Column("converted_amount", TypeName = "decimal(18,2)")]
        public decimal ConvertedAmount { get; set; }

        [Column("conversion_timestamp")]
        public DateTime? ConversionTimestamp { get; set; }

        [StringLength(40)]
        [Column("status")]
        public string Status { get; set; } = "Draft";

        [Column("supplier_response_date")]
        public DateTime? SupplierResponseDate { get; set; }

        [Column("supplier_response_deadline")]
        public DateTime? SupplierResponseDeadline { get; set; }

        [Column("supplier_commit_ship_date")]
        public DateTime? SupplierCommitShipDate { get; set; }

        [Column("revised_delivery_date")]
        public DateTime? RevisedDeliveryDate { get; set; }

        [StringLength(500)]
        [Column("delay_reason")]
        public string? DelayReason { get; set; }

        [Column("shipment_date")]
        public DateTime? ShipmentDate { get; set; }

        [Column("received_date")]
        public DateTime? ReceivedDate { get; set; }

        [StringLength(500)]
        [Column("rejection_reason")]
        public string? RejectionReason { get; set; }

        /// <summary>
        /// null = company-wide procurement.
        /// non-null = belongs to a specific branch.
        /// </summary>
        [Column("branch_id")]
        public int? BranchId { get; set; }

        // Navigation properties
        public virtual Branch? Branch { get; set; }
        public virtual Supplier? Supplier { get; set; }
        public virtual Policy? RelatedPolicy { get; set; }
        public virtual ICollection<ProcurementStatusHistory> StatusHistory { get; set; } = new List<ProcurementStatusHistory>();
    }
}
