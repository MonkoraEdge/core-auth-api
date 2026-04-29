namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;

public class TokenRequest
{
    public string GrantType { get; set; }          // authorization_code | client_credentials | refresh_token
    public string? Code { get; set; }              // authorization_code
    public string? RedirectUri { get; set; }       // authorization_code
    public string? CodeVerifier { get; set; }      // PKCE
    public string? RefreshToken { get; set; }      // refresh_token
    public string? Scope { get; set; }
    public string? ClientId { get; set; }          // public clients
    public string? ClientSecret { get; set; }      // confidential clients (form post)
    // RFC 8693 — Token Exchange
    public string? SubjectToken { get; set; }
    public string? SubjectTokenType { get; set; }   // urn:ietf:params:oauth:token-type:access_token etc.
    public string? ActorToken { get; set; }
    public string? ActorTokenType { get; set; }
    public string? RequestedTokenType { get; set; } // desired output token type
    public string? Audience { get; set; }
    public string? Resource { get; set; }
}
