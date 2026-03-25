using System.Security.Cryptography;
using System.Text;

namespace MonkoraEdge.Core.DotNet.Utilities
{
    public static class StringHelper
    {
        public static string Normalize(string input)
            => input.Trim().Replace("\r", "").Replace("\n", "");

        public static string RemoveSpaces(string input)
            => new(input.Where(c => !char.IsWhiteSpace(c)).ToArray());

        public static string Truncate(string input, int max)
            => input.Length <= max ? input : input[..max];

        public static bool SecureEquals(string a, string b)
            => CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(a),
                Encoding.UTF8.GetBytes(b));

        public static string MaskString(string input)
        {
            var random = new Random();
            var maskedChars = new char[input.Length];

            for (var i = 0; i < input.Length; i++)
            {
                if (input[i] != ' ' && random.Next(2) == 1)
                {
                    maskedChars[i] = '*';
                }
                else
                {
                    maskedChars[i] = input[i];
                }
            }

            return new string(maskedChars);
        }

        public static string MaskNumber(string input)
        {
            var random = new Random();
            var maskedChars = new char[input.Length];

            for (var i = 0; i < input.Length; i++)
            {
                if (char.IsDigit(input[i]) && random.Next(2) == 1)
                {
                    maskedChars[i] = 'X';
                }
                else
                {
                    maskedChars[i] = input[i];
                }
            }

            return new string(maskedChars);
        }
    }
}
