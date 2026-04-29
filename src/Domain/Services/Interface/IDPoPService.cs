namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

/// <summary>
/// RFC 9449 — Demonstrating Proof of Possession (DPoP).
/// Validates DPoP proof JWTs attached by clients to token requests and resource access.
/// </summary>
public interface IDPoPService
{
    /// <summary>
    /// Validate a DPoP proof JWT header and return the JWK thumbprint (jkt) of the
    /// sender's public key. Throws <see cref="Domain.Exceptions.DomainException"/> on failure.
    /// </summary>
    /// <param name="dpopHeader">Raw value of the <c>DPoP</c> HTTP header.</param>
    /// <param name="httpMethod">HTTP method of the current request (e.g., "POST").</param>
    /// <param name="httpUrl">Absolute URL of the current request (e.g., "https://auth.example.com/token").</param>
    /// <returns>Base64url-encoded SHA-256 JWK thumbprint per RFC 7638.</returns>
    Task<string> ValidateProofAsync(string dpopHeader, string httpMethod, string httpUrl);
}
