using System.Text;

namespace MonkoraEdge.Core.DotNet.Utilities
{
    public static class StreamHelper
    {
        public static byte[] ToBytes(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        public static Stream ToStream(byte[] bytes)
            => new MemoryStream(bytes);

        public static async Task<string> ReadAsStringAsync(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
            stream.Position = 0;
            return await reader.ReadToEndAsync();
        }

        public static Stream ToStream(string text)
            => new MemoryStream(Encoding.UTF8.GetBytes(text));
    }
}
