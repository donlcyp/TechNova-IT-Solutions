namespace TechNova_IT_Solutions.Services.Interfaces
{
    public interface IBranchService
    {
        Task<List<BranchData>> GetAllBranchesAsync();
        Task<BranchData?> GetBranchByIdAsync(int branchId);
        Task<bool> CreateBranchAsync(BranchData branchData);
        Task<bool> UpdateBranchAsync(BranchData branchData);
        Task<bool> DeactivateBranchAsync(int branchId);
        Task<bool> ReactivateBranchAsync(int branchId);
        Task<bool> DeleteBranchAsync(int branchId);
        Task<List<UserData>> GetAvailableAdminsAsync();
        Task<bool> AssignAdminToBranchAsync(int branchId, int adminUserId);
        Task<bool> UnassignAdminFromBranchAsync(int branchId);
    }

    public class BranchData
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? Region { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? ManagerFirstName { get; set; }
        public string? ManagerLastName { get; set; }
        public string? ManagerEmail { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
        // Assigned admin info
        public int? AssignedAdminId { get; set; }
        public string? AssignedAdminName { get; set; }
    }
}
