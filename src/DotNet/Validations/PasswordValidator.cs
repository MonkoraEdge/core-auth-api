using System.Text.RegularExpressions;

namespace MonkoraEdge.Core.DotNet.Validations
{
    public static class PasswordValidator
    {
        private static readonly Regex _policy =
            new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)"
               + @"(?=.*[!@#$%^&*()_+\-=\[\]{};:'""\\|,.<>\/?]).{8,}$",
               RegexOptions.Compiled);

        public static bool IsValid(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            if (password.Contains(" "))
                return false;

            return _policy.IsMatch(password);
        }
    }
}
