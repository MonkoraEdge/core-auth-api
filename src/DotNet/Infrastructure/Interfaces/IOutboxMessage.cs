namespace MonkoraEdge.Core.DotNet.Infrastructure.Interfaces
{
    /// <summary>
    /// Represents a message persisted in the outbox table before being published to a message broker.
    /// Implements the Transactional Outbox pattern to guarantee at-least-once delivery.
    /// </summary>
    public interface IOutboxMessage
    {
        /// <summary>Unique identifier for this outbox message.</summary>
        Guid Id { get; }

        /// <summary>Fully-qualified CLR type name of the original event/message.</summary>
        string Type { get; }

        /// <summary>JSON-serialized message payload.</summary>
        string Content { get; }

        /// <summary>UTC time when this message was created / the event occurred.</summary>
        DateTime OccurredOn { get; }

        /// <summary>UTC time when this message was successfully published. Null if pending.</summary>
        DateTime? ProcessedOn { get; }

        /// <summary>Last error message if publishing failed. Null if not yet attempted or successful.</summary>
        string Error { get; }

        /// <summary>Number of delivery attempts made so far.</summary>
        int RetryCount { get; }
    }
}
