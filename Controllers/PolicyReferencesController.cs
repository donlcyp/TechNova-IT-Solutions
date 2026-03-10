using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechNova_IT_Solutions.Constants;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Infrastructure;
using TechNova_IT_Solutions.Models;
using TechNova_IT_Solutions.Services;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    [ApiController]
    [Route("api/policy-references")]
    public class PolicyReferencesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPolicyReferenceApiService _policyReferenceApiService;
        private readonly IAdminService _adminService;

        public PolicyReferencesController(
            ApplicationDbContext context,
            IPolicyReferenceApiService policyReferenceApiService,
            IAdminService adminService)
        {
            _context = context;
            _policyReferenceApiService = policyReferenceApiService;
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPolicyReferences(
            [FromQuery] string? category,
            [FromQuery] string? q,
            [FromQuery] string? source,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            // Read-only search: Employee + ComplianceManager + SuperAdmin (Branch Admin excluded)
            var unauthorized = RoleAccess.RequireRoleOrUnauthorized(
                this,
                RoleNames.Employee,
                RoleNames.SuperAdmin,
                RoleNames.ChiefComplianceManager,
                RoleNames.ComplianceManager);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (!string.IsNullOrWhiteSpace(source) &&
                !source.Equals("Federal Register", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unsupported source. Only 'Federal Register' is currently available."
                });
            }

            var selectedCategory = string.IsNullOrWhiteSpace(category) ? "all" : category.Trim();
            var results = await _policyReferenceApiService.SearchPoliciesByCategoryAsync(selectedCategory);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var query = q.Trim();
                results = results
                    .Where(r =>
                        (!string.IsNullOrWhiteSpace(r.PolicyTitle) && r.PolicyTitle.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(r.Description) && r.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(r.Category) && r.Category.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(r.DocumentNumber) && r.DocumentNumber.Contains(query, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(source))
            {
                results = results
                    .Where(r => !string.IsNullOrWhiteSpace(r.SourceApi) &&
                                r.SourceApi.Equals(source.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (fromDate.HasValue)
            {
                var start = fromDate.Value.Date;
                results = results
                    .Where(r => r.DateUploaded.HasValue && r.DateUploaded.Value.Date >= start)
                    .ToList();
            }

            if (toDate.HasValue)
            {
                var end = toDate.Value.Date;
                results = results
                    .Where(r => r.DateUploaded.HasValue && r.DateUploaded.Value.Date <= end)
                    .ToList();
            }

            results = results
                .OrderByDescending(r => r.DateUploaded ?? DateTime.MinValue)
                .ThenBy(r => r.PolicyTitle)
                .ToList();

            return Ok(new
            {
                success = true,
                count = results.Count,
                items = results
            });
        }

        [HttpGet("staging")]
        public async Task<IActionResult> GetStagingQueue([FromQuery] string? status)
        {
            // Staging queue: Chief Compliance Manager + Super Admin only (branch CM excluded)
            var unauthorized = RoleAccess.RequireRoleOrUnauthorized(
                this,
                RoleNames.ChiefComplianceManager,
                RoleNames.SuperAdmin);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            var query = _context.ExternalPolicyImports
                .AsNoTracking()
                .Include(i => i.ImportedByUser)
                .Include(i => i.ReviewedByUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(i => i.ReviewStatus == status);
            }

            var items = await query
                .OrderByDescending(i => i.ImportedAt)
                .Select(i => new
                {
                    i.ImportId,
                    i.PolicyTitle,
                    i.Description,
                    i.Category,
                    i.SourceApi,
                    i.DocumentNumber,
                    i.ExternalUrl,
                    i.PublicationDate,
                    i.ReviewStatus,
                    i.ImportedAt,
                    i.ReviewedAt,
                    i.ReviewNotes,
                    i.ApprovedPolicyId,
                    importedBy = i.ImportedByUser != null
                        ? (i.ImportedByUser.FirstName + " " + i.ImportedByUser.LastName).Trim()
                        : "System",
                    reviewedBy = i.ReviewedByUser != null
                        ? (i.ReviewedByUser.FirstName + " " + i.ReviewedByUser.LastName).Trim()
                        : null
                })
                .ToListAsync();

            return Ok(new { success = true, count = items.Count, items });
        }

        [HttpPost("staging/import")]
        public async Task<IActionResult> StagePolicyReference([FromBody] StagePolicyReferenceRequest? request)
        {
            // Import to staging: Chief Compliance Manager + Super Admin only (branch CM excluded)
            var unauthorized = RoleAccess.RequireRoleOrUnauthorized(
                this,
                RoleNames.ChiefComplianceManager,
                RoleNames.SuperAdmin);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (request == null)
            {
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { success = false, message = "Not logged in" });
            }

            var externalData = await ResolveExternalDataAsync(
                request.DocumentNumber,
                request.ExternalUrl,
                request.PolicyTitle,
                request.Description,
                request.Category,
                request.SourceApi);

            if (!externalData.Success || externalData.Data == null)
            {
                return BadRequest(new { success = false, message = externalData.ErrorMessage ?? "Unable to stage policy reference." });
            }

            var stagedData = externalData.Data;

            var existingImport = await FindExistingImportAsync(stagedData.SourceApi, stagedData.DocumentNumber, stagedData.ExternalUrl, stagedData.PolicyTitle);
            if (existingImport != null)
            {
                return Ok(new
                {
                    success = true,
                    alreadyExists = true,
                    importId = existingImport.ImportId,
                    reviewStatus = existingImport.ReviewStatus,
                    message = "Reference already exists in review queue."
                });
            }

            var import = new ExternalPolicyImport
            {
                SourceApi = (stagedData.SourceApi ?? "Federal Register").Trim(),
                DocumentNumber = string.IsNullOrWhiteSpace(stagedData.DocumentNumber) ? null : stagedData.DocumentNumber.Trim(),
                ExternalUrl = string.IsNullOrWhiteSpace(stagedData.ExternalUrl) ? null : stagedData.ExternalUrl.Trim(),
                PolicyTitle = stagedData.PolicyTitle.Trim(),
                Description = string.IsNullOrWhiteSpace(stagedData.Description) ? null : stagedData.Description.Trim(),
                Category = string.IsNullOrWhiteSpace(stagedData.Category) ? "General" : stagedData.Category.Trim(),
                PublicationDate = stagedData.DateUploaded,
                ReviewStatus = "PendingReview",
                ImportedAt = DateTime.Now,
                ImportedByUserId = currentUserId
            };

            _context.ExternalPolicyImports.Add(import);
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = currentUserId,
                Module = "Policy",
                Action = TruncateAction($"Queued external policy for review: {import.PolicyTitle}"),
                LogDate = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                importId = import.ImportId,
                reviewStatus = import.ReviewStatus,
                message = "External reference queued for compliance review."
            });
        }

        [HttpPost("staging/{importId:int}/approve")]
        public async Task<IActionResult> ApproveStagedPolicy(int importId, [FromBody] ApproveStagedPolicyRequest? request)
        {
            // Approve: Chief Compliance Manager + Super Admin only (branch CM excluded)
            var unauthorized = RoleAccess.RequireRoleOrUnauthorized(
                this,
                RoleNames.ChiefComplianceManager,
                RoleNames.SuperAdmin);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (!TryGetCurrentUserId(out var reviewerUserId))
            {
                return Unauthorized(new { success = false, message = "Not logged in" });
            }

            request ??= new ApproveStagedPolicyRequest();

            var staged = await _context.ExternalPolicyImports.FirstOrDefaultAsync(i => i.ImportId == importId);
            if (staged == null)
            {
                return NotFound(new { success = false, message = "Staged policy not found." });
            }

            Policy? policy = null;

            if (staged.ApprovedPolicyId.HasValue)
            {
                policy = await _context.Policies.FirstOrDefaultAsync(p => p.PolicyId == staged.ApprovedPolicyId.Value);
            }

            if (policy == null)
            {
                policy = await _context.Policies
                    .FirstOrDefaultAsync(p => !p.IsArchived && p.PolicyTitle == staged.PolicyTitle);

                if (policy == null || request.ForceCreate)
                {
                    policy = new Policy
                    {
                        PolicyTitle = staged.PolicyTitle,
                        Description = staged.Description,
                        Category = FirstNonEmpty(request.Category, staged.Category, "General"),
                        DateUploaded = staged.PublicationDate ?? DateTime.Now,
                        UploadedBy = reviewerUserId
                    };
                    _context.Policies.Add(policy);
                    await _context.SaveChangesAsync();
                }
            }

            staged.ReviewStatus = "Approved";
            staged.ReviewedAt = DateTime.Now;
            staged.ReviewedByUserId = reviewerUserId;
            staged.ReviewNotes = string.IsNullOrWhiteSpace(request.ReviewNotes) ? staged.ReviewNotes : request.ReviewNotes.Trim();
            staged.ApprovedPolicyId = policy.PolicyId;

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = reviewerUserId,
                Module = "Policy",
                Action = TruncateAction($"Approved external policy import #{staged.ImportId} -> Policy ID: {policy.PolicyId}"),
                LogDate = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                importId = staged.ImportId,
                reviewStatus = staged.ReviewStatus,
                policyId = policy.PolicyId,
                policyTitle = policy.PolicyTitle,
                message = "Policy approved successfully. You can now assign it."
            });
        }

        [HttpPost("staging/{importId:int}/assign")]
        public async Task<IActionResult> AssignApprovedStagedPolicy(int importId, [FromBody] AssignApprovedPolicyRequest? request)
        {
            // Assign approved policy: Chief Compliance Manager + Super Admin only (branch CM excluded)
            var unauthorized = RoleAccess.RequireRoleOrUnauthorized(
                this,
                RoleNames.ChiefComplianceManager,
                RoleNames.SuperAdmin);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (!TryGetCurrentUserId(out var reviewerUserId))
            {
                return Unauthorized(new { success = false, message = "Not logged in" });
            }

            request ??= new AssignApprovedPolicyRequest();

            var staged = await _context.ExternalPolicyImports.FirstOrDefaultAsync(i => i.ImportId == importId);
            if (staged == null)
            {
                return NotFound(new { success = false, message = "Staged policy not found." });
            }

            if (staged.ReviewStatus != "Approved" || !staged.ApprovedPolicyId.HasValue)
            {
                return BadRequest(new { success = false, message = "Policy must be approved before assignment." });
            }

            var policyId = staged.ApprovedPolicyId.Value;
            var employeeIds = request.EmployeeIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
            var supplierIds = request.SupplierIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();

            if (!employeeIds.Any() && !supplierIds.Any())
            {
                return BadRequest(new { success = false, message = "Select at least one employee or supplier." });
            }

            var existingEmployeeIds = employeeIds.Any()
                ? await _context.PolicyAssignments
                    .AsNoTracking()
                    .Where(pa => pa.PolicyId == policyId && employeeIds.Contains(pa.UserId))
                    .Select(pa => pa.UserId)
                    .Distinct()
                    .ToListAsync()
                : new List<int>();

            var existingSupplierIds = supplierIds.Any()
                ? await _context.SupplierPolicies
                    .AsNoTracking()
                    .Where(sp => sp.PolicyId == policyId && supplierIds.Contains(sp.SupplierId))
                    .Select(sp => sp.SupplierId)
                    .Distinct()
                    .ToListAsync()
                : new List<int>();

            var newEmployeeIds = employeeIds.Except(existingEmployeeIds).ToList();
            var newSupplierIds = supplierIds.Except(existingSupplierIds).ToList();

            if (newEmployeeIds.Any())
            {
                await _adminService.AssignPolicyToEmployeesAsync(policyId, newEmployeeIds);
            }

            if (newSupplierIds.Any())
            {
                await _adminService.AssignPolicyToSuppliersAsync(policyId, newSupplierIds);
            }

            staged.ReviewedByUserId = reviewerUserId;
            if (!string.IsNullOrWhiteSpace(request.ReviewNotes))
            {
                staged.ReviewNotes = request.ReviewNotes.Trim();
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = reviewerUserId,
                Module = "Policy",
                Action = TruncateAction($"Assigned approved external policy import #{staged.ImportId} -> Policy ID: {policyId}. New employees: {newEmployeeIds.Count}, skipped employees: {existingEmployeeIds.Count}, new suppliers: {newSupplierIds.Count}, skipped suppliers: {existingSupplierIds.Count}"),
                LogDate = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                importId = staged.ImportId,
                policyId,
                assignedEmployees = newEmployeeIds.Count,
                assignedSuppliers = newSupplierIds.Count,
                skippedEmployees = existingEmployeeIds.Count,
                skippedSuppliers = existingSupplierIds.Count,
                message = (newEmployeeIds.Count == 0 && newSupplierIds.Count == 0)
                    ? "No new assignments created. Selected users/suppliers already have this policy."
                    : "Policy assigned successfully."
            });
        }

        [HttpPost("staging/{importId:int}/reject")]
        public async Task<IActionResult> RejectStagedPolicy(int importId, [FromBody] RejectStagedPolicyRequest? request)
        {
            // Reject: Chief Compliance Manager + Super Admin only (branch CM excluded)
            var unauthorized = RoleAccess.RequireRoleOrUnauthorized(
                this,
                RoleNames.ChiefComplianceManager,
                RoleNames.SuperAdmin);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (!TryGetCurrentUserId(out var reviewerUserId))
            {
                return Unauthorized(new { success = false, message = "Not logged in" });
            }

            var staged = await _context.ExternalPolicyImports.FirstOrDefaultAsync(i => i.ImportId == importId);
            if (staged == null)
            {
                return NotFound(new { success = false, message = "Staged policy not found." });
            }

            staged.ReviewStatus = "Rejected";
            staged.ReviewedAt = DateTime.Now;
            staged.ReviewedByUserId = reviewerUserId;
            staged.ReviewNotes = string.IsNullOrWhiteSpace(request?.ReviewNotes)
                ? "Rejected by reviewer"
                : request!.ReviewNotes!.Trim();

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = reviewerUserId,
                Module = "Policy",
                Action = TruncateAction($"Rejected external policy import #{staged.ImportId}"),
                LogDate = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                importId = staged.ImportId,
                reviewStatus = staged.ReviewStatus,
                message = "Policy reference rejected."
            });
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportPolicyReference([FromBody] ImportPolicyReferenceRequest? request)
        {
            // Direct import: Chief Compliance Manager + Super Admin only (branch CM excluded)
            var unauthorized = RoleAccess.RequireRoleOrUnauthorized(
                this,
                RoleNames.ChiefComplianceManager,
                RoleNames.SuperAdmin);

            if (unauthorized != null)
            {
                return unauthorized;
            }

            if (request == null)
            {
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized(new { success = false, message = "Not logged in" });
            }

            var externalData = await ResolveExternalDataAsync(
                request.DocumentNumber,
                request.ExternalUrl,
                request.PolicyTitle,
                request.Description,
                request.Category,
                request.SourceApi);

            if (!externalData.Success || externalData.Data == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = externalData.ErrorMessage ?? "Unable to fetch external policy data."
                });
            }

            var data = externalData.Data;

            var existingPolicy = await _context.Policies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => !p.IsArchived && p.PolicyTitle == data.PolicyTitle);

            if (existingPolicy != null && !request.ForceCreate)
            {
                return Conflict(new
                {
                    success = false,
                    message = "A non-archived policy with the same title already exists.",
                    existingPolicyId = existingPolicy.PolicyId
                });
            }

            var newPolicy = new Policy
            {
                PolicyTitle = data.PolicyTitle.Trim(),
                Category = (data.Category ?? "General").Trim(),
                Description = (data.Description ?? "Imported external policy reference.").Trim(),
                DateUploaded = data.DateUploaded ?? DateTime.Now,
                UploadedBy = currentUserId
            };

            _context.Policies.Add(newPolicy);
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = currentUserId,
                Module = "Policy",
                Action = TruncateAction($"Imported external policy '{newPolicy.PolicyTitle}' from {data.SourceApi}"),
                LogDate = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                policyId = newPolicy.PolicyId,
                policyTitle = newPolicy.PolicyTitle,
                category = newPolicy.Category,
                sourceApi = data.SourceApi,
                documentNumber = data.DocumentNumber,
                externalUrl = data.ExternalUrl
            });
        }

        private async Task<ExternalPolicyResponse> ResolveExternalDataAsync(
            string? documentNumber,
            string? externalUrl,
            string? policyTitle,
            string? description,
            string? category,
            string? sourceApi)
        {
            ExternalPolicyData? externalData = null;

            if (!string.IsNullOrWhiteSpace(documentNumber))
            {
                var response = await _policyReferenceApiService.GetPolicyDataAsync(documentNumber.Trim());
                if (!response.Success || response.Data == null)
                {
                    return new ExternalPolicyResponse
                    {
                        Success = false,
                        ErrorMessage = response.ErrorMessage ?? "Unable to fetch external policy by document number."
                    };
                }

                externalData = response.Data;
            }
            else if (!string.IsNullOrWhiteSpace(externalUrl))
            {
                var response = await _policyReferenceApiService.ValidateAndFetchByUrlAsync(externalUrl.Trim());
                if (!response.Success || response.Data == null)
                {
                    return new ExternalPolicyResponse
                    {
                        Success = false,
                        ErrorMessage = response.ErrorMessage ?? "Unable to fetch external policy by URL."
                    };
                }

                externalData = response.Data;
            }

            var finalTitle = FirstNonEmpty(policyTitle, externalData?.PolicyTitle);
            if (string.IsNullOrWhiteSpace(finalTitle))
            {
                return new ExternalPolicyResponse
                {
                    Success = false,
                    ErrorMessage = "Policy title is required."
                };
            }

            return new ExternalPolicyResponse
            {
                Success = true,
                Data = new ExternalPolicyData
                {
                    PolicyTitle = finalTitle,
                    Description = FirstNonEmpty(description, externalData?.Description, "Imported external policy reference.") ?? "Imported external policy reference.",
                    Category = FirstNonEmpty(category, externalData?.Category, "General") ?? "General",
                    ExternalUrl = FirstNonEmpty(externalUrl, externalData?.ExternalUrl) ?? string.Empty,
                    DateUploaded = externalData?.DateUploaded,
                    Status = "External Reference",
                    DocumentNumber = FirstNonEmpty(documentNumber, externalData?.DocumentNumber) ?? string.Empty,
                    SourceApi = FirstNonEmpty(sourceApi, externalData?.SourceApi, "Federal Register") ?? "Federal Register"
                }
            };
        }

        private async Task<ExternalPolicyImport?> FindExistingImportAsync(string sourceApi, string? documentNumber, string? externalUrl, string? policyTitle = null)
        {
            var normalizedSource = sourceApi.Trim();
            var normalizedDoc = string.IsNullOrWhiteSpace(documentNumber) ? null : documentNumber.Trim();
            var normalizedUrl = string.IsNullOrWhiteSpace(externalUrl) ? null : externalUrl.Trim();
            var normalizedTitle = string.IsNullOrWhiteSpace(policyTitle) ? null : policyTitle.Trim();

            return await _context.ExternalPolicyImports
                .FirstOrDefaultAsync(i =>
                    i.SourceApi == normalizedSource &&
                    ((normalizedDoc != null && i.DocumentNumber == normalizedDoc) ||
                     (normalizedUrl != null && i.ExternalUrl == normalizedUrl) ||
                     (normalizedDoc == null && normalizedUrl == null && normalizedTitle != null && i.PolicyTitle == normalizedTitle)));
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var rawUserId = HttpContext.Session.GetString(SessionKeys.UserId);
            return !string.IsNullOrWhiteSpace(rawUserId) && int.TryParse(rawUserId, out userId);
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }

        private static string TruncateAction(string action)
        {
            return action.Length <= 255 ? action : action.Substring(0, 255);
        }

        public class ImportPolicyReferenceRequest
        {
            public string? SourceApi { get; set; }
            public string? DocumentNumber { get; set; }
            public string? ExternalUrl { get; set; }
            public string? PolicyTitle { get; set; }
            public string? Description { get; set; }
            public string? Category { get; set; }
            public bool ForceCreate { get; set; }
        }

        public class StagePolicyReferenceRequest
        {
            public string? SourceApi { get; set; }
            public string? DocumentNumber { get; set; }
            public string? ExternalUrl { get; set; }
            public string? PolicyTitle { get; set; }
            public string? Description { get; set; }
            public string? Category { get; set; }
        }

        public class ApproveStagedPolicyRequest
        {
            public string? Category { get; set; }
            public string? ReviewNotes { get; set; }
            public bool ForceCreate { get; set; }
        }

        public class AssignApprovedPolicyRequest
        {
            public string? ReviewNotes { get; set; }
            public List<int>? EmployeeIds { get; set; }
            public List<int>? SupplierIds { get; set; }
        }

        public class RejectStagedPolicyRequest
        {
            public string? ReviewNotes { get; set; }
        }
    }
}
