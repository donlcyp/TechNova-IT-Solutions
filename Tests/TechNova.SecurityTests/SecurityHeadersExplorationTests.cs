using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

namespace TechNova.SecurityTests
{
    /// <summary>
    /// Bug Condition Exploration Tests for Security Headers (Task 1.3)
    /// 
    /// **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6**
    /// 
    /// CRITICAL: These tests are EXPECTED TO FAIL on unfixed code.
    /// Failure confirms the bugs exist. DO NOT attempt to fix the tests or code when they fail.
    /// 
    /// These tests encode the expected behavior - they will validate the fixes when they pass after implementation.
    /// </summary>
    public class SecurityHeadersExplorationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public SecurityHeadersExplorationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false // Don't follow redirects to test actual responses
            });
        }

        /// <summary>
        /// Test 1.3.1: HTTP response lacks Content-Security-Policy header
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Application does not include Content-Security-Policy (CSP) header in responses
        /// Expected Behavior: All HTTP responses should include CSP header with restrictive directives
        /// Current Behavior: No CSP header is set, allowing XSS attacks
        /// 
        /// Requirement 3.1: WHEN the application responds to HTTP requests THEN the system does not include 
        /// Content-Security-Policy (CSP) headers, allowing XSS attacks
        /// </summary>
        [Fact]
        public async Task HttpResponse_ShouldHaveContentSecurityPolicyHeader_ButDoesNot()
        {
            // Arrange & Act - Make request to application
            var response = await _client.GetAsync("/Account/Login");
            
            // Check if CSP header exists
            bool hasCspHeader = response.Headers.Contains("Content-Security-Policy");
            
            // Assert - This test FAILS because CSP header is missing on unfixed code
            // After fix: SecurityHeadersMiddleware should add CSP header to all responses
            Assert.True(hasCspHeader,
                "EXPECTED FAILURE: HTTP response lacks Content-Security-Policy header (no security headers middleware). " +
                "After fix, all responses should include CSP header with restrictive directives like: " +
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
                "font-src 'self'; connect-src 'self'; frame-ancestors 'none'");
            
            // If header exists, verify it has restrictive directives
            if (hasCspHeader)
            {
                var cspValue = response.Headers.GetValues("Content-Security-Policy").FirstOrDefault();
                Assert.NotNull(cspValue);
                Assert.Contains("default-src", cspValue);
                Assert.Contains("'self'", cspValue);
            }
        }

        /// <summary>
        /// Test 1.3.2: HTTP response lacks X-Frame-Options header
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Application does not include X-Frame-Options header in responses
        /// Expected Behavior: All HTTP responses should include X-Frame-Options header to prevent clickjacking
        /// Current Behavior: No X-Frame-Options header is set, allowing clickjacking attacks
        /// 
        /// Requirement 3.2: WHEN the application responds to HTTP requests THEN the system does not include 
        /// X-Frame-Options headers, allowing clickjacking attacks
        /// </summary>
        [Fact]
        public async Task HttpResponse_ShouldHaveXFrameOptionsHeader_ButDoesNot()
        {
            // Arrange & Act - Make request to application
            var response = await _client.GetAsync("/Account/Login");
            
            // Check if X-Frame-Options header exists
            bool hasXFrameOptionsHeader = response.Headers.Contains("X-Frame-Options");
            
            // Assert - This test FAILS because X-Frame-Options header is missing on unfixed code
            // After fix: SecurityHeadersMiddleware should add X-Frame-Options header to all responses
            Assert.True(hasXFrameOptionsHeader,
                "EXPECTED FAILURE: HTTP response lacks X-Frame-Options header (no security headers middleware). " +
                "After fix, all responses should include X-Frame-Options header with value 'DENY' or 'SAMEORIGIN' " +
                "to prevent clickjacking attacks.");
            
            // If header exists, verify it has correct value
            if (hasXFrameOptionsHeader)
            {
                var xFrameOptionsValue = response.Headers.GetValues("X-Frame-Options").FirstOrDefault();
                Assert.NotNull(xFrameOptionsValue);
                Assert.True(xFrameOptionsValue == "DENY" || xFrameOptionsValue == "SAMEORIGIN",
                    $"X-Frame-Options should be 'DENY' or 'SAMEORIGIN', but was '{xFrameOptionsValue}'");
            }
        }

        /// <summary>
        /// Test 1.3.3: HTTP response lacks X-Content-Type-Options header
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Application does not include X-Content-Type-Options header in responses
        /// Expected Behavior: All HTTP responses should include X-Content-Type-Options: nosniff
        /// Current Behavior: No X-Content-Type-Options header is set, allowing MIME-sniffing attacks
        /// 
        /// Requirement 3.3: WHEN the application responds to HTTP requests THEN the system does not include 
        /// X-Content-Type-Options headers, allowing MIME-sniffing attacks
        /// </summary>
        [Fact]
        public async Task HttpResponse_ShouldHaveXContentTypeOptionsHeader_ButDoesNot()
        {
            // Arrange & Act - Make request to application
            var response = await _client.GetAsync("/Account/Login");
            
            // Check if X-Content-Type-Options header exists
            bool hasXContentTypeOptionsHeader = response.Headers.Contains("X-Content-Type-Options");
            
            // Assert - This test FAILS because X-Content-Type-Options header is missing on unfixed code
            // After fix: SecurityHeadersMiddleware should add X-Content-Type-Options header to all responses
            Assert.True(hasXContentTypeOptionsHeader,
                "EXPECTED FAILURE: HTTP response lacks X-Content-Type-Options header (no security headers middleware). " +
                "After fix, all responses should include X-Content-Type-Options: nosniff to prevent MIME-sniffing attacks.");
            
            // If header exists, verify it has correct value
            if (hasXContentTypeOptionsHeader)
            {
                var xContentTypeOptionsValue = response.Headers.GetValues("X-Content-Type-Options").FirstOrDefault();
                Assert.Equal("nosniff", xContentTypeOptionsValue);
            }
        }

        /// <summary>
        /// Test 1.3.4: HTTPS response lacks HSTS header
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Application does not include Strict-Transport-Security (HSTS) header in HTTPS responses
        /// Expected Behavior: HTTPS responses should include HSTS header to enforce HTTPS connections
        /// Current Behavior: No HSTS header is set, allowing protocol downgrade attacks
        /// 
        /// Requirement 3.4: WHEN the application responds to HTTP requests THEN the system does not include 
        /// Strict-Transport-Security (HSTS) headers, allowing protocol downgrade attacks
        /// 
        /// Note: HSTS should only be set on HTTPS responses, not HTTP
        /// </summary>
        [Fact]
        public async Task HttpsResponse_ShouldHaveHstsHeader_ButDoesNot()
        {
            // Arrange & Act - Make request to application
            // Note: In test environment, this may be HTTP, but we're testing the middleware logic
            var response = await _client.GetAsync("/Account/Login");
            
            // Check if Strict-Transport-Security header exists
            bool hasHstsHeader = response.Headers.Contains("Strict-Transport-Security");
            
            // Assert - This test FAILS because HSTS header is missing on unfixed code
            // After fix: SecurityHeadersMiddleware should add HSTS header to HTTPS responses
            // Note: The test documents the expected behavior even if running over HTTP in test environment
            Assert.True(hasHstsHeader,
                "EXPECTED FAILURE: HTTPS response lacks Strict-Transport-Security (HSTS) header (no security headers middleware). " +
                "After fix, HTTPS responses should include HSTS header with value like: " +
                "max-age=31536000; includeSubDomains to enforce HTTPS connections and prevent protocol downgrade attacks.");
            
            // If header exists, verify it has correct value
            if (hasHstsHeader)
            {
                var hstsValue = response.Headers.GetValues("Strict-Transport-Security").FirstOrDefault();
                Assert.NotNull(hstsValue);
                Assert.Contains("max-age=", hstsValue);
            }
        }

        /// <summary>
        /// Test 1.3.5: HTTP response lacks Referrer-Policy header
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Application does not include Referrer-Policy header in responses
        /// Expected Behavior: All HTTP responses should include Referrer-Policy header
        /// Current Behavior: No Referrer-Policy header is set, potentially leaking sensitive URL information
        /// 
        /// Requirement 3.5: WHEN the application responds to HTTP requests THEN the system does not include 
        /// Referrer-Policy headers, potentially leaking sensitive URL information
        /// </summary>
        [Fact]
        public async Task HttpResponse_ShouldHaveReferrerPolicyHeader_ButDoesNot()
        {
            // Arrange & Act - Make request to application
            var response = await _client.GetAsync("/Account/Login");
            
            // Check if Referrer-Policy header exists
            bool hasReferrerPolicyHeader = response.Headers.Contains("Referrer-Policy");
            
            // Assert - This test FAILS because Referrer-Policy header is missing on unfixed code
            // After fix: SecurityHeadersMiddleware should add Referrer-Policy header to all responses
            Assert.True(hasReferrerPolicyHeader,
                "EXPECTED FAILURE: HTTP response lacks Referrer-Policy header (no security headers middleware). " +
                "After fix, all responses should include Referrer-Policy header with value like: " +
                "strict-origin-when-cross-origin to prevent leaking sensitive URL information in referrer headers.");
            
            // If header exists, verify it has correct value
            if (hasReferrerPolicyHeader)
            {
                var referrerPolicyValue = response.Headers.GetValues("Referrer-Policy").FirstOrDefault();
                Assert.NotNull(referrerPolicyValue);
                // Common secure values: strict-origin-when-cross-origin, strict-origin, no-referrer
                Assert.True(
                    referrerPolicyValue.Contains("strict-origin") || 
                    referrerPolicyValue.Contains("no-referrer"),
                    $"Referrer-Policy should be restrictive, but was '{referrerPolicyValue}'");
            }
        }

        /// <summary>
        /// Test 1.3.6: HTTP response lacks Permissions-Policy header
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Application does not include Permissions-Policy header in responses
        /// Expected Behavior: All HTTP responses should include Permissions-Policy header
        /// Current Behavior: No Permissions-Policy header is set, allowing unrestricted access to browser features
        /// 
        /// Requirement 3.6: WHEN the application responds to HTTP requests THEN the system does not include 
        /// Permissions-Policy headers, allowing unrestricted access to browser features
        /// </summary>
        [Fact]
        public async Task HttpResponse_ShouldHavePermissionsPolicyHeader_ButDoesNot()
        {
            // Arrange & Act - Make request to application
            var response = await _client.GetAsync("/Account/Login");
            
            // Check if Permissions-Policy header exists
            bool hasPermissionsPolicyHeader = response.Headers.Contains("Permissions-Policy");
            
            // Assert - This test FAILS because Permissions-Policy header is missing on unfixed code
            // After fix: SecurityHeadersMiddleware should add Permissions-Policy header to all responses
            Assert.True(hasPermissionsPolicyHeader,
                "EXPECTED FAILURE: HTTP response lacks Permissions-Policy header (no security headers middleware). " +
                "After fix, all responses should include Permissions-Policy header with restrictive feature permissions like: " +
                "geolocation=(), microphone=(), camera=() to control access to browser features.");
            
            // If header exists, verify it has restrictive directives
            if (hasPermissionsPolicyHeader)
            {
                var permissionsPolicyValue = response.Headers.GetValues("Permissions-Policy").FirstOrDefault();
                Assert.NotNull(permissionsPolicyValue);
                // Should contain restrictive policies for sensitive features
                Assert.True(
                    permissionsPolicyValue.Contains("geolocation=()") || 
                    permissionsPolicyValue.Contains("microphone=()") ||
                    permissionsPolicyValue.Contains("camera=()"),
                    $"Permissions-Policy should restrict sensitive features, but was '{permissionsPolicyValue}'");
            }
        }

        /// <summary>
        /// Test 1.3.7: Multiple endpoints lack security headers
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Verifies that security headers are missing across multiple application endpoints
        /// </summary>
        [Theory]
        [InlineData("/Account/Login")]
        [InlineData("/")]
        [InlineData("/health")]
        public async Task MultipleEndpoints_ShouldHaveSecurityHeaders_ButDoNot(string endpoint)
        {
            // Arrange & Act - Make request to various endpoints
            var response = await _client.GetAsync(endpoint);
            
            // Check for all security headers
            bool hasCsp = response.Headers.Contains("Content-Security-Policy");
            bool hasXFrameOptions = response.Headers.Contains("X-Frame-Options");
            bool hasXContentTypeOptions = response.Headers.Contains("X-Content-Type-Options");
            bool hasReferrerPolicy = response.Headers.Contains("Referrer-Policy");
            bool hasPermissionsPolicy = response.Headers.Contains("Permissions-Policy");
            
            bool hasAllSecurityHeaders = hasCsp && hasXFrameOptions && hasXContentTypeOptions && 
                                        hasReferrerPolicy && hasPermissionsPolicy;
            
            // Assert - This test FAILS because security headers are missing on unfixed code
            // After fix: All endpoints should have security headers
            Assert.True(hasAllSecurityHeaders,
                $"EXPECTED FAILURE: Endpoint '{endpoint}' lacks security headers (no security headers middleware). " +
                $"Missing headers: " +
                $"{(hasCsp ? "" : "Content-Security-Policy ")} " +
                $"{(hasXFrameOptions ? "" : "X-Frame-Options ")} " +
                $"{(hasXContentTypeOptions ? "" : "X-Content-Type-Options ")} " +
                $"{(hasReferrerPolicy ? "" : "Referrer-Policy ")} " +
                $"{(hasPermissionsPolicy ? "" : "Permissions-Policy")}. " +
                "After fix, SecurityHeadersMiddleware should add all security headers to all responses.");
        }

        /// <summary>
        /// Test 1.3.8: Verify no security headers middleware is configured
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// This test documents that the root cause is missing security headers middleware in Program.cs
        /// </summary>
        [Fact]
        public async Task Application_ShouldHaveSecurityHeadersMiddleware_ButDoesNot()
        {
            // Arrange & Act - Make request and check for any security header
            var response = await _client.GetAsync("/Account/Login");
            
            // Check if ANY security header exists (indicates middleware is present)
            bool hasAnyCspHeader = response.Headers.Contains("Content-Security-Policy");
            bool hasAnyXFrameOptions = response.Headers.Contains("X-Frame-Options");
            bool hasAnyXContentTypeOptions = response.Headers.Contains("X-Content-Type-Options");
            bool hasAnyReferrerPolicy = response.Headers.Contains("Referrer-Policy");
            bool hasAnyPermissionsPolicy = response.Headers.Contains("Permissions-Policy");
            
            bool hasAnySecurityHeader = hasAnyCspHeader || hasAnyXFrameOptions || 
                                       hasAnyXContentTypeOptions || hasAnyReferrerPolicy || 
                                       hasAnyPermissionsPolicy;
            
            // Assert - This test FAILS because no security headers middleware is configured
            // After fix: Program.cs should register SecurityHeadersMiddleware in the request pipeline
            Assert.True(hasAnySecurityHeader,
                "EXPECTED FAILURE: Application does not have security headers middleware configured in Program.cs. " +
                "After fix, Program.cs should register SecurityHeadersMiddleware that adds all required security headers " +
                "(Content-Security-Policy, X-Frame-Options, X-Content-Type-Options, Strict-Transport-Security, " +
                "Referrer-Policy, Permissions-Policy) to all HTTP responses.");
        }

        /// <summary>
        /// Test 1.3.9: Document counterexamples - Security headers completely absent
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// This test documents the complete absence of security headers as a counterexample
        /// </summary>
        [Fact]
        public async Task CounterExample_AllSecurityHeadersMissing()
        {
            // Arrange & Act - Make request to application
            var response = await _client.GetAsync("/Account/Login");
            
            // Document all missing security headers
            var missingHeaders = new System.Collections.Generic.List<string>();
            
            if (!response.Headers.Contains("Content-Security-Policy"))
                missingHeaders.Add("Content-Security-Policy");
            
            if (!response.Headers.Contains("X-Frame-Options"))
                missingHeaders.Add("X-Frame-Options");
            
            if (!response.Headers.Contains("X-Content-Type-Options"))
                missingHeaders.Add("X-Content-Type-Options");
            
            if (!response.Headers.Contains("Strict-Transport-Security"))
                missingHeaders.Add("Strict-Transport-Security");
            
            if (!response.Headers.Contains("Referrer-Policy"))
                missingHeaders.Add("Referrer-Policy");
            
            if (!response.Headers.Contains("Permissions-Policy"))
                missingHeaders.Add("Permissions-Policy");
            
            // Assert - This test FAILS and documents all missing headers as counterexamples
            Assert.Empty(missingHeaders);
            
            // If test fails, output will show: "Expected empty collection, but found: [list of missing headers]"
            // This documents the counterexamples proving the bugs exist
        }
    }
}
