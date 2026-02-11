using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechNova_IT_Solutions.Models
{
    [Table("Policy_Assignments")]
    public class PolicyAssignment
    {
        [Key]
        [Column("assignmentID")]
        public int AssignmentId { get; set; }

        [Required]
        [ForeignKey("Policy")]
        [Column("policyID")]
        public int PolicyId { get; set; }

        [Required]
        [ForeignKey("User")]
        [Column("userID")]
        public int UserId { get; set; }

        [Column("assigned_date")]
        public DateTime? AssignedDate { get; set; }

        // Navigation properties
        public virtual Policy Policy { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual ComplianceStatus? ComplianceStatus { get; set; }
    }
}
