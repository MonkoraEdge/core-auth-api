using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.DotNet.Domain.ValueObjects
{
    public sealed class MimeType : Enumeration
    {
        public string Category { get; }

        private MimeType(int value, string name, string category)
            : base(value, name)
        {
            Category = category;
        }

        // ========================
        // Images
        // ========================
        public static readonly MimeType ImageJpeg =
            new(1, "image/jpeg", "IMAGE");

        public static readonly MimeType ImagePng =
            new(2, "image/png", "IMAGE");

        public static readonly MimeType ImageWebp =
            new(3, "image/webp", "IMAGE");

        public static readonly MimeType ImageGif =
            new(4, "image/gif", "IMAGE");

        // ========================
        // Documents
        // ========================
        public static readonly MimeType Pdf =
            new(10, "application/pdf", "DOCUMENT");

        public static readonly MimeType Docx =
            new(11, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "DOCUMENT");

        // ========================
        // Excel
        // ========================
        public static readonly MimeType ExcelXlsx =
            new(20, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DOCUMENT");

        // ========================
        // Text
        // ========================
        public static readonly MimeType TextPlain =
            new(30, "text/plain", "TEXT");

        public static readonly MimeType Csv =
            new(31, "text/csv", "TEXT");

        // ========================
        // Archive
        // ========================
        public static readonly MimeType Zip =
            new(40, "application/zip", "ARCHIVE");

        // ========================
        // Video
        // ========================
        public static readonly MimeType Mp4 =
            new(50, "video/mp4", "VIDEO");

        // ========================
        // Audio
        // ========================
        public static readonly MimeType Mp3 =
            new(60, "audio/mpeg", "AUDIO");
    }
}
