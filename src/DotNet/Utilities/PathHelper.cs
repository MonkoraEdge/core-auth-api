
namespace MonkoraEdge.Core.DotNet.Utilities
{
    public static class PathHelper
    {
        public static string Combine(params string[] parts)
            => Path.Combine(parts);

        public static string Normalize(string path)
            => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);

        public static string SafeFileName(string fileName)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c, '_');
            return fileName;
        }

        public static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
