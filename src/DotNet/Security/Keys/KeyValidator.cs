
namespace MonkoraEdge.Core.DotNet.Security.Keys
{
    public static class KeyValidator
    {
        public static bool ValidateAesKey(string key) => key.Length == 32;
        public static bool ValidateAesIv(string iv) => iv.Length == 16;
        public static bool ValidateHexKey(string hex) => hex.Length % 2 == 0;
    }
}
