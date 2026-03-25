using System.Text;

namespace MonkoraEdge.Core.DotNet.Security.Encryption
{
    public static class Base64UrlHelper
    {
        /// <summary>
        /// Encode bytes into Base64Url without padding following RFC 4648.
        /// </summary>
        public static string Encode(byte[] bytes)
        {
            // Standard Base64 Encode
            string base64 = Convert.ToBase64String(bytes);

            // Convert to Base64Url
            return base64
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        /// <summary>
        /// Encode string (UTF-8) into Base64Url.
        /// </summary>
        public static string Encode(string plainText)
        {
            var bytes = Encoding.UTF8.GetBytes(plainText);
            return Encode(bytes);
        }

        /// <summary>
        /// Decode Base64Url string back to bytes.
        /// </summary>
        public static byte[] DecodeToBytes(string base64Url)
        {
            string base64 = base64Url.Replace("-", "+").Replace("_", "/");

            // Add padding back if missing
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            return Convert.FromBase64String(base64);
        }

        /// <summary>
        /// Decode Base64Url string back to UTF-8 string.
        /// </summary>
        public static string Decode(string base64Url)
        {
            var bytes = DecodeToBytes(base64Url);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
