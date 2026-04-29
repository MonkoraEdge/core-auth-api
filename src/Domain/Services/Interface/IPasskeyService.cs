using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

/// <summary>
/// Orchestrates WebAuthn / FIDO2 passkey registration (attestation) and
/// authentication (assertion) flows per W3C WebAuthn Level 3 and FIDO2 spec.
/// </summary>
public interface IPasskeyService
{
    /// <summary>
    /// Begin the passkey registration ceremony for an authenticated user.
    /// Returns JSON-serialised <c>CredentialCreateOptions</c> to be passed to the browser
    /// navigator.credentials.create() call.
    /// </summary>
    Task<string> BeginRegistrationAsync(Guid userId, string email, string? displayName);

    /// <summary>
    /// Complete the passkey registration ceremony.
    /// Validates the attestation response and persists the new <see cref="PasskeyCredential"/>.
    /// </summary>
    Task<PasskeyCredential> CompleteRegistrationAsync(
        Guid userId, string attestationResponseJson, string? friendlyName = null);

    /// <summary>
    /// Begin the passkey authentication ceremony.
    /// Returns the JSON-serialised <c>AssertionOptions</c> and a <c>challengeId</c>
    /// that the client must echo back in <see cref="CompleteAuthenticationAsync"/>.
    /// </summary>
    Task<(string optionsJson, string challengeId)> BeginAuthenticationAsync(string? email);

    /// <summary>
    /// Complete the passkey authentication ceremony.
    /// Validates the assertion, updates the signature counter, and issues JWT tokens.
    /// </summary>
    Task<LoginResponse> CompleteAuthenticationAsync(
        string challengeId, string assertionResponseJson,
        string? ipAddress, string? userAgent);

    /// <summary>Returns all active passkey credentials registered for a user.</summary>
    Task<IEnumerable<PasskeyCredential>> GetCredentialsAsync(Guid userId);

    /// <summary>Permanently removes a passkey credential owned by the user.</summary>
    Task DeleteCredentialAsync(Guid userId, Guid credentialId);
}
