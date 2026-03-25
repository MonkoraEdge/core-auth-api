namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.ClientAggregate;

public class ClientCreateRequest
{
    public Guid? TenantId { get; set; }
    public string ClientName { get; set; }
    public string ClientType { get; set; } = "CONFIDENTIAL";  // PUBLIC | CONFIDENTIAL
    public string TokenEndpointAuthMethod { get; set; } = "CLIENT_SECRET_BASIC";
    public bool RequirePkce { get; set; } = true;
    public bool RequireConsent { get; set; } = true;
    public string[] RedirectUris { get; set; } = Array.Empty<string>();
    public string[]? PostLogoutRedirectUris { get; set; }
    public string[]? AllowedGrantTypes { get; set; }
    public string[]? AllowedResponseTypes { get; set; }
    public string[]? ScopeIds { get; set; }
    public int AccessTokenLifetime { get; set; } = 3600;
    public int RefreshTokenLifetime { get; set; } = 2592000;
    public string? LogoUri { get; set; }
    public string? ClientUri { get; set; }
    public string? JwksUri { get; set; }
}

public class ClientUpdateRequest
{
    public string? ClientName { get; set; }
    public bool? RequirePkce { get; set; }
    public bool? RequireConsent { get; set; }
    public string[]? RedirectUris { get; set; }
    public string[]? PostLogoutRedirectUris { get; set; }
    public string[]? AllowedGrantTypes { get; set; }
    public string[]? AllowedResponseTypes { get; set; }
    public string[]? ScopeIds { get; set; }
    public int? AccessTokenLifetime { get; set; }
    public int? RefreshTokenLifetime { get; set; }
    public string? LogoUri { get; set; }
    public string? ClientUri { get; set; }
    public bool? IsActive { get; set; }
}

public class ClientResponse
{
    public string Id { get; set; }
    public string? TenantId { get; set; }
    public string ClientId { get; set; }
    public string ClientName { get; set; }
    public string ClientType { get; set; }
    public string TokenEndpointAuthMethod { get; set; }
    public bool RequirePkce { get; set; }
    public bool RequireConsent { get; set; }
    public string[] RedirectUris { get; set; }
    public string[]? PostLogoutRedirectUris { get; set; }
    public string[]? AllowedGrantTypes { get; set; }
    public string[]? AllowedResponseTypes { get; set; }
    public List<string> Scopes { get; set; } = new();
    public int AccessTokenLifetime { get; set; }
    public int RefreshTokenLifetime { get; set; }
    public string? LogoUri { get; set; }
    public string? ClientUri { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ClientSecretResponse
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }  // only returned on creation/rotation
    public DateTime? ExpiresAt { get; set; }
}
