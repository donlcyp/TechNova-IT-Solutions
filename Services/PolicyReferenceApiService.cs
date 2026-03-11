using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;
using TechNova_IT_Solutions.Models;

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
    /// </summary>
    public class PolicyReferenceApiService : IPolicyReferenceApiService
    {
        private readonly HttpClient _httpClient;
        private readonly FederalRegisterApiSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions;

        // Curated keyword sets for TechNova policy categories.
        private static readonly Dictionary<string, string[]> PolicySearchTerms = new()
        {
            ["Information Security Policy"] = new[]
            {
                "information security", "cybersecurity", "access control", "authentication", "network security", "incident response"
            },
            ["Data Privacy Policy"] = new[]
            {
                "data privacy", "personally identifiable information", "PII", "confidential records", "data protection"
            },
            ["Acceptable Use of IT Resources"] = new[]
            {
                "acceptable use", "information technology", "computer network", "system access", "user behavior"
            },
            ["Employee Code of Conduct"] = new[]
            {
                "employee conduct", "ethics", "workplace standards", "professional conduct", "disciplinary action"
            },
            ["Procurement Policy"] = new[]
            {
                "procurement", "purchasing", "acquisition", "contracting", "vendor selection"
            },
            ["Supplier Compliance Policy"] = new[]
            {
                "supplier compliance", "vendor security", "supply chain", "third-party risk", "vendor management"
            },
            ["Document Retention Policy"] = new[]
            {
                "document retention", "records management", "records disposal", "retention schedule", "archiving"
            },
            ["Remote Work & BYOD Policy"] = new[]
            {
                "remote work", "telework", "bring your own device", "BYOD", "mobile device security", "flexible work"
            },
            ["Business Continuity Policy"] = new[]
            {
                "business continuity", "disaster recovery", "emergency response", "operational resilience", "continuity of operations"
            },
            ["Change Management Policy"] = new[]
            {
                "change management", "IT governance", "system change control", "configuration management", "change control"
            },
            ["Incident Response Policy"] = new[]
            {
                "incident response", "breach notification", "security incident", "cyber incident", "data breach", "incident handling"
            },
            ["Vendor Risk Management Policy"] = new[]
            {
                "vendor risk", "third-party assessment", "supplier audit", "supply chain risk", "vendor due diligence"
            }
        };

        public static int CategoryCount => PolicySearchTerms.Count;

        private static readonly string[] GlobalPolicyKeywords =
        {
            "policy", "security", "privacy", "compliance", "supplier", "records", "retention", "technology"
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

        private static string? ExtractDocumentNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            input = input.Trim();

            var match = System.Text.RegularExpressions.Regex.Match(
                input, @"federalregister\.gov/d/([\w-]+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            match = System.Text.RegularExpressions.Regex.Match(
                input, @"federalregister\.gov/(?:api/v1/)?documents/(?:\d{4}/\d{2}/\d{2}/)?([\w-]+?)(?:\.json)?(?:/|$)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            match = System.Text.RegularExpressions.Regex.Match(
                input, @"api\.federalregister\.gov/v1/documents/([\w-]+?)(?:\.json)?$");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            match = System.Text.RegularExpressions.Regex.Match(input, @"^\d{4}-\d{4,6}$");
            if (match.Success)
            {
                return input;
            }

            return null;
        }

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

        public async Task<List<ExternalPolicyData>> GetAllPoliciesAsync()
        {
            var allResults = new List<ExternalPolicyData>();

            var tasks = PolicySearchTerms.Select(async kvp =>
            {
                var results = await SearchFederalRegisterAsync(kvp.Value, perPage: 5);
                foreach (var item in results)
                {
                    item.Category = kvp.Key;
                }
                return results;
            });

            var batchResults = await Task.WhenAll(tasks);
            var seenDocNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var batch in batchResults)
            {
                foreach (var item in batch)
                {
                    var docNum = item.DocumentNumber?.Trim();
                    var url = item.ExternalUrl?.Trim();

                    bool isNew = docNum != null
                        ? seenDocNumbers.Add(docNum)
                        : url != null && seenUrls.Add(url);

                    if (isNew)
                    {
                        allResults.Add(item);
                    }
                }
            }

            return allResults;
        }

        public async Task<List<ExternalPolicyData>> SearchPoliciesByCategoryAsync(string category)
        {
            if (string.IsNullOrWhiteSpace(category) || category.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return await GetAllPoliciesAsync();
            }

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

            var fallbackTerms = category
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => s.Length > 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return await SearchFederalRegisterAsync(fallbackTerms, perPage: 5);
        }

        private async Task<List<ExternalPolicyData>> SearchFederalRegisterAsync(IEnumerable<string> searchTerms, int? perPage = null)
        {
            try
            {
                var count = perPage ?? _settings.PerPage;
                var normalizedTerms = searchTerms
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (!normalizedTerms.Any())
                {
                    normalizedTerms = new[] { _settings.DefaultSearchTerm };
                }

                var queryExpression = string.Join(" OR ", normalizedTerms);
                var encodedTerm = Uri.EscapeDataString(queryExpression);
                var minDate = DateTime.UtcNow.AddYears(-5).ToString("yyyy-MM-dd");

                var url = $"/api/v1/documents.json"
                    + $"?conditions[type][]=RULE"
                    + $"&conditions[term]={encodedTerm}"
                    + $"&conditions[publication_date][gte]={minDate}"
                    + $"&per_page={count}"
                    + "&fields[]=title&fields[]=abstract&fields[]=html_url"
                    + "&fields[]=publication_date&fields[]=type&fields[]=document_number"
                    + "&order=newest";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var searchResult = JsonSerializer.Deserialize<FederalRegisterSearchResult>(content, _jsonOptions);

                if (searchResult?.Results == null)
                {
                    return new List<ExternalPolicyData>();
                }

                var relevanceTerms = normalizedTerms
                    .Concat(GlobalPolicyKeywords)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return searchResult.Results
                    .Where(doc => IsRelevantPolicyDocument(doc, relevanceTerms))
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
            {
                publicationDate = parsed;
            }

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

        private static bool IsRelevantPolicyDocument(FederalRegisterDocument doc, IEnumerable<string> terms)
        {
            var haystack = $"{doc.Title} {doc.Abstract}".ToLowerInvariant();
            return terms.Any(term => haystack.Contains(term.ToLowerInvariant()));
        }
    }
}
