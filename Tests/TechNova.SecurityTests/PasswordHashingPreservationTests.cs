using Xunit;
using TechNova_IT_Solutions.Services;
using System.Text;

namespace TechNova.SecurityTests
{
    /// <summary>
    /// Property-Based Tests for Password Hashing Preservation (Task 2.2)
    /// 
    /// **Validates: Requirements 3.4, 3.5**
    /// 
    /// IMPORTANT: These tests verify baseline behavior that must be preserved after security fixes.
    /// Tests should PASS on UNFIXED code to confirm current BCrypt functionality.
    /// 
    /// Property 2: Preservation - Existing Functionality Unchanged
    /// For any input where security vulnerabilities do NOT exist, the fixed application SHALL produce
    /// exactly the same behavior as the original application.
    /// 
    /// This file uses property-based testing approach with Theory tests to verify BCrypt hashing 
    /// and verification work correctly across a wide range of password inputs.
    /// </summary>
    public class PasswordHashingPreservationTests
    {
        /// <summary>
        /// Property Test 2.2.1: BCrypt hashing produces valid hash for all passwords
        /// 
        /// **Validates: Requirement 3.4**
        /// 
        /// Property: For all passwords, BCrypt.HashPassword produces a valid BCrypt hash
        /// 
        /// This test verifies that BCrypt hashing works correctly for any password input.
        /// A valid BCrypt hash should:
        /// - Not be null or empty
        /// - Start with "$2" (BCrypt identifier)
        /// - Be at least 59 characters long (BCrypt hash format)
        /// - Be different from the original password
        /// 
        /// This behavior must be preserved after security fixes.
        /// </summary>
        [Theory]
        [InlineData("password123")]
        [InlineData("P@ssw0rd!")]
        [InlineData("a")]
        [InlineData("VeryLongPasswordThatIsStillValidForBCryptHashingAlgorithm123456789")]
        [InlineData("12345678")]
        [InlineData("!@#$%^&*()")]
        [InlineData("MixedCase123!@#")]
        [InlineData("   spaces   ")]
        [InlineData("tab\ttab")]
        [InlineData("newline\nnewline")]
        public void BCryptHashing_ProducesValidHash_ForAllPasswords(string password)
        {
            // Act
            string hashedPassword = PasswordHasher.HashPassword(password);

            // Assert - Verify hash has valid BCrypt format
            Assert.NotNull(hashedPassword);
            Assert.NotEmpty(hashedPassword);
            Assert.StartsWith("$2", hashedPassword);
            Assert.True(hashedPassword.Length >= 59, $"BCrypt hash should be at least 59 characters, got {hashedPassword.Length}");
            Assert.NotEqual(password, hashedPassword);
        }

        /// <summary>
        /// Property Test 2.2.2: BCrypt.Verify works correctly for all password verifications
        /// 
        /// **Validates: Requirement 3.5**
        /// 
        /// Property: For all passwords, BCrypt.Verify correctly verifies matching passwords
        /// and rejects non-matching passwords
        /// 
        /// This test verifies that BCrypt verification works correctly:
        /// - Correct password should verify successfully
        /// - Incorrect password should fail verification
        /// - Verification is consistent (same result for same inputs)
        /// 
        /// This behavior must be preserved after security fixes.
        /// </summary>
        [Theory]
        [InlineData("password123")]
        [InlineData("P@ssw0rd!")]
        [InlineData("a")]
        [InlineData("VeryLongPasswordThatIsStillValidForBCryptHashingAlgorithm123456789")]
        [InlineData("12345678")]
        [InlineData("!@#$%^&*()")]
        [InlineData("MixedCase123!@#")]
        [InlineData("   spaces   ")]
        public void BCryptVerify_WorksCorrectly_ForAllPasswords(string password)
        {
            // Arrange
            string hashedPassword = PasswordHasher.HashPassword(password);
            string wrongPassword = password + "_wrong";

            // Act
            bool correctPasswordVerifies = PasswordHasher.VerifyPassword(password, hashedPassword);
            bool wrongPasswordFails = !PasswordHasher.VerifyPassword(wrongPassword, hashedPassword);
            
            // Verify consistency - same inputs should produce same result
            bool isConsistent = PasswordHasher.VerifyPassword(password, hashedPassword) == correctPasswordVerifies;

            // Assert
            Assert.True(correctPasswordVerifies, $"Correct password should verify for: {MaskPassword(password)}");
            Assert.True(wrongPasswordFails, $"Wrong password should fail verification for: {MaskPassword(password)}");
            Assert.True(isConsistent, "Verification should be consistent");
        }

