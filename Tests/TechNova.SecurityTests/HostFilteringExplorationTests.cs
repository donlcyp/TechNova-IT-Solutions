using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace TechNova.SecurityTests
{
    /// <summary>
    /// Bug Condition Exploration Tests for Host Filtering (Task 1.5)
    /// 
    /// **Validates: Requirements 5.1, 5.2**
    /// 
    /// CRITICAL: These tests are EXPECTED TO FAIL on unfixed code.
    /// Failure confirms the bugs exist. DO NOT attempt to fix the tests or code when they fail.
    /// 
    /// These tests encode the expected behavior - they will validate the fixes when they pass after implementation.
    /// </summary>
    public class HostFilteringExplorationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public HostFilteringExplorationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Test 1.5.1: Request with malicious Host header (evil.com) is accepted
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Application accepts requests with arbitrary host headers because AllowedHosts is set to "*"
        /// Expected Behavior: Only requests with allowed host headers should be accepted
        /// Current Behavior: All host headers are accepted, allowing host header injection attacks
        /// 
        /// Requirement 5.1: WHEN AllowedHosts is set to "*" in appsettings.json THEN the system disables 
        /// host header validation, allowing host header injection attacks
        /// </summary>
        [Fact]
        public async Task Request_WithMaliciousHostHeader_ShouldBeRejected_ButIsAccepted()
        {
            // Arrange - Create client and prepare request with malicious host header
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var request = new HttpRequestMessage(HttpMethod.Get, "/Account/Login");
            request.Headers.Host = "evil.com";

            // Act - Send request with malicious host header
            var response = await client.SendAsync(request);

            // Assert - This test FAILS because malicious host header is accepted on unfixed code
            // After fix: AllowedHosts should be set to specific hostnames, and requests with 
            // non-allowed hosts should be rejected with 400 Bad Request
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest,
                $"EXPECTED FAILURE: Request with Host: evil.com was accepted with status {response.StatusCode} " +
                "(AllowedHosts is set to '*' in appsettings.json). " +
                "After fix, AllowedHosts should be set to specific hostnames like 'technova.com;www.technova.com;localhost;127.0.0.1', " +
                "and requests with non-allowed host headers should be rejected with 400 Bad Request. " +
                "This prevents host header injection attacks, cache poisoning, and password reset poisoning.");
        }

        /// <summary>
        /// Test 1.5.2: Request with attacker-controlled Host header (attacker.com) is accepted
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Application accepts requests with arbitrary host headers
        /// Expected Behavior: Only requests with whitelisted host headers should be accepted
        /// Current Behavior: All host headers are accepted, enabling various attacks
        /// 
        /// Requirement 5.1: WHEN an attacker sends requests with malicious host headers THEN the system 
        /// accepts and processes them, potentially enabling cache poisoning and password reset poisoning
        /// </summary>
        [Fact]
        public async Task Request_WithAttackerHostHeader_ShouldBeRejected_ButIsAccepted()
        {
            // Arrange - Create client and prepare request with attacker-controlled host header
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Host = "attacker.com";

            // Act - Send request with attacker-controlled host header
            var response = await client.SendAsync(request);

            // Assert - This test FAILS because attacker host header is accepted on unfixed code
            // After fix: Host filtering middleware should reject non-whitelisted hosts
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest,
                $"EXPECTED FAILURE: Request with Host: attacker.com was accepted with status {response.StatusCode} " +
                "(AllowedHosts is set to '*' in appsettings.json). " +
                "After fix, host filtering middleware should validate the Host header against the AllowedHosts whitelist " +
                "and reject requests with non-allowed hosts with 400 Bad Request.");
        }

        /// <summary>
        /// Test 1.5.3: AllowedHosts configuration is set to "*" (wildcard)
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: AllowedHosts is set to "*" in appsettings.json, disabling host filtering
        /// Expected Behavior: AllowedHosts should be set to specific hostnames
        /// Current Behavior: AllowedHosts is "*", allowing all host headers
        /// 
        /// Requirement 5.2: WHEN AllowedHosts is set to "*" in appsettings.json THEN the system 
        /// disables host header validation
        /// </summary>
        [Fact]
        public void Configuration_AllowedHosts_ShouldBeSpecific_ButIsWildcard()
        {
            // Arrange - Get configuration from the application
            var configuration = _factory.Services.GetService(typeof(IConfiguration)) as IConfiguration;
            Assert.NotNull(configuration);

            // Act - Read AllowedHosts configuration value
            var allowedHosts = configuration["AllowedHosts"];

            // Assert - This test FAILS because AllowedHosts is set to "*" on unfixed code
            // After fix: AllowedHosts should be set to specific hostnames
            Assert.True(allowedHosts != "*",
                $"EXPECTED FAILURE: AllowedHosts is set to '*' in appsettings.json (disables host filtering). " +
                "After fix, AllowedHosts should be set to specific hostnames like: " +
                "'technova.com;www.technova.com;localhost;127.0.0.1' (semicolon-separated list). " +
                "Use environment-specific configuration: Development includes localhost, Production only production domains. " +
                $"Current value: '{allowedHosts}'");
        }

        /// <summary>
        /// Test 1.5.4: Multiple malicious host headers are accepted
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Verifies that various malicious host headers are accepted across different endpoints
        /// </summary>
        [Theory]
        [InlineData("evil.com", "/Account/Login")]
        [InlineData("phishing-site.com", "/")]
        [InlineData("malicious-domain.net", "/health")]
        [InlineData("attacker-controlled.org", "/Account/Login")]
        public async Task MultipleEndpoints_WithMaliciousHosts_ShouldBeRejected_ButAreAccepted(string maliciousHost, string endpoint)
        {
            // Arrange - Create client and prepare request with malicious host header
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Host = maliciousHost;

            // Act - Send request with malicious host header
            var response = await client.SendAsync(request);

            // Assert - This test FAILS because malicious host headers are accepted on unfixed code
            // After fix: All endpoints should reject non-whitelisted host headers
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest,
                $"EXPECTED FAILURE: Request to '{endpoint}' with Host: {maliciousHost} was accepted with status {response.StatusCode} " +
                "(AllowedHosts is set to '*' in appsettings.json). " +
                "After fix, all endpoints should reject non-whitelisted host headers with 400 Bad Request.");
        }

        /// <summary>
        /// Test 1.5.5: Host header injection enables cache poisoning attack
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Accepting arbitrary host headers can enable cache poisoning attacks
        /// Expected Behavior: Only whitelisted host headers should be accepted
        /// Current Behavior: Arbitrary host headers are accepted, potentially poisoning caches
        /// 
        /// Requirement 5.1: Host header injection can enable cache poisoning attacks
        /// </summary>
        [Fact]
        public async Task HostHeaderInjection_EnablesCachePoisoning_ShouldBePrevented()
        {
            // Arrange - Create client and prepare request with cache poisoning attempt
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var request = new HttpRequestMessage(HttpMethod.Get, "/");
            // Attacker tries to poison cache by injecting malicious host header
            request.Headers.Host = "cache-poison-attack.com";

            // Act - Send request with cache poisoning attempt
            var response = await client.SendAsync(request);

            // Assert - This test FAILS because cache poisoning attempt is not blocked on unfixed code
            // After fix: Host filtering should prevent cache poisoning by rejecting non-whitelisted hosts
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest,
                $"EXPECTED FAILURE: Cache poisoning attempt with Host: cache-poison-attack.com was not blocked (status {response.StatusCode}). " +
                "AllowedHosts is set to '*' in appsettings.json, allowing host header injection attacks. " +
                "After fix, host filtering middleware should reject non-whitelisted hosts to prevent cache poisoning attacks.");
        }

        /// <summary>
        /// Test 1.5.6: Host header injection enables password reset poisoning attack
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Accepting arbitrary host headers can enable password reset poisoning
        /// Expected Behavior: Only whitelisted host headers should be accepted
        /// Current Behavior: Arbitrary host headers are accepted, potentially enabling password reset poisoning
        /// 
        /// Requirement 5.1: Host header injection can enable password reset poisoning attacks
        /// </summary>
        [Fact]
        public async Task HostHeaderInjection_EnablesPasswordResetPoisoning_ShouldBePrevented()
        {
            // Arrange - Create client and prepare request with password reset poisoning attempt
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var request = new HttpRequestMessage(HttpMethod.Get, "/Account/Login");
            // Attacker tries to poison password reset emails by injecting malicious host header
            // This could cause password reset links to point to attacker's domain
            request.Headers.Host = "password-reset-poison.com";

            // Act - Send request with password reset poisoning attempt
            var response = await client.SendAsync(request);

            // Assert - This test FAILS because password reset poisoning attempt is not blocked on unfixed code
            // After fix: Host filtering should prevent password reset poisoning by rejecting non-whitelisted hosts
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest,
                $"EXPECTED FAILURE: Password reset poisoning attempt with Host: password-reset-poison.com was not blocked (status {response.StatusCode}). " +
                "AllowedHosts is set to '*' in appsettings.json, allowing host header injection attacks. " +
                "After fix, host filtering middleware should reject non-whitelisted hosts to prevent password reset poisoning attacks " +
                "where password reset links could point to attacker-controlled domains.");
        }

        /// <summary>
        /// Test 1.5.7: Verify host filtering middleware is not configured
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// This test documents that the root cause is AllowedHosts set to "*" in appsettings.json
        /// and potentially missing host filtering middleware configuration in Program.cs
        /// </summary>
        [Fact]
        public async Task Application_ShouldHaveHostFiltering_ButDoesNot()
        {
            // Arrange - Create client and prepare request with obviously malicious host
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Host = "definitely-not-allowed-host.com";

            // Act - Send request with obviously malicious host header
            var response = await client.SendAsync(request);

            // Assert - This test FAILS because host filtering is not properly configured
            // After fix: Program.cs should ensure UseHostFiltering() is called and AllowedHosts is properly configured
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest,
                $"EXPECTED FAILURE: Request with obviously malicious Host: definitely-not-allowed-host.com was accepted (status {response.StatusCode}). " +
                "Root cause: AllowedHosts is set to '*' in appsettings.json, disabling host header validation. " +
                "After fix: " +
                "1. Update appsettings.json to set AllowedHosts to specific hostnames (e.g., 'technova.com;www.technova.com;localhost;127.0.0.1') " +
                "2. Ensure UseHostFiltering() is called in Program.cs middleware pipeline " +
                "3. Configure forwarded headers middleware if behind proxy/load balancer " +
                "4. Use environment-specific configuration (Development includes localhost, Production only production domains)");
        }

        /// <summary>
        /// Test 1.5.8: Document counterexamples - AllowedHosts wildcard and accepted malicious hosts
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// This test documents the counterexamples proving the host filtering bugs exist
        /// </summary>
        [Fact]
        public async Task CounterExample_AllowedHostsWildcardAndMaliciousHostsAccepted()
        {
            // Counterexample 1: AllowedHosts is set to "*"
            var configuration = _factory.Services.GetService(typeof(IConfiguration)) as IConfiguration;
            Assert.NotNull(configuration);
            var allowedHosts = configuration["AllowedHosts"];
            
            Assert.True(allowedHosts != "*",
                $"COUNTEREXAMPLE 1: AllowedHosts is set to '*' in appsettings.json, disabling host filtering. " +
                $"Current value: '{allowedHosts}'");

            // Counterexample 2: Malicious host headers are accepted
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var maliciousHosts = new[] { "evil.com", "attacker.com", "phishing-site.com" };
            var acceptedMaliciousHosts = new System.Collections.Generic.List<string>();

            foreach (var maliciousHost in maliciousHosts)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/health");
                request.Headers.Host = maliciousHost;
                var response = await client.SendAsync(request);

                if (response.StatusCode != HttpStatusCode.BadRequest)
                {
                    acceptedMaliciousHosts.Add($"{maliciousHost} (status: {response.StatusCode})");
                }
            }

            Assert.Empty(acceptedMaliciousHosts);
            
            // If test fails, output will show: "Expected empty collection, but found: [list of accepted malicious hosts]"
            // This documents the counterexamples proving the host filtering bugs exist
        }
    }
}
