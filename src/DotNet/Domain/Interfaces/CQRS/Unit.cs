namespace MonkoraEdge.Core.DotNet.Domain.Interfaces.CQRS
{
    /// <summary>
    /// Represents a void return value for commands that produce no result.
    /// Used internally by the Dispatcher to unify the pipeline for void and value-returning commands.
    /// </summary>
    public readonly struct Unit
    {
        public static readonly Unit Value = default;
    }
}
