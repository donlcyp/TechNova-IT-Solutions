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

        [StringLength(40)]
        [Column("status")]
        public string Status { get; set; } = "Draft";

        [Column("supplier_response_date")]
        public DateTime? SupplierResponseDate { get; set; }

        [Column("supplier_response_deadline")]
        public DateTime? SupplierResponseDeadline { get; set; }

        [Column("supplier_commit_ship_date")]
        public DateTime? SupplierCommitShipDate { get; set; }

        [Column("shipment_date")]
        public DateTime? ShipmentDate { get; set; }

        [Column("received_date")]
        public DateTime? ReceivedDate { get; set; }

        [StringLength(500)]
        [Column("rejection_reason")]
        public string? RejectionReason { get; set; }

        // Navigation properties
        public virtual Supplier? Supplier { get; set; }
        public virtual Policy? RelatedPolicy { get; set; }
        public virtual ICollection<ProcurementStatusHistory> StatusHistory { get; set; } = new List<ProcurementStatusHistory>();
    }
}
