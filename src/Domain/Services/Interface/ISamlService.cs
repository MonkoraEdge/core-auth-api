using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

/// <summary>
/// Orchestrates SAML 2.0 SP-initiated Single Sign-On.
/// The HTTP binding layer (Saml2RedirectBinding / Saml2PostBinding) lives in the controller;
/// this service owns the business logic: provider CRUD, user provisioning, and token issuance.
/// </summary>
public interface ISamlService
{
    /// <summary>Look up an active SAML provider by its URL code slug.</summary>
    Task<SamlProvider?> GetProviderAsync(string providerCode);

    /// <summary>
    /// Resolves the provider config, decrypts the SP private key (if present), and returns
    /// a POCO holding all data needed for the controller to build a <c>Saml2Configuration</c>.
    /// </summary>
    Task<SamlSpConfig> GetSpConfigAsync(SamlProvider provider);

    /// <summary>
    /// After a successful SAML assertion, find-or-provision the local user and issue JWT tokens.
    /// Auto-provision behaviour is controlled by <see cref="SamlProvider.AutoProvisionUsers"/>.
    /// </summary>
    Task<LoginResponse> IssueTokensAsync(
        SamlProvider provider,
        string nameIdentifier,
        string? email,
        string? firstName,
        string? lastName,
        string? ipAddress,
        string? userAgent);

    /// <summary>
    /// Store a <see cref="LoginResponse"/> in Redis under a short-lived (2-min) opaque code.
    /// Returns the code. Clients use this code with <see cref="ExchangeSamlCodeAsync"/>
    /// to retrieve the token pair after the browser-redirect SAML dance completes.
    /// </summary>
    Task<string> StoreSamlCodeAsync(LoginResponse loginResponse);

    /// <summary>Exchange a previously issued SAML code for the stored token pair (one-time use).</summary>
    Task<LoginResponse> ExchangeSamlCodeAsync(string samlCode);

    // ─── Admin CRUD ───────────────────────────────────────────────────────────

    Task<IEnumerable<SamlProvider>> GetAllProvidersAsync();
    Task<SamlProvider> CreateProviderAsync(SamlProviderCreateRequest request);
    Task UpdateProviderAsync(Guid id, SamlProviderUpdateRequest request);
    Task DeleteProviderAsync(Guid id);
}
