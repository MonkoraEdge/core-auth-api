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
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "response_type")]
    [System.ComponentModel.DataAnnotations.MaxLength(32)]
    public string? ResponseType { get; set; }

    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "client_id")]
    [System.ComponentModel.DataAnnotations.MaxLength(255)]
    public string? ClientId { get; set; }

    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "redirect_uri")]
    [System.ComponentModel.DataAnnotations.MaxLength(2048)]
    public string? RedirectUri { get; set; }

    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "scope")]
    [System.ComponentModel.DataAnnotations.MaxLength(1024)]
    public string? Scope { get; set; }

    // RFC 6749 §10.12: state is an opaque CSRF value. 2048 chars is ample for any JWT/encoded state.
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "state")]
    [System.ComponentModel.DataAnnotations.MaxLength(2048)]
    public string? State { get; set; }

    // RFC 7636: code_challenge is BASE64URL(SHA-256(verifier)) = exactly 43 chars.
    // Allow up to 128 to accommodate future hash methods.
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "code_challenge")]
    [System.ComponentModel.DataAnnotations.MaxLength(128)]
    public string? CodeChallenge { get; set; }

    // Only "S256" is valid in OAuth 2.1; "plain" is prohibited.
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "code_challenge_method")]
    [System.ComponentModel.DataAnnotations.MaxLength(16)]
    public string? CodeChallengeMethod { get; set; }

    // Nonce is an opaque value for OIDC replay protection.
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "nonce")]
    [System.ComponentModel.DataAnnotations.MaxLength(256)]
    public string? Nonce { get; set; }

    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "prompt")]
    [System.ComponentModel.DataAnnotations.MaxLength(64)]
    public string? Prompt { get; set; }

    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "max_age")]
    [System.ComponentModel.DataAnnotations.MaxLength(20)]
    public string? MaxAge { get; set; }

    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "login_hint")]
    [System.ComponentModel.DataAnnotations.MaxLength(256)]
    public string? LoginHint { get; set; }

    /// <summary>RFC 9126: PAR request_uri returned by the /par endpoint.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinder(Name = "request_uri")]
    [System.ComponentModel.DataAnnotations.MaxLength(512)]
    public string? RequestUri { get; set; }
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
