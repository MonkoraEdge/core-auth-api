using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.WebhookAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;

namespace MonkoraEdge.Core.Auth.Infrastructure.Services;

/// <summary>
/// Background service that reads <see cref="WebhookMessage"/> items from the in-process channel,
/// fans out to all active subscribed endpoints, POSTs the payload with an HMAC-SHA256 signature
/// header, and records delivery results in <c>tx_webhook_delivery_logs</c>.
/// </summary>
public sealed class WebhookDeliveryBackgroundService : BackgroundService
{
    private const int MaxRetries         = 3;
    private const int RetryDelaySeconds  = 5;
    private const int HttpTimeoutSeconds = 10;

    private readonly WebhookChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDeliveryBackgroundService> _logger;

    public WebhookDeliveryBackgroundService(
        WebhookChannel channel,
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDeliveryBackgroundService> logger)
    {
        _channel           = channel;
        _scopeFactory      = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WebhookDeliveryBackgroundService started.");

        await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await DeliverAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error delivering webhook {Id} ({Event})",
                    message.Id, message.EventType);
            }
        }
    }

    private async Task DeliverAsync(WebhookMessage message, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var endpointRepo = scope.ServiceProvider.GetRequiredService<IWebhookEndpointRepository>();
        var logRepo      = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryLogRepository>();
        var unitOfWork   = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();

        var endpoints = await endpointRepo.GetActiveByEventAsync(message.EventType, ct);

        foreach (var endpoint in endpoints)
        {
            await DeliverToEndpointAsync(endpoint, message, logRepo, unitOfWork, ct);
        }
    }

    private async Task DeliverToEndpointAsync(
        WebhookEndpoint endpoint,
        WebhookMessage message,
        IWebhookDeliveryLogRepository logRepo,
        AuthenticationDbContext dbContext,
        CancellationToken ct)
    {
        var client   = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds);

        var body      = message.Payload;
        var signature = WebhookService.ComputeSignature(endpoint.Secret, body);

        var log = new WebhookDeliveryLog
        {
            WebhookEndpointId = endpoint.Id,
            EventType         = message.EventType,
            Payload           = body,
            AttemptCount      = 0
        };

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            log.AttemptCount = attempt;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                request.Headers.Add("X-Webhook-Id",        message.Id);
                request.Headers.Add("X-Webhook-Event",     message.EventType);
                request.Headers.Add("X-Webhook-Signature", signature);
                request.Headers.Add("X-Webhook-Timestamp", message.EnqueuedAt.ToString("O"));

                var response = await client.SendAsync(request, ct);
                log.ResponseStatusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    log.Success = true;
                    _logger.LogDebug("Webhook {Id} delivered to {Url} — {Status}",
                        message.Id, endpoint.Url, response.StatusCode);
                    break;
                }

                _logger.LogWarning("Webhook {Id} attempt {Attempt} to {Url} got {Status}",
                    message.Id, attempt, endpoint.Url, response.StatusCode);
            }
            catch (Exception ex)
            {
                log.ErrorMessage = ex.Message;
                _logger.LogWarning(ex, "Webhook {Id} attempt {Attempt} to {Url} failed",
                    message.Id, attempt, endpoint.Url);
            }

            if (attempt < MaxRetries)
                await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds * attempt), ct);
        }

        logRepo.Insert(log);
        await dbContext.SaveChangesAsync(ct);
    }
}
