using Xunit;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Models;
using TechNova_IT_Solutions.Services;
using TechNova_IT_Solutions.Services.Interfaces;
using TechNova_IT_Solutions.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TechNova_IT_Solutions.Controllers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

namespace TechNova.SecurityTests
{
    /// <summary>
    /// Preservation Property Tests for Authentication and Session Management (Task 2.1)
    /// 
    /// **Validates: Requirements 3.1, 3.2, 3.3**
    /// 
    /// IMPORTANT: These tests verify baseline behavior that must be preserved after security fixes.
    /// Tests should PASS on UNFIXED code to confirm current functionality.
    /// 
    /// Property 2: Preservation - Existing Functionality Unchanged
    /// For any input where security vulnerabilities do NOT exist, the fixed application SHALL produce
    /// exactly the same behavior as the original application.
    /// </summary>
    public class AuthenticationSessionPreservationTests
    {
        /// <summary>
        /// Creates an in-memory database context for testing
        /// </summary>
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        /// <summary>
        /// Creates a test user in the database with valid credentials
        /// </summary>
        private async Task<User> CreateTestUserAsync(ApplicationDbContext context, string email, string password, string role = RoleNames.Employee)
        {
            var user = new User
            {
                FirstName = "Test",
                LastName = "User",
                Email = email,
                Password = PasswordHasher.HashPassword(password),
                Role = role,
                Status = "Active"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        /// <summary>
        /// Creates a mock HTTP session for testing
        /// </summary>
        private ISession CreateMockSession()
        {
            var sessionData = new Dictionary<string, byte[]>();
            var session = new MockSession(sessionData);
            return session;
        }

        /// <summary>
        /// Test 2.1.1: Valid login credentials authenticate users correctly
        /// 
        /// **Validates: Requirement 3.1**
        /// 
        /// Property: For all valid credentials (email, password), authentication succeeds
        /// 
        /// This test verifies that the authentication service correctly authenticates users
        /// with valid credentials. This behavior must be preserved after security fixes.
        /// </summary>
        [Fact]
        public async Task ValidCredentials_AuthenticateSuccessfully()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var mockLogger = new Mock<ILogger<AuthenticationService>>();
            var authService = new AuthenticationService(context, memoryCache, mockLogger.Object);
            
            string email = "testuser@technova.com";
            string password = "ValidPassword123!";
            
            await CreateTestUserAsync(context, email, password);

            // Act
            var result = await authService.AuthenticateUserAsync(email, password);

            // Assert
            Assert.True(result.Success, "Valid credentials should authenticate successfully");
            Assert.NotNull(result.User);
            Assert.Equal(email, result.User.Email);
        }

        /// <summary>
        /// Test 2.1.2: Multiple valid credentials authenticate successfully
        /// 
        /// **Validates: Requirement 3.1**
        /// 
        /// Property: For all valid credentials, authentication succeeds
        /// 
        /// This test verifies authentication works for multiple different valid credential combinations.
        /// </summary>
        [Theory]
        [InlineData("user1@technova.com", "Password123!")]
        [InlineData("user2@technova.com", "SecurePass456@")]
        [InlineData("user3@technova.com", "ComplexPwd789#")]
        [InlineData("admin@technova.com", "AdminPass000$")]
        public async Task MultipleValidCredentials_AuthenticateSuccessfully(string email, string password)
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var mockLogger = new Mock<ILogger<AuthenticationService>>();
            var authService = new AuthenticationService(context, memoryCache, mockLogger.Object);
            
            await CreateTestUserAsync(context, email, password);

            // Act
            var result = await authService.AuthenticateUserAsync(email, password);

            // Assert
            Assert.True(result.Success, $"Valid credentials {email} should authenticate successfully");
            Assert.NotNull(result.User);
            Assert.Equal(email, result.User.Email);
        }

