namespace MonkoraEdge.Core.DotNet.Validations
{
    public static class DateTimeValidator
    {
        public static bool IsValid(string value)
            => DateTime.TryParse(value, out _);

        public static bool IsValidFormat(string value, string format)
            => DateTime.TryParseExact(value, format, null,
                System.Globalization.DateTimeStyles.None, out _);
    }
}
