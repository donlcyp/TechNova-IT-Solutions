using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechNova_IT_Solutions.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("userID")]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("firstname")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Column("lastname")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Column("role")]
        public string Role { get; set; } = string.Empty; // SuperAdmin, Admin, ChiefComplianceManager, ComplianceManager, Employee, Supplier

        [StringLength(20)]
        [Column("status")]
        public string Status { get; set; } = "Active"; // Active, Inactive

        [Column("mustChangePassword")]
        public bool MustChangePassword { get; set; } = false;

        [Column("branchId")]
        public int? BranchId { get; set; }

        // Navigation properties
        public virtual Branch? Branch { get; set; }
        public virtual ICollection<Policy> UploadedPolicies { get; set; } = new List<Policy>();
        public virtual ICollection<PolicyAssignment> PolicyAssignments { get; set; } = new List<PolicyAssignment>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
