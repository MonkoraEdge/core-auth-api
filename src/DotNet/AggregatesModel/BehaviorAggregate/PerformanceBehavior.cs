using MonkoraEdge.Core.DotNet.Domain.Interfaces.CQRS;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;
using System.Diagnostics;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.BehaviorAggregate
{
    /// <summary>
    /// Pipeline behavior that measures the execution time of each request.
    /// Logs a warning when the handler takes longer than the configured threshold.
    /// Default threshold: 500 ms.
    /// </summary>
    public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ILoggerService<PerformanceBehavior<TRequest, TResponse>> _logger;
        private readonly int _thresholdMs;

        public PerformanceBehavior(
            ILoggerService<PerformanceBehavior<TRequest, TResponse>> logger,
            int thresholdMs = 500)
        {
            _logger = logger;
            _thresholdMs = thresholdMs;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var timer = Stopwatch.StartNew();

            var response = await next();

            timer.Stop();

            if (timer.ElapsedMilliseconds > _thresholdMs)
            {
                _logger.LogWarning(
                    "Long-running request detected: {RequestName} ({ElapsedMs}ms) — threshold is {Threshold}ms. Request: {@Request}",
                    typeof(TRequest).Name,
                    timer.ElapsedMilliseconds,
                    _thresholdMs,
                    request);
            }

            return response;
        }
    }
}