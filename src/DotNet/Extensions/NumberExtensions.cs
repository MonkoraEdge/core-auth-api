
namespace MonkoraEdge.Core.DotNet.Extensions
{
    public static class NumberExtensions
    {
        public static decimal? ToPercentage(this decimal? value)
        {
            return value * 100;
        }

        public static decimal ToDigits(this decimal input, int digit = 2)
        {
            return Math.Round(input, digit);
        }

        public static decimal? ToDigits(this decimal? input, int digit = 2)
        {
            if (input.HasValue)
            {
                return Math.Round(input.Value, digit);
            }

            return null;
        }
    }
}
