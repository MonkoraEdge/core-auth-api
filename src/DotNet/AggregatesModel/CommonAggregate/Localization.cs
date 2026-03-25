using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate.Interfaces;
using MonkoraEdge.Core.DotNet.Utilities;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate
{
    public class Localization : ILocalization
    {
        public string GetText(string key)
            => LocalizationHelper.Text(key);

        public string GetText(string key, string culture)
            => LocalizationHelper.Text(key, culture);
    }
}
