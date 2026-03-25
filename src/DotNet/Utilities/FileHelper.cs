
namespace MonkoraEdge.Core.DotNet.Utilities
{
    public static class FileHelper
    {
        public static async Task<string> ReadTextAsync(string path)
            => await File.ReadAllTextAsync(path);

        public static async Task WriteTextAsync(string path, string content)
            => await File.WriteAllTextAsync(path, content);

        public static byte[] ReadBytes(string path)
            => File.ReadAllBytes(path);

        public static async Task<byte[]> ReadBytesAsync(string path)
            => await File.ReadAllBytesAsync(path);

        public static async Task WriteBytesAsync(string path, byte[] data)
            => await File.WriteAllBytesAsync(path, data);

        public static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        public static bool IsAllowedExtension(string fileName, params string[] allowed)
            => allowed.Contains(Path.GetExtension(fileName).ToLower());

        public static bool IsSizeValid(long size, long maxSize)
            => size <= maxSize;
    }
}
