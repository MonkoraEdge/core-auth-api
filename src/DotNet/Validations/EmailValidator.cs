using System.Globalization;
using System.Text.RegularExpressions;

namespace MonkoraEdge.Core.DotNet.Validations
{
    public static class EmailValidator
    {
        // RFC 5322 Official Email Regex (simplified for performance)
        private static readonly Regex _emailRegex = new Regex(
            @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|"
            + @"([-a-zA-Z0-9!#$%&'*+/=?^_`{|}~]+(?:\.[-a-zA-Z0-9!#$%&'*+/=?^_`{|}~]+)*)"
            + @")@((\[[0-9]{1,3}(\.[0-9]{1,3}){3}\])|"
            + @"(([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}))$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Validate email format (RFC 5322 + IDN)
        /// </summary>
        public static bool IsValid(string email, bool strict = false)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            if (!TryNormalize(email, out var normalized))
                return false;

            if (!_emailRegex.IsMatch(normalized))
                return false;

            if (strict)
                return HasValidDomain(normalized);

            return true;
        }

        /// <summary>
        /// Safe try-validate (no exception)
        /// </summary>
        public static bool TryValidate(string email)
            => IsValid(email);

        /// <summary>
        /// Normalize email: trim, lowercase domain, convert IDN domain → ASCII
        /// </summary>
        public static bool TryNormalize(string email, out string normalized)
        {
            normalized = string.Empty;

            if (string.IsNullOrWhiteSpace(email))
                return false;

            email = email.Trim();

            try
            {
                // split local-part and domain
                var parts = email.Split('@');
                if (parts.Length != 2)
                    return false;

                string local = parts[0];
                string domain = parts[1];

                // Normalize domain (IDN)
                var idn = new IdnMapping();
                string asciiDomain = idn.GetAscii(domain);

                normalized = $"{local}@{asciiDomain}".ToLowerInvariant();

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check DNS/domain rule (optional strict mode)
        /// Example: no consecutive hyphens, no starting hyphen, TLD rules, length rules
        /// </summary>
        private static bool HasValidDomain(string email)
        {
            try
            {
                var domain = email.Split('@')[1];

                if (domain.StartsWith('-') || domain.EndsWith('-'))
                    return false;

                if (domain.Contains("--"))
                    return false;

                // TLD minimum length
                if (!domain.Contains('.') || domain.Split('.').Last().Length < 2)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
