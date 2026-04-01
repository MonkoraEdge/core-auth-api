namespace MonkoraEdge.Core.Auth.API.Controllers;

// ─── OAuth2 form-encoded request models ──────────────────────────────────────
// These are kept in the API project (not Domain) because they exist only to
// bridge HTML form posts (application/x-www-form-urlencoded) to the domain
// request models. They carry no business logic.

public class TokenFormRequest
{
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "grant_type")]   public string? GrantType { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "code")]         public string? Code { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "redirect_uri")] public string? RedirectUri { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "code_verifier")]public string? CodeVerifier { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "refresh_token")]public string? RefreshToken { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "scope")]        public string? Scope { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "client_id")]    public string? ClientId { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "client_secret")]public string? ClientSecret { get; set; }
}

public class RevocationFormRequest
{
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "token")]           public string? Token { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "token_type_hint")] public string? TokenTypeHint { get; set; }
}

public class IntrospectFormRequest
{
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "token")]           public string? Token { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "token_type_hint")] public string? TokenTypeHint { get; set; }
}

public class ConsentSubmitRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string ClientId { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    public string RedirectUri { get; set; }
    public string? Scope { get; set; }
    public string? State { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
    public string? Nonce { get; set; }
    public bool Approved { get; set; }
    public bool RememberConsent { get; set; }
}
