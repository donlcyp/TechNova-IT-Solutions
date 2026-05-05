using System.Text.RegularExpressions;

namespace TechNova_IT_Solutions.Infrastructure
{
    /// <summary>
    /// Filters sensitive data from log messages to prevent accidental exposure of
    /// passwords, tokens, credit card numbers, SSNs, and other PII.
    /// </summary>
    public static class SensitiveDataFilter
    {
        // Regex patterns for detecting sensitive data
        private static readonly Regex PasswordPattern = new Regex(
            @"(password|pwd|pass|passwd)[\s]*[=:]\s*[""']?([^""'\s,}]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TokenPattern = new Regex(
            @"(token|bearer|authorization|api[_-]?key|secret)[\s]*[=:]\s*[""']?([^""'\s,}]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex CreditCardPattern = new Regex(
            @"\b(?:\d{4}[-\s]?){3}\d{4}\b",
            RegexOptions.Compiled);

        private static readonly Regex SsnPattern = new Regex(
            @"\b\d{3}-\d{2}-\d{4}\b",
            RegexOptions.Compiled);

        private static readonly Regex EmailPasswordPattern = new Regex(
            @"(email[_-]?password|smtp[_-]?password)[\s]*[=:]\s*[""']?([^""'\s,}]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const string RedactedPlaceholder = "[REDACTED]";

        /// <summary>
        /// Filters sensitive data from a log message by replacing it with [REDACTED].
        /// </summary>
        /// <param name="message">The log message to filter</param>
        /// <returns>The filtered message with sensitive data redacted</returns>
        public static string FilterSensitiveData(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return message;
            }

            // Filter passwords
            message = PasswordPattern.Replace(message, m =>
                $"{m.Groups[1].Value}={RedactedPlaceholder}");

            // Filter tokens and API keys
            message = TokenPattern.Replace(message, m =>
                $"{m.Groups[1].Value}={RedactedPlaceholder}");

            // Filter email passwords
            message = EmailPasswordPattern.Replace(message, m =>
                $"{m.Groups[1].Value}={RedactedPlaceholder}");

            // Filter credit card numbers (keep last 4 digits)
            message = CreditCardPattern.Replace(message, m =>
            {
                var cardNumber = m.Value.Replace("-", "").Replace(" ", "");
                if (cardNumber.Length >= 4)
                {
                    var lastFour = cardNumber.Substring(cardNumber.Length - 4);
                    return $"****-****-****-{lastFour}";
                }
                return RedactedPlaceholder;
            });

            // Filter SSNs (keep last 4 digits)
            message = SsnPattern.Replace(message, m =>
            {
                var parts = m.Value.Split('-');
                if (parts.Length == 3)
                {
                    return $"***-**-{parts[2]}";
                }
                return RedactedPlaceholder;
            });

            return message;
        }

        /// <summary>
        /// Checks if a message contains sensitive data patterns.
        /// </summary>
        /// <param name="message">The message to check</param>
        /// <returns>True if sensitive data is detected, false otherwise</returns>
        public static bool ContainsSensitiveData(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            return PasswordPattern.IsMatch(message) ||
                   TokenPattern.IsMatch(message) ||
                   CreditCardPattern.IsMatch(message) ||
                   SsnPattern.IsMatch(message) ||
                   EmailPasswordPattern.IsMatch(message);
        }
    }
}
