using MonkoraEdge.Core.DotNet.Domain.ValueObjects;

namespace MonkoraEdge.Core.DotNet.Converter
{
    public static class MimeTypeConverter
    {
        private static readonly Dictionary<string, MimeType> _map =
            MonkoraEdge.Core.DotNet.Domain.SeedWork.Enumeration.GetAll<MimeType>()
                .ToDictionary(
                    x => x.Name,
                    x => x,
                    StringComparer.OrdinalIgnoreCase);

        public static MimeType FromMimeString(string mimeString)
        {
            if (string.IsNullOrWhiteSpace(mimeString))
                throw new ArgumentException("MIME type cannot be null or empty.", nameof(mimeString));

            if (_map.TryGetValue(mimeString, out var mime))
                return mime;

            throw new NotSupportedException($"Unsupported MIME type: {mimeString}");
        }

        public static bool TryFromMimeString(
            string mimeString,
            out MimeType mimeType)
        {
            mimeType = null;

            if (string.IsNullOrWhiteSpace(mimeString))
                return false;

            return _map.TryGetValue(mimeString, out mimeType);
        }
    }
}
