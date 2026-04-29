using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;

namespace MonkoraEdge.Core.Auth.Infrastructure.Services;

/// <summary>
/// Enqueues webhook event payloads onto an in-process <see cref="Channel{T}"/> for async delivery.
/// A singleton <see cref="WebhookChannel"/> provides the shared channel between the service
/// and <c>WebhookDeliveryBackgroundService</c>.
/// </summary>
public sealed class WebhookService : IWebhookService
{
    private readonly WebhookChannel _channel;

    public WebhookService(WebhookChannel channel) => _channel = channel;

    public async Task EnqueueAsync(string eventType, object payload, CancellationToken ct = default)
    {
        var message = new WebhookMessage
        {
            Id        = Guid.NewGuid().ToString("N"),
            EventType = eventType,
            Payload   = JsonSerializer.Serialize(payload),
            EnqueuedAt = DateTime.UtcNow
        };

        await _channel.Writer.WriteAsync(message, ct);
    }

    /// <summary>
    /// Computes the HMAC-SHA256 delivery signature.
    /// Header value format: <c>sha256={lowercase hex}</c>
    /// </summary>
    public static string ComputeSignature(string secret, string payload)
    {
        var key  = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
        return "sha256=" + Convert.ToHexString(HMACSHA256.HashData(key, data)).ToLowerInvariant();
    }
}

/// <summary>Singleton channel shared between <see cref="WebhookService"/> and the background delivery worker.</summary>
public sealed class WebhookChannel
{
    private readonly Channel<WebhookMessage> _channel =
        Channel.CreateBounded<WebhookMessage>(new BoundedChannelOptions(4096)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelWriter<WebhookMessage> Writer => _channel.Writer;
    public ChannelReader<WebhookMessage> Reader => _channel.Reader;
}

public sealed class WebhookMessage
{
    public string Id        { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Payload   { get; init; } = string.Empty;
    public DateTime EnqueuedAt { get; init; }
}