        /// <summary>
        /// Test 2.1.3: Session state and user context are maintained correctly
        /// 
        /// **Validates: Requirement 3.2**
        /// 
        /// Property: For all active sessions, user context (UserId, UserRole, UserEmail, UserName) is maintained
        /// 
        /// This test verifies that session data is correctly stored and retrieved
        /// after a successful login. This behavior must be preserved.
        /// </summary>
        [Fact]
        public async Task ActiveSession_MaintainsUserContext()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var mockLogger = new Mock<ILogger<AuthenticationService>>();
            var authService = new AuthenticationService(context, memoryCache, mockLogger.Object);
            var mockEmailService = new Mock<IEmailService>();
            var userService = new UserService(context, mockEmailService.Object);
            
            string email = "sessiontest@technova.com";
            string password = "SessionTest123!";
            string role = RoleNames.Employee;
            
            var user = await CreateTestUserAsync(context, email, password, role);

            // Create AccountController with mock session
            var sessionData = new Dictionary<string, byte[]>();
            var mockSession = new MockSession(sessionData);
            var httpContext = new DefaultHttpContext();
            httpContext.Session = mockSession;
            
            var mockAdminService = new Mock<IAdminService>();
            var controller = new AccountController(authService, userService, mockAdminService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            // Act - Login
            var loginResult = await controller.Login(email, password);

            // Assert - Verify session contains user context
            Assert.True(mockSession.TryGetValue(SessionKeys.UserId, out var userIdBytes));
            Assert.True(mockSession.TryGetValue(SessionKeys.UserRole, out var userRoleBytes));
            Assert.True(mockSession.TryGetValue(SessionKeys.UserEmail, out var userEmailBytes));
            Assert.True(mockSession.TryGetValue(SessionKeys.UserName, out var userNameBytes));

            var storedUserId = Encoding.UTF8.GetString(userIdBytes);
            var storedRole = Encoding.UTF8.GetString(userRoleBytes);
            var storedEmail = Encoding.UTF8.GetString(userEmailBytes);
            var storedName = Encoding.UTF8.GetString(userNameBytes);

            Assert.Equal(user.UserId.ToString(), storedUserId);
            Assert.Equal(role, storedRole);
            Assert.Equal(email, storedEmail);
            Assert.Equal("Test User", storedName);
        }

        /// <summary>
        /// Test 2.1.4: Session maintains user context for different roles
        /// 
        /// **Validates: Requirement 3.2**
        /// 
        /// Property: For all active sessions, user context is maintained
        /// 
        /// This test verifies session management works for various user types.
        /// </summary>
        [Theory]
        [InlineData("employee@technova.com", "EmpPass123!", RoleNames.Employee)]
        [InlineData("branchadmin@technova.com", "AdminPass456@", RoleNames.BranchAdmin)]
        [InlineData("compliance@technova.com", "CompPass789#", RoleNames.ComplianceManager)]
        public async Task ActiveSessions_MaintainUserContextForAllRoles(string email, string password, string role)
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var mockLogger = new Mock<ILogger<AuthenticationService>>();
            var authService = new AuthenticationService(context, memoryCache, mockLogger.Object);
            var mockEmailService = new Mock<IEmailService>();
            var userService = new UserService(context, mockEmailService.Object);
            
            var user = await CreateTestUserAsync(context, email, password, role);

            var sessionData = new Dictionary<string, byte[]>();
            var mockSession = new MockSession(sessionData);
            var httpContext = new DefaultHttpContext();
            httpContext.Session = mockSession;
            
            var mockAdminService = new Mock<IAdminService>();
            var controller = new AccountController(authService, userService, mockAdminService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            // Act
            await controller.Login(email, password);

            // Assert
            bool hasUserId = mockSession.TryGetValue(SessionKeys.UserId, out _);
            bool hasUserRole = mockSession.TryGetValue(SessionKeys.UserRole, out _);
            bool hasUserEmail = mockSession.TryGetValue(SessionKeys.UserEmail, out _);
            bool hasUserName = mockSession.TryGetValue(SessionKeys.UserName, out _);

            Assert.True(hasUserId, "Session should contain UserId");
            Assert.True(hasUserRole, "Session should contain UserRole");
            Assert.True(hasUserEmail, "Session should contain UserEmail");
            Assert.True(hasUserName, "Session should contain UserName");
        }

