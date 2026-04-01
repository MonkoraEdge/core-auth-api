using System.Security.Cryptography;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.ClientAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services;

public class ClientService : IClientService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthorizationClientRepository _clientRepo;
    private readonly IAuthorizationClientScopeRepository _clientScopeRepo;
    private readonly IScopeRepository _scopeRepo;
    private readonly IPasswordService _passwordService;

    public ClientService(
        IUnitOfWork unitOfWork,
        IAuthorizationClientRepository clientRepo,
        IAuthorizationClientScopeRepository clientScopeRepo,
        IScopeRepository scopeRepo,
        IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _clientRepo = clientRepo;
        _clientScopeRepo = clientScopeRepo;
        _scopeRepo = scopeRepo;
        _passwordService = passwordService;
    }

    public async Task<ClientResponse> GetByIdAsync(Guid id)
    {
        var client = await _clientRepo.GetByIdAsync(id);
        if (client == null) throw new DomainException("client", "Client not found.");
        return await MapToResponseAsync(client);
    }

    public async Task<ClientResponse> GetByClientIdAsync(string clientId)
    {
        var client = await _clientRepo.GetByClientIdAsync(clientId);
        if (client == null) throw new DomainException("client", "Client not found.");
        return await MapToResponseAsync(client);
    }

    public async Task<(List<ClientResponse> Items, int Total)> GetListAsync(Guid? tenantId, int page, int pageSize, string? search)
    {
        var all = await _clientRepo.ListAsync(c =>
            (tenantId == null || c.TenantId == tenantId) &&
            (string.IsNullOrEmpty(search) || c.ClientName.Contains(search) || c.ClientId.Contains(search)) &&
            c.DeletedAt == null);

        var sorted = all.OrderByDescending(c => c.CreatedAt);
        var total = sorted.Count();
        var items = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var responses = new List<ClientResponse>();
        foreach (var c in items)
            responses.Add(await MapToResponseAsync(c));

        return (responses, total);
    }

    // Permitted lowercase grant type values — matches the DB CHECK constraint and OAuth 2.1.
    private static readonly HashSet<string> AllowedGrantTypeValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization_code", "client_credentials", "refresh_token", "device_code", "jwt_bearer"
    };

    // Permitted token endpoint auth method values — matches the DB CHECK constraint.
    private static readonly HashSet<string> AllowedAuthMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "NONE", "CLIENT_SECRET_BASIC", "CLIENT_SECRET_POST", "CLIENT_SECRET_JWT", "PRIVATE_KEY_JWT"
    };

    public async Task<(ClientResponse Client, ClientSecretResponse Secret)> CreateAsync(ClientCreateRequest request, string? createdBy)
    {
        var clientType = (request.ClientType ?? "CONFIDENTIAL").ToUpperInvariant();
        bool isPublic = clientType == "PUBLIC";

        if (string.IsNullOrWhiteSpace(request.ClientName))
            throw new DomainException("client", "client_name must not be empty.");

        ValidateLifetimes(request.AccessTokenLifetime, request.RefreshTokenLifetime);

        // Redirect URI requirements
        var effectiveGrantTypes = (request.AllowedGrantTypes ?? new[] { "authorization_code", "refresh_token" })
            .Select(g => g.ToLowerInvariant()).ToArray();

        ValidateGrantTypes(effectiveGrantTypes);

        var authMethod = (request.TokenEndpointAuthMethod ?? (isPublic ? "NONE" : "CLIENT_SECRET_BASIC")).ToUpperInvariant();
        if (!AllowedAuthMethods.Contains(authMethod))
            throw new DomainException("client",
                $"token_endpoint_auth_method '{authMethod}' is not supported. Allowed: {string.Join(", ", AllowedAuthMethods)}.");

        bool needsRedirectUri = effectiveGrantTypes.Contains("authorization_code", StringComparer.OrdinalIgnoreCase);

        if (needsRedirectUri && request.RedirectUris.Length == 0)
            throw new DomainException("client", "At least one redirect_uri is required for clients using authorization_code grant.");

        foreach (var uri in request.RedirectUris)
            ValidateRedirectUri(uri);

        if (request.PostLogoutRedirectUris != null)
            foreach (var uri in request.PostLogoutRedirectUris)
                ValidateRedirectUri(uri);

        // PUBLIC clients must never hold a secret and must enforce PKCE (RFC 6749 §2.1 / OAuth 2.1).
        // CONFIDENTIAL clients use bcrypt — SHA-256 is not suitable for secret storage.
        string? rawSecret = null;
        string? secretHash = null;
        if (!isPublic)
        {
            rawSecret = GenerateClientSecret();
            secretHash = _passwordService.HashPassword(rawSecret);
        }

        var client = new AuthorizationClient
        {
            TenantId = request.TenantId,
            ClientId = GenerateClientId(),
            ClientSecretHash = secretHash,
            ClientName = request.ClientName,
            ClientType = clientType,
            // PUBLIC clients MUST use "NONE" auth method and MUST require PKCE.
            TokenEndpointAuthMethod = isPublic ? "NONE" : authMethod,
            RequirePkce = isPublic || request.RequirePkce,
            RequireConsent = request.RequireConsent,
            RedirectUris = request.RedirectUris,
            PostLogoutRedirectUris = request.PostLogoutRedirectUris,
            AllowedGrantTypes = effectiveGrantTypes, // already lowercased during construction above
            AllowedResponseTypes = request.AllowedResponseTypes ?? new[] { "code" },
            AccessTokenLifetime = request.AccessTokenLifetime,
            RefreshTokenLifetime = request.RefreshTokenLifetime,
            LogoUri = request.LogoUri,
            ClientUri = request.ClientUri,
            JwksUri = request.JwksUri,
            IsActive = true
        };
        _clientRepo.Insert(client);

        // Assign requested scopes
        if (request.ScopeIds != null)
        {
            foreach (var scopeId in request.ScopeIds)
            {
                if (Guid.TryParse(scopeId, out var scopeGuid))
                {
                    _clientScopeRepo.Insert(new AuthorizationClientScope
                    {
                        ClientId = client.Id,
                        ScopeId = scopeGuid
                    });
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();

        var response = await MapToResponseAsync(client);
        var secret = new ClientSecretResponse
        {
            ClientId = client.ClientId,
            // null for PUBLIC clients — they have no secret to return
            ClientSecret = rawSecret!,
            ExpiresAt = null
        };

        return (response, secret);
    }

    public async Task<UpdateResponse> UpdateAsync(Guid id, ClientUpdateRequest request, string? updatedBy)
    {
        var client = await _clientRepo.GetByIdAsync(id);
        if (client == null) throw new DomainException("client", "Client not found.");

        if (request.ClientName != null)
        {
            if (string.IsNullOrWhiteSpace(request.ClientName))
                throw new DomainException("client", "client_name must not be empty.");
            client.ClientName = request.ClientName;
        }

        if (request.AccessTokenLifetime.HasValue || request.RefreshTokenLifetime.HasValue)
            ValidateLifetimes(
                request.AccessTokenLifetime ?? client.AccessTokenLifetime,
                request.RefreshTokenLifetime ?? client.RefreshTokenLifetime);

        if (request.AllowedGrantTypes != null)
            ValidateGrantTypes(request.AllowedGrantTypes.Select(g => g.ToLowerInvariant()).ToArray());

        if (request.RequirePkce.HasValue)
        {
            // If the client is PUBLIC, PKCE can only be strengthened, never removed.
            if (client.ClientType == "PUBLIC" && !request.RequirePkce.Value)
                throw new DomainException("client", "PKCE cannot be disabled for public clients.");
            client.RequirePkce = request.RequirePkce.Value;
        }
        if (request.RequireConsent.HasValue) client.RequireConsent = request.RequireConsent.Value;
        if (request.RedirectUris != null)
        {
            foreach (var uri in request.RedirectUris)
                ValidateRedirectUri(uri);
            client.RedirectUris = request.RedirectUris;
        }
        if (request.PostLogoutRedirectUris != null)
        {
            foreach (var uri in request.PostLogoutRedirectUris)
                ValidateRedirectUri(uri);
            client.PostLogoutRedirectUris = request.PostLogoutRedirectUris;
        }
        if (request.AllowedGrantTypes != null) client.AllowedGrantTypes = request.AllowedGrantTypes.Select(g => g.ToLowerInvariant()).ToArray();
        if (request.AllowedResponseTypes != null) client.AllowedResponseTypes = request.AllowedResponseTypes;
        if (request.AccessTokenLifetime.HasValue) client.AccessTokenLifetime = request.AccessTokenLifetime.Value;
        if (request.RefreshTokenLifetime.HasValue) client.RefreshTokenLifetime = request.RefreshTokenLifetime.Value;
        if (request.LogoUri != null) client.LogoUri = request.LogoUri;
        if (request.ClientUri != null) client.ClientUri = request.ClientUri;
        if (request.IsActive.HasValue) client.IsActive = request.IsActive.Value;

        _clientRepo.Update(client);

        // Update scopes if provided
        if (request.ScopeIds != null)
        {
            var existingScopes = await _clientScopeRepo.GetByClientIdAsync(client.Id);
            _clientScopeRepo.DeleteRange(existingScopes.ToList());

            foreach (var scopeId in request.ScopeIds)
            {
                if (Guid.TryParse(scopeId, out var scopeGuid))
                {
                    _clientScopeRepo.Insert(new AuthorizationClientScope
                    {
                        ClientId = client.Id,
                        ScopeId = scopeGuid
                    });
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = client.Id, IsSuccess = true, Message = "Client updated." };
    }

    public async Task<DeleteResponse> DeleteAsync(Guid id, string? deletedBy)
    {
        var client = await _clientRepo.GetByIdAsync(id);
        if (client == null) throw new DomainException("client", "Client not found.");
        client.DeletedAt = DateTime.UtcNow;
        client.DeletedBy = deletedBy;
        client.IsActive = false;
        _clientRepo.Update(client);
        await _unitOfWork.SaveChangesAsync();
        return new DeleteResponse { Id = client.Id, IsSuccess = true, Message = "Client deleted." };
    }

    public async Task<ClientSecretResponse> RotateSecretAsync(Guid id, string? updatedBy)
    {
        var client = await _clientRepo.GetByIdAsync(id);
        if (client == null) throw new DomainException("client", "Client not found.");

        if (client.ClientType == "PUBLIC")
            throw new DomainException("client", "Public clients do not have secrets to rotate.");

        var rawSecret = GenerateClientSecret();
        client.ClientSecretHash = _passwordService.HashPassword(rawSecret);
        client.ClientSecretExpiresAt = null;
        _clientRepo.Update(client);

        await _unitOfWork.SaveChangesAsync();
        return new ClientSecretResponse
        {
            ClientId = client.ClientId,
            ClientSecret = rawSecret
        };
    }

    public async Task<UpdateResponse> ActivateAsync(Guid id)
    {
        var client = await _clientRepo.GetByIdAsync(id);
        if (client == null) throw new DomainException("client", "Client not found.");
        client.IsActive = true;
        _clientRepo.Update(client);
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = client.Id, IsSuccess = true, Message = "Client activated." };
    }

    public async Task<UpdateResponse> DeactivateAsync(Guid id)
    {
        var client = await _clientRepo.GetByIdAsync(id);
        if (client == null) throw new DomainException("client", "Client not found.");
        client.IsActive = false;
        _clientRepo.Update(client);
        await _unitOfWork.SaveChangesAsync();
        return new UpdateResponse { Id = client.Id, IsSuccess = true, Message = "Client deactivated." };
    }

    private async Task<ClientResponse> MapToResponseAsync(AuthorizationClient client)
    {
        var clientScopes = await _clientScopeRepo.GetByClientIdAsync(client.Id);
        var scopeIds = clientScopes.Select(cs => cs.ScopeId);
        var scopes = await _scopeRepo.GetByIdsAsync(scopeIds);
        var scopeNames = scopes.Where(s => s.IsActive).Select(s => s.ScopeName).ToList();

        return new ClientResponse
        {
            Id = client.Id.ToString(),
            TenantId = client.TenantId?.ToString(),
            ClientId = client.ClientId,
            ClientName = client.ClientName,
            ClientType = client.ClientType,
            TokenEndpointAuthMethod = client.TokenEndpointAuthMethod,
            RequirePkce = client.RequirePkce,
            RequireConsent = client.RequireConsent,
            RedirectUris = client.RedirectUris,
            PostLogoutRedirectUris = client.PostLogoutRedirectUris,
            AllowedGrantTypes = client.AllowedGrantTypes,
            AllowedResponseTypes = client.AllowedResponseTypes,
            Scopes = scopeNames,
            AccessTokenLifetime = client.AccessTokenLifetime,
            RefreshTokenLifetime = client.RefreshTokenLifetime,
            LogoUri = client.LogoUri,
            ClientUri = client.ClientUri,
            IsActive = client.IsActive,
            CreatedAt = client.CreatedAt
        };
    }

    private static void ValidateLifetimes(int accessTokenLifetime, int refreshTokenLifetime)
    {
        if (accessTokenLifetime <= 0 || accessTokenLifetime > 86400)
            throw new DomainException("client",
                "access_token_lifetime must be between 1 and 86400 seconds (24 h).");
        if (refreshTokenLifetime <= 0)
            throw new DomainException("client",
                "refresh_token_lifetime must be greater than 0 seconds.");
    }

    private static void ValidateGrantTypes(string[] grantTypes)
    {
        var invalid = grantTypes.Where(g => !AllowedGrantTypeValues.Contains(g)).ToArray();
        if (invalid.Length > 0)
            throw new DomainException("client",
                $"Unsupported grant type(s): {string.Join(", ", invalid)}. " +
                $"Allowed: {string.Join(", ", AllowedGrantTypeValues)}.");
    }

    private static string GenerateClientId() =>
        "client_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();

    private static string GenerateClientSecret() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>
    /// Validate a single redirect URI at registration time (RFC 6749 §3.1.2 + OAuth 2.1).
    /// Rules:
    ///   - Must be an absolute URI.
    ///   - Must not contain a fragment component (#).
    ///   - Must not contain wildcards (*).
    ///   - Must use https, OR http://localhost / http://127.0.0.1 (native-app dev — RFC 8252),
    ///     OR a custom non-http scheme (mobile/native apps).
    /// </summary>
    private static void ValidateRedirectUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new DomainException("client", "Redirect URI must not be empty.");

        if (uri.Contains('*'))
            throw new DomainException("client", $"Wildcard redirect URIs are not permitted: {uri}");

        if (uri.Contains('#'))
            throw new DomainException("client", $"Redirect URI must not contain a fragment (#): {uri}");

        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
            throw new DomainException("client", $"Redirect URI must be an absolute URI: {uri}");

        var scheme = parsed.Scheme.ToLowerInvariant();
        bool isHttps = scheme == "https";
        bool isHttpLocalhost = scheme == "http"
            && (parsed.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || parsed.Host == "127.0.0.1");
        bool isCustomScheme = scheme != "http" && scheme != "https";

        if (!isHttps && !isHttpLocalhost && !isCustomScheme)
            throw new DomainException("client",
                $"Redirect URI must use https (or http://localhost for development): {uri}");
    }
}
