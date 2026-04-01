using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services.Interface;

public interface IRefreshTokenProcessor
{
    /// <summary>Load and validate a refresh token, including reuse detection and family revocation.</summary>
    Task<RefreshToken> ValidateActiveAsync(string refreshToken, string errorSource, string invalidMessage, string expiredMessage);

    /// <summary>Rotate a validated refresh token and issue a new token pair. Pass <paramref name="requestedScopes"/> to narrow the granted scope per RFC 6749 §6.</summary>
    Task<TokenResponse> RotateAsync(RefreshToken refreshToken, AuthorizationClient client, int refreshTokenLifetimeSeconds, string? ipAddress, string? userAgent, string[]? requestedScopes = null);
}