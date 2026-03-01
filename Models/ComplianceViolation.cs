using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechNova_IT_Solutions.Models
{
    [Table("compliance_violations")]
    public class ComplianceViolation
    {
        [Key]
        [Column("violationID")]
        public int ViolationId { get; set; }

        /// <summary>Employee policy assignment FK (nullable — set when violation is employee-related).</summary>
        [Column("assignment_id")]
        public int? PolicyAssignmentId { get; set; }

        /// <summary>Supplier-policy FK (nullable — set when violation is supplier-related).</summary>
        [Column("supplier_policy_id")]
        public int? SupplierPolicyId { get; set; }

        /// <summary>"Employee" or "Supplier"</summary>
        [Required]
        [Column("violation_type")]
        [StringLength(20)]
        public string ViolationType { get; set; } = "Employee";

        /// <summary>Open | UnderReview | Escalated | Resolved</summary>
        [Required]
        [Column("status")]
        [StringLength(30)]
        public string Status { get; set; } = "Open";

        /// <summary>Short description of the violation.</summary>
        [Column("description")]
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>Internal notes by the compliance manager.</summary>
        [Column("notes")]
        public string? Notes { get; set; }

        /// <summary>Resolution summary when the violation is closed.</summary>
        [Column("resolution")]
        [StringLength(500)]
        public string? Resolution { get; set; }

        [Column("raised_date")]
        public DateTime RaisedDate { get; set; } = DateTime.UtcNow;

        [Column("resolved_date")]
        public DateTime? ResolvedDate { get; set; }

        /// <summary>The user (CM / Admin) who raised the violation.</summary>
        [Column("raised_by_user_id")]
        public int? RaisedByUserId { get; set; }

        // ── Navigation ──────────────────────────────────────────
        [ForeignKey("PolicyAssignmentId")]
        public PolicyAssignment? PolicyAssignment { get; set; }

        [ForeignKey("SupplierPolicyId")]
        public SupplierPolicy? SupplierPolicy { get; set; }

        [ForeignKey("RaisedByUserId")]
        public User? RaisedByUser { get; set; }
    }
}
