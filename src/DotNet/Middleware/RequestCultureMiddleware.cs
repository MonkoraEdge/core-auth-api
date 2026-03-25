using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace MonkoraEdge.Core.DotNet.Middleware
{
    /*
         
     Accept-Language: en-US,en;q=0.8 // header

     */
    public class RequestCultureMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly string DefaultCulture = "en-US";

        private static readonly HashSet<string> SupportedCultures = new()
    {
        "en-US",
        "th-TH"
    };

        public RequestCultureMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            string cultureCode = null;

            // 1? Query param: ?lang=th, en, en-us
            if (context.Request.Query.TryGetValue("lang", out var queryCulture))
            {
                cultureCode = ConvertToStandardCulture(queryCulture.ToString());
            }

            // 2? Header Accept-Language: en-US,en;q=0.8
            if (cultureCode == null &&
                context.Request.Headers.TryGetValue("Accept-Language", out var headerCulture))
            {
                var firstLang = headerCulture.ToString().Split(',').FirstOrDefault();
                cultureCode = ConvertToStandardCulture(firstLang);
            }

            // 3? Validate Culture
            if (cultureCode == null || !SupportedCultures.Contains(cultureCode))
            {
                cultureCode = DefaultCulture;
            }

            var culture = new CultureInfo(cultureCode);

            // 4? Apply culture globally
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            await _next(context);
        }

        private static string ConvertToStandardCulture(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            input = input.Trim().ToLower();

            return input switch
            {
                "en" => "en-US",
                "en-us" => "en-US",
                "us" => "en-US",

                "th" => "th-TH",
                "th-th" => "th-TH",

                _ => null // invalid  null
            };
        }
    }
}
