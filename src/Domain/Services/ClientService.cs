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

    public async Task<(ClientResponse Client, ClientSecretResponse Secret)> CreateAsync(ClientCreateRequest request, string? createdBy)
    {
        var rawSecret = GenerateClientSecret();
        // Client secrets must use bcrypt — SHA-256 is not suitable for secret storage
        var secretHash = _passwordService.HashPassword(rawSecret);

        var client = new AuthorizationClient
        {
            TenantId = request.TenantId,
            ClientId = GenerateClientId(),
            ClientSecretHash = secretHash,
            ClientName = request.ClientName,
            ClientType = request.ClientType,
            TokenEndpointAuthMethod = request.TokenEndpointAuthMethod,
            RequirePkce = request.RequirePkce,
            RequireConsent = request.RequireConsent,
            RedirectUris = request.RedirectUris,
            PostLogoutRedirectUris = request.PostLogoutRedirectUris,
            AllowedGrantTypes = request.AllowedGrantTypes ?? new[] { "authorization_code", "refresh_token" },
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
            ClientSecret = rawSecret,
            ExpiresAt = null
        };

        return (response, secret);
    }

    public async Task<UpdateResponse> UpdateAsync(Guid id, ClientUpdateRequest request, string? updatedBy)
    {
        var client = await _clientRepo.GetByIdAsync(id);
        if (client == null) throw new DomainException("client", "Client not found.");

        if (request.ClientName != null) client.ClientName = request.ClientName;
        if (request.RequirePkce.HasValue) client.RequirePkce = request.RequirePkce.Value;
        if (request.RequireConsent.HasValue) client.RequireConsent = request.RequireConsent.Value;
        if (request.RedirectUris != null) client.RedirectUris = request.RedirectUris;
        if (request.PostLogoutRedirectUris != null) client.PostLogoutRedirectUris = request.PostLogoutRedirectUris;
        if (request.AllowedGrantTypes != null) client.AllowedGrantTypes = request.AllowedGrantTypes;
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
        var scopeNames = new List<string>();
        foreach (var cs in clientScopes)
        {
            var scope = await _scopeRepo.GetByIdAsync(cs.ScopeId);
            if (scope != null) scopeNames.Add(scope.ScopeName);
        }

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

    private static string GenerateClientId() =>
        "client_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();

    private static string GenerateClientSecret() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
