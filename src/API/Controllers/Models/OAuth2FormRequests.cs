namespace MonkoraEdge.Core.Auth.API.Controllers;

// ─── OAuth2 form-encoded request models ──────────────────────────────────────
// These are kept in the API project (not Domain) because they exist only to
// bridge HTML form posts / query strings (snake_case parameter names) to the
// domain request models. They carry no business logic.

/// <summary>
/// API-layer DTO for the /authorize query string.
/// Bridges OAuth2 snake_case query parameters to the domain AuthorizeRequest.
/// Required because ASP.NET Core query-string binding is case-insensitive but
/// NOT underscore-aware: "response_type" does NOT bind to "ResponseType".
/// </summary>
public class AuthorizeQueryRequest
{
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "response_type")]        public string? ResponseType { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "client_id")]            public string? ClientId { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "redirect_uri")]         public string? RedirectUri { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "scope")]                public string? Scope { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "state")]                public string? State { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "code_challenge")]       public string? CodeChallenge { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "code_challenge_method")] public string? CodeChallengeMethod { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "nonce")]                public string? Nonce { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "prompt")]               public string? Prompt { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "max_age")]              public string? MaxAge { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "login_hint")]           public string? LoginHint { get; set; }
    /// <summary>RFC 9126: PAR request_uri returned by the /par endpoint.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "request_uri")]          public string? RequestUri { get; set; }
}

public class TokenFormRequest
{
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "grant_type")]        public string? GrantType { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "code")]              public string? Code { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "redirect_uri")]      public string? RedirectUri { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "code_verifier")]     public string? CodeVerifier { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "refresh_token")]     public string? RefreshToken { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "scope")]             public string? Scope { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "client_id")]         public string? ClientId { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "client_secret")]     public string? ClientSecret { get; set; }
    // RFC 8693 — Token Exchange
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "subject_token")]           public string? SubjectToken { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "subject_token_type")]      public string? SubjectTokenType { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "actor_token")]             public string? ActorToken { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "actor_token_type")]        public string? ActorTokenType { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "requested_token_type")]    public string? RequestedTokenType { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "audience")]                public string? Audience { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "resource")]                public string? Resource { get; set; }
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
