using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

/// <summary>
/// A WebAuthn / FIDO2 public-key credential registered by a user.
/// One user may own multiple passkeys (e.g. hardware key + platform authenticator).
/// </summary>
public class PasskeyCredential : BaseEntity
{
    /// <summary>Owner of this credential.</summary>
    public Guid UserId { get; set; }

    /// <summary>Raw credential ID bytes as returned by the authenticator.</summary>
    public byte[] CredentialId { get; set; } = [];

    /// <summary>Base64Url-encoded credential ID — used for uniqueness checks and fast lookups.</summary>
    public string CredentialIdBase64Url { get; set; } = string.Empty;

    /// <summary>COSE-encoded public key bytes, persisted for assertion verification.</summary>
    public byte[] PublicKey { get; set; } = [];

    /// <summary>
    /// Monotonically increasing signature counter from the authenticator.
    /// Used to detect cloned credentials (counter must advance on every assertion).
    /// </summary>
    public uint SignatureCounter { get; set; }

    /// <summary>AAGUID that identifies the authenticator model.</summary>
    public string AaGuid { get; set; } = string.Empty;

    /// <summary>Authenticator transport hints (e.g. "usb", "nfc", "ble", "internal").</summary>
    public string[]? Transports { get; set; }

    /// <summary>True when the authenticator supports credential backup (synced passkey).</summary>
    public bool IsBackupEligible { get; set; }

    /// <summary>True when the credential is currently backed up to a cloud keychain.</summary>
    public bool IsBackedUp { get; set; }

    /// <summary>Attestation statement type, e.g. "none", "packed", "tpm".</summary>
    public string? AttestationType { get; set; }

    /// <summary>User-supplied friendly name, e.g. "iPhone 16 Face ID" or "YubiKey 5C".</summary>
    public string? FriendlyName { get; set; }

    /// <summary>UTC timestamp of the most recent successful assertion.</summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>Whether this credential is available for authentication.</summary>
    public bool IsActive { get; set; } = true;
}
