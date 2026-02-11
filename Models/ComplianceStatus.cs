using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechNova_IT_Solutions.Models
{
    [Table("Compliance_Status")]
    public class ComplianceStatus
    {
        [Key]
        [Column("complianceID")]
        public int ComplianceId { get; set; }

        [Required]
        [ForeignKey("PolicyAssignment")]
        [Column("assignmentID")]
        public int AssignmentId { get; set; }

        [StringLength(50)]
        [Column("status")]
        public string Status { get; set; } = "Pending"; // Pending, Acknowledged, Overdue

        [Column("acknowledged_date")]
        public DateTime? AcknowledgedDate { get; set; }

        // Navigation property
        public virtual PolicyAssignment PolicyAssignment { get; set; } = null!;
    }
}
