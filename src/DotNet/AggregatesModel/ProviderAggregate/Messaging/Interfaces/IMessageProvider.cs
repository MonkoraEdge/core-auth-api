namespace MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Messaging.Interfaces
{
    /// <summary>
    /// Abstraction for message broker operations (Kafka, RabbitMQ, Azure Service Bus, etc.).
    /// </summary>
    public interface IMessageProvider
    {
        /// <summary>Publish a message to a topic/exchange.</summary>
        Task PublishAsync<T>(
            string topic,
            T message,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>Subscribe to a topic/queue and process messages via a handler.</summary>
        Task SubscribeAsync<T>(
            string topic,
            Func<T, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default) where T : class;
    }
}
