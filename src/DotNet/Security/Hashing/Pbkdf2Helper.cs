using System.Security.Cryptography;
using System.Text;

namespace MonkoraEdge.Core.DotNet.Security.Hashing
{
    public static class Pbkdf2Helper
    {
        private const int DefaultIterations = 100000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        /// <summary>
        /// Returns "{base64(salt)}.{base64(hash)}.{iterations}" so Verify can always use
        /// the same parameters that were used at hash time.
        /// </summary>
        public static string HashPassword(string password, int iterations = DefaultIterations)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}.{iterations}";
        }

        public static bool Verify(string password, string storedHash)
        {
            var parts = storedHash.Split('.');
            // Support both legacy (2-part) and current (3-part) format
            if (parts.Length < 2)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var hash = Convert.FromBase64String(parts[1]);
            var iterations = parts.Length >= 3 && int.TryParse(parts[2], out var i) ? i : DefaultIterations;

            var testHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return CryptographicOperations.FixedTimeEquals(hash, testHash);
        }
    }
}
