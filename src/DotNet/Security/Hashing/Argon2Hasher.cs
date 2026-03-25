using Konscious.Security.Cryptography;
using System.Text;

namespace MonkoraEdge.Core.DotNet.Security.Hashing
{
    //High security (OWASP-recommended: Argon2id)
    public static class Argon2Hasher
    {
        // OWASP recommended minimum: 64 MB memory, 3 iterations, 4 parallelism
        // Adjust upward based on available hardware for higher security contexts.
        public static async Task<string> HashAsync(string input, int memorySizeKb = 65536, int iterations = 3, int parallelism = 4)
        {
            var argon = new Argon2id(Encoding.UTF8.GetBytes(input))
            {
                DegreeOfParallelism = parallelism,
                Iterations = iterations,
                MemorySize = memorySizeKb // default 64 MB (65536 KB)
            };

            var hash = await argon.GetBytesAsync(32);
            return Convert.ToHexString(hash);
        }
    }
}
