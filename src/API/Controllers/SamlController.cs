using Asp.Versioning;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MonkoraEdge.Core.Auth.API.Controllers;

/// <summary>
/// SAML 2.0 SP-initiated Single Sign-On endpoints.
/// <para>
/// Browser flow: <c>GET /auth/saml/{code}/signin</c> → redirect to IdP →
/// IdP POSTs to <c>/auth/saml/{code}/acs</c> → redirect to <c>{relay_state}?saml_code=xxx</c> →
/// front-end exchanges code via <c>POST /auth/saml/token</c>.
/// </para>
/// </summary>
[Route("auth/saml")]
[ApiController]
[ApiVersion("1.0")]
public class SamlController : MonkoraControllerBase
{
    private readonly ISamlService _samlService;

    public SamlController(ISamlService samlService)
    {
        _samlService = samlService;
    }

    // ─── SP Metadata ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the SAML 2.0 SP metadata XML for this provider.
    /// Register this URL as the SP metadata endpoint in your IdP.
    /// </summary>
    [HttpGet("{providerCode}/metadata")]
    [AllowAnonymous]
    public async Task<IActionResult> Metadata([FromRoute] string providerCode)
    {
        var provider = await _samlService.GetProviderAsync(providerCode)
            ?? throw new DomainException("saml", "SAML provider not found.");

        var spConfig = await _samlService.GetSpConfigAsync(provider);
        var config   = BuildSaml2Config(spConfig);

        var baseUrl  = $"{Request.Scheme}://{Request.Host}";
        var acsUrl   = $"{baseUrl}/auth/saml/{providerCode}/acs";
        var sloUrl   = string.IsNullOrEmpty(spConfig.IdpSloUrl)
            ? null
            : $"{baseUrl}/auth/saml/{providerCode}/sls";

        var entityDescriptor = new EntityDescriptor(config)
        {
            ValidUntil = 365
        };

        var spDescriptor = new SPSsoDescriptor
        {
            AuthnRequestsSigned  = provider.SignAuthRequests,
            WantAssertionsSigned = provider.WantAssertionsSigned,
            NameIDFormats        = new[] { new Uri(ResolveNameIdFormat(spConfig.NameIdFormat)) },
            AssertionConsumerServices = new[]
            {
                new AssertionConsumerService
                {
                    Binding  = ProtocolBindings.HttpPost,
                    Location = new Uri(acsUrl)
                }
            }
        };

        // Include SP signing certificate in metadata when request signing is enabled.
        if (provider.SignAuthRequests && !string.IsNullOrEmpty(spConfig.SpCertificatePem))
        {
            var spCert = LoadCertificateFromPem(spConfig.SpCertificatePem);
            if (spCert != null)
                spDescriptor.SigningCertificates = new[] { spCert };
        }

        // Include SLO endpoint in metadata when IdP supports SLO.
        if (sloUrl != null)
        {
            spDescriptor.SingleLogoutServices = new[]
            {
                new SingleLogoutService
                {
                    Binding  = ProtocolBindings.HttpRedirect,
                    Location = new Uri(sloUrl)
                }
            };
        }

        entityDescriptor.SPSsoDescriptor = spDescriptor;

        var metadataXml = new Saml2Metadata(entityDescriptor).CreateMetadata().ToXml();
        return Content(metadataXml, "application/samlmetadata+xml");
    }

    // ─── SP-initiated Sign-In ─────────────────────────────────────────────────

