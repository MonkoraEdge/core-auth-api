using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;

namespace MonkoraEdge.Core.Auth.Domain.Services;

/// <summary>
/// Centralizes refresh-token validation, reuse detection, rotation, and token issuance.
/// This keeps OAuth2 and legacy auth refresh paths behaviorally aligned.
/// </summary>
public sealed class RefreshTokenProcessor : IRefreshTokenProcessor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly ITokenService _tokenService;

    public RefreshTokenProcessor(
        IUnitOfWork unitOfWork,
        IRefreshTokenRepository refreshTokenRepo,
        ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _refreshTokenRepo = refreshTokenRepo;
        _tokenService = tokenService;
    }

    public async Task<RefreshToken> ValidateActiveAsync(string refreshToken, string errorSource, string invalidMessage, string expiredMessage)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new DomainException(errorSource, ErrorCodeType.INVALID_GRANT, invalidMessage);

        var tokenHash = _tokenService.HashToken(refreshToken);
        var storedToken = await _refreshTokenRepo.GetByTokenHashAsync(tokenHash);

        if (storedToken == null)
            throw new DomainException(errorSource, ErrorCodeType.INVALID_GRANT, invalidMessage);

        if (storedToken.RevokedAt.HasValue)
        {
            if (storedToken.FamilyId != Guid.Empty)
                await _tokenService.RevokeTokenFamilyAsync(storedToken.FamilyId, "refresh_token_reuse_detected");

            throw new DomainException(errorSource, ErrorCodeType.TOKEN_REVOKED,
                "The refresh token has already been used. All sessions in this chain have been revoked for security.");
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
            throw new DomainException(errorSource, ErrorCodeType.TOKEN_EXPIRED, expiredMessage);

        return storedToken;
    }

    public async Task<TokenResponse> RotateAsync(RefreshToken refreshToken, AuthorizationClient client, int refreshTokenLifetimeSeconds, string? ipAddress, string? userAgent, string[]? requestedScopes = null)
    {
        if (refreshToken.ClientId != client.Id)
            throw new DomainException("refresh_token", "Client mismatch.");

        // Use narrowed scope if the client requested it (RFC 6749 §6); fall back to original grant scopes.
        var scopes = requestedScopes ?? refreshToken.Scopes;
        var userId = refreshToken.UserId == Guid.Empty ? (Guid?)null : refreshToken.UserId;

        // RFC 6749 §5.1: expires_in MUST reflect the actual JWT lifetime, not the client config value.
        var expiresIn = _tokenService.GetAccessTokenLifetimeSeconds(client.AccessTokenLifetime);
        var newAccessToken = await _tokenService.GenerateAccessTokenAsync(
            client.Id, userId, scopes, "refresh_token", ipAddress, userAgent, expiresIn);
        // Generate the new refresh token first so we know its ID before we write anything.
        var (newRefreshToken, newRefreshTokenId) = await _tokenService.GenerateRefreshTokenAsync(
            client.Id, userId, refreshToken.SessionId, scopes,
            refreshTokenLifetimeSeconds, familyId: refreshToken.FamilyId,
            ipAddress: ipAddress, userAgent: userAgent);

        // Atomically revoke the old token and record its replacement in a single UPDATE.
        // - If this returns false, a concurrent rotation or reuse beat us here.
        // - ExecuteUpdateAsync bypasses EF change tracking so no in-memory state conflict.
        var revoked = await _refreshTokenRepo.TryRevokeWithRotationAsync(
            refreshToken.Id, DateTime.UtcNow, newRefreshTokenId);
        if (!revoked)
        {
            if (refreshToken.FamilyId != Guid.Empty)
                await _tokenService.RevokeTokenFamilyAsync(refreshToken.FamilyId, "refresh_token_reuse_detected");

            throw new DomainException("refresh_token", ErrorCodeType.TOKEN_REVOKED,
                "The refresh token has already been used. All sessions in this chain have been revoked for security.");
        }

        await _unitOfWork.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = newAccessToken,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            RefreshToken = newRefreshToken,
            Scope = string.Join(" ", scopes)
        };
    }
}