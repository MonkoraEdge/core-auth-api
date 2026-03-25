using MonkoraEdge.Core.DotNet.Domain.Interfaces.CQRS;
using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.BehaviorAggregate
{
    /// <summary>
    /// Pipeline behavior that logs the entry and exit of every command/query.
    /// Logs request name, parameters, and elapsed time.
    /// </summary>
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ILoggerService<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILoggerService<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogInformation("Handling {RequestName}: {@Request}", requestName, request);

            var response = await next();

            _logger.LogInformation("Handled {RequestName}", requestName);

            return response;
        }
    }
}