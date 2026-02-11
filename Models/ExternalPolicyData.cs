using System.Text.Json.Serialization;

namespace TechNova_IT_Solutions.Models
{
    /// <summary>
    /// Maps to a single document from the Federal Register API response
    /// </summary>
    public class ExternalPolicyData
    {
        public string PolicyTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ExternalUrl { get; set; } = string.Empty;
        public DateTime? DateUploaded { get; set; }
        public string Status { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string SourceApi { get; set; } = "Federal Register";
    }

    /// <summary>
    /// Wrapper for the API call result
    /// </summary>
    public class ExternalPolicyResponse
    {
        public bool Success { get; set; }
        public ExternalPolicyData? Data { get; set; }
        public string? ErrorMessage { get; set; }
    }

    // ── Federal Register API JSON deserialization models ──

    /// <summary>
    /// Root object returned by the Federal Register /documents.json endpoint
    /// </summary>
    public class FederalRegisterSearchResult
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("results")]
        public List<FederalRegisterDocument> Results { get; set; } = new();
    }

    /// <summary>
    /// A single Federal Register document (rule, proposed rule, notice, etc.)
    /// </summary>
    public class FederalRegisterDocument
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("abstract")]
        public string? Abstract { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("publication_date")]
        public string? PublicationDate { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("document_number")]
        public string DocumentNumber { get; set; } = string.Empty;
    }
}
