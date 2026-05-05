using Xunit;
using TechNova_IT_Solutions.Data;
using TechNova_IT_Solutions.Models;
using TechNova_IT_Solutions.Services;
using TechNova_IT_Solutions.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TechNova.SecurityTests
{
    /// <summary>
    /// Bug Condition Exploration Tests for Audit Logging (Task 1.9)
    /// 
    /// **Validates: Requirements 9.1, 9.2, 9.3, 9.4**
    /// 
    /// CRITICAL: These tests are EXPECTED TO FAIL on unfixed code.
    /// Failure confirms the bugs exist. DO NOT attempt to fix the tests or code when they fail.
    /// 
    /// These tests encode the expected behavior - they will validate the fixes when they pass after implementation.
    /// </summary>
    public class AuditLoggingExplorationTests
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
        /// Test 1.9.1: User creation should create audit log entry but currently does NOT
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: When a user is created via UserService.CreateUserAsync, no audit log entry is created
        /// Expected Behavior: System should create audit log entry with action "Created user: {email} with role {role}" and module "UserManagement"
        /// Current Behavior: UserService.CreateUserAsync does not call LogActivityAsync
        /// </summary>
        [Fact]
        public async Task UserCreation_ShouldCreateAuditLog_ButCurrentlyDoesNot()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var mockEmailService = new Mock<IEmailService>();
            mockEmailService
                .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new EmailSendResult { Success = true });

            var userService = new UserService(context, mockEmailService.Object);

            var userData = new UserData
            {
                FirstName = "Test",
                LastName = "User",
                Email = "testuser@technova.com",
                Role = "Employee",
                Status = "Active",
                BranchId = null
            };

            // Get initial audit log count
            int initialAuditLogCount = await context.AuditLogs.CountAsync();

            // Act - Create user (currently does NOT create audit log)
            var result = await userService.CreateUserAsync(userData);

            // Assert - Check if audit log was created
            int finalAuditLogCount = await context.AuditLogs.CountAsync();
            bool auditLogCreated = finalAuditLogCount > initialAuditLogCount;

            // This test FAILS because user creation does NOT create audit log
            // After fix: UserService.CreateUserAsync should call LogActivityAsync
            Assert.True(auditLogCreated,
                "EXPECTED FAILURE: User creation does not create audit log entry. " +
                "After fix, UserService.CreateUserAsync should call LogActivityAsync with action " +
                "'Created user: {email} with role {role}' and module 'UserManagement'.");
        }

        /// <summary>
        /// Test 1.9.2: User deletion should create audit log entry but currently does NOT
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: When a user is deleted via UserService.DeleteUserAsync, no audit log entry is created
        /// Expected Behavior: System should create audit log entry with action "Deleted user: {email}" and module "UserManagement"
        /// Current Behavior: UserService.DeleteUserAsync does not call LogActivityAsync
        /// </summary>
        [Fact]
        public async Task UserDeletion_ShouldCreateAuditLog_ButCurrentlyDoesNot()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var mockEmailService = new Mock<IEmailService>();
            var userService = new UserService(context, mockEmailService.Object);

            // Create a user to delete
            var user = new User
            {
                FirstName = "Delete",
                LastName = "Me",
                Email = "deleteme@technova.com",
                Role = "Employee",
                Status = "Active",
                Password = PasswordHasher.HashPassword("TempPassword123!"),
                MustChangePassword = true
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Get initial audit log count
            int initialAuditLogCount = await context.AuditLogs.CountAsync();

            // Act - Delete user (currently does NOT create audit log)
            var result = await userService.DeleteUserAsync(user.UserId);

            // Assert - Check if audit log was created
            int finalAuditLogCount = await context.AuditLogs.CountAsync();
            bool auditLogCreated = finalAuditLogCount > initialAuditLogCount;

            // This test FAILS because user deletion does NOT create audit log
            // After fix: UserService.DeleteUserAsync should call LogActivityAsync
            Assert.True(auditLogCreated,
                "EXPECTED FAILURE: User deletion does not create audit log entry. " +
                "After fix, UserService.DeleteUserAsync should call LogActivityAsync with action " +
                "'Deleted user: {email}' and module 'UserManagement'.");
        }

        /// <summary>
        /// Test 1.9.3: Password change should create audit log entry but currently does NOT
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: When a password is changed via UserService.SetPasswordAsync, no audit log entry is created
        /// Expected Behavior: System should create audit log entry with action "Password changed by user" and module "Authentication"
        /// Current Behavior: UserService.SetPasswordAsync does not call LogActivityAsync
        /// </summary>
        [Fact]
        public async Task PasswordChange_ShouldCreateAuditLog_ButCurrentlyDoesNot()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var mockEmailService = new Mock<IEmailService>();
            var userService = new UserService(context, mockEmailService.Object);

            // Create a user
            var user = new User
            {
                FirstName = "Change",
                LastName = "Password",
                Email = "changepass@technova.com",
                Role = "Employee",
                Status = "Active",
                Password = PasswordHasher.HashPassword("OldPassword123!"),
                MustChangePassword = false
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Get initial audit log count
            int initialAuditLogCount = await context.AuditLogs.CountAsync();

            // Act - Change password (currently does NOT create audit log)
            var result = await userService.SetPasswordAsync(user.UserId, "NewPassword123!");

            // Assert - Check if audit log was created
            int finalAuditLogCount = await context.AuditLogs.CountAsync();
            bool auditLogCreated = finalAuditLogCount > initialAuditLogCount;

            // This test FAILS because password change does NOT create audit log
            // After fix: UserService.SetPasswordAsync should call LogActivityAsync
            Assert.True(auditLogCreated,
                "EXPECTED FAILURE: Password change does not create audit log entry. " +
                "After fix, UserService.SetPasswordAsync should call LogActivityAsync with action " +
                "'Password changed by user' and module 'Authentication'.");
        }

        /// <summary>
        /// Test 1.9.4: Password reset should create audit log entry but currently does NOT
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: When a password is reset via UserService.ResetPasswordByRoleAsync, no audit log entry is created
        /// Expected Behavior: System should create audit log entry with action "Password reset for user: {email}" and module "Authentication"
        /// Current Behavior: UserService.ResetPasswordByRoleAsync does not call LogActivityAsync
        /// </summary>
        [Fact]
        public async Task PasswordReset_ShouldCreateAuditLog_ButCurrentlyDoesNot()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var mockEmailService = new Mock<IEmailService>();
            var userService = new UserService(context, mockEmailService.Object);

            // Create a user
            var user = new User
            {
                FirstName = "Reset",
                LastName = "Password",
                Email = "resetpass@technova.com",
                Role = "Employee",
                Status = "Active",
                Password = PasswordHasher.HashPassword("OldPassword123!"),
                MustChangePassword = false
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Get initial audit log count
            int initialAuditLogCount = await context.AuditLogs.CountAsync();

            // Act - Reset password (currently does NOT create audit log)
            var result = await userService.ResetPasswordByRoleAsync(user.UserId);

            // Assert - Check if audit log was created
            int finalAuditLogCount = await context.AuditLogs.CountAsync();
            bool auditLogCreated = finalAuditLogCount > initialAuditLogCount;

            // This test FAILS because password reset does NOT create audit log
            // After fix: UserService.ResetPasswordByRoleAsync should call LogActivityAsync
            Assert.True(auditLogCreated,
                "EXPECTED FAILURE: Password reset does not create audit log entry. " +
                "After fix, UserService.ResetPasswordByRoleAsync should call LogActivityAsync with action " +
                "'Password reset for user: {email}' and module 'Authentication'.");
        }

        /// <summary>
        /// Test 1.9.5: Role modification should create audit log entry but currently does NOT
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: When a user's role is changed via UserService.UpdateUserAsync, no audit log entry is created
        /// Expected Behavior: System should create audit log entry with action "Changed role for {email} from {oldRole} to {newRole}" and module "UserManagement"
        /// Current Behavior: UserService.UpdateUserAsync does not call LogActivityAsync
        /// </summary>
        [Fact]
        public async Task RoleModification_ShouldCreateAuditLog_ButCurrentlyDoesNot()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var mockEmailService = new Mock<IEmailService>();
            var userService = new UserService(context, mockEmailService.Object);

            // Create a user
            var user = new User
            {
                FirstName = "Change",
                LastName = "Role",
                Email = "changerole@technova.com",
                Role = "Employee",
                Status = "Active",
                Password = PasswordHasher.HashPassword("Password123!"),
                MustChangePassword = false
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Get initial audit log count
            int initialAuditLogCount = await context.AuditLogs.CountAsync();

            // Act - Update user role (currently does NOT create audit log)
            var userData = new UserData
            {
                UserId = user.UserId.ToString(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = "BranchAdmin", // Changed from Employee to BranchAdmin
                Status = user.Status,
                BranchId = user.BranchId
            };
            var result = await userService.UpdateUserAsync(userData);

            // Assert - Check if audit log was created
            int finalAuditLogCount = await context.AuditLogs.CountAsync();
            bool auditLogCreated = finalAuditLogCount > initialAuditLogCount;

            // This test FAILS because role modification does NOT create audit log
            // After fix: UserService.UpdateUserAsync should call LogActivityAsync when role changes
            Assert.True(auditLogCreated,
                "EXPECTED FAILURE: Role modification does not create audit log entry. " +
                "After fix, UserService.UpdateUserAsync should call LogActivityAsync with action " +
                "'Changed role for {email} from {oldRole} to {newRole}' and module 'UserManagement'.");
        }

        /// <summary>
        /// Test 1.9.6: User deactivation should create audit log entry but currently does NOT
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: When a user is deactivated via UserService.DeactivateUserAsync, no audit log entry is created
        /// Expected Behavior: System should create audit log entry with action "Deactivated user: {email}" and module "UserManagement"
        /// Current Behavior: UserService.DeactivateUserAsync does not call LogActivityAsync
        /// </summary>
        [Fact]
        public async Task UserDeactivation_ShouldCreateAuditLog_ButCurrentlyDoesNot()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var mockEmailService = new Mock<IEmailService>();
            var userService = new UserService(context, mockEmailService.Object);

            // Create a user
            var user = new User
            {
                FirstName = "Deactivate",
                LastName = "Me",
                Email = "deactivateme@technova.com",
                Role = "Employee",
                Status = "Active",
                Password = PasswordHasher.HashPassword("Password123!"),
                MustChangePassword = false
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Get initial audit log count
            int initialAuditLogCount = await context.AuditLogs.CountAsync();

            // Act - Deactivate user (currently does NOT create audit log)
            var result = await userService.DeactivateUserAsync(user.UserId);

            // Assert - Check if audit log was created
            int finalAuditLogCount = await context.AuditLogs.CountAsync();
            bool auditLogCreated = finalAuditLogCount > initialAuditLogCount;

            // This test FAILS because user deactivation does NOT create audit log
            // After fix: UserService.DeactivateUserAsync should call LogActivityAsync
            Assert.True(auditLogCreated,
                "EXPECTED FAILURE: User deactivation does not create audit log entry. " +
                "After fix, UserService.DeactivateUserAsync should call LogActivityAsync with action " +
                "'Deactivated user: {email}' and module 'UserManagement'.");
        }

        /// <summary>
        /// Test 1.9.7: User reactivation should create audit log entry but currently does NOT
        /// 
        /// **EXPECTED OUTCOME**: This test FAILS on unfixed code (proves bug exists)
        /// 
        /// Bug Condition: When a user is reactivated via UserService.ReactivateUserAsync, no audit log entry is created
        /// Expected Behavior: System should create audit log entry with action "Reactivated user: {email}" and module "UserManagement"
        /// Current Behavior: UserService.ReactivateUserAsync does not call LogActivityAsync
        /// </summary>
        [Fact]
        public async Task UserReactivation_ShouldCreateAuditLog_ButCurrentlyDoesNot()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var mockEmailService = new Mock<IEmailService>();
            var userService = new UserService(context, mockEmailService.Object);

            // Create an inactive user
            var user = new User
            {
                FirstName = "Reactivate",
                LastName = "Me",
                Email = "reactivateme@technova.com",
                Role = "Employee",
                Status = "Inactive",
                Password = PasswordHasher.HashPassword("Password123!"),
                MustChangePassword = false
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Get initial audit log count
            int initialAuditLogCount = await context.AuditLogs.CountAsync();

            // Act - Reactivate user (currently does NOT create audit log)
            var result = await userService.ReactivateUserAsync(user.UserId);

            // Assert - Check if audit log was created
            int finalAuditLogCount = await context.AuditLogs.CountAsync();
            bool auditLogCreated = finalAuditLogCount > initialAuditLogCount;

            // This test FAILS because user reactivation does NOT create audit log
            // After fix: UserService.ReactivateUserAsync should call LogActivityAsync
            Assert.True(auditLogCreated,
                "EXPECTED FAILURE: User reactivation does not create audit log entry. " +
                "After fix, UserService.ReactivateUserAsync should call LogActivityAsync with action " +
                "'Reactivated user: {email}' and module 'UserManagement'.");
        }

        /// <summary>
        /// Test 1.9.8: Verify existing audit logging for policy operations still works
        /// 
        /// **EXPECTED OUTCOME**: This test PASSES on unfixed code (confirms existing functionality)
        /// 
        /// This test verifies that existing audit logging for policy operations is working correctly
        /// and should continue to work after fixes are implemented (preservation requirement)
        /// </summary>
        [Fact]
        public async Task ExistingPolicyAuditLogging_ShouldStillWork()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var mockEmailService = new Mock<IEmailService>();
            var mockExchangeRateService = new Mock<IExchangeRateService>();
            var mockOptions = new Mock<IOptions<ExternalApisConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new ExternalApisConfiguration());
            var mockLogger = new Mock<ILogger<AdminService>>();
            
            var adminService = new AdminService(
                context, 
                mockEmailService.Object, 
                mockExchangeRateService.Object,
                mockOptions.Object,
                mockLogger.Object);

            // Get initial audit log count
            int initialAuditLogCount = await context.AuditLogs.CountAsync();

            // Act - Log a policy operation (this should work on unfixed code)
            await adminService.LogActivityAsync(1, "Created policy: Test Policy", "Policy");

            // Assert - Check if audit log was created
            int finalAuditLogCount = await context.AuditLogs.CountAsync();
            bool auditLogCreated = finalAuditLogCount > initialAuditLogCount;

            // This test PASSES because existing audit logging works
            Assert.True(auditLogCreated,
                "Existing audit logging for policy operations should work. " +
                "This is a preservation requirement - existing functionality must remain unchanged.");

            // Verify the audit log entry details
            var auditLog = await context.AuditLogs
                .OrderByDescending(a => a.LogDate)
                .FirstOrDefaultAsync();

            Assert.NotNull(auditLog);
            Assert.Equal(1, auditLog.UserId);
            Assert.Equal("Created policy: Test Policy", auditLog.Action);
            Assert.Equal("Policy", auditLog.Module);
        }
    }
}
