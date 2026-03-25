using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MonkoraEdge.Core.DotNet.Security.Hashing
{
    //Used with passwords
    public static class SHAHelper
    {
        public static string HashSha256(string input, bool lowerCase = false)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var hex = Convert.ToHexString(bytes);
            return lowerCase ? hex.ToLowerInvariant() : hex;
        }

        public static string HashSha512(string input, bool lowerCase = false)
        {
            using var sha = SHA512.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var hex = Convert.ToHexString(bytes);
            return lowerCase ? hex.ToLowerInvariant() : hex;
        }

        public static string HashSha256File(string filePath)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(filePath);
            var hash = sha.ComputeHash(fs);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static string ComputeSha256Hash(object obj)
            => HashSha256(JsonSerializer.Serialize(obj));

        public static string ComputeSha512Hash(object obj)
            => HashSha512(JsonSerializer.Serialize(obj));

    }
}
