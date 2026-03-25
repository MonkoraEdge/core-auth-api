using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate.Interfaces;

namespace MonkoraEdge.Core.DotNet.Extensions
{
    public static class LocalizationExtensions
    {
        public static string Localize(this ILocalization localizer, string key)
            => localizer.GetText(key);

        public static string Localize(this ILocalization localizer, string key, string culture)
            => localizer.GetText(key, culture);
    }
}
