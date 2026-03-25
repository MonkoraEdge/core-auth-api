using System.Text.RegularExpressions;

namespace MonkoraEdge.Core.DotNet.Utilities
{
    public static class RegexHelper
    {
        public static bool IsMatch(string input, string pattern)
            => Regex.IsMatch(input, pattern);

        public static MatchCollection Matches(string input, string pattern)
            => Regex.Matches(input, pattern);

        public static string Replace(string input, string pattern, string replacement)
            => Regex.Replace(input, pattern, replacement);
    }
}
