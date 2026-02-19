
namespace TechNova_IT_Solutions.Pages
{
    public class ProcurementModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProcurementModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        // Summary Data
        public int TotalProcurements { get; set; }
        public int PendingApprovals { get; set; }
        public int ProcurementsThisMonth { get; set; }

        // Procurement Records
        public List<ProcurementRecord> ProcurementRecords { get; set; } = new();

        // Reference Data
        public List<SupplierReference> Suppliers { get; set; } = new();
        public List<PolicyReference> Policies { get; set; } = new();
        public List<SupplierItemReference> SupplierItems { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            // Check authentication
            var userIdString = HttpContext.Session.GetString(SessionKeys.UserId);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only Admin can access
            var userRole = HttpContext.Session.GetString(SessionKeys.UserRole);
            if (userRole != RoleNames.Admin && userRole != RoleNames.SuperAdmin)
            {
                if (userRole == RoleNames.Employee) return RedirectToPage("/Employee/Dashboard");
                if (userRole == RoleNames.ComplianceManager) return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString(SessionKeys.UserEmail) ?? "admin@technova.com";
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Administrator";

            // Calculate summary statistics
            TotalProcurements = await _context.Procurements.CountAsync();
            
            PendingApprovals = await _context.Procurements
                .Where(p => p.Status == ProcurementStatuses.Submitted)
                .CountAsync();
            
            // Count procurements this month
            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            ProcurementsThisMonth = await _context.Procurements
                .Where(p => p.PurchaseDate >= firstDayOfMonth)
                .CountAsync();

            // Fetch procurement records from database
            ProcurementRecords = await _context.Procurements
                .Include(p => p.Supplier)
                .Include(p => p.RelatedPolicy)
                .OrderByDescending(p => p.PurchaseDate)
                .Select(p => new ProcurementRecord
                {
                    ProcurementId = "PROC-" + p.ProcurementId.ToString("D3"),
                    ItemName = p.ItemName ?? string.Empty,
                    Category = p.Category ?? string.Empty,
                    Quantity = p.Quantity ?? 0,
                    SupplierName = p.Supplier != null ? p.Supplier.SupplierName : "N/A",
                    LinkedPolicy = p.RelatedPolicy != null ? p.RelatedPolicy.PolicyTitle : "General",
                    PurchaseDate = p.PurchaseDate ?? DateTime.Now,
                    DeliveryBegin = p.SupplierCommitShipDate,
                    // Inclusive 7-day window: Delivery Begin is Day 1, so Possible Arrival is +6 days.
                    PossibleArrival = p.SupplierCommitShipDate.HasValue ? p.SupplierCommitShipDate.Value.AddDays(6) : null,
                    ApprovalStatus = string.IsNullOrWhiteSpace(p.Status) ? ProcurementStatuses.Draft : p.Status,
                    SupplierResponseDeadline = p.SupplierResponseDeadline
                })
                .ToListAsync();

            // Fetch suppliers for dropdown
            Suppliers = await _context.Suppliers
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.SupplierName)
                .Select(s => new SupplierReference
                {
                    Id = s.SupplierId,
                    Name = s.SupplierName ?? string.Empty
                })
                .ToListAsync();

            // Fetch policies for dropdown
            Policies = await _context.Policies
                .OrderBy(p => p.PolicyTitle)
                .Select(p => new PolicyReference
                {
                    Id = p.PolicyId,
                    Title = p.PolicyTitle ?? string.Empty
                })
                .ToListAsync();

            SupplierItems = await _context.SupplierItems
                .OrderBy(i => i.ItemName)
                .Select(i => new SupplierItemReference
                {
                    Id = i.SupplierItemId,
                    SupplierId = i.SupplierId,
                    Name = i.ItemName,
                    Category = i.Category ?? string.Empty,
                    QuantityAvailable = i.QuantityAvailable,
                    Status = i.Status
                })
                .ToListAsync();

            return Page();
        }
    }

    public class ProcurementRecord
    {
        public string ProcurementId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string LinkedPolicy { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public DateTime? DeliveryBegin { get; set; }
        public DateTime? PossibleArrival { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public DateTime? SupplierResponseDeadline { get; set; }
    }

    public class SupplierReference
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PolicyReference
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class SupplierItemReference
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int QuantityAvailable { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}





