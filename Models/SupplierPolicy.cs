using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechNova_IT_Solutions.Models
{
    [Table("Supplier_Policies")]
    public class SupplierPolicy
    {
        [Key]
        [Column("supplier_policiesID")]
        public int SupplierPolicyId { get; set; }

        [Required]
        [ForeignKey("Supplier")]
        [Column("supplierID")]
        public int SupplierId { get; set; }

        [Required]
        [ForeignKey("Policy")]
        [Column("policyID")]
        public int PolicyId { get; set; }

        [Column("assigned_date")]
        public DateTime? AssignedDate { get; set; }

        [StringLength(50)]
        [Column("compliance_status")]
        public string ComplianceStatus { get; set; } = "Pending"; // Pending, Compliant, Non-Compliant

        // Navigation properties
        public virtual Supplier Supplier { get; set; } = null!;
        public virtual Policy Policy { get; set; } = null!;
    }
}
