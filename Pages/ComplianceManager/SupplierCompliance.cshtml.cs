
namespace TechNova_IT_Solutions.Pages.ComplianceManager
{
    public class SupplierComplianceModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SupplierComplianceModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalSuppliersAssigned { get; set; }
        public int SuppliersCompliant { get; set; }
        public int SuppliersNotCompliant { get; set; }
        public int RecentlyAssigned { get; set; }

        public List<SupplierRecord> SupplierRecords { get; set; } = new();
        public List<SupplierDetail> SupplierDetails { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only ComplianceManager and Admin can access
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.ComplianceManager && userRole != RoleNames.Admin && userRole != RoleNames.SuperAdmin)
            {
                // Redirect to appropriate dashboard based on role
                if (userRole == RoleNames.Employee)
                {
                    return RedirectToPage("/Employee/Dashboard");
                }
                return RedirectToPage("/Account/Login");
            }

            // Load supplier policy assignments from database
            var supplierPolicies = await _context.SupplierPolicies
                .Include(sp => sp.Supplier)
                .Include(sp => sp.Policy)
                .ToListAsync();

            SupplierRecords = supplierPolicies
                .Select(sp => new SupplierRecord
                {
                    SupplierId = sp.Supplier.SupplierId,
                    SupplierName = sp.Supplier.SupplierName,
                    ContactPerson = $"{sp.Supplier.ContactPersonFirstName} {sp.Supplier.ContactPersonLastName}".Trim(),
                    AssignedPolicy = sp.Policy.PolicyTitle,
                    DateAssigned = sp.AssignedDate?.ToString("yyyy-MM-dd") ?? "N/A",
                    ComplianceStatus = sp.ComplianceStatus == "Compliant" ? "Compliant" : "Not Compliant"
                })
                .OrderBy(s => s.SupplierName)
                .ToList();

            // Build supplier details for modal
            var suppliers = await _context.Suppliers
                .Include(s => s.SupplierPolicies)
                .ThenInclude(sp => sp.Policy)
                .ToListAsync();

            SupplierDetails = suppliers
                .Select(s => new SupplierDetail
                {
                    SupplierId = s.SupplierId,
                    SupplierName = s.SupplierName,
                    ContactPerson = $"{s.ContactPersonFirstName} {s.ContactPersonLastName}".Trim(),
                    Email = s.Email ?? "N/A",
                    Phone = s.ContactPersonNumber ?? "N/A",
                    Country = s.Address ?? "N/A",
                    Policies = s.SupplierPolicies.Select(sp => new SupplierPolicyAssignment
                    {
                        PolicyName = sp.Policy.PolicyTitle,
                        DateAssigned = sp.AssignedDate?.ToString("yyyy-MM-dd") ?? "N/A",
                        Status = sp.ComplianceStatus == "Compliant" ? "Compliant" : "Not Compliant",
                        AcknowledgedDate = sp.ComplianceStatus == "Compliant" ? sp.AssignedDate?.AddDays(5).ToString("yyyy-MM-dd") ?? "N/A" : "Pending"
                    }).ToList()
                })
                .ToList();

            // Calculate summary statistics
            TotalSuppliersAssigned = SupplierRecords.Select(s => s.SupplierName).Distinct().Count();
            SuppliersCompliant = SupplierRecords.Where(s => s.ComplianceStatus == "Compliant").Select(s => s.SupplierName).Distinct().Count();
            SuppliersNotCompliant = SupplierRecords.Where(s => s.ComplianceStatus == "Not Compliant").Select(s => s.SupplierName).Distinct().Count();
            
            // Count policies assigned in last 7 days
            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            RecentlyAssigned = supplierPolicies
                .Where(sp => sp.AssignedDate.HasValue && sp.AssignedDate.Value >= sevenDaysAgo)
                .Count();

            return Page();
        }
    }

    public class SupplierRecord
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public string DateAssigned { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = string.Empty;
    }

    public class SupplierDetail
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public List<SupplierPolicyAssignment> Policies { get; set; } = new();
    }

    public class SupplierPolicyAssignment
    {
        public string PolicyName { get; set; } = string.Empty;
        public string DateAssigned { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AcknowledgedDate { get; set; } = string.Empty;
    }
}





