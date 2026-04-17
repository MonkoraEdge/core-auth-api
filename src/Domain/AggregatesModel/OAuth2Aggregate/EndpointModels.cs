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

// ─── RFC 8628 — Device Authorization Grant ───────────────────────────────────

/// <summary>RFC 8628 §3.1 device authorization request from a constrained client.</summary>
public sealed class DeviceAuthorizationRequest
{
    public string? Scope { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}

/// <summary>RFC 8628 §3.2 device authorization response.</summary>
public sealed class DeviceAuthorizationResponse
{
    [JsonPropertyName("device_code")]
    public string DeviceCode { get; set; } = string.Empty;

    [JsonPropertyName("user_code")]
    public string UserCode { get; set; } = string.Empty;

    [JsonPropertyName("verification_uri")]
    public string VerificationUri { get; set; } = string.Empty;

    [JsonPropertyName("verification_uri_complete")]
    public string? VerificationUriComplete { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; } = 1800;

    [JsonPropertyName("interval")]
    public int Interval { get; set; } = 5;
}

/// <summary>Internal approval request from the verification UI.</summary>
public sealed class DeviceApprovalRequest
{
    public string UserCode { get; set; } = string.Empty;
    public bool Approved { get; set; }
}

/// <summary>Form-encoded device authorization request (RFC 8628 §3.1).</summary>
public sealed class DeviceAuthorizationFormRequest
{
    [Microsoft.AspNetCore.Mvc.FromForm(Name = "client_id")]
    public string? ClientId { get; set; }

    [Microsoft.AspNetCore.Mvc.FromForm(Name = "client_secret")]
    public string? ClientSecret { get; set; }

    [Microsoft.AspNetCore.Mvc.FromForm(Name = "scope")]
    public string? Scope { get; set; }
}