    /// <summary>
    /// Initiates SP-initiated SSO by redirecting the browser to the IdP.
    /// </summary>
    /// <param name="providerCode">SAML provider slug.</param>
    /// <param name="relayState">
    /// Optional absolute URI the browser should be redirected to after successful
    /// authentication (with <c>?saml_code=xxx</c> appended). Must be HTTPS.
    /// </param>
    [HttpGet("{providerCode}/signin")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SignIn(
        [FromRoute] string providerCode,
        [FromQuery] string? relayState = null)
    {
        var provider = await _samlService.GetProviderAsync(providerCode)
            ?? throw new DomainException("saml", "SAML provider not found.");

        // Validate relay_state to prevent open redirect.
        if (!string.IsNullOrEmpty(relayState))
        {
            if (!Uri.TryCreate(relayState, UriKind.Absolute, out var relayUri)
                || (relayUri.Scheme != Uri.UriSchemeHttps && relayUri.Scheme != Uri.UriSchemeHttp))
                throw new DomainException("saml", "Invalid relay_state: must be an absolute URI.");
        }

        var spConfig = await _samlService.GetSpConfigAsync(provider);
        var config   = BuildSaml2Config(spConfig);

        var acsUrl = $"{Request.Scheme}://{Request.Host}/auth/saml/{providerCode}/acs";

        var authnRequest = new Saml2AuthnRequest(config)
        {
            AssertionConsumerServiceUrl = new Uri(acsUrl),
            NameIdPolicy = new NameIdPolicy
            {
                AllowCreate = true,
                Format      = ResolveNameIdFormat(spConfig.NameIdFormat)
            }
        };

        var binding = new Saml2RedirectBinding();
        if (!string.IsNullOrEmpty(relayState))
            binding.RelayState = relayState;

        binding.Bind(authnRequest);

        return Redirect(binding.RedirectLocation.OriginalString);
    }

    // ─── Assertion Consumer Service (ACS) ─────────────────────────────────────

    /// <summary>
    /// Receives the SAML 2.0 assertion POST-back from the IdP.
    /// On success, issues tokens and either redirects (browser flow) or returns JSON.
    /// </summary>
    [HttpPost("{providerCode}/acs")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> AssertionConsumerService([FromRoute] string providerCode)
    {
        var provider = await _samlService.GetProviderAsync(providerCode)
            ?? throw new DomainException("saml", "SAML provider not found.");

        var spConfig = await _samlService.GetSpConfigAsync(provider);
        var config   = BuildSaml2Config(spConfig);

        var saml2Response = new Saml2AuthnResponse(config);
        var binding = new Saml2PostBinding();

        try
        {
            var genericRequest = Request.ToGenericHttpRequest();
            binding.ReadSamlResponse(genericRequest, saml2Response);
            binding.Unbind(genericRequest, saml2Response);
        }
        catch (Exception ex)
        {
            throw new DomainException("saml",
                $"SAML assertion validation failed: {ex.Message}");
        }

        // Extract identity claims from the validated assertion.
        var nameId    = saml2Response.NameId?.Value;
        if (string.IsNullOrEmpty(nameId))
            throw new DomainException("saml", "SAML assertion missing NameID.");

        var identity  = saml2Response.ClaimsIdentity;
        var email     = ResolveEmailClaim(identity, spConfig.AttributeMapping)
                        ?? (nameId.Contains('@') ? nameId : null);
        var firstName = identity?.FindFirst(ClaimTypes.GivenName)?.Value;
        var lastName  = identity?.FindFirst(ClaimTypes.Surname)?.Value;

        var loginResponse = await _samlService.IssueTokensAsync(
            provider, nameId, email, firstName, lastName,
            GetIpAddress(), GetUserAgent());

        var relayState = binding.RelayState;

        if (!string.IsNullOrEmpty(relayState) &&
            Uri.TryCreate(relayState, UriKind.Absolute, out var relayUri))
        {
            // Browser redirect flow: store tokens in Redis and pass back a short-lived code.
            var samlCode    = await _samlService.StoreSamlCodeAsync(loginResponse);
            var redirectUrl = $"{relayUri}?saml_code={Uri.EscapeDataString(samlCode)}";
            return Redirect(redirectUrl);
        }

        // API / headless flow: return tokens directly.
        return Ok(loginResponse);
    }

    // ─── SAML Code Exchange ───────────────────────────────────────────────────

    /// <summary>
    /// Exchanges a short-lived <c>saml_code</c> (received as a query parameter after
    /// the browser-redirect ACS flow) for a full JWT token pair.
    /// The code is consumed on first use and expires after 2 minutes.
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ExchangeToken([FromBody] SamlTokenRequest request)
    {
        var loginResponse = await _samlService.ExchangeSamlCodeAsync(request.SamlCode);
        return Ok(loginResponse);
    }

    // ─── Admin CRUD ───────────────────────────────────────────────────────────

    /// <summary>Returns all SAML providers (admin).</summary>
    [HttpGet("providers")]
    [Authorize]
    public async Task<IActionResult> GetProviders()
    {
        var providers = await _samlService.GetAllProvidersAsync();
        return Ok(providers.Select(MapToResponse));
    }

    /// <summary>Creates a new SAML provider (admin).</summary>
    [HttpPost("providers")]
    [Authorize]
    public async Task<IActionResult> CreateProvider([FromBody] SamlProviderCreateRequest request)
    {
        var provider = await _samlService.CreateProviderAsync(request);
        return CreatedAtAction(nameof(GetProviders), new { }, MapToResponse(provider));
    }

    /// <summary>Updates an existing SAML provider (admin).</summary>
    [HttpPut("providers/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateProvider(
        [FromRoute] Guid id, [FromBody] SamlProviderUpdateRequest request)
    {
        await _samlService.UpdateProviderAsync(id, request);
        return NoContent();
    }

    /// <summary>Deletes a SAML provider (admin).</summary>
    [HttpDelete("providers/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteProvider([FromRoute] Guid id)
    {
        await _samlService.DeleteProviderAsync(id);
        return NoContent();
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Builds an <see cref="Saml2Configuration"/> from the resolved SP config POCO.
    /// </summary>
    private static Saml2Configuration BuildSaml2Config(SamlSpConfig spConfig)
    {
        var config = new Saml2Configuration
        {
            Issuer             = spConfig.SpEntityId,
            SignatureAlgorithm = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256",
            SingleSignOnDestination = new Uri(spConfig.IdpSsoUrl)
        };

        if (!string.IsNullOrEmpty(spConfig.IdpSloUrl))
            config.SingleLogoutDestination = new Uri(spConfig.IdpSloUrl);

        config.AllowedAudienceUris.Add(spConfig.SpEntityId);

        // Add IdP certificate for assertion signature validation.
        var idpCert = LoadCertificateFromPem(spConfig.IdpCertificatePem);
        if (idpCert != null)
            config.SignatureValidationCertificates.Add(idpCert);

        // Attach SP private key when signing authentication requests.
        if (spConfig.SignAuthRequests
            && !string.IsNullOrEmpty(spConfig.SpCertificatePem)
            && !string.IsNullOrEmpty(spConfig.SpPrivateKeyPem))
        {
            var spCert = LoadCertificateFromPem(spConfig.SpCertificatePem);
            if (spCert != null)
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(spConfig.SpPrivateKeyPem);
                config.SigningCertificate = spCert.CopyWithPrivateKey(rsa);
            }
        }

        return config;
    }

    /// <summary>
    /// Loads a public-key-only X.509 certificate from a PEM string.
    /// Handles both headers-wrapped and raw base64 PEM formats.
    /// </summary>
    private static X509Certificate2? LoadCertificateFromPem(string? pem)
    {
        if (string.IsNullOrWhiteSpace(pem)) return null;

        try
        {
            // .NET 9: create directly from PEM span (handles -----BEGIN CERTIFICATE----- header).
            return X509Certificate2.CreateFromPem(pem);
        }
        catch
        {
            // Fallback: strip PEM headers and decode raw base64.
            var base64 = pem
                .Replace("-----BEGIN CERTIFICATE-----", "")
                .Replace("-----END CERTIFICATE-----", "")
                .Replace("\n", "").Replace("\r", "").Trim();
            try { return new X509Certificate2(Convert.FromBase64String(base64)); }
            catch { return null; }
        }
    }

    /// <summary>
    /// Attempts to resolve the email address from a SAML claims identity using
    /// well-known claim type URIs and an optional IdP-specific attribute mapping.
    /// </summary>
    private static string? ResolveEmailClaim(
        ClaimsIdentity? identity, Dictionary<string, string>? mapping)
    {
        if (identity == null) return null;

        // Try mapped attribute first (e.g. IdP uses "emailAddress" instead of the WS-Fed URI).
        if (mapping != null)
        {
            foreach (var (samlAttr, localField) in mapping)
            {
                if (localField.Equals("email", StringComparison.OrdinalIgnoreCase))
                {
                    var val = identity.FindFirst(samlAttr)?.Value;
                    if (!string.IsNullOrEmpty(val)) return val;
                }
            }
        }

        // Standard WS-Federation URIs tried in priority order.
        return identity.FindFirst(ClaimTypes.Email)?.Value
            ?? identity.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
            ?? identity.FindFirst("email")?.Value;
    }

    private static string ResolveNameIdFormat(string? configured) =>
        string.IsNullOrEmpty(configured)
            ? "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress"
            : configured;

    private static SamlProviderResponse MapToResponse(SamlProvider p) => new()
    {
        Id                   = p.Id,
        ProviderCode         = p.ProviderCode,
        DisplayName          = p.DisplayName,
        SpEntityId           = p.SpEntityId,
        SignAuthRequests     = p.SignAuthRequests,
        WantAssertionsSigned = p.WantAssertionsSigned,
        IdpEntityId          = p.IdpEntityId,
        IdpSsoUrl            = p.IdpSsoUrl,
        IdpSloUrl            = p.IdpSloUrl,
        IdpMetadataUrl       = p.IdpMetadataUrl,
        NameIdFormat         = p.NameIdFormat,
        AutoProvisionUsers   = p.AutoProvisionUsers,
        IsActive             = p.IsActive,
        CreatedAt            = p.CreatedAt
    };
}
