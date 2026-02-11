using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechNova_IT_Solutions.Models
{
    [Table("Audit_Logs")]
    public class AuditLog
    {
        [Key]
        [Column("logID")]
        public int LogId { get; set; }

        [ForeignKey("User")]
        [Column("userID")]
        public int? UserId { get; set; }

        [StringLength(255)]
        [Column("action")]
        public string? Action { get; set; }

        [StringLength(100)]
        [Column("module")]
        public string? Module { get; set; }

        [Column("logdate")]
        public DateTime LogDate { get; set; } = DateTime.Now;

        // Navigation property
        public virtual User? User { get; set; }
    }
}
