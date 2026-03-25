using System.Security.Cryptography;

namespace MonkoraEdge.Core.DotNet.Security.Signing
{
    public static class JwtSigningHelper
    {
        public static byte[] GenerateHmacKey()
            => RandomNumberGenerator.GetBytes(32); // HS256

        public static RSA GenerateRsaKey(int size = 2048)
            => RSA.Create(size);
    }
}
