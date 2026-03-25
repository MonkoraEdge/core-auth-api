using System.Net.Http.Json;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.Extensions.Logging;

namespace MonkoraEdge.Core.Auth.Infrastructure.ExternalApis;

/// <summary>
/// HTTP client adapter that calls the external Business Profile API (BP API).
/// After each new user registration or social account linking,
/// the auth server notifies BP API so it can maintain a corresponding customer record.
/// </summary>
public sealed class BpApiClient : IBpApi
{
    private readonly HttpClient _http;
    private readonly ILogger<BpApiClient> _logger;

    public BpApiClient(HttpClient http, ILogger<BpApiClient> logger)
        => (_http, _logger) = (http, logger);

    public async Task<BpCustomerResult?> EnsureCustomerAsync(
        Guid userId, string email, string? displayName, string? phone,
        string? locale = null, CancellationToken ct = default)
    {
        var payload = new { userId, email, displayName, phone, locale };

        try
        {
            var response = await _http.PostAsJsonAsync("/api/customers/ensure", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "BP API EnsureCustomer returned non-success. UserId={UserId} Status={Status}",
                    userId, response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<BpCustomerResult>(cancellationToken: ct);
            _logger.LogInformation(
                "BP API customer ensured. UserId={UserId} CustomerId={CustomerId} IsNew={IsNew}",
                userId, result?.CustomerId, result?.IsNew);
            return result;
        }
        catch (HttpRequestException ex)
        {
            // Non-fatal — BP API sync failure must not block the auth flow.
            // The user is already authenticated; BP API can reconcile later.
            _logger.LogError(ex, "BP API EnsureCustomer call failed. UserId={UserId}", userId);
            return null;
        }
    }

    public async Task LinkIdentityAsync(
        Guid userId, string providerCode, string externalUserId,
        string? externalEmail, CancellationToken ct = default)
    {
        var payload = new { userId, providerCode, externalUserId, externalEmail };

        try
        {
            var response = await _http.PostAsJsonAsync("/api/customers/link-identity", payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "BP API LinkIdentity returned non-success. UserId={UserId} Provider={Provider} Status={Status}",
                    userId, providerCode, response.StatusCode);
            }
            else
            {
                _logger.LogInformation(
                    "BP API identity linked. UserId={UserId} Provider={Provider}",
                    userId, providerCode);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "BP API LinkIdentity call failed. UserId={UserId} Provider={Provider}",
                userId, providerCode);
        }
    }
}
