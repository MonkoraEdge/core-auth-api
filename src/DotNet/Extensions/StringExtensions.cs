
namespace MonkoraEdge.Core.DotNet.Extensions
{
    //Domain/Application
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string? value)
            => string.IsNullOrWhiteSpace(value);

        public static string ToSnakeCase(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return string.Concat(
                value.Select((c, i) =>
                    i > 0 && char.IsUpper(c)
                        ? "_" + c
                        : c.ToString()
                )
            ).ToLower();
        }

        public static string ToKebabCase(this string value)
            => value.Replace(' ', '-').ToLower();

        public static string OnlyDigits(this string value)
            => new(value.Where(char.IsDigit).ToArray());

        public static string Mask(this string value, int start, int length)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var mask = new string('*', length);
            return value.Remove(start, Math.Min(length, value.Length - start))
                        .Insert(start, mask);
        }
    }
}
