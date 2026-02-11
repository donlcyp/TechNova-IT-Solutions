using Microsoft.Extensions.Options;
using TechNova_IT_Solutions.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace TechNova_IT_Solutions.Services
{
    public interface IPolicyReferenceApiService
    {
        Task<ExternalPolicyResponse> GetPolicyDataAsync(string documentNumber);
        Task<ExternalPolicyResponse> ValidateAndFetchByUrlAsync(string federalRegisterUrl);
        Task<List<ExternalPolicyData>> GetAllPoliciesAsync();
        Task<List<ExternalPolicyData>> SearchPoliciesByCategoryAsync(string category);
    }

    /// <summary>
    /// Fetches regulatory policy data from the Federal Register public API.
    /// No API key required — this is a free, open government data source.
    /// Docs: https://www.federalregister.gov/developers/documentation/api/v1
    /// </summary>
    public class PolicyReferenceApiService : IPolicyReferenceApiService
    {
        private readonly HttpClient _httpClient;
        private readonly FederalRegisterApiSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions;

        // ── The 7 TechNova policy categories and their Federal Register search terms ──
        private static readonly Dictionary<string, string> PolicySearchTerms = new()
        {
            ["Information Security Policy"]       = "information security password data protection access control",
            ["Data Privacy Policy"]               = "data privacy personally identifiable information confidential records",
            ["Acceptable Use of IT Resources"]    = "acceptable use information technology computer network",
            ["Employee Code of Conduct"]          = "employee conduct ethics workplace professional standards",
            ["Procurement Policy"]                = "procurement purchasing government equipment acquisition",
            ["Supplier Compliance Policy"]        = "supplier compliance vendor security supply chain standards",
            ["Document Retention Policy"]         = "document retention records management disposal"
        };

        public PolicyReferenceApiService(
            IHttpClientFactory httpClientFactory,
            IOptions<ExternalApisConfiguration> apiConfiguration)
        {
            _settings = apiConfiguration.Value.FederalRegisterApi;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.Timeout);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Accepts a Federal Register web URL (e.g. https://www.federalregister.gov/d/2019-25554)
        /// or a direct document number, extracts the document number, and fetches validated
        /// data from the official API endpoint.
        /// </summary>
        public async Task<ExternalPolicyResponse> ValidateAndFetchByUrlAsync(string federalRegisterUrl)
        {
            var docNumber = ExtractDocumentNumber(federalRegisterUrl);
            if (string.IsNullOrEmpty(docNumber))
            {
                return new ExternalPolicyResponse
                {
                    Success = false,
                    ErrorMessage = $"Could not extract a document number from: {federalRegisterUrl}"
                };
            }

            return await GetPolicyDataAsync(docNumber);
        }

        /// <summary>
        /// Extracts the document number from a Federal Register URL or returns the input
        /// if it already looks like a document number.
        /// Supports formats:
        ///   https://www.federalregister.gov/d/2019-25554
        ///   https://www.federalregister.gov/documents/2019/11/27/2019-25554/...
        ///   https://api.federalregister.gov/v1/documents/2019-25554.json
        ///   2019-25554  (plain document number)
        /// </summary>
        private static string? ExtractDocumentNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            input = input.Trim();

            // Pattern: /d/{doc_number}
            var match = System.Text.RegularExpressions.Regex.Match(
                input, @"federalregister\.gov/d/([\w-]+)");
            if (match.Success)
                return match.Groups[1].Value;

            // Pattern: /documents/YYYY/MM/DD/{doc_number}/...
            match = System.Text.RegularExpressions.Regex.Match(
                input, @"federalregister\.gov/(?:api/v1/)?documents/(?:\d{4}/\d{2}/\d{2}/)?([\w-]+?)(?:\.json)?(?:/|$)");
            if (match.Success)
                return match.Groups[1].Value;

            // Pattern: /v1/documents/{doc_number}.json  (API subdomain)
            match = System.Text.RegularExpressions.Regex.Match(
                input, @"api\.federalregister\.gov/v1/documents/([\w-]+?)(?:\.json)?$");
            if (match.Success)
                return match.Groups[1].Value;

            // If it looks like a bare document number (e.g. "2019-25554")
            match = System.Text.RegularExpressions.Regex.Match(input, @"^\d{4}-\d{4,6}$");
            if (match.Success)
                return input;

            return null;
        }

        /// <summary>
        /// Fetch a single Federal Register document by its document number.
        /// </summary>
        public async Task<ExternalPolicyResponse> GetPolicyDataAsync(string documentNumber)
        {
            try
            {
                var url = $"/api/v1/documents/{documentNumber}.json"
                    + "?fields[]=title&fields[]=abstract&fields[]=html_url"
                    + "&fields[]=publication_date&fields[]=type&fields[]=document_number";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var doc = JsonSerializer.Deserialize<FederalRegisterDocument>(content, _jsonOptions);

                if (doc == null)
                {
                    return new ExternalPolicyResponse
                    {
                        Success = false,
                        ErrorMessage = "Document not found"
                    };
                }

                return new ExternalPolicyResponse
                {
                    Success = true,
                    Data = MapToExternalPolicyData(doc, "General")
                };
            }
            catch (HttpRequestException ex)
            {
                return new ExternalPolicyResponse
                {
                    Success = false,
                    ErrorMessage = $"Federal Register API request failed: {ex.Message}"
                };
            }
            catch (JsonException ex)
            {
                return new ExternalPolicyResponse
                {
                    Success = false,
                    ErrorMessage = $"Failed to parse Federal Register response: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Fetch results for ALL 7 TechNova policy categories in parallel,
        /// returning up to 3 Federal Register documents per category.
        /// </summary>
        public async Task<List<ExternalPolicyData>> GetAllPoliciesAsync()
        {
            var allResults = new List<ExternalPolicyData>();

            // Fire all 7 searches in parallel for speed
            var tasks = PolicySearchTerms.Select(async kvp =>
            {
                var results = await SearchFederalRegisterAsync(kvp.Value, perPage: 3);
                foreach (var item in results)
                {
                    item.Category = kvp.Key; // Tag with TechNova category name
                }
                return results;
            });

            var batchResults = await Task.WhenAll(tasks);
            foreach (var batch in batchResults)
            {
                allResults.AddRange(batch);
            }

            return allResults;
        }

        /// <summary>
        /// Search for a specific TechNova policy category.
        /// If the category matches one of the 7, uses its curated search term.
        /// </summary>
        public async Task<List<ExternalPolicyData>> SearchPoliciesByCategoryAsync(string category)
        {
            // If "all" or empty, return all 7 categories
            if (string.IsNullOrWhiteSpace(category) || category.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return await GetAllPoliciesAsync();
            }

            // Find matching TechNova policy category
            var matchedKey = PolicySearchTerms.Keys
                .FirstOrDefault(k => k.Equals(category, StringComparison.OrdinalIgnoreCase)
                    || category.Contains(k, StringComparison.OrdinalIgnoreCase)
                    || k.Contains(category, StringComparison.OrdinalIgnoreCase));

            if (matchedKey != null)
            {
                var results = await SearchFederalRegisterAsync(PolicySearchTerms[matchedKey], perPage: 5);
                foreach (var item in results)
                {
                    item.Category = matchedKey;
                }
                return results;
            }

            // Fallback: use raw category text as search term
            return await SearchFederalRegisterAsync(category, perPage: 5);
        }

        // ── Private helpers ─────────────────────────────────────────

        private async Task<List<ExternalPolicyData>> SearchFederalRegisterAsync(string searchTerm, int? perPage = null)
        {
            try
            {
                var count = perPage ?? _settings.PerPage;
                var encodedTerm = Uri.EscapeDataString(searchTerm);
                var url = $"/api/v1/documents.json"
                    + $"?conditions[type][]=RULE"
                    + $"&conditions[term]={encodedTerm}"
                    + $"&per_page={count}"
                    + "&fields[]=title&fields[]=abstract&fields[]=html_url"
                    + "&fields[]=publication_date&fields[]=type&fields[]=document_number"
                    + "&order=newest";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var searchResult = JsonSerializer.Deserialize<FederalRegisterSearchResult>(content, _jsonOptions);

                if (searchResult?.Results == null)
                    return new List<ExternalPolicyData>();

                return searchResult.Results
                    .Select(doc => MapToExternalPolicyData(doc, "Rule"))
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching from Federal Register API: {ex.Message}");
                return new List<ExternalPolicyData>();
            }
        }

        private static ExternalPolicyData MapToExternalPolicyData(FederalRegisterDocument doc, string defaultCategory)
        {
            DateTime? publicationDate = null;
            if (DateTime.TryParse(doc.PublicationDate, out var parsed))
                publicationDate = parsed;

            return new ExternalPolicyData
            {
                PolicyTitle = doc.Title,
                Description = doc.Abstract ?? "No description available.",
                Category = defaultCategory,
                ExternalUrl = doc.HtmlUrl,
                DateUploaded = publicationDate,
                Status = "External Reference",
                DocumentNumber = doc.DocumentNumber,
                SourceApi = "Federal Register"
            };
        }
    }
}
