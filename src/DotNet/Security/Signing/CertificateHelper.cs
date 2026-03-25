using System.Security.Cryptography.X509Certificates;

namespace MonkoraEdge.Core.DotNet.Security.Signing
{
    public static class CertificateHelper
    {
        public static X509Certificate2 LoadFromFile(string path, string? password = null)
            => password == null
                ? X509CertificateLoader.LoadCertificateFromFile(path)
                : X509CertificateLoader.LoadPkcs12FromFile(path, password);

        public static byte[] ExportPfx(X509Certificate2 cert, string password)
            => cert.Export(X509ContentType.Pfx, password);

        public static byte[] ExportPem(X509Certificate2 cert)
            => cert.Export(X509ContentType.Cert);
    }
}
