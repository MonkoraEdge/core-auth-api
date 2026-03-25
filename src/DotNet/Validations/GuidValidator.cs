namespace MonkoraEdge.Core.DotNet.Validations
{
    public static class GuidValidator
    {
        public static bool IsValid(string value)
            => Guid.TryParse(value, out _);
    }
}
