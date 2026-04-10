using System.Security.Cryptography;
using System.Text;

namespace MonkoraEdge.Core.DotNet.Security.Encryption
{
    // Encrypt PDPA data — uses AES-256-GCM (AEAD) for confidentiality + integrity.
    // AES-CBC + PKCS7 was replaced: CBC without a MAC is vulnerable to padding-oracle attacks
    // (POODLE / BEAST variants). GCM authenticates the ciphertext with a 128-bit tag, so any
    // tampering is detected before decryption, eliminating the oracle entirely.
    //
    // Wire format (Encrypt output): "<nonce_b64>:<tag_b64>:<ciphertext_b64>"
    // IMPORTANT: existing CBC-encrypted values must be re-encrypted before deploying this change.
    public static class AesHelper
    {
        private const int KeySize   = 32; // 256-bit key
        private const int NonceSize = 12; // 96-bit nonce (NIST recommended for GCM)
        private const int TagSize   = 16; // 128-bit authentication tag

        // ------------------- ENCRYPT -------------------
        public static string Encrypt(string plainText, string key)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                throw new ArgumentNullException(nameof(plainText));

            var keyBytes   = GetKeyBytes(key);
            var nonce      = RandomNumberGenerator.GetBytes(NonceSize);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipher     = new byte[plainBytes.Length];
            var tag        = new byte[TagSize];

            using var aesGcm = new AesGcm(keyBytes, TagSize);
            aesGcm.Encrypt(nonce, plainBytes, cipher, tag);

            // Prepend nonce and tag so Decrypt is self-contained — no external IV/key store needed.
            return $"{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(tag)}:{Convert.ToBase64String(cipher)}";
        }

        // ------------------- DECRYPT -------------------
        public static string Decrypt(string encryptedText, string key)
        {
            if (string.IsNullOrWhiteSpace(encryptedText))
                throw new ArgumentNullException(nameof(encryptedText));

            var parts = encryptedText.Split(':');
            if (parts.Length != 3)
                throw new FormatException("Invalid encrypted format. Expected 'nonce:tag:ciphertext'.");

            var nonce      = Convert.FromBase64String(parts[0]);
            var tag        = Convert.FromBase64String(parts[1]);
            var cipher     = Convert.FromBase64String(parts[2]);
            var keyBytes   = GetKeyBytes(key);
            var plainBytes = new byte[cipher.Length];

            // AesGcm.Decrypt throws CryptographicException if the tag does not match —
            // this prevents partial decryption of tampered ciphertext (padding-oracle impossible).
            using var aesGcm = new AesGcm(keyBytes, TagSize);
            aesGcm.Decrypt(nonce, cipher, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
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
                return SHA256.HashData(bytes);

            return bytes;
        }
    }
}
