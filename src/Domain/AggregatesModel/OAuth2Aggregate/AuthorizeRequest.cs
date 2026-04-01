namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;

public class AuthorizeRequest
{
    public string? ResponseType { get; set; }       // required: "code" only (OAuth 2.1)
    public string? ClientId { get; set; }           // required
    public string? RedirectUri { get; set; }        // required; must exactly match registered URI
    public string? Scope { get; set; }              // optional; defaults to "openid"
    public string? State { get; set; }              // recommended; opaque CSRF token from client
    public string? CodeChallenge { get; set; }      // required for authorization_code (OAuth 2.1 §4.1)
    public string? CodeChallengeMethod { get; set; } // required: "S256" only (plain is prohibited)
    public string? Nonce { get; set; }              // recommended for OIDC ID token binding
    public string? Prompt { get; set; }             // "none" | "login" | "consent" | "select_account"
    public string? MaxAge { get; set; }
    public string? LoginHint { get; set; }
}
