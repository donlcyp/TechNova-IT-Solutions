using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechNova_IT_Solutions.Models
{
    [Table("Supplier_Items")]
    public class SupplierItem
    {
        [Key]
        [Column("supplier_itemID")]
        public int SupplierItemId { get; set; }

        [Required]
        [ForeignKey("Supplier")]
        [Column("supplierID")]
        public int SupplierId { get; set; }

        [Required]
        [StringLength(255)]
        [Column("item_name")]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(100)]
        [Column("category")]
        public string? Category { get; set; }

        [Column("quantity_available")]
        public int QuantityAvailable { get; set; }

        [Required]
        [StringLength(30)]
        [Column("status")]
        public string Status { get; set; } = "Available"; // Available / OutOfStock

        [Column("last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public virtual Supplier Supplier { get; set; } = null!;
    }
}
