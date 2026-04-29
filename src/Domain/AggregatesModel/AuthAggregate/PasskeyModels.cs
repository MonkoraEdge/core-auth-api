using System.ComponentModel.DataAnnotations;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;

/// <summary>Request to complete a passkey registration — body is the raw JSON attestation object
/// returned by <c>navigator.credentials.create()</c>.</summary>
public class PasskeyRegisterCompleteRequest
{
    /// <summary>The <c>challengeId</c> returned by the register/begin endpoint.</summary>
    [Required]
    public string ChallengeId { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialised <c>AuthenticatorAttestationRawResponse</c> produced by the browser
    /// (i.e. <c>JSON.stringify(credential.toJSON())</c> or the raw credential object).
    /// </summary>
    [Required]
    public string AttestationResponse { get; set; } = string.Empty;

    /// <summary>Optional human-readable label for the new key, e.g. "MacBook Touch ID".</summary>
    [MaxLength(128)]
    public string? FriendlyName { get; set; }
}

/// <summary>Optional request body for beginning the passkey authentication ceremony.</summary>
public class PasskeyAuthBeginRequest
{
    /// <summary>
    /// When provided, the server restricts the <c>allowCredentials</c> list to keys
    /// registered for this email. Leave empty for a fully discoverable-credential flow.
    /// </summary>
    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; set; }
}

/// <summary>Request to complete the passkey authentication ceremony.</summary>
public class PasskeyAuthCompleteRequest
{
    /// <summary>The <c>challengeId</c> returned by the begin endpoint — used to retrieve
    /// the stored <c>AssertionOptions</c> from the server-side cache.</summary>
    [Required]
    public string ChallengeId { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialised <c>AuthenticatorAssertionRawResponse</c> from
    /// <c>navigator.credentials.get()</c>.
    /// </summary>
    [Required]
    public string AssertionResponse { get; set; } = string.Empty;
}

/// <summary>DTO returned when listing registered passkeys.</summary>
public class PasskeyCredentialDto
{
    public Guid Id { get; set; }
    public string CredentialIdBase64Url { get; set; } = string.Empty;
    public string? FriendlyName { get; set; }
    public string AaGuid { get; set; } = string.Empty;
    public string[]? Transports { get; set; }
    public bool IsBackupEligible { get; set; }
    public bool IsBackedUp { get; set; }
    public string? AttestationType { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Response from both registration-begin and authentication-begin endpoints.
/// Clients must pass the embedded JSON to the WebAuthn API and echo the challengeId back.</summary>
public class PasskeyOptionsResponse
{
    /// <summary>Opaque identifier for the server-side challenge — echo in the complete call.</summary>
    public string ChallengeId { get; set; } = string.Empty;

    /// <summary>JSON-serialised WebAuthn options object.</summary>
    public string Options { get; set; } = string.Empty;
}