        /// <summary>
        /// Test 2.1.5: Logout clears session and redirects to login page
        /// 
        /// **Validates: Requirement 3.3**
        /// 
        /// Property: For all logout operations, session is cleared
        /// 
        /// This test verifies that logout correctly clears all session data
        /// and redirects to the login page. This behavior must be preserved.
        /// </summary>
        [Fact]
        public async Task Logout_ClearsSessionAndRedirects()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var mockLogger = new Mock<ILogger<AuthenticationService>>();
            var authService = new AuthenticationService(context, memoryCache, mockLogger.Object);
            var mockEmailService = new Mock<IEmailService>();
            var userService = new UserService(context, mockEmailService.Object);
            
            string email = "logouttest@technova.com";
            string password = "LogoutTest123!";
            
            await CreateTestUserAsync(context, email, password);

            // Create session with user data
            var sessionData = new Dictionary<string, byte[]>();
            var mockSession = new MockSession(sessionData);
            mockSession.SetString(SessionKeys.UserId, "123");
            mockSession.SetString(SessionKeys.UserRole, RoleNames.Employee);
            mockSession.SetString(SessionKeys.UserEmail, email);
            mockSession.SetString(SessionKeys.UserName, "Test User");

            var httpContext = new DefaultHttpContext();
            httpContext.Session = mockSession;
            
            var mockAdminService = new Mock<IAdminService>();
            var controller = new AccountController(authService, userService, mockAdminService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            // Verify session has data before logout
            Assert.True(mockSession.TryGetValue(SessionKeys.UserId, out _));

            // Act
            var result = controller.Logout();

            // Assert - Session should be cleared
            Assert.False(mockSession.TryGetValue(SessionKeys.UserId, out _));
            Assert.False(mockSession.TryGetValue(SessionKeys.UserRole, out _));
            Assert.False(mockSession.TryGetValue(SessionKeys.UserEmail, out _));
            Assert.False(mockSession.TryGetValue(SessionKeys.UserName, out _));

            // Assert - Should redirect to Login
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        /// <summary>
        /// Test 2.1.6: Logout clears session for different session states
        /// 
        /// **Validates: Requirement 3.3**
        /// 
        /// Property: For all logout operations, session is cleared
        /// 
        /// This test verifies logout works correctly regardless of
        /// what data was in the session before logout.
        /// </summary>
        [Theory]
        [InlineData("100", RoleNames.Employee, "emp@technova.com")]
        [InlineData("200", RoleNames.BranchAdmin, "admin@technova.com")]
        [InlineData("300", RoleNames.ComplianceManager, "compliance@technova.com")]
        public void Logout_ClearsSessionForAllStates(string userId, string userRole, string userEmail)
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var mockLogger = new Mock<ILogger<AuthenticationService>>();
            var authService = new AuthenticationService(context, memoryCache, mockLogger.Object);
            var mockEmailService = new Mock<IEmailService>();
            var userService = new UserService(context, mockEmailService.Object);

            var sessionData = new Dictionary<string, byte[]>();
            var mockSession = new MockSession(sessionData);
            mockSession.SetString(SessionKeys.UserId, userId);
            mockSession.SetString(SessionKeys.UserRole, userRole);
            mockSession.SetString(SessionKeys.UserEmail, userEmail);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = mockSession;
            
            var mockAdminService = new Mock<IAdminService>();
            var controller = new AccountController(authService, userService, mockAdminService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            // Act
            controller.Logout();

            // Assert
            bool sessionIsEmpty = !mockSession.TryGetValue(SessionKeys.UserId, out _) &&
                                 !mockSession.TryGetValue(SessionKeys.UserRole, out _) &&
                                 !mockSession.TryGetValue(SessionKeys.UserEmail, out _);

            Assert.True(sessionIsEmpty, "Session should be completely cleared after logout");
        }

        /// <summary>
        /// Test 2.1.7: Invalid credentials fail authentication
        /// 
        /// **Validates: Requirement 3.1 (negative case)**
        /// 
        /// This test verifies that invalid credentials correctly fail authentication.
        /// This behavior must be preserved - we don't want to break security by
        /// accidentally allowing invalid credentials after fixes.
        /// </summary>
        [Fact]
        public async Task InvalidCredentials_FailAuthentication()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var mockLogger = new Mock<ILogger<AuthenticationService>>();
            var authService = new AuthenticationService(context, memoryCache, mockLogger.Object);
            
            string email = "testuser@technova.com";
            string correctPassword = "CorrectPassword123!";
            string wrongPassword = "WrongPassword123!";
            
            await CreateTestUserAsync(context, email, correctPassword);

            // Act
            var result = await authService.AuthenticateUserAsync(email, wrongPassword);

            // Assert
            Assert.False(result.Success, "Invalid credentials should fail authentication");
            Assert.Null(result.User);
        }

