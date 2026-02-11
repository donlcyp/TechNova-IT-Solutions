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

        // Navigation properties
        public virtual Supplier? Supplier { get; set; }
        public virtual Policy? RelatedPolicy { get; set; }
    }
}
