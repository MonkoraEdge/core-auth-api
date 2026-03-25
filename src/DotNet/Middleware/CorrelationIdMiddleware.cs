using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Diagnostics;

namespace MonkoraEdge.Core.DotNet.Middleware
{
    public class CorrelationIdMiddleware
    {
        private const string HeaderName = "X-Correlation-Id";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string correlationId;

            // 1) If there is a header from the client or YARP  use the existing one.
            if (context.Request.Headers.TryGetValue(HeaderName, out var cidHeader))
            {
                correlationId = cidHeader!;
            }
            else
            {
                // 2) If not exist  create a new one (UUID).
                correlationId = Guid.NewGuid().ToString();
                context.Request.Headers[HeaderName] = correlationId;
            }

            // 3) Make HttpContext use this ID.
            context.TraceIdentifier = correlationId;

            // 4) Use with OpenTelemetry  Activity.
            var activity = Activity.Current;

            if (activity == null)
            {
                activity = new Activity("Incoming Request");
                activity.SetIdFormat(ActivityIdFormat.W3C);
                activity.SetParentId($"00-{correlationId.Replace("-", "")}-0000000000000001-01");
                activity.Start();
            }

            // Inject CorrelationId so that OTel can capture it.
            activity.SetTag("correlation_id", correlationId);

            // 5) Work with Serilog  Structured Log.
            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("TraceId", activity.TraceId.ToString()))
            {
                // 6) Include it in the response before sending back.
                context.Response.OnStarting(() =>
                {
                    if (!context.Response.Headers.ContainsKey(HeaderName))
                        context.Response.Headers[HeaderName] = correlationId;

                    return Task.CompletedTask;
                });

                await _next(context);
            }
        }
    }
}
