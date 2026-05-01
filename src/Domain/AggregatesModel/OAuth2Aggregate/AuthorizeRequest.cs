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
    /// <summary>RFC 9126: PAR request_uri returned by the /par endpoint.</summary>
    public string? RequestUri { get; set; }
    /// <summary>
    /// The authenticated user's current session ID extracted from the bearer token <c>sid</c> claim.
    /// Populated by the controller; used to bind the issued authorization code to its originating
    /// UserSession, enabling accurate session-scoped token revocation.
    /// </summary>
    public Guid? SessionId { get; set; }
}
