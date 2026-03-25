using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MonkoraEdge.Core.DotNet.Security.Hashing
{
    public static class HmacHelper
    {
        /// <summary>
        /// Computes HMAC-SHA256 of <paramref name="input"/> using a separate <paramref name="key"/>.
        /// </summary>
        public static string HmacHash256(string key, string input, bool lowerCase = false)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            var hex = Convert.ToHexString(bytes);
            return lowerCase ? hex.ToLowerInvariant() : hex;
        }

        /// <summary>
        /// Serializes <paramref name="obj"/> to JSON and computes its HMAC-SHA256 using <paramref name="key"/>.
        /// </summary>
        public static string ComputeHmacSha256Hash(string key, object obj)
            => HmacHash256(key, JsonSerializer.Serialize(obj));
    }
}
