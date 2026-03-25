using MonkoraEdge.Core.DotNet.Security.Hashing;
using System.Security.Cryptography;

namespace MonkoraEdge.Core.DotNet.Security.Random
{
    public static class SecureRandomHelper
    {
        private const string Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private const string Base62CharsSymbol = Base62Chars + "@#$%!";

        public static byte[] GenerateSecureBytes(int length)
            => RandomNumberGenerator.GetBytes(length);

        public static int GenerateSecureInt(int min, int max)
            => RandomNumberGenerator.GetInt32(min, max);

        public static string GenerateSecureToken(int length = 32)
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(length));

        public static Guid NewSequentialGuid()
        {
            var random = RandomNumberGenerator.GetBytes(10);
            var timestamp = BitConverter.GetBytes(DateTime.UtcNow.Ticks);

            var guidBytes = new byte[16];
            Array.Copy(timestamp, 2, guidBytes, 0, 6);
            Array.Copy(random, 0, guidBytes, 6, 10);

            return new Guid(guidBytes);
        }

        public static string GenerateSecureRandomString(int length, string chars)
        {
            var bytes = RandomNumberGenerator.GetBytes(length);
            var result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = chars[bytes[i] % chars.Length];
            }

            return new string(result);
        }

        public static string GenerateRandomString(int length, bool isSymbol)
        {
            var chars = "";
            if (isSymbol)
            {
                chars = Base62CharsSymbol;
            }
            else
            {
                chars = Base62Chars;
            }

            return GenerateSecureRandomString(length, chars);
        }

        public static string GenerateRandomInt(int length)
            => GenerateSecureRandomString(length, "0123456789");

        public static string NewIdentity(int keyLength = 32, bool isSymbol = true)
            => GenerateRandomString(keyLength, isSymbol);

        public static string NewIdentityInt(int keyLength = 32)
            => GenerateRandomInt(keyLength);

        public static string NewRandomSha256Hash()
            => SHAHelper.HashSha256($"{NewIdentity(32)}{DateTime.UtcNow:yyyyMMddhhmmssfffffff}");

        public static string NewRandomSha512Hash()
            => SHAHelper.HashSha512($"{NewIdentity(32)}{DateTime.UtcNow:yyyyMMddhhmmssfffffff}");

        public static string NewRandomHmacSha256Hash()
        {
            var identity = $"{NewIdentity(32)}{DateTime.UtcNow:yyyyMMddhhmmssfffffff}";
            // Use identity as both key and data for a self-keyed digest (non-MAC use-case)
            return HmacHelper.HmacHash256(identity, identity);
        }

        public static string GenerateBase62Key(int keyLength)
        {
            var bytes = RandomNumberGenerator.GetBytes(keyLength);
            var result = new char[keyLength];

            for (int i = 0; i < keyLength; i++)
            {
                result[i] = Base62Chars[bytes[i] % Base62Chars.Length];
            }

            return new string(result);
        }
    }
}