        /// <summary>
        /// Property Test 2.2.3: BCrypt hashing is deterministically random
        /// 
        /// **Validates: Requirement 3.4**
        /// 
        /// Property: For all passwords, hashing the same password twice produces different hashes
        /// (due to random salt), but both hashes verify correctly
        /// 
        /// This test verifies that BCrypt's salt generation works correctly:
        /// - Same password hashed twice produces different hashes (random salt)
        /// - Both hashes verify the original password correctly
        /// 
        /// This behavior must be preserved after security fixes.
        /// </summary>
        [Theory]
        [InlineData("password123")]
        [InlineData("P@ssw0rd!")]
        [InlineData("SimplePassword")]
        [InlineData("ComplexP@ssw0rd!123")]
        [InlineData("12345678")]
        public void BCryptHashing_UsesDifferentSalts_ForSamePassword(string password)
        {
            // Act - Hash the same password twice
            string hash1 = PasswordHasher.HashPassword(password);
            string hash2 = PasswordHasher.HashPassword(password);

            // Assert
            Assert.NotEqual(hash1, hash2); // Different salts should produce different hashes
            Assert.True(PasswordHasher.VerifyPassword(password, hash1), "Hash1 should verify the password");
            Assert.True(PasswordHasher.VerifyPassword(password, hash2), "Hash2 should verify the password");
        }

        /// <summary>
        /// Property Test 2.2.4: BCrypt handles various password lengths correctly
        /// 
        /// **Validates: Requirement 3.4, 3.5**
        /// 
        /// Property: For all password lengths (1 to 72 characters), BCrypt hashing and verification work correctly
        /// 
        /// BCrypt has a maximum password length of 72 characters. This test verifies:
        /// - Passwords of any length up to 72 chars are hashed correctly
        /// - Verification works for all password lengths
        /// 
        /// This behavior must be preserved after security fixes.
        /// </summary>
        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(8)]
        [InlineData(12)]
        [InlineData(20)]
        [InlineData(30)]
        [InlineData(50)]
        [InlineData(72)]
        public void BCrypt_HandlesVariousPasswordLengths_Correctly(int passwordLength)
        {
            // Arrange - Generate a password of specific length
            string password = new string('a', passwordLength);

            // Act
            string hashedPassword = PasswordHasher.HashPassword(password);
            bool verifies = PasswordHasher.VerifyPassword(password, hashedPassword);

            // Assert
            Assert.True(verifies, $"BCrypt should handle password length {passwordLength} correctly");
            Assert.NotEmpty(hashedPassword);
            Assert.StartsWith("$2", hashedPassword);
        }

        /// <summary>
        /// Property Test 2.2.5: BCrypt handles special characters correctly
        /// 
        /// **Validates: Requirement 3.4, 3.5**
        /// 
        /// Property: For all passwords containing special characters, BCrypt hashing and verification work correctly
        /// 
        /// This test verifies that BCrypt correctly handles passwords with:
        /// - Special characters (!@#$%^&*()_+-=[]{}|;:,.<>?)
        /// - Unicode characters
        /// - Whitespace
        /// 
        /// This behavior must be preserved after security fixes.
        /// </summary>
        [Theory]
        [InlineData("password!@#$%")]
        [InlineData("pass word with spaces")]
        [InlineData("пароль")] // Cyrillic
        [InlineData("密码")] // Chinese
        [InlineData("🔒password🔑")] // Emoji
        [InlineData("tab\ttab")]
        [InlineData("newline\nnewline")]
        [InlineData("quote\"quote")]
        [InlineData("backslash\\backslash")]
        [InlineData("special!@#$%^&*()_+-=[]{}|;:,.<>?")]
        public void BCrypt_HandlesSpecialCharacters_Correctly(string password)
        {
            // Act
            string hashedPassword = PasswordHasher.HashPassword(password);
            bool verifies = PasswordHasher.VerifyPassword(password, hashedPassword);
            bool wrongPasswordFails = !PasswordHasher.VerifyPassword(password + "x", hashedPassword);

            // Assert
            Assert.True(verifies, $"BCrypt should verify password with special characters: {MaskPassword(password)}");
            Assert.True(wrongPasswordFails, "BCrypt should reject incorrect password");
            Assert.NotEmpty(hashedPassword);
            Assert.StartsWith("$2", hashedPassword);
        }

        /// <summary>
        /// Property Test 2.2.6: BCrypt verification is case-sensitive
        /// 
        /// **Validates: Requirement 3.5**
        /// 
        /// Property: For all passwords, BCrypt verification is case-sensitive
        /// 
        /// This test verifies that BCrypt correctly distinguishes between:
        /// - "Password" and "password"
        /// - "PASSWORD" and "password"
        /// 
        /// This behavior must be preserved after security fixes.
        /// </summary>
        [Theory]
        [InlineData("Password", "password")]
        [InlineData("PASSWORD", "password")]
        [InlineData("MixedCase", "mixedcase")]
        [InlineData("TestPassword123", "testpassword123")]
        [InlineData("UPPERCASE", "uppercase")]
        public void BCryptVerify_IsCaseSensitive(string password, string differentCasePassword)
        {
            // Arrange
            string hashedPassword = PasswordHasher.HashPassword(password);

            // Act
            bool correctPasswordVerifies = PasswordHasher.VerifyPassword(password, hashedPassword);
            bool differentCaseFails = !PasswordHasher.VerifyPassword(differentCasePassword, hashedPassword);

            // Assert
            Assert.True(correctPasswordVerifies, $"Correct password should verify: {password}");
            Assert.True(differentCaseFails, $"Different case password should fail: {differentCasePassword}");
        }

