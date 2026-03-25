using FluentValidation.Results;
using MonkoraEdge.Core.DotNet.Validations;

namespace MonkoraEdge.Core.DotNet.Extensions
{
    //Application
    public static class ValidationExtensions
    {
        public static List<string> ToMessages(this IEnumerable<ValidationFailure> failures)
            => failures.Select(x => x.ErrorMessage).ToList();

        public static Dictionary<string, string[]> ToDictionary(this IEnumerable<ValidationFailure> failures)
            => failures.GroupBy(x => x.PropertyName)
                       .ToDictionary(
                           g => g.Key,
                           g => g.Select(x => x.ErrorMessage).ToArray()
                       );

        public static bool IsValidEmail(this string value)
            => EmailValidator.IsValid(value);

        public static bool IsValidPhone(this string value)
            => PhoneValidator.IsValid(value);

        public static bool IsValidPassword(this string value)
            => PasswordValidator.IsValid(value);

        public static bool IsValidUrl(this string value)
            => UrlValidator.IsValid(value);

        public static bool IsValidDomain(this string value)
            => DomainValidator.IsValid(value);

        public static bool IsValidIp(this string value)
            => IpAddressValidator.IsValid(value);

        public static bool IsValidUsername(this string value)
            => UsernameValidator.IsValid(value);

        public static bool IsValidGuid(this string value)
            => GuidValidator.IsValid(value);

        public static bool IsValidDate(this string value)
            => DateTimeValidator.IsValid(value);
    }
}
