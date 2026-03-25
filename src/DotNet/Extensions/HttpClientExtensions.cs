using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace MonkoraEdge.Core.DotNet.Extensions
{
    public static class HttpClientExtensions
    {
        public static void ForwardAcceptLanguage(this HttpClient httpClient) => httpClient.DefaultRequestHeaders.Add("Accept-Language", Thread.CurrentThread.CurrentUICulture == null ? "en" : Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName);

        public static void ForwardAuthorization(this HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            if (!httpContextAccessor.HasAuthorization(out var authorization))
                throw new UnauthorizedAccessException();

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorization.Substring("Bearer ".Length).Trim());
        }

        public static void ForwardHeader(this HttpClient httpClient, IHttpContextAccessor httpContextAccessor, string headerKey)
        {
            if (httpContextAccessor.HttpContext is not null && httpContextAccessor.HttpContext.Request.Headers.ContainsKey(headerKey))
                httpClient.DefaultRequestHeaders.Add(headerKey, httpContextAccessor.HttpContext.Request.Headers[headerKey].ToString());
        }

        public static void ForwardTracing(this HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            httpClient.ForwardHeader(httpContextAccessor, StandardHeader.XApplication);
            httpClient.ForwardHeader(httpContextAccessor, StandardHeader.XTraceId);
        }

        public static void ForwardHeaders(this HttpClient httpClient, IHttpContextAccessor httpContextAccessor, bool requiredAuthorization = true)
        {
            if (requiredAuthorization || httpContextAccessor.HasAuthorization())
                httpClient.ForwardAuthorization(httpContextAccessor);

            httpClient.ForwardAcceptLanguage();

            httpClient.ForwardTracing(httpContextAccessor);
        }
    }

}
