using MonkoraEdge.Core.DotNet.Validations;
using System.Text.RegularExpressions;

namespace MonkoraEdge.Core.DotNet.Extensions.Validations
{
    public class ValidatorExtensions
    {
        public static bool IsValidEmail(string email) =>  EmailValidator.IsValid(email);

        public static bool IsValidPhoneNumber(string phoneNumber) => Regex.IsMatch(phoneNumber, "^\\d+$");
    }
}
