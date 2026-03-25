using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace MonkoraEdge.Core.DotNet.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // crete or read CorrelationId
            var correlationId = GetOrCreateCorrelationId(context);
            context.Response.Headers["X-Correlation-Id"] = correlationId;

            // Read Request Body
            context.Request.EnableBuffering();
            var bodyText = await ReadRequestBody(context.Request);

            // Log Request
            _logger.LogInformation(
                "?? HTTP Request | {method} {path} | Query: {query} | Body: {body} | CorrelationId: {correlationId}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString.Value,
                bodyText,
                correlationId
            );

            // next
            await _next(context);

            stopwatch.Stop();

            // Log Response  
            _logger.LogInformation(
                "?? HTTP Response | {method} {path} | Status: {statusCode} | Time: {time}ms | CorrelationId: {correlationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId
            );
        }

        private static async Task<string> ReadRequestBody(HttpRequest request)
        {
            if (request.ContentLength == null || request.ContentLength == 0)
                return string.Empty;

            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            return body;
        }

        private static string GetOrCreateCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            {
                return correlationId!;
            }

            var generated = Guid.NewGuid().ToString();
            context.Request.Headers["X-Correlation-Id"] = generated;

            return generated;
        }
    }
}
