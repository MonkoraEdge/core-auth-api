using System.Security.Cryptography;

namespace MonkoraEdge.Core.DotNet.Security.Keys
{
    public static class KeyGenerator
    {
        public static byte[] GenerateBytes(int length)
            => RandomNumberGenerator.GetBytes(length);

        public static string GenerateBase64(int length)
            => Convert.ToBase64String(GenerateBytes(length));

        public static string GenerateBase64Url(int length)
            => Encryption.Base64UrlHelper.Encode(GenerateBytes(length));

        public static string GenerateHex(int length)
            => Convert.ToHexString(GenerateBytes(length));
    }
}
