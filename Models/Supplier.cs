using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechNova_IT_Solutions.Models
{
    [Table("Suppliers")]
    public class Supplier
    {
        [Key]
        [Column("supplierID")]
        public int SupplierId { get; set; }

        [Required]
        [StringLength(255)]
        [Column("supplier_name")]
        public string SupplierName { get; set; } = string.Empty;

        [StringLength(100)]
        [Column("contact_person_fname")]
        public string? ContactPersonFirstName { get; set; }

        [StringLength(100)]
        [Column("contact_person_lname")]
        public string? ContactPersonLastName { get; set; }

        [StringLength(255)]
        [Column("email")]
        public string? Email { get; set; }

        [StringLength(20)]
        [Column("contact_person_number")]
        public string? ContactPersonNumber { get; set; }

        [StringLength(500)]
        [Column("address")]
        public string? Address { get; set; }

        [StringLength(20)]
        [Column("status")]
        public string Status { get; set; } = "Active"; // Active, Inactive

        // Navigation properties
        public virtual ICollection<SupplierPolicy> SupplierPolicies { get; set; } = new List<SupplierPolicy>();
        public virtual ICollection<Procurement> Procurements { get; set; } = new List<Procurement>();
        public virtual ICollection<SupplierItem> SupplierItems { get; set; } = new List<SupplierItem>();
    }
}
