using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova_IT_Solutions.Pages.SuperAdmin
{
    public class SystemSettingsPoliciesModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public SystemSettingsPoliciesModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Email Settings
        public string EmailHost { get; set; } = string.Empty;
        public string EmailPort { get; set; } = string.Empty;
        public string EmailUsername { get; set; } = string.Empty;
        public string EmailFrom { get; set; } = string.Empty;
        public bool EmailUseSsl { get; set; }

        // Federal Register API
        public string FederalRegisterBaseUrl { get; set; } = string.Empty;
        public string FederalRegisterPerPage { get; set; } = string.Empty;
        public string FederalRegisterTimeout { get; set; } = string.Empty;
        public string FederalRegisterSearchTerm { get; set; } = string.Empty;

        // Exchange Rate API
        public string ExchangeRateBaseUrl { get; set; } = string.Empty;
        public string ExchangeRateBaseCurrency { get; set; } = string.Empty;
        public string ExchangeRateTimeout { get; set; } = string.Empty;

        // Database
        public bool AutoMigrateOnStartup { get; set; }

        // Bootstrap
        public bool BootstrapEnabled { get; set; }
        public string BootstrapEmail { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            var denied = RoleAccess.RequireRoleOrRedirect(
                this,
                new[] { RoleNames.SuperAdmin },
                new Dictionary<string, string>
                {
                    [RoleNames.ChiefComplianceManager] = "/ComplianceManager/ComplianceDashboard",
                    [RoleNames.ComplianceManager] = "/ComplianceManager/ComplianceDashboard",
                    [RoleNames.Employee] = "/Employee/Dashboard",
                    [RoleNames.Supplier] = "/Supplier/Dashboard"
                });
            if (denied != null) return denied;

            EmailHost = _configuration["EmailSettings:Host"] ?? "—";
            EmailPort = _configuration["EmailSettings:Port"] ?? "—";
            EmailUsername = _configuration["EmailSettings:Username"] ?? "—";
            EmailFrom = _configuration["EmailSettings:FromEmail"] ?? "—";
            EmailUseSsl = bool.TryParse(_configuration["EmailSettings:UseSsl"], out var ssl) && ssl;

            FederalRegisterBaseUrl = _configuration["ExternalApis:FederalRegisterApi:BaseUrl"] ?? "—";
            FederalRegisterPerPage = _configuration["ExternalApis:FederalRegisterApi:PerPage"] ?? "—";
            FederalRegisterTimeout = _configuration["ExternalApis:FederalRegisterApi:Timeout"] ?? "—";
            FederalRegisterSearchTerm = _configuration["ExternalApis:FederalRegisterApi:DefaultSearchTerm"] ?? "—";

            ExchangeRateBaseUrl = _configuration["ExternalApis:ExchangeRateApi:BaseUrl"] ?? "—";
            ExchangeRateBaseCurrency = _configuration["ExternalApis:ExchangeRateApi:BaseCurrency"] ?? "—";
            ExchangeRateTimeout = _configuration["ExternalApis:ExchangeRateApi:Timeout"] ?? "—";

            AutoMigrateOnStartup = bool.TryParse(_configuration["Database:AutoMigrateOnStartup"], out var migrate) && migrate;

            BootstrapEnabled = bool.TryParse(_configuration["BootstrapSuperAdmin:Enabled"], out var boot) && boot;
            BootstrapEmail = _configuration["BootstrapSuperAdmin:Email"] ?? "—";

            return Page();
        }
    }
}



