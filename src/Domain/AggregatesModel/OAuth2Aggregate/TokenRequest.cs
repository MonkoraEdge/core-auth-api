namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;

public class TokenRequest
{
    public string GrantType { get; set; }          // authorization_code | client_credentials | refresh_token | password
    public string? Code { get; set; }              // authorization_code
    public string? RedirectUri { get; set; }       // authorization_code
    public string? CodeVerifier { get; set; }      // PKCE
    public string? RefreshToken { get; set; }      // refresh_token
    public string? Username { get; set; }          // password
    public string? Password { get; set; }          // password
    public string? Scope { get; set; }
    public string? ClientId { get; set; }          // public clients
    public string? ClientSecret { get; set; }      // confidential clients (form post)
}
