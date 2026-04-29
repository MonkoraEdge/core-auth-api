using System.ComponentModel.DataAnnotations;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;

// ─── SP config POCO ──────────────────────────────────────────────────────────

/// <summary>
/// Resolved SP configuration returned by <c>ISamlService.GetSpConfigAsync</c>.
/// The SP private key is decrypted before this POCO is constructed.
/// </summary>
public class SamlSpConfig
{
    public Guid ProviderId { get; set; }
    public string ProviderCode { get; set; } = string.Empty;

    // SP
    public string SpEntityId { get; set; } = string.Empty;
    public string? SpCertificatePem { get; set; }
    /// <summary>Decrypted PEM-encoded RSA private key. Null when signing is disabled.</summary>
    public string? SpPrivateKeyPem { get; set; }
    public bool SignAuthRequests { get; set; }
    public bool WantAssertionsSigned { get; set; }

    // IdP
    public string IdpEntityId { get; set; } = string.Empty;
    public string IdpSsoUrl { get; set; } = string.Empty;
    public string? IdpSloUrl { get; set; }
    public string IdpCertificatePem { get; set; } = string.Empty;

    // Behavior
    public string? NameIdFormat { get; set; }
    public bool AutoProvisionUsers { get; set; }
    /// <summary>Parsed from <c>AttributeMappingJson</c>: IdP attribute name → local field name.</summary>
    public Dictionary<string, string>? AttributeMapping { get; set; }
}

// ─── SAML provider CRUD models ────────────────────────────────────────────────

public class SamlProviderCreateRequest
{
    [Required][MaxLength(64)]  public string ProviderCode { get; set; } = string.Empty;
    [Required][MaxLength(256)] public string DisplayName { get; set; } = string.Empty;

    // SP
    [Required][MaxLength(512)] public string SpEntityId { get; set; } = string.Empty;
    public string? SpCertificatePem { get; set; }
    /// <summary>Plain-text PEM private key. Stored encrypted.</summary>
    public string? SpPrivateKeyPem { get; set; }
    public bool SignAuthRequests { get; set; } = false;
    public bool WantAssertionsSigned { get; set; } = true;

    // IdP
    [Required][MaxLength(512)] public string IdpEntityId { get; set; } = string.Empty;
    [Required][MaxLength(2048)] public string IdpSsoUrl { get; set; } = string.Empty;
    [MaxLength(2048)] public string? IdpSloUrl { get; set; }
    [Required] public string IdpCertificatePem { get; set; } = string.Empty;
    [MaxLength(2048)] public string? IdpMetadataUrl { get; set; }

    // Behavior
    [MaxLength(256)] public string? NameIdFormat { get; set; }
    public string? AttributeMappingJson { get; set; }
    public bool AutoProvisionUsers { get; set; } = false;
    public bool IsActive { get; set; } = true;
}

public class SamlProviderUpdateRequest
{
    [MaxLength(256)] public string? DisplayName { get; set; }

    // SP
    [MaxLength(512)] public string? SpEntityId { get; set; }
    public string? SpCertificatePem { get; set; }
    public string? SpPrivateKeyPem { get; set; }
    public bool? SignAuthRequests { get; set; }
    public bool? WantAssertionsSigned { get; set; }

    // IdP
    [MaxLength(512)] public string? IdpEntityId { get; set; }
    [MaxLength(2048)] public string? IdpSsoUrl { get; set; }
    [MaxLength(2048)] public string? IdpSloUrl { get; set; }
    public string? IdpCertificatePem { get; set; }
    [MaxLength(2048)] public string? IdpMetadataUrl { get; set; }

    // Behavior
    [MaxLength(256)] public string? NameIdFormat { get; set; }
    public string? AttributeMappingJson { get; set; }
    public bool? AutoProvisionUsers { get; set; }
    public bool? IsActive { get; set; }
}

public class SamlProviderResponse
{
    public Guid Id { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SpEntityId { get; set; } = string.Empty;
    public bool SignAuthRequests { get; set; }
    public bool WantAssertionsSigned { get; set; }
    public string IdpEntityId { get; set; } = string.Empty;
    public string IdpSsoUrl { get; set; } = string.Empty;
    public string? IdpSloUrl { get; set; }
    public string? IdpMetadataUrl { get; set; }
    public string? NameIdFormat { get; set; }
    public bool AutoProvisionUsers { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ─── SAML code exchange ───────────────────────────────────────────────────────

public class SamlTokenRequest
{
    [Required][MaxLength(64)] public string SamlCode { get; set; } = string.Empty;
}
