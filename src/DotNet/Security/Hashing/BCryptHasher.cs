
namespace MonkoraEdge.Core.DotNet.Security.Hashing
{
    //Used with passwords
    public static class BCryptHasher
    {
        public static string Hash(string input)
            => BCrypt.Net.BCrypt.HashPassword(input);

        public static bool Verify(string input, string hash)
            => BCrypt.Net.BCrypt.Verify(input, hash);
    }
}
