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

    [JsonPropertyName("device_authorization_endpoint")]
    public string? DeviceAuthorizationEndpoint { get; set; }

    /// <summary>RFC 9126 PAR endpoint.</summary>
    [JsonPropertyName("pushed_authorization_request_endpoint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PushedAuthorizationRequestEndpoint { get; set; }

    /// <summary>RFC 7591 Dynamic Client Registration endpoint.</summary>
    [JsonPropertyName("registration_endpoint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RegistrationEndpoint { get; set; }

    /// <summary>RFC 9449 DPoP — signing algorithms supported for DPoP proofs.</summary>
    [JsonPropertyName("dpop_signing_alg_values_supported")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? DPoPSigningAlgValuesSupported { get; set; }
}

// ─── RFC 9126 — Pushed Authorization Requests ────────────────────────────────

/// <summary>Form-encoded PAR request — same shape as AuthorizeRequest but posted by the client.</summary>
public sealed class PushedAuthorizationFormRequest
{
    [Microsoft.AspNetCore.Mvc.FromForm(Name = "response_type")]        public string? ResponseType { get; set; }
    [Microsoft.AspNetCore.Mvc.FromForm(Name = "client_id")]            public string? ClientId { get; set; }
    [Microsoft.AspNetCore.Mvc.FromForm(Name = "client_secret")]        public string? ClientSecret { get; set; }
    [Microsoft.AspNetCore.Mvc.FromForm(Name = "redirect_uri")]         public string? RedirectUri { get; set; }
    [Microsoft.AspNetCore.Mvc.FromForm(Name = "scope")]                public string? Scope { get; set; }
    [Microsoft.AspNetCore.Mvc.FromForm(Name = "state")]                public string? State { get; set; }
    [Microsoft.AspNetCore.Mvc.FromForm(Name = "code_challenge")]       public string? CodeChallenge { get; set; }
    [Microsoft.AspNetCore.Mvc.FromForm(Name = "code_challenge_method")] public string? CodeChallengeMethod { get; set; }
    [Microsoft.AspNetCore.Mvc.FromForm(Name = "nonce")]                public string? Nonce { get; set; }
    [Microsoft.AspNetCore.Mvc.FromForm(Name = "prompt")]               public string? Prompt { get; set; }
    [Microsoft.AspNetCore.Mvc.FromForm(Name = "max_age")]              public string? MaxAge { get; set; }
    [Microsoft.AspNetCore.Mvc.FromForm(Name = "login_hint")]           public string? LoginHint { get; set; }
}

/// <summary>RFC 9126 §2.2 PAR endpoint response.</summary>
public sealed class PushedAuthorizationResponse
{
    [JsonPropertyName("request_uri")]
    public string RequestUri { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; } = 90;
}

// ─── RFC 7591 — Dynamic Client Registration ──────────────────────────────────

/// <summary>RFC 7591 §2 client metadata request.</summary>
public sealed class DynamicClientRegistrationRequest
{
    [JsonPropertyName("client_name")]
    public string? ClientName { get; set; }

    [JsonPropertyName("redirect_uris")]
    public string[]? RedirectUris { get; set; }

    [JsonPropertyName("grant_types")]
    public string[]? GrantTypes { get; set; }

    [JsonPropertyName("response_types")]
    public string[]? ResponseTypes { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; set; }

    [JsonPropertyName("logo_uri")]
    public string? LogoUri { get; set; }

    [JsonPropertyName("client_uri")]
    public string? ClientUri { get; set; }

    [JsonPropertyName("jwks_uri")]
    public string? JwksUri { get; set; }

    [JsonPropertyName("require_pkce")]
    public bool? RequirePkce { get; set; }
}

/// <summary>RFC 7591 §3.2 registration response — includes assigned client_id and optional secret.</summary>
public sealed class DynamicClientRegistrationResponse
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("client_secret")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("client_name")]
    public string? ClientName { get; set; }

    [JsonPropertyName("redirect_uris")]
    public string[] RedirectUris { get; set; } = Array.Empty<string>();

    [JsonPropertyName("grant_types")]
    public string[] GrantTypes { get; set; } = Array.Empty<string>();

    [JsonPropertyName("response_types")]
    public string[] ResponseTypes { get; set; } = Array.Empty<string>();

    [JsonPropertyName("token_endpoint_auth_method")]
    public string TokenEndpointAuthMethod { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("logo_uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LogoUri { get; set; }

    [JsonPropertyName("client_uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClientUri { get; set; }

    [JsonPropertyName("jwks_uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? JwksUri { get; set; }
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
