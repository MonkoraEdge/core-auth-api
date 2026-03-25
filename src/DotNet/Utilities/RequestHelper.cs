using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using Microsoft.AspNetCore.Http;

namespace MonkoraEdge.Core.DotNet.Utilities
{
    public class RequestHelper
    {
        public static bool IsLanguageThai()
            => Thread.CurrentThread.CurrentUICulture.Name.ToLower() == LanguageCode.TH || Thread.CurrentThread.CurrentUICulture.Name.ToLower() == "th-th";
        
        public static string? GetIp(HttpContext context)
            => context.Connection.RemoteIpAddress?.ToString();

        public static string? GetUserAgent(HttpContext context)
            => context.Request.Headers["User-Agent"].FirstOrDefault();

        public static string? GetBearerToken(HttpContext context)
            => context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

        public static string GetCorrelationId(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var id))
            {
                id = Guid.NewGuid().ToString();
                context.Response.Headers["X-Correlation-Id"] = id!;
            }
            return id!;
        }
    }
}
