namespace TechNova_IT_Solutions.Constants
{
    public static class RoleNames
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string SystemAdmin = "SystemAdmin";
        public const string BranchAdmin = "BranchAdmin";
        public const string ChiefComplianceManager = "ChiefComplianceManager";
        public const string ComplianceManager = "ComplianceManager";
        public const string Employee = "Employee";
        public const string Supplier = "Supplier";

        /// <summary>
        /// Returns true for any admin-level role (SystemAdmin or BranchAdmin).
        /// </summary>
        public static bool IsAdminRole(string? role) =>
            string.Equals(role, SystemAdmin, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, BranchAdmin, StringComparison.OrdinalIgnoreCase);
    }
}
