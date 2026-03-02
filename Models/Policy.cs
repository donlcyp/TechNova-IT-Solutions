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

        [Column("is_archived")]
        public bool IsArchived { get; set; } = false;

        [Column("archived_date")]
        public DateTime? ArchivedDate { get; set; }

        /// <summary>
        /// null = company-wide policy (created by Super Admin / CCM).
        /// non-null = branch-specific policy (created by Branch Admin / Compliance Manager).
        /// </summary>
        [Column("branch_id")]
        public int? BranchId { get; set; }

        // ── Review Workflow Fields ─────────────────────────────

        /// <summary>
        /// PendingReview  – newly created by branch, awaiting CCM approval.
        /// PendingUpdate  – branch requested an update, awaiting CCM approval.
        /// Approved       – CCM has approved this policy for use.
        /// Rejected       – CCM has rejected this policy.
        /// Archived       – branch archived the policy (CCM notified).
        /// Policies created by SuperAdmin / CCM are auto-approved.
        /// </summary>
        [StringLength(30)]
        [Column("review_status")]
        public string ReviewStatus { get; set; } = "Approved";

        [Column("reviewed_by")]
        public int? ReviewedBy { get; set; }

        [Column("reviewed_at")]
        public DateTime? ReviewedAt { get; set; }

        [Column("review_notes")]
        public string? ReviewNotes { get; set; }

        // ── Pending-update staging fields ──────────────────────
        // When a branch CM updates a policy, the new values are stored here
        // until the CCM approves. The live policy keeps its current values.

        [StringLength(255)]
        [Column("pending_title")]
        public string? PendingTitle { get; set; }

        [Column("pending_description")]
        public string? PendingDescription { get; set; }

        [StringLength(100)]
        [Column("pending_category")]
        public string? PendingCategory { get; set; }

        [StringLength(500)]
        [Column("pending_file_path")]
        public string? PendingFilePath { get; set; }

        [Column("pending_updated_by")]
        public int? PendingUpdatedBy { get; set; }

        [Column("pending_updated_at")]
        public DateTime? PendingUpdatedAt { get; set; }

        // Navigation properties
        public virtual Branch? Branch { get; set; }
        public virtual User? UploadedByUser { get; set; }
        public virtual User? ReviewedByUser { get; set; }
        public virtual ICollection<PolicyAssignment> PolicyAssignments { get; set; } = new List<PolicyAssignment>();
        public virtual ICollection<SupplierPolicy> SupplierPolicies { get; set; } = new List<SupplierPolicy>();
        public virtual ICollection<Procurement> Procurements { get; set; } = new List<Procurement>();
    }
}