        /// <summary>
        /// Property Test 2.2.7: BCrypt hashing is idempotent in verification
        /// 
        /// **Validates: Requirement 3.5**
        /// 
        /// Property: For all passwords and hashes, verification produces consistent results
        /// 
        /// This test verifies that calling VerifyPassword multiple times with the same
        /// inputs produces the same result every time.
        /// 
        /// This behavior must be preserved after security fixes.
        /// </summary>
        [Theory]
        [InlineData("password123")]
        [InlineData("P@ssw0rd!")]
        [InlineData("TestPassword")]
        [InlineData("ComplexP@ss123!")]
        public void BCryptVerify_IsIdempotent(string password)
        {
            // Arrange
            string hashedPassword = PasswordHasher.HashPassword(password);

            // Act - Verify multiple times
            bool result1 = PasswordHasher.VerifyPassword(password, hashedPassword);
            bool result2 = PasswordHasher.VerifyPassword(password, hashedPassword);
            bool result3 = PasswordHasher.VerifyPassword(password, hashedPassword);

            // Assert - All results should be the same
            Assert.True(result1, "First verification should succeed");
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
        }

        /// <summary>
        /// Unit Test 2.2.8: BCrypt handles empty string edge case
        /// 
        /// **Validates: Requirement 3.4, 3.5**
        /// 
        /// This test verifies BCrypt's behavior with empty strings.
        /// While empty passwords shouldn't be allowed by validation,
        /// BCrypt should handle them gracefully.
        /// </summary>
        [Fact]
        public void BCrypt_HandlesEmptyString_Gracefully()
        {
            // Arrange
            string emptyPassword = "";

            // Act & Assert - Should not throw exception
            string hashedPassword = PasswordHasher.HashPassword(emptyPassword);
            Assert.NotNull(hashedPassword);
            Assert.NotEmpty(hashedPassword);
            
            bool verifies = PasswordHasher.VerifyPassword(emptyPassword, hashedPassword);
            Assert.True(verifies);
        }

        /// <summary>
        /// Property Test 2.2.9: BCrypt handles maximum length passwords
        /// 
        /// **Validates: Requirement 3.4, 3.5**
        /// 
        /// BCrypt has a maximum password length of 72 characters.
        /// This test verifies that passwords at or near this limit work correctly.
        /// </summary>
        [Fact]
        public void BCrypt_HandlesMaximumLengthPassword_Correctly()
        {
            // Arrange - Create a 72-character password (BCrypt's maximum)
            string maxLengthPassword = new string('a', 72);

            // Act
            string hashedPassword = PasswordHasher.HashPassword(maxLengthPassword);
            bool verifies = PasswordHasher.VerifyPassword(maxLengthPassword, hashedPassword);

            // Assert
            Assert.True(verifies, "BCrypt should handle 72-character password correctly");
            Assert.NotEmpty(hashedPassword);
            Assert.StartsWith("$2", hashedPassword);
        }

        /// <summary>
        /// Property Test 2.2.10: BCrypt handles numeric-only passwords
        /// 
        /// **Validates: Requirement 3.4, 3.5**
        /// 
        /// This test verifies that BCrypt correctly handles passwords
        /// that contain only numeric characters.
        /// </summary>
        [Theory]
        [InlineData("12345678")]
        [InlineData("00000000")]
        [InlineData("98765432")]
        [InlineData("123")]
        [InlineData("999999999999")]
        public void BCrypt_HandlesNumericPasswords_Correctly(string numericPassword)
        {
            // Act
            string hashedPassword = PasswordHasher.HashPassword(numericPassword);
            bool verifies = PasswordHasher.VerifyPassword(numericPassword, hashedPassword);
            bool wrongPasswordFails = !PasswordHasher.VerifyPassword(numericPassword + "1", hashedPassword);

            // Assert
            Assert.True(verifies, $"BCrypt should verify numeric password: {numericPassword}");
            Assert.True(wrongPasswordFails, "BCrypt should reject incorrect numeric password");
            Assert.NotEmpty(hashedPassword);
            Assert.StartsWith("$2", hashedPassword);
        }

        /// <summary>
        /// Helper method to mask passwords in test output for security
        /// </summary>
        private static string MaskPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return "[empty]";
            if (password.Length <= 3)
                return "***";
            return password.Substring(0, 1) + "***" + password.Substring(password.Length - 1);
        }
    }
}
