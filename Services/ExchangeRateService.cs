using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TechNova_IT_Solutions.Models;

namespace TechNova_IT_Solutions.Services
{
    public interface IExchangeRateService
    {
        Task<ExchangeRateQuoteResult> GetRateAsync(string fromCurrency, string toCurrency);
    }

    public class ExchangeRateQuoteResult
    {
        public bool Success { get; set; }
        public decimal Rate { get; set; }
        public string SourceCurrency { get; set; } = string.Empty;
        public string TargetCurrency { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime RetrievedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class ExchangeRateService : IExchangeRateService
    {
        private readonly HttpClient _httpClient;
        private readonly ExchangeRateApiSettings _settings;

        public ExchangeRateService(
            IHttpClientFactory httpClientFactory,
            IOptions<ExternalApisConfiguration> apiConfiguration)
        {
            _settings = apiConfiguration.Value.ExchangeRateApi;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.Timeout);
        }

        public async Task<ExchangeRateQuoteResult> GetRateAsync(string fromCurrency, string toCurrency)
        {
            var source = (fromCurrency ?? string.Empty).Trim().ToUpperInvariant();
            var target = (toCurrency ?? string.Empty).Trim().ToUpperInvariant();

            if (source.Length != 3 || target.Length != 3)
            {
                return new ExchangeRateQuoteResult
                {
                    Success = false,
                    SourceCurrency = source,
                    TargetCurrency = target,
                    ErrorMessage = "Currency codes must be 3-letter ISO values."
                };
            }

            if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
            {
                return new ExchangeRateQuoteResult
                {
                    Success = true,
                    Rate = 1m,
                    SourceCurrency = source,
                    TargetCurrency = target
                };
            }

            var requestUrl = $"{_settings.BaseUrl.TrimEnd('/')}/{_settings.ApiKey}/latest/{Uri.EscapeDataString(source)}";

            try
            {
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                await using var contentStream = await response.Content.ReadAsStreamAsync();
                using var document = await JsonDocument.ParseAsync(contentStream);

                
                var ratesPropertyName = document.RootElement.TryGetProperty("conversion_rates", out _)
                    ? "conversion_rates"
                    : "rates";

                if (!document.RootElement.TryGetProperty(ratesPropertyName, out var ratesElement) ||
                    ratesElement.ValueKind != JsonValueKind.Object ||
                    !ratesElement.TryGetProperty(target, out var rateElement))
                {
                    return new ExchangeRateQuoteResult
                    {
                        Success = false,
                        SourceCurrency = source,
                        TargetCurrency = target,
                        ErrorMessage = $"Target currency {target} is not available from exchange rate provider."
                    };
                }

                decimal rate;
                if (rateElement.ValueKind == JsonValueKind.Number && rateElement.TryGetDecimal(out rate))
                {
                    // Parsed as decimal successfully.
                }
                else if (rateElement.ValueKind == JsonValueKind.String &&
                         decimal.TryParse(rateElement.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedRate))
                {
                    rate = parsedRate;
                }
                else
                {
                    return new ExchangeRateQuoteResult
                    {
                        Success = false,
                        SourceCurrency = source,
                        TargetCurrency = target,
                        ErrorMessage = "Exchange rate provider returned an invalid rate format."
                    };
                }

                if (rate <= 0)
                {
                    return new ExchangeRateQuoteResult
                    {
                        Success = false,
                        SourceCurrency = source,
                        TargetCurrency = target,
                        ErrorMessage = "Exchange rate provider returned a non-positive rate."
                    };
                }

                return new ExchangeRateQuoteResult
                {
                    Success = true,
                    Rate = rate,
                    SourceCurrency = source,
                    TargetCurrency = target
                };
            }
            catch (Exception ex)
            {
                return new ExchangeRateQuoteResult
                {
                    Success = false,
                    SourceCurrency = source,
                    TargetCurrency = target,
                    ErrorMessage = $"Exchange rate lookup failed: {ex.Message}"
                };
            }
        }
    }
}
