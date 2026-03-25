using System.Text.RegularExpressions;

namespace MonkoraEdge.Core.DotNet.Validations
{
    public static class UsernameValidator
    {
        private static readonly Regex _username = new(
            @"^[a-zA-Z0-9._-]{4,32}$",
            RegexOptions.Compiled);

        public static bool IsValid(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            return _username.IsMatch(username);
        }
    }
}
