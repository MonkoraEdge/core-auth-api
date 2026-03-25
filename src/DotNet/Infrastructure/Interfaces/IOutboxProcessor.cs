namespace MonkoraEdge.Core.DotNet.Infrastructure.Interfaces
{
    /// <summary>
    /// Processes pending outbox messages and publishes them to the message broker.
    /// Implement as a hosted background service or scheduled job.
    ///
    /// Example registration:
    /// <code>
    /// builder.Services.AddScoped&lt;IOutboxProcessor, OutboxProcessor&gt;();
    /// builder.Services.AddHostedService&lt;OutboxBackgroundService&gt;();
    /// </code>
    /// </summary>
    public interface IOutboxProcessor
    {
        /// <summary>
        /// Find all unprocessed outbox messages and publish them to the broker.
        /// Should be idempotent — already-published messages are skipped.
        /// </summary>
        Task ProcessPendingAsync(CancellationToken cancellationToken = default);
    }
}
