using MonkoraEdge.Core.DotNet.Domain.ValueObjects;

namespace MonkoraEdge.Core.DotNet.Extensions
{
    public static class MimeTypeExtensions
    {
        private static readonly Dictionary<string, MimeType> _map =
            MimeType.GetAll<MimeType>()
                .ToDictionary(
                    x => x.Name,
                    x => x,
                    StringComparer.OrdinalIgnoreCase);

        public static MimeType ToMimeType(this string mimeString)
        {
            if (string.IsNullOrWhiteSpace(mimeString))
                throw new ArgumentException("MIME type cannot be null or empty.", nameof(mimeString));

            if (_map.TryGetValue(mimeString, out var mime))
                return mime;

            throw new NotSupportedException($"Unsupported MIME type: {mimeString}");
        }

        public static bool TryToMimeType(this string? mimeString, out MimeType? mimeType)
        {
            mimeType = null;

            if (string.IsNullOrWhiteSpace(mimeString))
                return false;

            return _map.TryGetValue(mimeString, out mimeType);
        }


        public static string ToMimeString(this MimeType mimeType)
        {
            if (mimeType is null)
                throw new ArgumentNullException(nameof(mimeType));

            return mimeType.Name;
        }

        public static bool IsSupported(this string mimeString)
        {
            return MimeType
                .GetAll<MimeType>()
                .Any(x => x.Name.Equals(
                    mimeString,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}

//if (!file.ContentType.IsSupported())
//    throw new Exception("File type not allowed");

//public static readonly MimeType Svg =
//    new(5, "image/svg+xml", "IMAGE");