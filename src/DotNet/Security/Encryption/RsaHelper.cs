using System.Security.Cryptography;
using System.Text;

namespace MonkoraEdge.Core.DotNet.Security.Encryption
{
    //Public/Private Key
    public static class RsaHelper
    {
        public static string Encrypt(string publicKey, string data)
        {
            using var rsa = RSA.Create();
            rsa.FromXmlString(publicKey);

            var bytes = Encoding.UTF8.GetBytes(data);
            var encrypted = rsa.Encrypt(bytes, RSAEncryptionPadding.OaepSHA256);

            return Convert.ToBase64String(encrypted);
        }

        public static string Decrypt(string privateKey, string cipher)
        {
            using var rsa = RSA.Create();
            rsa.FromXmlString(privateKey);

            var cipherBytes = Convert.FromBase64String(cipher);
            var decrypted = rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256);

            return Encoding.UTF8.GetString(decrypted);
        }
    }
}
