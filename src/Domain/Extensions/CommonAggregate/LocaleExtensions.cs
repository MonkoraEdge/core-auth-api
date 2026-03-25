using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;

namespace MonkoraEdge.Core.Auth.Domain.Extensions.CommonAggregate;

public static class LocaleExtensions
{
    public static bool Contains(this Locale locale, string compare)
    {
        return locale.EN.Contains(compare) || locale.TH.Contains(compare) || locale.ZH.Contains(compare);
    }

    public static bool Contains(this Locale locale, string[] compare)
    {
        return compare.Any(locale.Contains);
    }

    public static Locale ToLocale(this string text, bool en, bool th, bool zh)
    {
        return new Locale
        {
            EN = en ? text : null,
            TH = th ? text : null,
            ZH = zh ? text : null,
        };
    }
}