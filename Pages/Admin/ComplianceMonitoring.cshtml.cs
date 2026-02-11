using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Data;

namespace TechNova_IT_Solutions.Pages
{
    public class ComplianceMonitoringModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ComplianceMonitoringModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        // Summary Cards Data
        public int TotalPoliciesAssigned { get; set; }
        public int EmployeesCompliant { get; set; }
        public int EmployeesNotCompliant { get; set; }
        public int SuppliersCompliant { get; set; }

        // Employee Compliance Data
        public List<EmployeeComplianceItem> EmployeeCompliance { get; set; } = new();

        // Supplier Compliance Data
        public List<SupplierComplianceItem> SupplierCompliance { get; set; } = new();

        public async Task<IActionResult> OnGet()
        {
            // Check authentication
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check user role - only Admin can access
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                if (userRole == "Employee") return RedirectToPage("/Employee/Dashboard");
                if (userRole == "ComplianceManager") return RedirectToPage("/ComplianceManager/ComplianceDashboard");
                return RedirectToPage("/Account/Login");
            }

            UserEmail = HttpContext.Session.GetString("UserEmail") ?? "admin@technova.com";
            UserName = HttpContext.Session.GetString("UserName") ?? "Administrator";

            // Calculate summary statistics
            TotalPoliciesAssigned = await _context.PolicyAssignments.CountAsync();
            
            EmployeesCompliant = await _context.ComplianceStatuses
                .Where(cs => cs.Status == "Acknowledged")
                .CountAsync();
            
            EmployeesNotCompliant = await _context.ComplianceStatuses
                .Where(cs => cs.Status == "Pending" || cs.Status == "Overdue")
                .CountAsync();
            
            SuppliersCompliant = await _context.SupplierPolicies
                .Where(sp => sp.ComplianceStatus == "Compliant")
                .Select(sp => sp.SupplierId)
                .Distinct()
                .CountAsync();

            // Fetch employee compliance data
            var employeeData = await _context.PolicyAssignments
                .Include(pa => pa.User)
                .Include(pa => pa.Policy)
                .Include(pa => pa.ComplianceStatus)
                .Where(pa => pa.User != null && pa.User.Role == "Employee")
                .ToListAsync();

            EmployeeCompliance = employeeData
                .Select(pa => new EmployeeComplianceItem
                {
                    Name = pa.User != null ? $"{pa.User.FirstName} {pa.User.LastName}" : "Unknown",
                    AssignedPolicy = pa.Policy != null ? pa.Policy.PolicyTitle : "Unknown",
                    DateAssigned = pa.AssignedDate ?? DateTime.Now,
                    ComplianceStatus = pa.ComplianceStatus != null 
                        ? (pa.ComplianceStatus.Status == "Acknowledged" ? "Compliant" : "Not Compliant")
                        : "Not Compliant",
                    AcknowledgedDate = pa.ComplianceStatus != null ? pa.ComplianceStatus.AcknowledgedDate : null
                })
                .OrderBy(x => x.Name)
                .ToList();

            // Fetch supplier compliance data
            SupplierCompliance = await _context.SupplierPolicies
                .Include(sp => sp.Supplier)
                .Include(sp => sp.Policy)
                .OrderBy(sp => sp.Supplier.SupplierName)
                .Select(sp => new SupplierComplianceItem
                {
                    Name = sp.Supplier != null ? sp.Supplier.SupplierName : "Unknown",
                    AssignedPolicy = sp.Policy != null ? sp.Policy.PolicyTitle : "Unknown",
                    ComplianceStatus = sp.ComplianceStatus ?? "Pending",
                    DateAssigned = sp.AssignedDate ?? DateTime.Now
                })
                .ToListAsync();

            return Page();
        }
    }

    public class EmployeeComplianceItem
    {
        public string Name { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public DateTime DateAssigned { get; set; }
        public string ComplianceStatus { get; set; } = string.Empty;
        public DateTime? AcknowledgedDate { get; set; }
    }

    public class SupplierComplianceItem
    {
        public string Name { get; set; } = string.Empty;
        public string AssignedPolicy { get; set; } = string.Empty;
        public string ComplianceStatus { get; set; } = string.Empty;
        public DateTime DateAssigned { get; set; }
    }
}
