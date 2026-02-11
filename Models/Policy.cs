using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechNova_IT_Solutions.Models
{
    [Table("Policies")]
    public class Policy
    {
        [Key]
        [Column("policyID")]
        public int PolicyId { get; set; }

        [Required]
        [StringLength(255)]
        [Column("policy_title")]
        public string PolicyTitle { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [StringLength(100)]
        [Column("category")]
        public string? Category { get; set; }

        [StringLength(500)]
        [Column("file_path")]
        public string? FilePath { get; set; }

        [ForeignKey("UploadedBy")]
        [Column("uploaded_by")]
        public int? UploadedBy { get; set; }

        [Column("date_uploaded")]
        public DateTime? DateUploaded { get; set; }

        // Navigation properties
        public virtual User? UploadedByUser { get; set; }
        public virtual ICollection<PolicyAssignment> PolicyAssignments { get; set; } = new List<PolicyAssignment>();
        public virtual ICollection<SupplierPolicy> SupplierPolicies { get; set; } = new List<SupplierPolicy>();
        public virtual ICollection<Procurement> Procurements { get; set; } = new List<Procurement>();
    }
}
