using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Net.Http.Headers;

namespace TechNova.SecurityTests
{
    /// <summary>
    /// Bug Condition Exploration Tests for Session Cookie Security (Task 1.10)
    /// 
    /// **Validates: Requirements 10.1, 10.2, 10.3**
    /// 
    /// CRITICAL: These tests are EXPECTED TO FAIL on unfixed code.
    /// Failure confirms the bugs exist. DO NOT attempt to fix the tests or code when they fail.
    /// 
    /// These tests encode the expected behavior - they will validate the fixes when they pass after implementation.
    /// </summary>
    public class SessionCookieSecurityExplorationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public SessionCookieSecurityExplorationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false, // Don't follow redirects to test actual responses
                HandleCookies = false // Handle cookies manually to inspect them
            });
        }

        /// <summary>
        /// Test 1.10.1: Session cookie lacks Secure flag
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Session cookies are created without the Secure flag
        /// Expected Behavior: Session cookies should have Secure flag set to true
        /// Current Behavior: Secure flag is not set, allowing transmission over unencrypted HTTP
        /// 
        /// Requirement 10.1: WHEN session cookies are created THEN the system does not set the Secure flag, 
        /// allowing transmission over unencrypted HTTP connections
        /// </summary>
        [Fact]
        public async Task SessionCookie_ShouldHaveSecureFlag_ButDoesNot()
        {
            // Arrange & Act - Make request that creates a session
            var response = await _client.GetAsync("/Account/Login");
            
            // Check if Set-Cookie header exists
            bool hasSetCookieHeader = response.Headers.Contains("Set-Cookie");
            
            if (!hasSetCookieHeader)
            {
                // No session cookie set yet, try to trigger session creation
                // by accessing an endpoint that uses session
                response = await _client.GetAsync("/");
            }
            
            // Get all Set-Cookie headers
            var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
            
            // Find session cookie (ASP.NET Core session cookie is typically named ".AspNetCore.Session")
            var sessionCookie = setCookieHeaders.FirstOrDefault(c => 
                c.Contains(".AspNetCore.Session") || c.Contains("session"));
            
            bool hasSecureFlag = false;
            if (sessionCookie != null)
            {
                // Check if cookie has Secure flag
                hasSecureFlag = sessionCookie.Contains("secure", System.StringComparison.OrdinalIgnoreCase);
            }
            
            // Assert - This test FAILS because Secure flag is missing on unfixed code
            // After fix: Program.cs should configure session cookies with SecurePolicy = CookieSecurePolicy.Always
            Assert.True(hasSecureFlag,
                "EXPECTED FAILURE: Session cookie lacks Secure flag (session configuration in Program.cs does not set SecurePolicy). " +
                "After fix, Program.cs should configure session cookies with: " +
                "options.Cookie.SecurePolicy = CookieSecurePolicy.Always; " +
                "This ensures session cookies are only transmitted over HTTPS, preventing session hijacking over unencrypted connections.");
        }

        /// <summary>
        /// Test 1.10.2: Session cookie lacks SameSite attribute
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Session cookies are created without the SameSite attribute
        /// Expected Behavior: Session cookies should have SameSite attribute set to Lax or Strict
        /// Current Behavior: SameSite attribute is not set, allowing CSRF attacks
        /// 
        /// Requirement 10.2: WHEN session cookies are created THEN the system does not set the SameSite attribute 
        /// to Strict or Lax, allowing CSRF attacks
        /// </summary>
        [Fact]
        public async Task SessionCookie_ShouldHaveSameSiteAttribute_ButDoesNot()
        {
            // Arrange & Act - Make request that creates a session
            var response = await _client.GetAsync("/Account/Login");
            
            // Check if Set-Cookie header exists
            bool hasSetCookieHeader = response.Headers.Contains("Set-Cookie");
            
            if (!hasSetCookieHeader)
            {
                // No session cookie set yet, try to trigger session creation
                response = await _client.GetAsync("/");
            }
            
            // Get all Set-Cookie headers
            var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
            
            // Find session cookie
            var sessionCookie = setCookieHeaders.FirstOrDefault(c => 
                c.Contains(".AspNetCore.Session") || c.Contains("session"));
            
            bool hasSameSiteAttribute = false;
            bool hasSameSiteLaxOrStrict = false;
            
            if (sessionCookie != null)
            {
                // Check if cookie has SameSite attribute
                hasSameSiteAttribute = sessionCookie.Contains("samesite", System.StringComparison.OrdinalIgnoreCase);
                
                if (hasSameSiteAttribute)
                {
                    // Check if SameSite is set to Lax or Strict (not None)
                    hasSameSiteLaxOrStrict = 
                        sessionCookie.Contains("samesite=lax", System.StringComparison.OrdinalIgnoreCase) ||
                        sessionCookie.Contains("samesite=strict", System.StringComparison.OrdinalIgnoreCase);
                }
            }
            
            // Assert - This test FAILS because SameSite attribute is missing or set to None on unfixed code
            // After fix: Program.cs should configure session cookies with SameSite = SameSiteMode.Lax
            Assert.True(hasSameSiteLaxOrStrict,
                "EXPECTED FAILURE: Session cookie lacks SameSite attribute or is set to None (session configuration in Program.cs does not set SameSite). " +
                "After fix, Program.cs should configure session cookies with: " +
                "options.Cookie.SameSite = SameSiteMode.Lax; " +
                "This prevents CSRF attacks by ensuring cookies are only sent in first-party contexts or safe cross-site requests.");
        }

        /// <summary>
        /// Test 1.10.3: Session cookie configuration lacks both Secure and SameSite
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Session cookies only have HttpOnly set but lack Secure and SameSite
        /// Expected Behavior: Session cookies should have HttpOnly, Secure, and SameSite all configured
        /// Current Behavior: Only HttpOnly is set, missing defense-in-depth security attributes
        /// 
        /// Requirement 10.3: WHEN session cookies are configured THEN the system only sets HttpOnly to true 
        /// but lacks other security attributes
        /// </summary>
        [Fact]
        public async Task SessionCookie_ShouldHaveAllSecurityAttributes_ButOnlyHasHttpOnly()
        {
            // Arrange & Act - Make request that creates a session
            var response = await _client.GetAsync("/Account/Login");
            
            // Check if Set-Cookie header exists
            bool hasSetCookieHeader = response.Headers.Contains("Set-Cookie");
            
            if (!hasSetCookieHeader)
            {
                // No session cookie set yet, try to trigger session creation
                response = await _client.GetAsync("/");
            }
            
            // Get all Set-Cookie headers
            var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
            
            // Find session cookie
            var sessionCookie = setCookieHeaders.FirstOrDefault(c => 
                c.Contains(".AspNetCore.Session") || c.Contains("session"));
            
            bool hasHttpOnly = false;
            bool hasSecure = false;
            bool hasSameSiteLaxOrStrict = false;
            
            if (sessionCookie != null)
            {
                // Check for HttpOnly (should already be set)
                hasHttpOnly = sessionCookie.Contains("httponly", System.StringComparison.OrdinalIgnoreCase);
                
                // Check for Secure flag
                hasSecure = sessionCookie.Contains("secure", System.StringComparison.OrdinalIgnoreCase);
                
                // Check for SameSite attribute
                hasSameSiteLaxOrStrict = 
                    sessionCookie.Contains("samesite=lax", System.StringComparison.OrdinalIgnoreCase) ||
                    sessionCookie.Contains("samesite=strict", System.StringComparison.OrdinalIgnoreCase);
            }
            
            bool hasAllSecurityAttributes = hasHttpOnly && hasSecure && hasSameSiteLaxOrStrict;
            
            // Assert - This test FAILS because only HttpOnly is set on unfixed code
            // After fix: Program.cs should configure all three security attributes
            Assert.True(hasAllSecurityAttributes,
                $"EXPECTED FAILURE: Session cookie lacks complete security configuration. " +
                $"Current state: HttpOnly={hasHttpOnly}, Secure={hasSecure}, SameSite(Lax/Strict)={hasSameSiteLaxOrStrict}. " +
                $"After fix, Program.cs should configure session cookies with all security attributes: " +
                $"options.Cookie.HttpOnly = true; " +
                $"options.Cookie.SecurePolicy = CookieSecurePolicy.Always; " +
                $"options.Cookie.SameSite = SameSiteMode.Lax; " +
                $"This provides defense-in-depth protection against XSS, session hijacking, and CSRF attacks.");
        }

        /// <summary>
        /// Test 1.10.4: Verify session configuration in Program.cs is incomplete
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// This test documents that the root cause is incomplete session cookie configuration in Program.cs
        /// </summary>
        [Fact]
        public async Task Application_ShouldHaveSecureSessionConfiguration_ButDoesNot()
        {
            // Arrange & Act - Make request and check session cookie configuration
            var response = await _client.GetAsync("/Account/Login");
            
            // Check if Set-Cookie header exists
            bool hasSetCookieHeader = response.Headers.Contains("Set-Cookie");
            
            if (!hasSetCookieHeader)
            {
                // No session cookie set yet, try to trigger session creation
                response = await _client.GetAsync("/");
            }
            
            // Get all Set-Cookie headers
            var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
            
            // Find session cookie
            var sessionCookie = setCookieHeaders.FirstOrDefault(c => 
                c.Contains(".AspNetCore.Session") || c.Contains("session"));
            
            bool hasSecureConfiguration = false;
            
            if (sessionCookie != null)
            {
                // Check if cookie has both Secure and SameSite configured
                bool hasSecure = sessionCookie.Contains("secure", System.StringComparison.OrdinalIgnoreCase);
                bool hasSameSite = 
                    sessionCookie.Contains("samesite=lax", System.StringComparison.OrdinalIgnoreCase) ||
                    sessionCookie.Contains("samesite=strict", System.StringComparison.OrdinalIgnoreCase);
                
                hasSecureConfiguration = hasSecure && hasSameSite;
            }
            
            // Assert - This test FAILS because session configuration is incomplete in Program.cs
            // After fix: Program.cs should have complete session cookie security configuration
            Assert.True(hasSecureConfiguration,
                "EXPECTED FAILURE: Application does not have secure session cookie configuration in Program.cs. " +
                "Current configuration only sets HttpOnly and IdleTimeout. " +
                "After fix, Program.cs should configure session cookies with: " +
                "builder.Services.AddSession(options => { " +
                "    options.Cookie.HttpOnly = true; " +
                "    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; " +
                "    options.Cookie.SameSite = SameSiteMode.Lax; " +
                "    options.Cookie.IsEssential = true; " +
                "    options.IdleTimeout = TimeSpan.FromMinutes(30); " +
                "});");
        }

        /// <summary>
        /// Test 1.10.5: Document counterexamples - Session cookie security attributes missing
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// This test documents the missing security attributes as counterexamples
        /// </summary>
        [Fact]
        public async Task CounterExample_SessionCookieMissingSecurityAttributes()
        {
            // Arrange & Act - Make request that creates a session
            var response = await _client.GetAsync("/Account/Login");
            
            // Check if Set-Cookie header exists
            bool hasSetCookieHeader = response.Headers.Contains("Set-Cookie");
            
            if (!hasSetCookieHeader)
            {
                // No session cookie set yet, try to trigger session creation
                response = await _client.GetAsync("/");
            }
            
            // Get all Set-Cookie headers
            var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
            
            // Find session cookie
            var sessionCookie = setCookieHeaders.FirstOrDefault(c => 
                c.Contains(".AspNetCore.Session") || c.Contains("session"));
            
            // Document all missing security attributes
            var missingAttributes = new System.Collections.Generic.List<string>();
            
            if (sessionCookie != null)
            {
                if (!sessionCookie.Contains("secure", System.StringComparison.OrdinalIgnoreCase))
                    missingAttributes.Add("Secure flag");
                
                bool hasSameSiteLaxOrStrict = 
                    sessionCookie.Contains("samesite=lax", System.StringComparison.OrdinalIgnoreCase) ||
                    sessionCookie.Contains("samesite=strict", System.StringComparison.OrdinalIgnoreCase);
                
                if (!hasSameSiteLaxOrStrict)
                    missingAttributes.Add("SameSite attribute (Lax or Strict)");
            }
            else
            {
                missingAttributes.Add("Session cookie not found in response");
            }
            
            // Assert - This test FAILS and documents all missing attributes as counterexamples
            Assert.Empty(missingAttributes);
            
            // If test fails, output will show: "Expected empty collection, but found: [list of missing attributes]"
            // This documents the counterexamples proving the bugs exist
        }

        /// <summary>
        /// Test 1.10.6: Session cookie allows transmission over HTTP
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Without Secure flag, session cookies can be transmitted over HTTP
        /// Expected Behavior: Session cookies should only be transmitted over HTTPS
        /// Current Behavior: Cookies can be sent over unencrypted HTTP, enabling session hijacking
        /// </summary>
        [Fact]
        public async Task SessionCookie_ShouldOnlyTransmitOverHttps_ButCanTransmitOverHttp()
        {
            // Arrange & Act - Make request that creates a session
            var response = await _client.GetAsync("/Account/Login");
            
            // Check if Set-Cookie header exists
            bool hasSetCookieHeader = response.Headers.Contains("Set-Cookie");
            
            if (!hasSetCookieHeader)
            {
                // No session cookie set yet, try to trigger session creation
                response = await _client.GetAsync("/");
            }
            
            // Get all Set-Cookie headers
            var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
            
            // Find session cookie
            var sessionCookie = setCookieHeaders.FirstOrDefault(c => 
                c.Contains(".AspNetCore.Session") || c.Contains("session"));
            
            bool isHttpsOnly = false;
            
            if (sessionCookie != null)
            {
                // Cookie is HTTPS-only if it has the Secure flag
                isHttpsOnly = sessionCookie.Contains("secure", System.StringComparison.OrdinalIgnoreCase);
            }
            
            // Assert - This test FAILS because cookie can be transmitted over HTTP on unfixed code
            // After fix: Secure flag should prevent transmission over HTTP
            Assert.True(isHttpsOnly,
                "EXPECTED FAILURE: Session cookie can be transmitted over unencrypted HTTP (no Secure flag). " +
                "This allows attackers to intercept session cookies through man-in-the-middle attacks. " +
                "After fix, session cookies should have Secure flag set, ensuring they are only transmitted over HTTPS.");
        }

        /// <summary>
        /// Test 1.10.7: Session cookie vulnerable to CSRF attacks
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Without SameSite attribute, session cookies are vulnerable to CSRF
        /// Expected Behavior: Session cookies should have SameSite=Lax to prevent CSRF
        /// Current Behavior: Cookies are sent with cross-site requests, enabling CSRF attacks
        /// </summary>
        [Fact]
        public async Task SessionCookie_ShouldPreventCsrf_ButIsVulnerable()
        {
            // Arrange & Act - Make request that creates a session
            var response = await _client.GetAsync("/Account/Login");
            
            // Check if Set-Cookie header exists
            bool hasSetCookieHeader = response.Headers.Contains("Set-Cookie");
            
            if (!hasSetCookieHeader)
            {
                // No session cookie set yet, try to trigger session creation
                response = await _client.GetAsync("/");
            }
            
            // Get all Set-Cookie headers
            var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
            
            // Find session cookie
            var sessionCookie = setCookieHeaders.FirstOrDefault(c => 
                c.Contains(".AspNetCore.Session") || c.Contains("session"));
            
            bool hasCsrfProtection = false;
            
            if (sessionCookie != null)
            {
                // Cookie has CSRF protection if SameSite is set to Lax or Strict
                hasCsrfProtection = 
                    sessionCookie.Contains("samesite=lax", System.StringComparison.OrdinalIgnoreCase) ||
                    sessionCookie.Contains("samesite=strict", System.StringComparison.OrdinalIgnoreCase);
            }
            
            // Assert - This test FAILS because cookie lacks CSRF protection on unfixed code
            // After fix: SameSite attribute should prevent CSRF attacks
            Assert.True(hasCsrfProtection,
                "EXPECTED FAILURE: Session cookie is vulnerable to CSRF attacks (no SameSite attribute). " +
                "Without SameSite=Lax or Strict, cookies are sent with cross-site requests, allowing attackers " +
                "to perform actions on behalf of authenticated users. " +
                "After fix, session cookies should have SameSite=Lax to prevent CSRF while maintaining usability.");
        }
    }
}
