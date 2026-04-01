using System.Text.Json.Serialization;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;

public class RevocationRequest
{
    public string Token { get; set; }
    public string? TokenTypeHint { get; set; }
}

public class OpenIdConfigurationResponse
{
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; }

    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; }

    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; }

    [JsonPropertyName("userinfo_endpoint")]
    public string UserInfoEndpoint { get; set; }

    [JsonPropertyName("jwks_uri")]
    public string JwksUri { get; set; }

    [JsonPropertyName("introspection_endpoint")]
    public string IntrospectionEndpoint { get; set; }

    [JsonPropertyName("revocation_endpoint")]
    public string RevocationEndpoint { get; set; }

    [JsonPropertyName("end_session_endpoint")]
    public string EndSessionEndpoint { get; set; }

    [JsonPropertyName("response_types_supported")]
    public string[] ResponseTypesSupported { get; set; }

    [JsonPropertyName("grant_types_supported")]
    public string[] GrantTypesSupported { get; set; }

    [JsonPropertyName("subject_types_supported")]
    public string[] SubjectTypesSupported { get; set; }

    [JsonPropertyName("id_token_signing_alg_values_supported")]
    public string[] IdTokenSigningAlgValuesSupported { get; set; }

    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public string[] TokenEndpointAuthMethodsSupported { get; set; }

    [JsonPropertyName("scopes_supported")]
    public string[] ScopesSupported { get; set; }

    [JsonPropertyName("claims_supported")]
    public string[] ClaimsSupported { get; set; }

    [JsonPropertyName("code_challenge_methods_supported")]
    public string[] CodeChallengeMethodsSupported { get; set; }

    [JsonPropertyName("response_modes_supported")]
    public string[] ResponseModesSupported { get; set; }

    [JsonPropertyName("request_parameter_supported")]
    public bool RequestParameterSupported { get; set; }

    [JsonPropertyName("require_pkce")]
    public bool RequirePkce { get; set; }
}

public class JwksResponse
{
    [JsonPropertyName("keys")]
    public List<JwkKey> Keys { get; set; } = new();
}

public class JwkKey
{
    [JsonPropertyName("kty")]
    public string Kty { get; set; }
    [JsonPropertyName("use")]
    public string Use { get; set; }
    [JsonPropertyName("kid")]
    public string Kid { get; set; }
    [JsonPropertyName("alg")]
    public string Alg { get; set; }
    [JsonPropertyName("n")]
    public string? N { get; set; }    // RSA modulus
    [JsonPropertyName("e")]
    public string? E { get; set; }    // RSA exponent
    [JsonPropertyName("x5t")]
    public string? X5t { get; set; }
}

public class UserInfoResponse
{
    [JsonPropertyName("sub")]
    public string Sub { get; set; }

    // OIDC Core §5.3: claims that are unavailable MUST be omitted, not null-serialized.
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    [JsonPropertyName("given_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GivenName { get; set; }

    [JsonPropertyName("family_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FamilyName { get; set; }

    [JsonPropertyName("email")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; set; }

    [JsonPropertyName("email_verified")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? EmailVerified { get; set; }

    [JsonPropertyName("phone_number")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("phone_number_verified")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? PhoneNumberVerified { get; set; }

    [JsonPropertyName("picture")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Picture { get; set; }

    [JsonPropertyName("locale")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Locale { get; set; }

    [JsonPropertyName("zoneinfo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Zoneinfo { get; set; }

    [JsonPropertyName("updated_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? UpdatedAt { get; set; }
}
