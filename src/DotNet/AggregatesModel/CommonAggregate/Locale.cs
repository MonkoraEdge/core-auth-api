using System.Globalization;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate
{
    public class Locale
    {
        public string EN { get; set; }
        public string TH { get; set; }
        public string ZH { get; set; }

        public string Resolve(string culture = null)
        {
            culture ??= CultureInfo.CurrentUICulture.Name.ToLower();

            return culture switch
            {
                "en" or "en-us" or "en-gb" => EN,
                "th" or "th-th" => TH,
                "zh" or "zh-cn" or "zh-hk" => ZH,
                _ => EN
            };
        }

        public override string ToString()
            => Resolve();
    }
}