        /// <summary>
        /// Test 2.1.8: BCrypt password verification works correctly
        /// 
        /// **Validates: Requirement 3.4, 3.5 (Password Hashing Preservation)**
        /// 
        /// This test verifies that BCrypt hashing and verification continue to work.
        /// This is critical for authentication to function after security fixes.
        /// </summary>
        [Fact]
        public void BCryptPasswordVerification_WorksCorrectly()
        {
            // Arrange
            string password = "TestPassword123!";
            string hashedPassword = PasswordHasher.HashPassword(password);

            // Act
            bool isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            bool isInvalid = BCrypt.Net.BCrypt.Verify("WrongPassword", hashedPassword);

            // Assert
            Assert.True(isValid, "BCrypt should verify correct password");
            Assert.False(isInvalid, "BCrypt should reject incorrect password");
        }

        /// <summary>
        /// Test 2.1.9: Session persists across multiple requests
        /// 
        /// **Validates: Requirement 3.2**
        /// 
        /// This test verifies that session data persists across multiple requests,
        /// which is essential for maintaining user context during their session.
        /// </summary>
        [Fact]
        public void Session_PersistsAcrossMultipleRequests()
        {
            // Arrange
            var sessionData = new Dictionary<string, byte[]>();
            var mockSession = new MockSession(sessionData);

            // Act - Set session data (simulating login)
            mockSession.SetString(SessionKeys.UserId, "123");
            mockSession.SetString(SessionKeys.UserRole, RoleNames.Employee);

            // Simulate multiple requests reading session data
            var userId1 = mockSession.GetString(SessionKeys.UserId);
            var userRole1 = mockSession.GetString(SessionKeys.UserRole);
            
            var userId2 = mockSession.GetString(SessionKeys.UserId);
            var userRole2 = mockSession.GetString(SessionKeys.UserRole);

            // Assert - Session data should be consistent across requests
            Assert.Equal("123", userId1);
            Assert.Equal(RoleNames.Employee, userRole1);
            Assert.Equal(userId1, userId2);
            Assert.Equal(userRole1, userRole2);
        }
    }

    /// <summary>
    /// Mock implementation of ISession for testing
    /// </summary>
    public class MockSession : ISession
    {
        private readonly Dictionary<string, byte[]> _sessionData;

        public MockSession(Dictionary<string, byte[]> sessionData)
        {
            _sessionData = sessionData;
        }

        public bool IsAvailable => true;
        public string Id => Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _sessionData.Keys;

        public void Clear()
        {
            _sessionData.Clear();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _sessionData.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _sessionData[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _sessionData.TryGetValue(key, out value);
        }
    }
}
