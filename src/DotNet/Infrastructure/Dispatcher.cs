using MonkoraEdge.Core.DotNet.Domain.Interfaces.CQRS;
using Microsoft.Extensions.DependencyInjection;

namespace MonkoraEdge.Core.DotNet.Infrastructure
{
    /// <summary>
    /// Default implementation of IDispatcher.
    /// Resolves the appropriate handler from DI and runs all registered
    /// IPipelineBehavior&lt;TRequest, TResponse&gt; implementations in order before invoking it.
    ///
    /// Pipeline order (as registered in DI):
    ///   ValidationBehavior → LoggingBehavior → PerformanceBehavior → Handler
    ///
    /// Register via:
    /// <code>
    /// services.AddCommandQueryPipeline(Assembly.GetExecutingAssembly());
    /// </code>
    /// </summary>
    public class Dispatcher : IDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public Dispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public async Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand
        {
            var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();

            RequestHandlerDelegate<Unit> core = async () =>
            {
                await handler.HandleAsync(command, cancellationToken);
                return Unit.Value;
            };

            await BuildPipeline<TCommand, Unit>(command, core, cancellationToken)();
        }

        /// <inheritdoc />
        public Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>
        {
            var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
            RequestHandlerDelegate<TResult> core = () => handler.HandleAsync(command, cancellationToken);
            return BuildPipeline<TCommand, TResult>(command, core, cancellationToken)();
        }

        /// <inheritdoc />
        public Task<TResult> QueryAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>
        {
            var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
            RequestHandlerDelegate<TResult> core = () => handler.HandleAsync(query, cancellationToken);
            return BuildPipeline<TQuery, TResult>(query, core, cancellationToken)();
        }

        // Wraps the core delegate with all registered behaviors in reverse order so that
        // the first registered behavior is the outermost wrapper.
        private RequestHandlerDelegate<TResponse> BuildPipeline<TRequest, TResponse>(
            TRequest request,
            RequestHandlerDelegate<TResponse> core,
            CancellationToken cancellationToken)
        {
            var behaviors = _serviceProvider
                .GetServices<IPipelineBehavior<TRequest, TResponse>>()
                .Reverse();

            return behaviors.Aggregate(core, (next, behavior) =>
                () => behavior.Handle(request, next, cancellationToken));
        }
    }
}
