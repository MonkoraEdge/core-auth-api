namespace MonkoraEdge.Core.DotNet.Validations
{
    public static class FileValidator
    {
        public static bool IsAllowedExtension(string fileName, params string[] extensions)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return extensions.Any(e => e.ToLower() == ext);
        }

        public static bool IsSizeWithinLimit(long sizeBytes, long maxSizeBytes)
            => sizeBytes <= maxSizeBytes;
    }
}
