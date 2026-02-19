using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechNova_IT_Solutions.Models
{
    [Table("Procurement_Status_History")]
    public class ProcurementStatusHistory
    {
        [Key]
        [Column("historyID")]
        public int HistoryId { get; set; }

        [Required]
        [ForeignKey("Procurement")]
        [Column("procurementID")]
        public int ProcurementId { get; set; }

        [Required]
        [StringLength(40)]
        [Column("from_status")]
        public string FromStatus { get; set; } = string.Empty;

        [Required]
        [StringLength(40)]
        [Column("to_status")]
        public string ToStatus { get; set; } = string.Empty;

        [StringLength(500)]
        [Column("reason")]
        public string? Reason { get; set; }

        [Column("changed_by_userID")]
        public int? ChangedByUserId { get; set; }

        [Column("changed_at")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public virtual Procurement Procurement { get; set; } = null!;
    }
}
