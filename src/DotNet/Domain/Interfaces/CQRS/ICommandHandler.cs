namespace MonkoraEdge.Core.DotNet.Domain.Interfaces.CQRS
{
    /// <summary>
    /// Handles a void command (no return value).
    /// </summary>
    public interface ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Handles a command that returns a value.
    /// </summary>
    public interface ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
    }
}
