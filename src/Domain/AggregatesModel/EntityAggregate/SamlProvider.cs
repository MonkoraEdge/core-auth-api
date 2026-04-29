using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

/// <summary>
/// Configuration for a SAML 2.0 Identity Provider (IdP).
/// MonkoraEdge acts as the Service Provider (SP); this entity stores everything needed
/// for SP-initiated Single Sign-On (SSO) and optional Single Logout (SLO).
/// </summary>
public class SamlProvider : BaseEntity
{
    /// <summary>Short code slug used in SAML URLs, e.g. <c>/auth/saml/{ProviderCode}/signin</c>.</summary>
    public string ProviderCode { get; set; } = string.Empty;

    /// <summary>Human-readable label shown in the admin UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    // ─── SP (Service Provider — our side) ─────────────────────────────────────

    /// <summary>SP entity ID (audience URI). Must exactly match what the IdP expects.</summary>
    public string SpEntityId { get; set; } = string.Empty;

    /// <summary>PEM-encoded X.509 certificate for the SP (public key only).</summary>
    public string? SpCertificatePem { get; set; }

    /// <summary>AES-256-GCM encrypted PEM private key for the SP signing certificate.</summary>
    public string? SpPrivateKeyEncrypted { get; set; }

    /// <summary>When true, authentication requests are signed with the SP private key.</summary>
    public bool SignAuthRequests { get; set; } = false;

    /// <summary>When true, the SP requires IdP assertions to be signed.</summary>
    public bool WantAssertionsSigned { get; set; } = true;

    // ─── IdP (Identity Provider — external side) ──────────────────────────────

    /// <summary>IdP entity ID from the IdP metadata.</summary>
    public string IdpEntityId { get; set; } = string.Empty;

    /// <summary>IdP Single Sign-On service URL (HTTP-Redirect binding).</summary>
    public string IdpSsoUrl { get; set; } = string.Empty;

    /// <summary>IdP Single Logout service URL. Null if SLO is not supported by the IdP.</summary>
    public string? IdpSloUrl { get; set; }

    /// <summary>PEM-encoded X.509 certificate used by the IdP to sign assertions.</summary>
    public string IdpCertificatePem { get; set; } = string.Empty;

    /// <summary>Optional URL to the IdP metadata XML for documentation / auto-refresh purposes.</summary>
    public string? IdpMetadataUrl { get; set; }

    // ─── Behavior ─────────────────────────────────────────────────────────────

    /// <summary>
    /// SAML NameID format URI.
    /// Defaults to <c>urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress</c>.
    /// </summary>
    public string? NameIdFormat { get; set; }

    /// <summary>
    /// JSON object mapping IdP-specific SAML attribute names to local field names.
    /// Example: <c>{"http://schemas.example.com/email":"email","givenName":"first_name"}</c>
    /// </summary>
    public string? AttributeMappingJson { get; set; }

    /// <summary>
    /// When true, a new local user account is automatically provisioned on first successful SSO
    /// if no existing account matches the NameID / email claim.
    /// </summary>
    public bool AutoProvisionUsers { get; set; } = false;

    public bool IsActive { get; set; } = true;
}
