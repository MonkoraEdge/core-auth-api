using System.Text.Json.Serialization;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;

public enum AuthorizeResponseKind
{
    Error,
    Redirect,
    LoginRequired,
    ConsentRequired
}

public sealed class AuthorizeEndpointResponse
{
    public AuthorizeResponseKind Kind { get; init; }
    public string RedirectUrl { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public string ErrorDescription { get; init; } = string.Empty;
    public AuthorizationClientInfo Client { get; init; }
    public string[] RequestedScopes { get; init; } = Array.Empty<string>();
}

public sealed class ConsentRequest
{
    public string ClientId { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string CodeChallenge { get; init; } = string.Empty;
    public string CodeChallengeMethod { get; init; } = string.Empty;
    public string Nonce { get; init; } = string.Empty;
    public bool Approved { get; init; }
    public bool RememberConsent { get; init; }
}

public sealed class ConsentResponse
{
    public string RedirectUrl { get; init; } = string.Empty;
}

public sealed class AuthorizationServerMetadataResponse
{
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = string.Empty;

    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("jwks_uri")]
    public string JwksUri { get; set; } = string.Empty;

    [JsonPropertyName("revocation_endpoint")]
    public string RevocationEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("introspection_endpoint")]
    public string IntrospectionEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("response_types_supported")]
    public string[] ResponseTypesSupported { get; set; } = Array.Empty<string>();

    [JsonPropertyName("grant_types_supported")]
    public string[] GrantTypesSupported { get; set; } = Array.Empty<string>();

    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public string[] TokenEndpointAuthMethodsSupported { get; set; } = Array.Empty<string>();

    [JsonPropertyName("code_challenge_methods_supported")]
    public string[] CodeChallengeMethodsSupported { get; set; } = Array.Empty<string>();
}