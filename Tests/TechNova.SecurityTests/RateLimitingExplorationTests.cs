using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace TechNova.SecurityTests
{
    /// <summary>
    /// Bug Condition Exploration Tests for Rate Limiting (Task 1.7)
    /// 
    /// **Validates: Requirements 7.1, 7.2, 7.3**
    /// 
    /// CRITICAL: These tests are EXPECTED TO FAIL on unfixed code.
    /// Failure confirms the bugs exist. DO NOT attempt to fix the tests or code when they fail.
    /// 
    /// These tests encode the expected behavior - they will validate the fixes when they pass after implementation.
    /// </summary>
    public class RateLimitingExplorationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public RateLimitingExplorationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false // Don't follow redirects to test actual responses
            });
        }

        /// <summary>
        /// Test 1.7.1: 100 login attempts in 1 minute are all processed
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Application does not enforce rate limiting on login endpoint
        /// Expected Behavior: After 5 failed login attempts in 15 minutes, subsequent attempts should be rate limited
        /// Current Behavior: All login attempts are processed without rate limiting
        /// 
        /// Requirement 7.1: WHEN a user makes repeated login attempts THEN the system does not enforce 
        /// rate limiting on the /Account/Login endpoint
        /// </summary>
        [Fact]
        public async Task LoginAttempts_100InOneMinute_ShouldBeRateLimited_ButAreAllProcessed()
        {
            // Arrange
            int attemptCount = 100;
            var loginUrl = "/Account/Login";
            var successfulRequests = 0;
            var rateLimitedRequests = 0;

            // Act - Make 100 login attempts rapidly
            var tasks = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < attemptCount; i++)
            {
                // Create POST request with invalid credentials
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Email", $"test{i}@example.com"),
                    new KeyValuePair<string, string>("Password", "InvalidPassword123!")
                });
                
                tasks.Add(_client.PostAsync(loginUrl, content));
            }

            var responses = await Task.WhenAll(tasks);

            // Count successful (non-rate-limited) requests
            foreach (var response in responses)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests) // 429
                {
                    rateLimitedRequests++;
                }
                else
                {
                    successfulRequests++;
                }
            }

            // Assert - This test FAILS because all requests are processed (no rate limiting)
            // After fix: Rate limiting middleware should block requests after 5 attempts per 15 minutes
            // Expected: At most 5 successful requests, rest should be rate limited (429)
            Assert.True(successfulRequests <= 5,
                $"EXPECTED FAILURE: All {successfulRequests} out of {attemptCount} login attempts were processed " +
                $"without rate limiting (0 requests returned 429 Too Many Requests). " +
                "After fix, rate limiting middleware should enforce limit of 5 login attempts per 15 minutes per IP, " +
                "returning HTTP 429 for subsequent attempts.");

            // Document counterexample: No rate limiting is applied
            Assert.True(rateLimitedRequests > 0,
                $"COUNTEREXAMPLE: {successfulRequests} login attempts were all processed, " +
                $"{rateLimitedRequests} were rate limited. Expected most requests to be rate limited after 5 attempts.");
        }

        /// <summary>
        /// Test 1.7.2: 1000 API requests in 1 minute are all processed
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Application does not enforce rate limiting on API endpoints
        /// Expected Behavior: After 100 requests per minute, subsequent requests should be rate limited
        /// Current Behavior: All API requests are processed without rate limiting
        /// 
        /// Requirement 7.2: WHEN API endpoints are called repeatedly THEN the system does not enforce 
        /// rate limiting, allowing potential DoS attacks
        /// </summary>
        [Fact]
        public async Task ApiRequests_1000InOneMinute_ShouldBeRateLimited_ButAreAllProcessed()
        {
            // Arrange
            int requestCount = 1000;
            var apiUrl = "/"; // Test with home page or any public endpoint
            var successfulRequests = 0;
            var rateLimitedRequests = 0;

            // Act - Make 1000 API requests rapidly
            var tasks = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < requestCount; i++)
            {
                tasks.Add(_client.GetAsync(apiUrl));
            }

            var responses = await Task.WhenAll(tasks);

            // Count successful (non-rate-limited) requests
            foreach (var response in responses)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests) // 429
                {
                    rateLimitedRequests++;
                }
                else
                {
                    successfulRequests++;
                }
            }

            // Assert - This test FAILS because all requests are processed (no rate limiting)
            // After fix: Rate limiting middleware should block requests after 100 per minute
            // Expected: At most 100 successful requests, rest should be rate limited (429)
            Assert.True(successfulRequests <= 100,
                $"EXPECTED FAILURE: All {successfulRequests} out of {requestCount} API requests were processed " +
                $"without rate limiting (0 requests returned 429 Too Many Requests). " +
                "After fix, rate limiting middleware should enforce limit of 100 requests per minute per user/IP, " +
                "returning HTTP 429 for subsequent requests.");

            // Document counterexample: No rate limiting is applied
            Assert.True(rateLimitedRequests > 0,
                $"COUNTEREXAMPLE: {successfulRequests} API requests were all processed, " +
                $"{rateLimitedRequests} were rate limited. Expected most requests to be rate limited after 100 requests.");
        }

        /// <summary>
        /// Test 1.7.3: No 429 response is returned for excessive requests
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Application never returns HTTP 429 (Too Many Requests) status code
        /// Expected Behavior: When rate limits are exceeded, server should return 429 with Retry-After header
        /// Current Behavior: All requests return 200, 302, 400, etc. but never 429
        /// 
        /// Requirement 7.3: WHEN password reset requests are made THEN the system does not limit 
        /// the number of requests per time period
        /// </summary>
        [Fact]
        public async Task ExcessiveRequests_ShouldReturn429_ButNeverDo()
        {
            // Arrange
            int requestCount = 50;
            var loginUrl = "/Account/Login";
            var has429Response = false;

            // Act - Make many rapid requests to trigger rate limiting
            var tasks = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < requestCount; i++)
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Email", "attacker@example.com"),
                    new KeyValuePair<string, string>("Password", "BruteForceAttempt123!")
                });
                
                tasks.Add(_client.PostAsync(loginUrl, content));
            }

            var responses = await Task.WhenAll(tasks);

            // Check if any response is 429 Too Many Requests
            has429Response = responses.Any(r => r.StatusCode == HttpStatusCode.TooManyRequests);

            // Assert - This test FAILS because no 429 response is ever returned
            // After fix: Rate limiting middleware should return 429 when limits are exceeded
            Assert.True(has429Response,
                $"EXPECTED FAILURE: Made {requestCount} rapid requests but no 429 (Too Many Requests) response was returned. " +
                "Status codes received: " + string.Join(", ", responses.Select(r => (int)r.StatusCode).Distinct()) + ". " +
                "After fix, rate limiting middleware should return HTTP 429 with Retry-After header when rate limits are exceeded.");
        }

        /// <summary>
        /// Test 1.7.4: Password reset requests are not rate limited
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Password reset endpoint does not enforce rate limiting
        /// Expected Behavior: After 3 password reset requests per hour per email, subsequent requests should be rate limited
        /// Current Behavior: All password reset requests are processed without rate limiting
        /// 
        /// Requirement 7.3: WHEN password reset requests are made THEN the system does not limit 
        /// the number of requests per time period
        /// </summary>
        [Fact]
        public async Task PasswordResetRequests_ShouldBeRateLimited_ButAreNot()
        {
            // Arrange
            int requestCount = 20;
            var resetUrl = "/Account/ForgotPassword";
            var successfulRequests = 0;
            var rateLimitedRequests = 0;

            // Act - Make 20 password reset requests rapidly for the same email
            var tasks = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < requestCount; i++)
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Email", "victim@technova.com")
                });
                
                tasks.Add(_client.PostAsync(resetUrl, content));
            }

            var responses = await Task.WhenAll(tasks);

            // Count successful (non-rate-limited) requests
            foreach (var response in responses)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests) // 429
                {
                    rateLimitedRequests++;
                }
                else
                {
                    successfulRequests++;
                }
            }

            // Assert - This test FAILS because all requests are processed (no rate limiting)
            // After fix: Rate limiting middleware should block requests after 3 per hour per email
            // Expected: At most 3 successful requests, rest should be rate limited (429)
            Assert.True(successfulRequests <= 3,
                $"EXPECTED FAILURE: All {successfulRequests} out of {requestCount} password reset requests were processed " +
                $"without rate limiting (0 requests returned 429 Too Many Requests). " +
                "After fix, rate limiting middleware should enforce limit of 3 password reset requests per hour per email, " +
                "returning HTTP 429 for subsequent requests.");

            // Document counterexample: No rate limiting is applied
            Assert.True(rateLimitedRequests > 0,
                $"COUNTEREXAMPLE: {successfulRequests} password reset requests were all processed, " +
                $"{rateLimitedRequests} were rate limited. Expected most requests to be rate limited after 3 attempts.");
        }

        /// <summary>
        /// Test 1.7.5: Verify no rate limiting middleware is configured
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// This test documents that the root cause is missing rate limiting middleware in Program.cs
        /// </summary>
        [Fact]
        public async Task Application_ShouldHaveRateLimitingMiddleware_ButDoesNot()
        {
            // Arrange
            int rapidRequestCount = 30;
            var testUrl = "/Account/Login";
            var any429Response = false;

            // Act - Make rapid requests that should trigger rate limiting if middleware exists
            var tasks = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < rapidRequestCount; i++)
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Email", "test@example.com"),
                    new KeyValuePair<string, string>("Password", "test")
                });
                
                tasks.Add(_client.PostAsync(testUrl, content));
            }

            var responses = await Task.WhenAll(tasks);

            // Check if any response indicates rate limiting
            any429Response = responses.Any(r => r.StatusCode == HttpStatusCode.TooManyRequests);

            // Assert - This test FAILS because no rate limiting middleware is configured
            // After fix: Program.cs should register rate limiting middleware (e.g., AspNetCoreRateLimit)
            Assert.True(any429Response,
                "EXPECTED FAILURE: Application does not have rate limiting middleware configured in Program.cs. " +
                $"Made {rapidRequestCount} rapid requests and none returned 429 (Too Many Requests). " +
                "After fix, Program.cs should register rate limiting middleware (e.g., AspNetCoreRateLimit or built-in .NET rate limiting) " +
                "that enforces limits on login attempts (5 per 15 min), API requests (100 per min), and password resets (3 per hour).");
        }

        /// <summary>
        /// Test 1.7.6: Document counterexamples - Complete absence of rate limiting
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// This test documents the complete absence of rate limiting as a counterexample
        /// </summary>
        [Fact]
        public async Task CounterExample_NoRateLimitingOnAnyEndpoint()
        {
            // Arrange - Test multiple endpoints
            var endpoints = new[]
            {
                ("/Account/Login", "POST", new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Email", "test@example.com"),
                    new KeyValuePair<string, string>("Password", "test")
                })),
                ("/", "GET", null),
                ("/Account/ForgotPassword", "POST", new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Email", "test@example.com")
                }))
            };

            var endpointsWithRateLimiting = new List<string>();
            var endpointsWithoutRateLimiting = new List<string>();

            // Act - Test each endpoint with rapid requests
            foreach (var (url, method, content) in endpoints)
            {
                var tasks = new List<Task<HttpResponseMessage>>();
                for (int i = 0; i < 20; i++)
                {
                    if (method == "POST" && content != null)
                    {
                        tasks.Add(_client.PostAsync(url, content));
                    }
                    else
                    {
                        tasks.Add(_client.GetAsync(url));
                    }
                }

                var responses = await Task.WhenAll(tasks);
                var has429 = responses.Any(r => r.StatusCode == HttpStatusCode.TooManyRequests);

                if (has429)
                {
                    endpointsWithRateLimiting.Add(url);
                }
                else
                {
                    endpointsWithoutRateLimiting.Add(url);
                }
            }

            // Assert - This test FAILS and documents all endpoints without rate limiting as counterexamples
            Assert.Empty(endpointsWithoutRateLimiting);
            
            // If test fails, output will show: "Expected empty collection, but found: [list of endpoints without rate limiting]"
            // This documents the counterexamples proving the bugs exist
        }

        /// <summary>
        /// Test 1.7.7: Retry-After header should be present in 429 responses
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Even if 429 is returned, Retry-After header is missing
        /// Expected Behavior: 429 responses should include Retry-After header indicating when to retry
        /// Current Behavior: No 429 responses are returned, so no Retry-After header exists
        /// </summary>
        [Fact]
        public async Task RateLimited429Response_ShouldIncludeRetryAfterHeader_ButDoesNot()
        {
            // Arrange
            int requestCount = 50;
            var loginUrl = "/Account/Login";
            var has429WithRetryAfter = false;

            // Act - Make many rapid requests to trigger rate limiting
            var tasks = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < requestCount; i++)
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Email", "test@example.com"),
                    new KeyValuePair<string, string>("Password", "test")
                });
                
                tasks.Add(_client.PostAsync(loginUrl, content));
            }

            var responses = await Task.WhenAll(tasks);

            // Check if any 429 response has Retry-After header
            var rateLimitedResponses = responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            if (rateLimitedResponses.Any())
            {
                has429WithRetryAfter = rateLimitedResponses.Any(r => r.Headers.Contains("Retry-After"));
            }

            // Assert - This test FAILS because no 429 responses exist, or they lack Retry-After header
            // After fix: Rate limiting middleware should include Retry-After header in 429 responses
            Assert.True(has429WithRetryAfter,
                $"EXPECTED FAILURE: Made {requestCount} rapid requests. " +
                $"Found {rateLimitedResponses.Count()} responses with status 429, " +
                $"but none included Retry-After header. " +
                "After fix, rate limiting middleware should return HTTP 429 with Retry-After header " +
                "indicating when the client can retry the request.");
        }
    }
}
