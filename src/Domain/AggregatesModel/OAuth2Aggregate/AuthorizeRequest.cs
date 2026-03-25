namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;

public class AuthorizeRequest
{
    public string ResponseType { get; set; }       // "code"
    public string ClientId { get; set; }
    public string RedirectUri { get; set; }
    public string Scope { get; set; }
    public string State { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; } // "S256" | "plain"
    public string? Nonce { get; set; }
    public string? Prompt { get; set; }             // "none" | "login" | "consent" | "select_account"
    public string? MaxAge { get; set; }
    public string? LoginHint { get; set; }
}
