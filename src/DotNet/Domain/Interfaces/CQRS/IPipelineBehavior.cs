namespace MonkoraEdge.Core.DotNet.Domain.Interfaces.CQRS
{
    /// <summary>
    /// Delegate representing the next step (handler or next behavior) in the pipeline.
    /// </summary>
    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

    /// <summary>
    /// Defines a behavior that wraps a command or query handler in the pipeline.
    /// Implement this interface to add cross-cutting concerns such as validation,
    /// logging, or performance monitoring.
    ///
    /// Behaviors are executed in registration order; the innermost call is the actual handler.
    /// </summary>
    /// <typeparam name="TRequest">The command or query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    public interface IPipelineBehavior<TRequest, TResponse>
    {
        Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken);
    }
}
