using System.Security.Cryptography;
using System.Text;

namespace TechNova_IT_Solutions.Services
{
    /// <summary>
    /// Provides secure password generation and handling following OWASP standards.
    /// </summary>
    public static class SecurePasswordService
    {
        private const int PasswordLength = 16;
        private const string UpperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LowerCase = "abcdefghijklmnopqrstuvwxyz";
        private const string Numbers = "0123456789";
        private const string Symbols = "!@#$%^&*-_=+";

        /// <summary>
        /// Generates a cryptographically secure random password.
        /// Password includes uppercase, lowercase, numbers, and symbols for strength.
        /// </summary>
        public static string GenerateSecurePassword(int length = PasswordLength)
        {
            if (length < 12) length = 12;

            var allChars = UpperCase + LowerCase + Numbers + Symbols;
            var result = new StringBuilder();

            // Ensure at least one character from each category
            using (var rng = new RNGCryptoServiceProvider())
            {
                result.Append(GetRandomChar(UpperCase, rng));
                result.Append(GetRandomChar(LowerCase, rng));
                result.Append(GetRandomChar(Numbers, rng));
                result.Append(GetRandomChar(Symbols, rng));

                // Fill the rest randomly
                for (int i = result.Length; i < length; i++)
                {
                    result.Append(GetRandomChar(allChars, rng));
                }
            }

            // Shuffle the password
            var passwordArray = result.ToString().ToCharArray();
            using (var rng = new RNGCryptoServiceProvider())
            {
                ShuffleArray(passwordArray, rng);
            }

            return new string(passwordArray);
        }

        private static char GetRandomChar(string chars, RNGCryptoServiceProvider rng)
        {
            byte[] randomBytes = new byte[1];
            rng.GetBytes(randomBytes);
            return chars[randomBytes[0] % chars.Length];
        }

        private static void ShuffleArray(char[] array, RNGCryptoServiceProvider rng)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                byte[] randomBytes = new byte[1];
                rng.GetBytes(randomBytes);
                int j = randomBytes[0] % (i + 1);

                // Swap
                var temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }
    }
}
