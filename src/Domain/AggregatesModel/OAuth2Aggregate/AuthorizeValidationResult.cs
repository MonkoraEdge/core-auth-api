namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;

/// <summary>
/// Result of the authorization request validation step.
/// Uses init-only setters for immutability — build via the static factory methods
/// rather than object initializers with set properties.
/// </summary>
public sealed class AuthorizeValidationResult
{
    public bool IsValid { get; init; }
    public string? Error { get; init; }
    public string? ErrorDescription { get; init; }
    public bool RequiresConsent { get; init; }
    public bool RequiresLogin { get; init; }
    public AuthorizationClientInfo? Client { get; init; }
    public string[] RequestedScopes { get; init; } = Array.Empty<string>();

    public static AuthorizeValidationResult Fail(string error, string description) =>
        new() { IsValid = false, Error = error, ErrorDescription = description };
}

/// <summary>Safe public projection of an AuthorizationClient — no secret material.</summary>
public sealed class AuthorizationClientInfo
{
    public string ClientId { get; init; } = "";
    public string ClientName { get; init; } = "";
    public string? LogoUri { get; init; }
    public string? ClientUri { get; init; }
}
