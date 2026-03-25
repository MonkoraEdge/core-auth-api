using System.Text.RegularExpressions;

namespace MonkoraEdge.Core.DotNet.Validations
{
    public static class PhoneValidator
    {
        // E.164 format: +66891234567
        private static readonly Regex _e164 = new(@"^\+[1-9]\d{7,14}$",
            RegexOptions.Compiled);

        // Thai mobile phone: 0812345678 or +66812345678
        private static readonly Regex _thai = new(@"^(0[689]\d{8})$",
            RegexOptions.Compiled);

        public static bool IsValid(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            phone = phone.Replace(" ", "").Replace("-", "").Trim();

            return _e164.IsMatch(phone) || _thai.IsMatch(phone);
        }

        public static bool TryNormalize(string phone, out string normalized)
        {
            normalized = string.Empty;

            if (string.IsNullOrWhiteSpace(phone))
                return false;

            phone = phone.Replace(" ", "").Replace("-", "").Trim();

            // Convert Thai phone: 0812345678 → +66812345678
            if (_thai.IsMatch(phone))
            {
                normalized = "+66" + phone.Substring(1);
                return true;
            }

            if (_e164.IsMatch(phone))
            {
                normalized = phone;
                return true;
            }

            return false;
        }
    }
}
