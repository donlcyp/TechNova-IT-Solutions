using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechNova_IT_Solutions.Models
{
    [Table("External_Policy_Imports")]
    public class ExternalPolicyImport
    {
        [Key]
        [Column("importID")]
        public int ImportId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("source_api")]
        public string SourceApi { get; set; } = "Federal Register";

        [StringLength(100)]
        [Column("document_number")]
        public string? DocumentNumber { get; set; }

        [StringLength(1000)]
        [Column("external_url")]
        public string? ExternalUrl { get; set; }

        [Required]
        [StringLength(255)]
        [Column("policy_title")]
        public string PolicyTitle { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [StringLength(100)]
        [Column("category")]
        public string? Category { get; set; }

        [Column("publication_date")]
        public DateTime? PublicationDate { get; set; }

        [Required]
        [StringLength(30)]
        [Column("review_status")]
        public string ReviewStatus { get; set; } = "PendingReview";

        [Column("imported_at")]
        public DateTime ImportedAt { get; set; } = DateTime.Now;

        [Column("reviewed_at")]
        public DateTime? ReviewedAt { get; set; }

        [StringLength(500)]
        [Column("review_notes")]
        public string? ReviewNotes { get; set; }

        [Column("imported_by_user_id")]
        public int? ImportedByUserId { get; set; }

        [Column("reviewed_by_user_id")]
        public int? ReviewedByUserId { get; set; }

        [Column("approved_policy_id")]
        public int? ApprovedPolicyId { get; set; }

        public virtual User? ImportedByUser { get; set; }
        public virtual User? ReviewedByUser { get; set; }
        public virtual Policy? ApprovedPolicy { get; set; }
    }
}
