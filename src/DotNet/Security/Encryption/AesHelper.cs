using System.Security.Cryptography;
using System.Text;

namespace MonkoraEdge.Core.DotNet.Security.Encryption
{
    //Encrypt PDPA data
    public static class AesHelper
    {
        private const int KeySize = 32;  // 256-bit key

        // ------------------- ENCRYPT -------------------
        public static string Encrypt(string plainText, string key)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                throw new ArgumentNullException(nameof(plainText));

            var keyBytes = GetKeyBytes(key);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                var bytes = Encoding.UTF8.GetBytes(plainText);
                cs.Write(bytes, 0, bytes.Length);
                cs.FlushFinalBlock();
            }

            return $"{Convert.ToBase64String(aes.IV)}:{Convert.ToBase64String(ms.ToArray())}";
        }

        // ------------------- DECRYPT -------------------
        public static string Decrypt(string encryptedText, string key)
        {
            if (string.IsNullOrWhiteSpace(encryptedText))
                throw new ArgumentNullException(nameof(encryptedText));

            var parts = encryptedText.Split(':');
            if (parts.Length != 2)
                throw new FormatException("Invalid encrypted format. Expected 'IV:CipherText'.");

            byte[] iv = Convert.FromBase64String(parts[0]);
            byte[] cipherBytes = Convert.FromBase64String(parts[1]);
            byte[] keyBytes = GetKeyBytes(key);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(cipherBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs, Encoding.UTF8);

            return reader.ReadToEnd();
        }

        // ------------------- KEY NORMALIZER -------------------
        private static byte[] GetKeyBytes(string key)
        {
            var bytes = Encoding.UTF8.GetBytes(key);

            if (bytes.Length < KeySize)
            {
                var padded = new byte[KeySize];
                Buffer.BlockCopy(bytes, 0, padded, 0, bytes.Length);
                return padded;
            }

            if (bytes.Length > KeySize)
            {
                using var sha = SHA256.Create();
                return sha.ComputeHash(bytes);
            }

            return bytes;
        }
    }
}
