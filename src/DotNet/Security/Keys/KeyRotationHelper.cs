
namespace MonkoraEdge.Core.DotNet.Security.Keys
{
    public static class KeyRotationHelper
    {
        public static string RotateBase64Key(int length)
            => KeyGenerator.GenerateBase64(length);

        public static string RotateBase64UrlKey(int length)
            => KeyGenerator.GenerateBase64Url(length);

        public static string RotateHexKey(int length)
            => KeyGenerator.GenerateHex(length);
    }
}
