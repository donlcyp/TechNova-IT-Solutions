using Xunit;
using TechNova_IT_Solutions.Infrastructure;

namespace TechNova.SecurityTests
{
    /// <summary>
    /// Tests for the SensitiveDataFilter to verify it correctly filters sensitive information
    /// from log messages.
    /// </summary>
    public class SensitiveDataFilterTests
    {
        [Fact]
        public void FilterSensitiveData_ShouldRedactPasswords()
        {
            // Arrange
            string message = "User login failed with password=SecretPass123";
            
            // Act
            string filtered = SensitiveDataFilter.FilterSensitiveData(message);
            
            // Assert
            Assert.DoesNotContain("SecretPass123", filtered);
            Assert.Contains("[REDACTED]", filtered);
        }

        [Fact]
        public void FilterSensitiveData_ShouldRedactTokens()
        {
            // Arrange
            string message = "API call failed with token=abc123xyz456";
            
            // Act
            string filtered = SensitiveDataFilter.FilterSensitiveData(message);
            
            // Assert
            Assert.DoesNotContain("abc123xyz456", filtered);
            Assert.Contains("[REDACTED]", filtered);
        }

        [Fact]
        public void FilterSensitiveData_ShouldRedactCreditCardNumbers()
        {
            // Arrange
            string message = "Payment failed for card 4532-1234-5678-9010";
            
            // Act
            string filtered = SensitiveDataFilter.FilterSensitiveData(message);
            
            // Assert
            Assert.DoesNotContain("4532-1234-5678-9010", filtered);
            Assert.Contains("9010", filtered); // Last 4 digits should be kept
        }

        [Fact]
        public void FilterSensitiveData_ShouldRedactSSNs()
        {
            // Arrange
            string message = "User SSN is 123-45-6789";
            
            // Act
            string filtered = SensitiveDataFilter.FilterSensitiveData(message);
            
            // Assert
            Assert.DoesNotContain("123-45-6789", filtered);
            Assert.Contains("6789", filtered); // Last 4 digits should be kept
        }

        [Fact]
        public void ContainsSensitiveData_ShouldDetectPasswords()
        {
            // Arrange
            string message = "Failed to authenticate with password=test123";
            
            // Act
            bool containsSensitive = SensitiveDataFilter.ContainsSensitiveData(message);
            
            // Assert
            Assert.True(containsSensitive);
        }

        [Fact]
        public void ContainsSensitiveData_ShouldReturnFalseForCleanMessages()
        {
            // Arrange
            string message = "User logged in successfully";
            
            // Act
            bool containsSensitive = SensitiveDataFilter.ContainsSensitiveData(message);
            
            // Assert
            Assert.False(containsSensitive);
        }

        [Fact]
        public void FilterSensitiveData_ShouldHandleMultiplePatterns()
        {
            // Arrange
            string message = "Login failed: password=Secret123, token=xyz789, card=4532123456789010";
            
            // Act
            string filtered = SensitiveDataFilter.FilterSensitiveData(message);
            
            // Assert
            Assert.DoesNotContain("Secret123", filtered);
            Assert.DoesNotContain("xyz789", filtered);
            Assert.DoesNotContain("4532123456789010", filtered);
            Assert.Contains("[REDACTED]", filtered);
        }
    }
}
