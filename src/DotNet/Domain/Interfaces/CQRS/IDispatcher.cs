namespace MonkoraEdge.Core.DotNet.Domain.Interfaces.CQRS
{
    /// <summary>
    /// Dispatches commands and queries through the pipeline to the appropriate handler.
    /// The pipeline runs registered IPipelineBehavior implementations (validation, logging,
    /// performance) before invoking the concrete handler.
    ///
    /// Usage:
    /// <code>
    /// // Void command
    /// await _dispatcher.SendAsync(new DeleteOrderCommand(id));
    ///
    /// // Command with result
    /// var orderId = await _dispatcher.SendAsync&lt;CreateOrderCommand, Guid&gt;(new CreateOrderCommand(...));
    ///
    /// // Query
    /// var orders = await _dispatcher.QueryAsync&lt;GetOrdersQuery, List&lt;OrderDto&gt;&gt;(new GetOrdersQuery(...));
    /// </code>
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>Dispatch a void command through the pipeline.</summary>
        Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand;

        /// <summary>Dispatch a command that returns a result through the pipeline.</summary>
        Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResult>;

        /// <summary>Dispatch a query through the pipeline.</summary>
        Task<TResult> QueryAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>;
    }
}
