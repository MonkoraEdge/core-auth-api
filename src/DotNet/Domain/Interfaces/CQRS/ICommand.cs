namespace MonkoraEdge.Core.DotNet.Domain.Interfaces.CQRS
{
    /// <summary>
    /// Marker interface for commands that produce no return value.
    /// Implement this on command objects that perform writes without a result.
    /// </summary>
    public interface ICommand { }

    /// <summary>
    /// Marker interface for commands that return a value.
    /// </summary>
    /// <typeparam name="TResult">The type of value the command returns.</typeparam>
    public interface ICommand<TResult> { }
}
