using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services;

/// <summary>
/// Handles client authentication and scope resolution for all OAuth2 endpoints.
/// Previously this logic was spread across private helpers in OAuth2Service.
/// </summary>
public sealed class ClientAuthenticator : IClientAuthenticator
{
    // Every client implicitly has access to these OIDC baseline scopes without
    // explicit registration — they match the standard OIDC claim sets.
    private static readonly string[] BaselineScopes =
        { "openid", "profile", "email" };

    // Cache client metadata in Redis for 5 minutes. This is the hot path for every token
    // and authorize request. TTL short enough that deactivated clients are refused quickly.
    private const string ClientCachePrefix = "oauth2:client:";
    private static readonly TimeSpan ClientCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IAuthorizationClientRepository _clientRepo;
    private readonly IAuthorizationClientScopeRepository _clientScopeRepo;
    private readonly IScopeRepository _scopeRepo;
    private readonly IPasswordService _passwordService;
    private readonly IDistributedCache _cache;

    public ClientAuthenticator(
        IAuthorizationClientRepository clientRepo,
        IAuthorizationClientScopeRepository clientScopeRepo,
        IScopeRepository scopeRepo,
        IPasswordService passwordService,
        IDistributedCache cache)
    {
        _clientRepo = clientRepo;
        _clientScopeRepo = clientScopeRepo;
        _scopeRepo = scopeRepo;
        _passwordService = passwordService;
        _cache = cache;
    }

    /// <summary>
    /// Look up client by client_id string, using Redis as a read-through cache.
    /// Returns null when the client does not exist in the DB.
    /// </summary>
    private async Task<AuthorizationClient?> GetCachedClientAsync(string clientId, CancellationToken ct = default)
    {
        var key = ClientCachePrefix + clientId;
        var cached = await _cache.GetStringAsync(key, ct).ConfigureAwait(false);
        if (cached != null)
            return JsonSerializer.Deserialize<AuthorizationClient>(cached);

        var client = await _clientRepo.GetByClientIdAsync(clientId).ConfigureAwait(false);
        if (client != null)
        {
            await _cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(client),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ClientCacheTtl },
                ct).ConfigureAwait(false);
        }
        return client;
    }

    public async Task<AuthorizationClient> LoadAsync(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new DomainException("authorize", ErrorCodeType.INVALID_CLIENT, "client_id is required.");

        var client = await GetCachedClientAsync(clientId).ConfigureAwait(false);
        if (client == null || !client.IsActive)
            throw new DomainException("authorize", ErrorCodeType.INVALID_CLIENT, "Client not found or inactive.");

        return client;
    }

    public async Task<AuthorizationClient> AuthenticateAsync(string? clientId, string? clientSecret)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new DomainException("token", ErrorCodeType.INVALID_CLIENT, "Client authentication failed.");

        var client = await GetCachedClientAsync(clientId).ConfigureAwait(false);
        if (client == null || !client.IsActive)
            throw new DomainException("token", ErrorCodeType.INVALID_CLIENT, "Client authentication failed.");

        if (client.IsPublic)
        {
            // RFC 6749 §2.1 / OAuth 2.1 §2.1: public clients MUST NOT be issued client credentials.
            // Reject any request that presents a secret for a public client — it is either a
            // misconfigured client or a probing attempt.
            if (!string.IsNullOrEmpty(clientSecret))
                throw new DomainException("token", ErrorCodeType.INVALID_CLIENT, "Client authentication failed.");

            return client;
        }

        // CONFIDENTIAL client — secret required.
        if (string.IsNullOrEmpty(clientSecret))
            throw new DomainException("token", ErrorCodeType.INVALID_CLIENT, "Client authentication failed.");

        // bcrypt constant-time comparison — prevents timing-based enumeration
        if (!_passwordService.VerifyPassword(clientSecret, client.ClientSecretHash))
            throw new DomainException("token", ErrorCodeType.INVALID_CLIENT, "Client authentication failed.");

        if (client.IsSecretExpired())
            throw new DomainException("token", ErrorCodeType.INVALID_CLIENT, "Client authentication failed.");

        return client;
    }

    public async Task<IReadOnlyList<string>> GetAllowedScopeNamesAsync(Guid clientId)
    {
        var clientScopes = await _clientScopeRepo.GetByClientIdAsync(clientId);

        // Single batched query replaces the N+1 GetByIdAsync loop
        var scopeIds = clientScopes.Select(cs => cs.ScopeId);
        var registeredScopes = await _scopeRepo.GetByIdsAsync(scopeIds);

        return BaselineScopes
            .Concat(registeredScopes.Where(s => s.IsActive).Select(s => s.ScopeName))
            .Distinct()
            .ToArray();
    }
}
