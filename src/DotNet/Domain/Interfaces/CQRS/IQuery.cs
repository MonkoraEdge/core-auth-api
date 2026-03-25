namespace MonkoraEdge.Core.DotNet.Domain.Interfaces.CQRS
{
    /// <summary>
    /// Marker interface for queries that return a value.
    /// Queries should not mutate state.
    /// </summary>
    /// <typeparam name="TResult">The type of value the query returns.</typeparam>
    public interface IQuery<TResult> { }
}
