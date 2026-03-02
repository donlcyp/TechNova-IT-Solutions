using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Models;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Services
{
    public class BranchService : IBranchService
    {
        private readonly ApplicationDbContext _db;

        public BranchService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<BranchData>> GetAllBranchesAsync()
        {
            var branches = await _db.Branches
                .OrderBy(b => b.BranchName)
                .ToListAsync();

            // Find which admin (if any) is assigned to each branch
            var admins = await _db.Users
                .Where(u => u.Role == RoleNames.BranchAdmin && u.BranchId != null)
                .ToListAsync();

            return branches.Select(b =>
            {
                var admin = admins.FirstOrDefault(u => u.BranchId == b.BranchId);
                return MapToData(b, admin);
            }).ToList();
        }

        public async Task<BranchData?> GetBranchByIdAsync(int branchId)
        {
            var branch = await _db.Branches.FindAsync(branchId);
            if (branch == null) return null;

            var admin = await _db.Users
                .FirstOrDefaultAsync(u => u.Role == RoleNames.BranchAdmin && u.BranchId == branchId);

            return MapToData(branch, admin);
        }

        public async Task<bool> CreateBranchAsync(BranchData branchData)
        {
            var branch = new Branch
            {
                BranchName  = branchData.BranchName,
                Address     = branchData.Address,
                City        = branchData.City,
                Region      = branchData.Region,
                Phone            = branchData.Phone,
                Email            = branchData.Email,
                ManagerFirstName = branchData.ManagerFirstName,
                ManagerLastName  = branchData.ManagerLastName,
                ManagerEmail     = branchData.ManagerEmail,
                Status           = "Active",
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            };

            _db.Branches.Add(branch);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateBranchAsync(BranchData branchData)
        {
            var branch = await _db.Branches.FindAsync(branchData.BranchId);
            if (branch == null) return false;

            branch.BranchName  = branchData.BranchName;
            branch.Address     = branchData.Address;
            branch.City        = branchData.City;
            branch.Region      = branchData.Region;
            branch.Phone            = branchData.Phone;
            branch.Email            = branchData.Email;
            branch.ManagerFirstName = branchData.ManagerFirstName;
            branch.ManagerLastName  = branchData.ManagerLastName;
            branch.ManagerEmail     = branchData.ManagerEmail;
            branch.UpdatedAt        = DateTime.UtcNow;

            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeactivateBranchAsync(int branchId)
        {
            var branch = await _db.Branches.FindAsync(branchId);
            if (branch == null) return false;

            branch.Status    = "Inactive";
            branch.UpdatedAt = DateTime.UtcNow;

            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> ReactivateBranchAsync(int branchId)
        {
            var branch = await _db.Branches.FindAsync(branchId);
            if (branch == null) return false;

            branch.Status    = "Active";
            branch.UpdatedAt = DateTime.UtcNow;

            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteBranchAsync(int branchId)
        {
            // Unassign any admin attached to this branch first
            var assignedAdmins = await _db.Users
                .Where(u => u.Role == RoleNames.BranchAdmin && u.BranchId == branchId)
                .ToListAsync();
            foreach (var a in assignedAdmins) a.BranchId = null;

            var branch = await _db.Branches.FindAsync(branchId);
            if (branch == null) return false;

            _db.Branches.Remove(branch);
            return await _db.SaveChangesAsync() > 0;
        }

        // ── Admin assignment ────────────────────────────────────
        /// <summary>Returns all active Admin accounts not yet assigned to any branch.</summary>
        public async Task<List<UserData>> GetAvailableAdminsAsync()
        {
            return await _db.Users
                .Where(u => u.Role == RoleNames.BranchAdmin && u.Status == "Active" && u.BranchId == null)
                .OrderBy(u => u.FirstName)
                .Select(u => new UserData
                {
                    UserId    = u.UserId.ToString(),
                    FirstName = u.FirstName,
                    LastName  = u.LastName,
                    Email     = u.Email,
                    Role      = u.Role,
                    Status    = u.Status
                })
                .ToListAsync();
        }

        public async Task<bool> AssignAdminToBranchAsync(int branchId, int adminUserId)
        {
            // Verify branch exists
            var branch = await _db.Branches.FindAsync(branchId);
            if (branch == null) return false;

            // Verify admin exists and has Admin role
            var newAdmin = await _db.Users.FindAsync(adminUserId);
            if (newAdmin == null || newAdmin.Role != RoleNames.BranchAdmin) return false;

            // Unassign any previously assigned admin from this branch
            var previousAdmins = await _db.Users
                .Where(u => u.Role == RoleNames.BranchAdmin && u.BranchId == branchId && u.UserId != adminUserId)
                .ToListAsync();
            foreach (var prev in previousAdmins) prev.BranchId = null;

            // Assign the new admin
            newAdmin.BranchId = branchId;

            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> UnassignAdminFromBranchAsync(int branchId)
        {
            var admins = await _db.Users
                .Where(u => u.Role == RoleNames.BranchAdmin && u.BranchId == branchId)
                .ToListAsync();

            if (!admins.Any()) return false;

            foreach (var a in admins) a.BranchId = null;
            return await _db.SaveChangesAsync() > 0;
        }

        // ──────────────────────────────────────────────
        private static BranchData MapToData(Branch b, User? assignedAdmin = null) => new()
        {
            BranchId          = b.BranchId,
            BranchName        = b.BranchName,
            Address           = b.Address,
            City              = b.City,
            Region            = b.Region,
            Phone              = b.Phone,
            Email              = b.Email,
            ManagerFirstName   = b.ManagerFirstName,
            ManagerLastName    = b.ManagerLastName,
            ManagerEmail       = b.ManagerEmail,
            Status             = b.Status,
            CreatedAt         = b.CreatedAt,
            AssignedAdminId   = assignedAdmin?.UserId,
            AssignedAdminName = assignedAdmin != null ? $"{assignedAdmin.FirstName} {assignedAdmin.LastName}" : null
        };
    }
}

