using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Services.Interfaces;

namespace TechNova_IT_Solutions.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _environment;

        public AdminController(IAdminService adminService, IUserService userService, IWebHostEnvironment environment)
        {
            _adminService = adminService;
            _userService = userService;
            _environment = environment;
        }

        private bool IsAdmin()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole == "Admin";
        }

        public async Task<IActionResult> Dashboard()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var data = await _adminService.GetDashboardDataAsync();
            return View(data);
        }

        public async Task<IActionResult> UserManagement()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserData userData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (userData == null)
            {
                return BadRequest(new { success = false, message = "Invalid user data" });
            }

            var result = await _userService.CreateUserAsync(userData);
            
            if (result)
            {
                return Ok(new { success = true, message = "User created successfully" });
            }
            
            return BadRequest(new { success = false, message = "Failed to create user" });
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(int userId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var user = await _userService.GetUserByIdAsync(userId);
            
            if (user != null)
            {
                return Ok(new { success = true, user = user });
            }
            
            return NotFound(new { success = false, message = "User not found" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser([FromBody] UserData userData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (userData == null)
            {
                return BadRequest(new { success = false, message = "Invalid user data" });
            }

            var result = await _userService.UpdateUserAsync(userData);
            
            if (result)
            {
                return Ok(new { success = true, message = "User updated successfully" });
            }
            
            return BadRequest(new { success = false, message = "Failed to update user" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var result = await _userService.DeleteUserAsync(userId);
            
            if (result)
            {
                return Ok(new { success = true, message = "User deleted successfully" });
            }
            
            return BadRequest(new { success = false, message = "Failed to delete user" });
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var result = await _userService.DeactivateUserAsync(userId);
            
            if (result)
            {
                return Ok(new { success = true, message = "User deactivated successfully" });
            }
            
            return BadRequest(new { success = false, message = "Failed to deactivate user" });
        }

        [HttpPost]
        public async Task<IActionResult> ReactivateUser(int userId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var result = await _userService.ReactivateUserAsync(userId);
            
            if (result)
            {
                return Ok(new { success = true, message = "User reactivated successfully" });
            }
            
            return BadRequest(new { success = false, message = "Failed to reactivate user" });
        }

        public IActionResult PolicyManagement()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }

        public IActionResult SupplierManagement()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }

        public IActionResult ComplianceMonitoring()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }

        public IActionResult AuditLogs()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }

        public IActionResult Reports()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }

        public IActionResult Procurement()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check user role
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }

        private int? GetCurrentUserId()
        {
            var s = HttpContext.Session.GetString("UserId");
            return int.TryParse(s, out var id) ? id : null;
        }

        // Policy Management POST actions
        [HttpPost]
        public async Task<IActionResult> CreatePolicy([FromBody] PolicyData policyData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (policyData == null) return BadRequest(new { success = false, message = "Invalid policy data" });

            policyData.UploadedDate ??= DateTime.Now;
            var result = await _adminService.CreatePolicyAsync(policyData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Created policy: {policyData.PolicyTitle}", "Policy");
                return Ok(new { success = true, message = "Policy created successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to create policy" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePolicy([FromBody] PolicyData policyData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (policyData == null) return BadRequest(new { success = false, message = "Invalid policy data" });

            var result = await _adminService.UpdatePolicyAsync(policyData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Updated policy: {policyData.PolicyTitle}", "Policy");
                return Ok(new { success = true, message = "Policy updated successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to update policy" });
        }

        /// <summary>
        /// Upload a policy with an attached file (PDF, DOCX, etc.).
        /// Accepts multipart/form-data.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UploadPolicy(IFormFile? policyFile, [FromForm] string policyTitle, [FromForm] string category, [FromForm] string description, [FromForm] string status, [FromForm] int? policyId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (string.IsNullOrWhiteSpace(policyTitle)) return BadRequest(new { success = false, message = "Policy title is required" });

            string filePath = string.Empty;

            // Save uploaded file
            if (policyFile != null && policyFile.Length > 0)
            {
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "policies");
                Directory.CreateDirectory(uploadsDir);

                // Create unique filename: timestamp_originalname
                var safeFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(policyFile.FileName)}";
                var fullPath = Path.Combine(uploadsDir, safeFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await policyFile.CopyToAsync(stream);
                }

                filePath = $"/uploads/policies/{safeFileName}";
            }

            var policyData = new PolicyData
            {
                PolicyTitle = policyTitle,
                Category = category,
                Description = description,
                FilePath = filePath,
                UploadedDate = DateTime.Now
            };

            bool result;
            if (policyId.HasValue && policyId.Value > 0)
            {
                policyData.PolicyId = policyId.Value;
                // Keep existing file if no new file uploaded
                if (string.IsNullOrEmpty(filePath))
                {
                    policyData.FilePath = null!; // signal to keep existing
                }
                result = await _adminService.UpdatePolicyAsync(policyData);
                if (result) await _adminService.LogActivityAsync(GetCurrentUserId(), $"Updated policy with file: {policyTitle}", "Policy");
            }
            else
            {
                result = await _adminService.CreatePolicyAsync(policyData);
                if (result) await _adminService.LogActivityAsync(GetCurrentUserId(), $"Created policy with file: {policyTitle}", "Policy");
            }

            if (result)
                return Ok(new { success = true, message = "Policy saved successfully" });
            return BadRequest(new { success = false, message = "Failed to save policy" });
        }

        /// <summary>
        /// Serve a policy file for viewing/download.
        /// </summary>
        [HttpGet]
        public IActionResult DownloadPolicyFile(int policyId)
        {
            // Allow any authenticated user to download
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized();

            var policy = _adminService.GetPolicyByIdAsync(policyId).Result;
            if (policy == null || string.IsNullOrEmpty(policy.FilePath))
                return NotFound(new { message = "File not found" });

            var fullPath = Path.Combine(_environment.WebRootPath, policy.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { message = "File not found on server" });

            var ext = Path.GetExtension(fullPath).ToLower();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };

            var fileName = Path.GetFileName(fullPath);
            // For PDFs and images, allow inline viewing; for others, force download
            if (ext == ".pdf" || ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".txt")
            {
                Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileName}\"");
                return PhysicalFile(fullPath, contentType);
            }
            return PhysicalFile(fullPath, contentType, fileName);
        }

        [HttpPost]
        public async Task<IActionResult> DeletePolicy(int policyId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var result = await _adminService.DeletePolicyAsync(policyId);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Deleted policy ID: {policyId}", "Policy");
                return Ok(new { success = true, message = "Policy deleted successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to delete policy" });
        }

        [HttpPost]
        public async Task<IActionResult> AssignPolicy([FromBody] PolicyAssignmentRequest request)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (request == null) return BadRequest(new { success = false, message = "Invalid request" });

            bool success = true;

            if (request.EmployeeIds.Any())
            {
                success &= await _adminService.AssignPolicyToEmployeesAsync(request.PolicyId, request.EmployeeIds);
            }
            if (request.SupplierIds.Any())
            {
                success &= await _adminService.AssignPolicyToSuppliersAsync(request.PolicyId, request.SupplierIds);
            }

            if (success)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(),
                    $"Assigned policy {request.PolicyId} to {request.EmployeeIds.Count} employee(s) and {request.SupplierIds.Count} supplier(s)",
                    "Policy");
                return Ok(new { success = true, message = "Policy assigned successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to assign policy" });
        }

        // Supplier Management POST actions
        [HttpPost]
        public async Task<IActionResult> CreateSupplier([FromBody] SupplierData supplierData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (supplierData == null) return BadRequest(new { success = false, message = "Invalid supplier data" });

            var result = await _adminService.CreateSupplierAsync(supplierData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Created supplier: {supplierData.SupplierName}", "Supplier");
                return Ok(new { success = true, message = "Supplier created successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to create supplier" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSupplier([FromBody] SupplierData supplierData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (supplierData == null) return BadRequest(new { success = false, message = "Invalid supplier data" });

            var result = await _adminService.UpdateSupplierAsync(supplierData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Updated supplier: {supplierData.SupplierName}", "Supplier");
                return Ok(new { success = true, message = "Supplier updated successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to update supplier" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSupplier(int supplierId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var result = await _adminService.DeleteSupplierAsync(supplierId);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Deleted supplier ID: {supplierId}", "Supplier");
                return Ok(new { success = true, message = "Supplier deleted successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to delete supplier" });
        }

        // Procurement POST actions
        [HttpPost]
        public async Task<IActionResult> CreateProcurement([FromBody] ProcurementData procurementData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (procurementData == null) return BadRequest(new { success = false, message = "Invalid procurement data" });

            var result = await _adminService.CreateProcurementAsync(procurementData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Created procurement: {procurementData.ItemName}", "Procurement");
                return Ok(new { success = true, message = "Procurement created successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to create procurement" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProcurement([FromBody] ProcurementData procurementData)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            if (procurementData == null) return BadRequest(new { success = false, message = "Invalid procurement data" });

            var result = await _adminService.UpdateProcurementAsync(procurementData);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Updated procurement: {procurementData.ItemName}", "Procurement");
                return Ok(new { success = true, message = "Procurement updated successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to update procurement" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProcurement(int procurementId)
        {
            if (!IsAdmin()) return Unauthorized(new { success = false, message = "Access denied" });
            var result = await _adminService.DeleteProcurementAsync(procurementId);
            if (result)
            {
                await _adminService.LogActivityAsync(GetCurrentUserId(), $"Deleted procurement ID: {procurementId}", "Procurement");
                return Ok(new { success = true, message = "Procurement deleted successfully" });
            }
            return BadRequest(new { success = false, message = "Failed to delete procurement" });
        }
    }
}
