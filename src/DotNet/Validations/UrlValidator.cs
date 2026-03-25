using System.Text.RegularExpressions;

namespace MonkoraEdge.Core.DotNet.Validations
{
    public static class UrlValidator
    {
        private static readonly Regex _url = new(
            @"^(https?|ftp):\/\/[^\s/$.?#].[^\s]*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool IsValid(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return _url.IsMatch(url.Trim());
        }
    }
}
