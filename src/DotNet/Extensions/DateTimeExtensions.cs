
using System.Globalization;
using System.Runtime.InteropServices;

namespace MonkoraEdge.Core.DotNet.Extensions
{
    //Domain/Application
    public static class DateTimeExtensions
    {
        public static long ToUnixTimestamp(this DateTime value)
            => new DateTimeOffset(value).ToUnixTimeSeconds();

        public static void SetCultureInfo(string culture)
        {
            CultureInfo enCulture = new CultureInfo(culture);

            // Set current thread's culture
           Thread.CurrentThread.CurrentCulture = enCulture;
           Thread.CurrentThread.CurrentUICulture = enCulture;
        }

        public static bool IsWeekend(this DateTime value)
            => value.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        public static string ToIsoString(this DateTime value)
            => value.ToString("yyyy-MM-ddTHH:mm:ssZ");

        public static DateTime StartOfDay(this DateTime value)
            => value.Date;

        public static DateTime EndOfDay(this DateTime value)
            => value.Date.AddDays(1).AddTicks(-1);

        public static long ToUnixTime(this DateTime dateTime)
        {
            DateTime dateTime1 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(dateTime - dateTime1).TotalSeconds;
        }

        public static DateTime ToDateTime(this long unixTime) => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds((double)unixTime);

        public static DateTime ToThaiDateTime(this DateTime dateTime) => dateTime.GmtToPacific(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "SE Asia Standard Time" : "Asia/Bangkok");

        public static DateTime GmtToPacific(this DateTime dateTime, string timeZoneId) => TimeZoneInfo.ConvertTimeFromUtc(dateTime, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));

    }
}
