using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.AgreementAggregate;

public class AgreementAcceptRequest
{
    /// <summary>Optional: OAuth2 client associated with this acceptance event.</summary>
    public Guid? ClientId { get; set; }
    /// <summary>CHECKBOX | SIGNATURE | CLICK_THROUGH | IMPLICIT — default CHECKBOX.</summary>
    public string AcceptanceMethod { get; set; } = "CHECKBOX";
}

public class AgreementCreateRequest
{
    public Guid? TenantId { get; set; }
    public string AgreementCode { get; set; } = string.Empty;
    /// <summary>TERMS_OF_SERVICE | PRIVACY_POLICY | PDPA_CONSENT | COOKIE_POLICY | MARKETING_CONSENT | DATA_PROCESSING_AGREEMENT | OTHER</summary>
    public string AgreementType { get; set; } = string.Empty;
    public Locale Title { get; set; } = new();
    public Locale? Content { get; set; }
    public Locale? Summary { get; set; }
    public string Version { get; set; } = "1.0.0";
    public DateTime EffectiveAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool RequiresExplicitAction { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class AgreementUpdateRequest
{
    public Locale? Title { get; set; }
    public Locale? Content { get; set; }
    public Locale? Summary { get; set; }
    public string? Version { get; set; }
    public DateTime? EffectiveAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool? IsRequired { get; set; }
    public bool? RequiresExplicitAction { get; set; }
    public bool? IsActive { get; set; }
}

public class AgreementResponse
{
    public string Id { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string AgreementCode { get; set; } = string.Empty;
    public string AgreementType { get; set; } = string.Empty;
    public Locale? Title { get; set; }
    public Locale? Content { get; set; }
    public Locale? Summary { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime EffectiveAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsRequired { get; set; }
    public bool RequiresExplicitAction { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
