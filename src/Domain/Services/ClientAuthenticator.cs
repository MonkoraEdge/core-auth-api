using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.DotNet.AggregatesModel.ExceptionAggregate;

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
        { "openid", "profile", "email", "offline_access" };

    private readonly IAuthorizationClientRepository _clientRepo;
    private readonly IAuthorizationClientScopeRepository _clientScopeRepo;
    private readonly IScopeRepository _scopeRepo;
    private readonly IPasswordService _passwordService;

    public ClientAuthenticator(
        IAuthorizationClientRepository clientRepo,
        IAuthorizationClientScopeRepository clientScopeRepo,
        IScopeRepository scopeRepo,
        IPasswordService passwordService)
    {
        _clientRepo = clientRepo;
        _clientScopeRepo = clientScopeRepo;
        _scopeRepo = scopeRepo;
        _passwordService = passwordService;
    }

    public async Task<AuthorizationClient> LoadAsync(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new CustomHttpBadRequestException("authorize", "client_id is required.");

        var client = await _clientRepo.GetByClientIdAsync(clientId);
        if (client == null || !client.IsActive)
            throw new CustomHttpBadRequestException("authorize", "Client not found or inactive.");

        return client;
    }

    public async Task<AuthorizationClient> AuthenticateAsync(string? clientId, string? clientSecret)
    {
        if (string.IsNullOrEmpty(clientId))
            throw new CustomHttpBadRequestException("token", "client_id is required.");

        var client = await _clientRepo.GetByClientIdAsync(clientId);
        if (client == null || !client.IsActive)
            throw new CustomHttpBadRequestException("token", "Invalid client.");

        if (client.ClientType == "CONFIDENTIAL")
        {
            if (string.IsNullOrEmpty(clientSecret))
                throw new CustomHttpBadRequestException("token",
                    "client_secret is required for confidential clients.");

            // bcrypt constant-time comparison — prevents timing-based enumeration
            if (!_passwordService.VerifyPassword(clientSecret, client.ClientSecretHash))
                throw new CustomHttpBadRequestException("token", "Invalid client credentials.");

            if (client.ClientSecretExpiresAt.HasValue && client.ClientSecretExpiresAt < DateTime.UtcNow)
                throw new CustomHttpBadRequestException("token",
                    "Client secret has expired. Please rotate the secret.");
        }

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
