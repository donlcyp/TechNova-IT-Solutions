namespace TechNova_IT_Solutions.Models
{
    public class ExternalApisConfiguration
    {
        public FederalRegisterApiSettings FederalRegisterApi { get; set; } = new();
        public ExchangeRateApiSettings ExchangeRateApi { get; set; } = new();
    }

    public class FederalRegisterApiSettings
    {
        public string BaseUrl { get; set; } = "https://www.federalregister.gov/api/v1";
        public int PerPage { get; set; } = 20;
        public int Timeout { get; set; } = 30;
        public string DefaultSearchTerm { get; set; } = "information technology supply chain";
    }

    public class ExchangeRateApiSettings
    {
        public string BaseUrl { get; set; } = "https://open.er-api.com/v6";
        public int Timeout { get; set; } = 30;
        public string BaseCurrency { get; set; } = "PHP";
    }

}
