using MonkoraEdge.Core.DotNet.AggregatesModel.CommonAggregate;
using MonkoraEdge.Core.DotNet.Domain.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

public class Provider : BaseEntity, ISoftDelete
{
    public string ProviderCode { get; set; }
    public Locale ProviderName { get; set; }
    public string? Protocol { get; set; }

    public string? ClientId { get; set; }
    public string? ClientSecretEncrypt { get; set; }
    public string[] Scopes { get; set; } = Array.Empty<string>();

    public string? Issuer { get; set; }
    public string? AuthorizationUrl { get; set; }
    public string? JwksUri { get; set; }
    public string? TokenUrl { get; set; }
    public string? UserinfoUrl { get; set; }
    public string? DiscoveryUrl { get; set; }
    public string? EndSessionEndpoint { get; set; }
    public string? CallbackUrl { get; set; }
    public bool PkceSupported { get; set; }

    public bool IsActive { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // ─── Behavior ──────────────────────────────────────────────────────

    /// <summary>Soft-deletes the provider so it can no longer be used for social login.</summary>
    public void SoftDelete(string? deletedBy)
    {
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
    }
}
