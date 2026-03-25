using System.Globalization;

namespace MonkoraEdge.Core.DotNet.Utilities
{
    public static class DateTimeHelper
    {
        public static void SetCultureInfo(string culture)
        {
            CultureInfo enCulture = new CultureInfo(culture);

            // Set current thread's culture
            System.Threading.Thread.CurrentThread.CurrentCulture = enCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = enCulture;
        }
    }
}
