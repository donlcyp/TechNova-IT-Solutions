using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechNova_IT_Solutions.Models
{
    [Table("PasswordResetTokens")]
    public class PasswordResetToken
    {
        [Key]
        [Column("tokenId")]
        public int TokenId { get; set; }

        [Required]
        [Column("userId")]
        public int UserId { get; set; }

        [Required]
        [StringLength(500)]
        [Column("token")]
        public string Token { get; set; } = string.Empty;

        [Required]
        [Column("expiryDate")]
        public DateTime ExpiryDate { get; set; }

        [Column("createdDate")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Column("usedDate")]
        public DateTime? UsedDate { get; set; }

        [Column("isUsed")]
        public bool IsUsed { get; set; } = false;

        // Navigation property
        public virtual User? User { get; set; }
    }
}
