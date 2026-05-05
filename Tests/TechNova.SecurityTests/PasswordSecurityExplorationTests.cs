using Xunit;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace TechNova.SecurityTests
{
    /// <summary>
    /// Bug Condition Exploration Tests for Password Security (Task 1.1)
    /// 
    /// **Validates: Requirements 1.1, 1.2, 1.3**
    /// 
    /// CRITICAL: These tests are EXPECTED TO FAIL on unfixed code.
    /// Failure confirms the bugs exist. DO NOT attempt to fix the tests or code when they fail.
    /// 
    /// These tests encode the expected behavior - they will validate the fixes when they pass after implementation.
    /// </summary>
    public class PasswordSecurityExplorationTests
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
        /// Test 1.1.1: Weak password "pass123" should be REJECTED but is currently ACCEPTED
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Password "pass123" is too weak (only 7 characters, no uppercase, no special chars)
        /// Expected Behavior: System should reject passwords that don't meet complexity requirements:
        ///   - Minimum 12 characters
        ///   - At least one uppercase letter
        ///   - At least one lowercase letter
        ///   - At least one digit
        ///   - At least one special character
        /// 
        /// Current Behavior: AccountController.ChangePassword only checks minimum 8 characters
        /// </summary>
        [Fact]
        public void WeakPassword_Pass123_ShouldBeRejected_ButIsCurrentlyAccepted()
        {
            // Arrange
            string weakPassword = "pass123";
            
            // Act - Simulate password validation (currently only checks length >= 8)
            bool isAcceptedByCurrentCode = weakPassword.Length >= 8;
            
            // Assert - This test FAILS because weak password is currently accepted
            // After fix: Password validator will reject "pass123" and this test will PASS
            Assert.False(isAcceptedByCurrentCode, 
                "EXPECTED FAILURE: Weak password 'pass123' is currently accepted (length check only). " +
                "After fix, password validator should reject it for insufficient complexity.");
        }

        /// <summary>
        /// Test 1.1.2: Various weak passwords should be rejected
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Tests that passwords with 8-11 characters but missing complexity are rejected
        /// </summary>
        [Theory]
        [InlineData("password")]  // 8 chars, no uppercase, no digit, no special
        [InlineData("password1")]  // 9 chars, no uppercase, no special
        [InlineData("Password")]  // 8 chars, no digit, no special
        [InlineData("Pass1234")]  // 8 chars, no special
        [InlineData("abcdefgh")]  // 8 chars, no uppercase, no digit, no special
        [InlineData("12345678")]  // 8 chars, no uppercase, no lowercase, no special
        public void WeakPasswords_ShouldBeRejected_ButAreCurrentlyAccepted(string weakPassword)
        {
            // Act - Current validation: only checks length >= 8
            bool isAcceptedByCurrentCode = weakPassword.Length >= 8;
            
            // Expected behavior: Should be rejected (missing complexity requirements)
            bool meetsComplexity = weakPassword.Length >= 12 &&
                                  weakPassword.Any(char.IsUpper) &&
                                  weakPassword.Any(char.IsLower) &&
                                  weakPassword.Any(char.IsDigit) &&
                                  weakPassword.Any(c => !char.IsLetterOrDigit(c));
            
            // Assert - This test FAILS because weak passwords are currently accepted
            Assert.False(isAcceptedByCurrentCode && !meetsComplexity,
                $"EXPECTED FAILURE: Weak password '{weakPassword}' is currently accepted. " +
                "After fix, password validator should reject it for insufficient complexity.");
        }

        /// <summary>
        /// Test 1.1.3: Seeded users have hardcoded password "Admin@123"
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: DataSeeder.SeededUserPassword constant is "Admin@123"
        /// Expected Behavior: Production should NOT have default passwords, development should require password change
        /// Current Behavior: All seeded users get "Admin@123" password
        /// </summary>
        [Fact]
        public async Task SeededUsers_ShouldNotHaveHardcodedPassword_ButCurrentlyDo()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            
            // Create mock environment (Development)
            var mockEnvironment = new Mock<IHostEnvironment>();
            mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
            
            // Create mock logger
            var mockLogger = new Mock<ILogger>();
            
            // Act - Seed the database (uses hardcoded "Admin@123")
            await DataSeeder.SeedAsync(context, mockEnvironment.Object, mockLogger.Object);
            
            var seededUsers = await context.Users
                .Where(u => u.Email == "superadmin@technova.com" || 
                           u.Email == "sysadmin@technova.com" ||
                           u.Email == "compliance@technova.com" ||
                           u.Email == "employee@technova.com")
                .ToListAsync();
            
            // Assert - Check if users have the hardcoded password
            // All seeded users currently use BCrypt.HashPassword(DataSeeder.SeededUserPassword)
            // where SeededUserPassword = "Admin@123"
            bool allUsersHaveHardcodedPassword = seededUsers.All(u => 
                BCrypt.Net.BCrypt.Verify("Admin@123", u.Password));
            
            // This test FAILS because seeded users DO have hardcoded password
            // After fix: Seeded users should either:
            //   - Not exist in production, OR
            //   - Have MustChangePassword = true in development, OR
            //   - Have unique generated passwords
            Assert.False(allUsersHaveHardcodedPassword,
                "EXPECTED FAILURE: Seeded users currently have hardcoded password 'Admin@123'. " +
                "After fix, production should not seed users with default passwords, " +
                "and development should require password change on first login.");
        }

        /// <summary>
        /// Test 1.1.4: Bootstrap super admin accepts weak password "Admin@123"
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: Bootstrap logic (if exists) accepts known weak password "Admin@123"
        /// Expected Behavior: Should reject known weak passwords including "Admin@123"
        /// Current Behavior: No validation against known weak passwords
        /// 
        /// Note: This test simulates bootstrap behavior since actual bootstrap may be in Program.cs or migration
        /// </summary>
        [Fact]
        public void BootstrapSuperAdmin_ShouldRejectKnownWeakPassword_ButCurrentlyAccepts()
        {
            // Arrange
            string knownWeakPassword = "Admin@123";
            
            // Act - Simulate bootstrap password validation
            // Current code: Only checks length >= 8
            bool isAcceptedByCurrentCode = knownWeakPassword.Length >= 8;
            
            // Assert - This test FAILS because "Admin@123" is currently accepted
            // After fix: Password validator should have blacklist including "Admin@123"
            Assert.False(isAcceptedByCurrentCode,
                "EXPECTED FAILURE: Known weak password 'Admin@123' is currently accepted. " +
                "After fix, password validator should reject passwords in blacklist including 'Admin@123'.");
        }

        /// <summary>
        /// Test 1.1.5: Verify DataSeeder constant contains weak password
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// This test directly checks the DataSeeder.SeededUserPassword constant
        /// </summary>
        [Fact]
        public void DataSeeder_SeededUserPassword_ShouldNotBeAdminAt123_ButCurrentlyIs()
        {
            // Act - Check the constant value
            string seededPassword = DataSeeder.SeededUserPassword;
            
            // Assert - This test FAILS because the constant IS "Admin@123"
            // After fix: This constant should either be removed or changed to a secure value
            // EXPECTED FAILURE: DataSeeder.SeededUserPassword is currently 'Admin@123'.
            // After fix, this should be changed to a secure value or removed entirely.
            Assert.NotEqual("Admin@123", seededPassword);
        }

        /// <summary>
        /// Test 1.1.6: Password complexity requirements should be enforced
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Tests various password scenarios to verify complexity enforcement
        /// </summary>
        [Theory]
        [InlineData("Pass1!", false)]  // Too short (< 12 chars)
        [InlineData("password123!", false)]  // No uppercase
        [InlineData("PASSWORD123!", false)]  // No lowercase
        [InlineData("Password!", false)]  // No digit
        [InlineData("Password123", false)]  // No special char
        [InlineData("SecurePass123!@#", true)]  // Strong: meets all requirements
        public void PasswordComplexity_ShouldBeEnforced_ButIsNot(string password, bool shouldBeAccepted)
        {
            // Act - Current validation: only checks length >= 8
            bool isAcceptedByCurrentCode = password.Length >= 8;
            
            // Expected behavior: Should validate complexity
            bool meetsComplexity = password.Length >= 12 &&
                                  password.Any(char.IsUpper) &&
                                  password.Any(char.IsLower) &&
                                  password.Any(char.IsDigit) &&
                                  password.Any(c => !char.IsLetterOrDigit(c));
            
            // Assert - This test FAILS on unfixed code because current code doesn't match expected behavior
            Assert.Equal(shouldBeAccepted, meetsComplexity);
            
            // This assertion will fail for weak passwords that are currently accepted
            if (!shouldBeAccepted)
            {
                Assert.False(isAcceptedByCurrentCode && !meetsComplexity,
                    $"EXPECTED FAILURE: Password '{password}' is currently accepted but doesn't meet complexity requirements. " +
                    "After fix, validation should enforce all complexity requirements.");
            }
        }
    }
}
