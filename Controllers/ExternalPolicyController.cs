using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Services; // Ensure this namespace matches where IPolicyReferenceApiService is
using TechNova_IT_Solutions.Models;   // Ensure this namespace matches where ExternalPolicyResponse/ExternalPolicyData are

namespace TechNova_IT_Solutions.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExternalPolicyController : ControllerBase
    {
        private readonly IPolicyReferenceApiService _policyService;

        public ExternalPolicyController(IPolicyReferenceApiService policyService)
        {
            _policyService = policyService;
        }

        // GET: api/ExternalPolicy
        [HttpGet]
        public async Task<IActionResult> GetRule()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != Constants.RoleNames.ChiefComplianceManager && userRole != Constants.RoleNames.SuperAdmin)
            {
                return Unauthorized(new { success = false, message = "Access denied. Only Chief Compliance Manager or Super Admin can access external policy data." });
            }
            
            string documentNumber = "2019-25554";

            var response = await _policyService.GetPolicyDataAsync(documentNumber);

            if (!response.Success || response.Data == null)
            {
                return NotFound(response.ErrorMessage ?? "Policy not found.");
            }

            return Ok(response.Data);
        }
    }
}
