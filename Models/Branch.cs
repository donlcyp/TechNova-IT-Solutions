using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechNova_IT_Solutions.Models
{
    [Table("Branches")]
    public class Branch
    {
        [Key]
        [Column("branchId")]
        public int BranchId { get; set; }

        [Required]
        [StringLength(150)]
        [Column("branchName")]
        public string BranchName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Column("city")]
        public string City { get; set; } = string.Empty;

        [StringLength(100)]
        [Column("region")]
        public string? Region { get; set; }

        [StringLength(20)]
        [Column("phone")]
        public string? Phone { get; set; }

        [StringLength(255)]
        [Column("email")]
        public string? Email { get; set; }

        [StringLength(100)]
        [Column("managerFirstName")]
        public string? ManagerFirstName { get; set; }

        [StringLength(100)]
        [Column("managerLastName")]
        public string? ManagerLastName { get; set; }

        [StringLength(255)]
        [Column("managerEmail")]
        public string? ManagerEmail { get; set; }

        [Required]
        [StringLength(20)]
        [Column("status")]
        public string Status { get; set; } = "Active"; // Active, Inactive

        [Column("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();
        public virtual ICollection<Procurement> Procurements { get; set; } = new List<Procurement>();
    }
}
