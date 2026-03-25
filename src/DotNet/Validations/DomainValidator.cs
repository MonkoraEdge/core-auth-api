using System.Text.RegularExpressions;

namespace MonkoraEdge.Core.DotNet.Validations
{
    public static class DomainValidator
    {
        private static readonly Regex _domain = new(
            @"^(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}"
            + @"[a-zA-Z0-9])?\.)+[A-Za-z]{2,}$",
            RegexOptions.Compiled);

        public static bool IsValid(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return false;

            return _domain.IsMatch(domain.Trim());
        }
    }
}
