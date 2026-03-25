using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

/// <summary>
/// Encapsulates client authentication and scope resolution for OAuth2 endpoints.
/// Extracted from OAuth2Service to give each concern a single home and to make
/// client auth independently testable.
/// </summary>
public interface IClientAuthenticator
{
    /// <summary>
    /// Load a client by its public client_id. Used on the authorization endpoint
    /// where no client secret is presented.
    /// Throws if the client is not found or inactive.
    /// </summary>
    Task<AuthorizationClient> LoadAsync(string clientId);

    /// <summary>
    /// Fully authenticate a client: load it by client_id, then validate the
    /// client_secret for CONFIDENTIAL clients using bcrypt constant-time compare.
    /// Also rejects expired secrets.
    /// Throws on any failure — callers do not need to check the return value.
    /// </summary>
    Task<AuthorizationClient> AuthenticateAsync(string? clientId, string? clientSecret);

    /// <summary>
    /// Return all scope names a client is allowed to request.
    /// Always includes the OIDC baseline scopes (openid, profile, email, offline_access)
    /// plus any scopes explicitly registered for the client in a single batched query.
    /// </summary>
    Task<IReadOnlyList<string>> GetAllowedScopeNamesAsync(Guid clientId);
}
