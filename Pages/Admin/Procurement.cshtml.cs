
namespace TechNova_IT_Solutions.Pages
{
    public class ProcurementModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAdminService _adminService;

        public ProcurementModel(ApplicationDbContext context, IAdminService adminService)
        {
            _context = context;
            _adminService = adminService;
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

            await _adminService.SyncLateProcurementsAsync();

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
                    CurrencyCode = p.CurrencyCode ?? "PHP",
                    OriginalAmount = p.OriginalAmount,
                    ExchangeRate = p.ExchangeRate,
                    ConvertedAmount = p.ConvertedAmount,
                    DeliveryBegin = p.SupplierCommitShipDate,
                    RevisedDeliveryDate = p.RevisedDeliveryDate,
                    DelayReason = p.DelayReason,
                    WorkflowStatus = string.IsNullOrWhiteSpace(p.Status) ? ProcurementStatuses.Draft : p.Status,
                    SupplierResponseDeadline = p.SupplierResponseDeadline
                })
                .ToListAsync();

            foreach (var record in ProcurementRecords)
            {
                // 7-day window starts on Delivery Begin date (Day 1), so Possible Arrival is +6 days.
                record.PossibleArrival = record.RevisedDeliveryDate ?? (record.DeliveryBegin?.AddDays(6));
                var today = DateTime.UtcNow.Date;

                record.ApprovalStatus = record.WorkflowStatus switch
                {
                    ProcurementStatuses.Draft => ProcurementStatuses.Draft,
                    ProcurementStatuses.Submitted => ProcurementStatuses.Submitted,
                    ProcurementStatuses.SupplierRejected => ProcurementStatuses.SupplierRejected,
                    _ => ProcurementStatuses.SupplierApproved
                };

                record.DeliveryStatus = record.WorkflowStatus switch
                {
                    ProcurementStatuses.Draft => "NotStarted",
                    ProcurementStatuses.Submitted => "PendingSupplier",
                    ProcurementStatuses.SupplierRejected => "Rejected",
                    ProcurementStatuses.Received => "Arrived",
                    ProcurementStatuses.Closed => ProcurementStatuses.Closed,
                    _ => record.PossibleArrival.HasValue
                        ? (today > record.PossibleArrival.Value.Date ? ProcurementStatuses.Late : "OnTheWay")
                        : "PendingSupplier"
                };

                record.CanEdit = record.WorkflowStatus == ProcurementStatuses.Draft || record.WorkflowStatus == ProcurementStatuses.Submitted;
                record.CanMarkDeliveryArrived = record.WorkflowStatus == ProcurementStatuses.SupplierApproved || record.WorkflowStatus == ProcurementStatuses.Late;
            }

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
                    UnitPrice = i.UnitPrice,
                    CurrencyCode = i.CurrencyCode,
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
        public string CurrencyCode { get; set; } = "PHP";
        public decimal? OriginalAmount { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? ConvertedAmount { get; set; }
        public DateTime? DeliveryBegin { get; set; }
        public DateTime? PossibleArrival { get; set; }
        public DateTime? RevisedDeliveryDate { get; set; }
        public string? DelayReason { get; set; }
        public string WorkflowStatus { get; set; } = string.Empty;
        public string ApprovalStatus { get; set; } = string.Empty;
        public string DeliveryStatus { get; set; } = string.Empty;
        public bool CanEdit { get; set; }
        public bool CanMarkDeliveryArrived { get; set; }
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
        public decimal UnitPrice { get; set; }
        public string CurrencyCode { get; set; } = "PHP";
        public int QuantityAvailable { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}





