using MonkoraEdge.Core.DotNet.Infrastructure.Interfaces;

namespace MonkoraEdge.Core.DotNet.Domain.SeedWork
{
    /// <summary>
    /// Persistent outbox message entity for the Transactional Outbox pattern.
    /// Add <c>DbSet&lt;OutboxMessage&gt; OutboxMessages</c> to your DbContext.
    ///
    /// Typical workflow:
    /// 1. Within a DB transaction, persist your aggregate AND insert an <see cref="OutboxMessage"/>.
    /// 2. A background job (<see cref="IOutboxProcessor"/>) polls for unprocessed messages
    ///    and publishes them to the message broker (Kafka, RabbitMQ, etc.).
    /// 3. On success, set <see cref="ProcessedOn"/>. On failure, increment <see cref="RetryCount"/>
    ///    and store the error in <see cref="Error"/>.
    /// </summary>
    public class OutboxMessage : IOutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <inheritdoc />
        public string Type { get; set; }

        /// <inheritdoc />
        public string Content { get; set; }

        /// <inheritdoc />
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;

        /// <inheritdoc />
        public DateTime? ProcessedOn { get; set; }

        /// <inheritdoc />
        public string Error { get; set; }

        /// <inheritdoc />
        public int RetryCount { get; set; }
    }
}
