namespace MonkoraEdge.Core.DotNet.Infrastructure.Interfaces.Jobs
{
    /// <summary>
    /// Marker interface for a background job unit of work.
    /// Implement this interface on job classes that are dispatched via
    /// <see cref="MonkoraEdge.Core.DotNet.Abstractions.Infrastructure.IBackgroundJobService"/>.
    /// </summary>
    public interface IJob
    {
        /// <summary>Execute the job logic.</summary>
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
