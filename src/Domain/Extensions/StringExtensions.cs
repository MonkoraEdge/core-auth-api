namespace MonkoraEdge.Core.Auth.Domain.Extensions;

public static class StringExtensions
{
    public static string RemoveSpace(this string value)
    {
        return value.Trim()
            .Replace("\u200B", "")
            .Replace("\u200C", "")
            .Replace("\u200D", "")
            .Replace("\uFEFF", "");
    }
}