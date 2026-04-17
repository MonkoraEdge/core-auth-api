using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.ProviderAggregate;

public class ProviderCreateRequest
{
    public string ProviderCode { get; set; } = string.Empty;
    public Locale ProviderName { get; set; } = new();
    /// <summary>OAUTH2 | OIDC | SAML | OTHER</summary>
    public string Protocol { get; set; } = "OIDC";
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string? Issuer { get; set; }
    public string? AuthorizationUrl { get; set; }
    public string? TokenUrl { get; set; }
    public string? UserinfoUrl { get; set; }
    public string? JwksUri { get; set; }
    public string? DiscoveryUrl { get; set; }
    public string? EndSessionEndpoint { get; set; }
    public string? CallbackUrl { get; set; }
    public bool PkceSupported { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class ProviderUpdateRequest
{
    public Locale? ProviderName { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string[]? Scopes { get; set; }
    public string? AuthorizationUrl { get; set; }
    public string? TokenUrl { get; set; }
    public string? UserinfoUrl { get; set; }
    public string? JwksUri { get; set; }
    public string? DiscoveryUrl { get; set; }
    public string? EndSessionEndpoint { get; set; }
    public string? CallbackUrl { get; set; }
    public bool? PkceSupported { get; set; }
    public bool? IsActive { get; set; }
}

public class ProviderResponse
{
    public string Id { get; set; } = string.Empty;
    public string ProviderCode { get; set; } = string.Empty;
    public Locale? ProviderName { get; set; }
    public string? Protocol { get; set; }
    public string? ClientId { get; set; }
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string? Issuer { get; set; }
    public string? AuthorizationUrl { get; set; }
    public string? TokenUrl { get; set; }
    public string? UserinfoUrl { get; set; }
    public string? JwksUri { get; set; }
    public string? DiscoveryUrl { get; set; }
    public string? EndSessionEndpoint { get; set; }
    public string? CallbackUrl { get; set; }
    public bool PkceSupported { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
