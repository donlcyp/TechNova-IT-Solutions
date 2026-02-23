using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Models;

namespace TechNova_IT_Solutions.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for all tables
        public DbSet<User> Users { get; set; }
        public DbSet<Policy> Policies { get; set; }
        public DbSet<PolicyAssignment> PolicyAssignments { get; set; }
        public DbSet<ComplianceStatus> ComplianceStatuses { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierPolicy> SupplierPolicies { get; set; }
        public DbSet<SupplierItem> SupplierItems { get; set; }
        public DbSet<Procurement> Procurements { get; set; }
        public DbSet<ProcurementStatusHistory> ProcurementStatusHistory { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ExternalPolicyImport> ExternalPolicyImports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            
            // User relationships
            modelBuilder.Entity<User>()
                .HasMany(u => u.UploadedPolicies)
                .WithOne(p => p.UploadedByUser)
                .HasForeignKey(p => p.UploadedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasMany(u => u.PolicyAssignments)
                .WithOne(pa => pa.User)
                .HasForeignKey(pa => pa.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.AuditLogs)
                .WithOne(al => al.User)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ExternalPolicyImport>()
                .HasOne(e => e.ImportedByUser)
                .WithMany()
                .HasForeignKey(e => e.ImportedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExternalPolicyImport>()
                .HasOne(e => e.ReviewedByUser)
                .WithMany()
                .HasForeignKey(e => e.ReviewedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExternalPolicyImport>()
                .HasOne(e => e.ApprovedPolicy)
                .WithMany()
                .HasForeignKey(e => e.ApprovedPolicyId)
                .OnDelete(DeleteBehavior.SetNull);

            // Policy relationships
            modelBuilder.Entity<Policy>()
                .HasMany(p => p.PolicyAssignments)
                .WithOne(pa => pa.Policy)
                .HasForeignKey(pa => pa.PolicyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Policy>()
                .HasMany(p => p.SupplierPolicies)
                .WithOne(sp => sp.Policy)
                .HasForeignKey(sp => sp.PolicyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Policy>()
                .HasMany(p => p.Procurements)
                .WithOne(pr => pr.RelatedPolicy)
                .HasForeignKey(pr => pr.RelatedPolicyId)
                .OnDelete(DeleteBehavior.SetNull);

            // PolicyAssignment - ComplianceStatus (One-to-One)
            modelBuilder.Entity<PolicyAssignment>()
                .HasOne(pa => pa.ComplianceStatus)
                .WithOne(cs => cs.PolicyAssignment)
                .HasForeignKey<ComplianceStatus>(cs => cs.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Supplier relationships
            modelBuilder.Entity<Supplier>()
                .HasMany(s => s.SupplierPolicies)
                .WithOne(sp => sp.Supplier)
                .HasForeignKey(sp => sp.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Supplier>()
                .HasMany(s => s.Procurements)
                .WithOne(p => p.Supplier)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Supplier>()
                .HasMany(s => s.SupplierItems)
                .WithOne(si => si.Supplier)
                .HasForeignKey(si => si.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Procurement>()
                .HasMany(p => p.StatusHistory)
                .WithOne(h => h.Procurement)
                .HasForeignKey(h => h.ProcurementId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add indexes for better query performance
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.Email)
                .IsUnique()
                .HasFilter("[email] IS NOT NULL");

            modelBuilder.Entity<PolicyAssignment>()
                .HasIndex(pa => new { pa.PolicyId, pa.UserId })
                .IsUnique();

            modelBuilder.Entity<SupplierPolicy>()
                .HasIndex(sp => new { sp.SupplierId, sp.PolicyId })
                .IsUnique();

            modelBuilder.Entity<SupplierItem>()
                .HasIndex(si => new { si.SupplierId, si.ItemName })
                .IsUnique();

            modelBuilder.Entity<ExternalPolicyImport>()
                .HasIndex(e => new { e.SourceApi, e.DocumentNumber });

            modelBuilder.Entity<ExternalPolicyImport>()
                .HasIndex(e => e.ReviewStatus);

            // ========== SEED DATA ==========
            
            // Seed Admin User Only
            // Password: Admin@123 (hashed with BCrypt)
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    FirstName = "System",
                    LastName = "Administrator",
                    Email = "admin@technova.com",
                    Password = "$2a$11$kzRScf92mLmEjZRJTh3BRub/Li1F07G3TA5vBdZXYQ7tM1C6Lm65i",
                    Role = "Admin",
                    Status = "Active"
                }
            );
        }
    }
}
