using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.AuthorizationAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.EntityAggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.OAuth2Aggregate;
using MonkoraEdge.Core.Auth.Domain.AggregatesModel.UserAggregate.Interfaces;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;
using Microsoft.IdentityModel.Tokens;

namespace MonkoraEdge.Core.Auth.Infrastructure.Services.Security;

public class TokenService : ITokenService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessTokenRepository _accessTokenRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IAuthorizationCodeRepository _authCodeRepo;
    private readonly IRevokedTokenRepository _revokedTokenRepo;
    private readonly IUserRepository _userRepo;
    private readonly RsaSecurityKey _signingKey;
    private readonly string _issuer;
    private readonly int _defaultAccessTokenLifetimeSeconds;

    public TokenService(
        IUnitOfWork unitOfWork,
        IAccessTokenRepository accessTokenRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IAuthorizationCodeRepository authCodeRepo,
        IRevokedTokenRepository revokedTokenRepo,
        IUserRepository userRepo,
        string privateKeyPem,
        string issuer,
        int defaultAccessTokenLifetimeSeconds = 3600)
    {
        _unitOfWork = unitOfWork;
        _accessTokenRepo = accessTokenRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _authCodeRepo = authCodeRepo;
        _revokedTokenRepo = revokedTokenRepo;
        _userRepo = userRepo;
        _issuer = issuer;
        _defaultAccessTokenLifetimeSeconds = defaultAccessTokenLifetimeSeconds;

        var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        _signingKey = new RsaSecurityKey(rsa) { KeyId = ComputeKeyId(rsa) };
    }

    public Task<string> GenerateAccessTokenAsync(Guid clientId, Guid? userId, string[] scopes, string? grantType, string? ipAddress, string? userAgent)
    {
        var now = DateTime.UtcNow;
        var jti = Guid.NewGuid().ToString("N");
        var expiry = now.AddSeconds(_defaultAccessTokenLifetimeSeconds);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(JwtRegisteredClaimNames.Iss, _issuer),
            new Claim(JwtRegisteredClaimNames.Aud, _issuer),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("client_id", clientId.ToString()),
            new Claim("scope", string.Join(" ", scopes)),
        };

        if (userId.HasValue)
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userId.Value.ToString()));

        if (!string.IsNullOrEmpty(grantType))
            claims.Add(new Claim("grant_type", grantType));

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            claims: claims,
            notBefore: now,
            expires: expiry,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        var tokenHash = HashToken(tokenString);

        _accessTokenRepo.Insert(new AccessToken
        {
            TokenHash = tokenHash,
            TokenType = "Bearer",
            ClientId = clientId,
            UserId = userId,
            Scopes = scopes,
            GrantType = grantType,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IssuedAt = now,
            ExpiresAt = expiry
        });

        return Task.FromResult(tokenString);
    }

    public Task<string> GenerateRefreshTokenAsync(Guid accessTokenId, Guid clientId, Guid? userId, Guid? sessionId, string[] scopes, int lifetimeSeconds, Guid? familyId = null)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var tokenHash = HashToken(rawToken);

        _refreshTokenRepo.Insert(new RefreshToken
        {
            RefreshTokenHash = tokenHash,
            FamilyId = familyId ?? Guid.NewGuid(),
            ClientId = clientId,
            UserId = userId ?? Guid.Empty,
            SessionId = sessionId,
            Scopes = scopes,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(lifetimeSeconds)
        });

        return Task.FromResult(rawToken);
    }

    public Task<string> GenerateAuthorizationCodeAsync(Guid clientId, Guid userId, Guid? sessionId, string[] scopes, string redirectUri, string? codeChallenge, string? codeChallengeMethod, string? nonce)
    {
        var rawCode = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var codeHash = HashToken(rawCode);

        _authCodeRepo.Insert(new AuthorizationCode
        {
            CodeHash = codeHash,
            ClientId = clientId,
            UserId = userId,
            SessionId = sessionId,
            Scopes = scopes,
            RedirectUri = redirectUri,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod ?? "S256",
            Nonce = nonce,
            AuthTime = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(60)
        });

        return Task.FromResult(rawCode);
    }

    public async Task<IntrospectResponse> IntrospectTokenAsync(string token, string? tokenTypeHint)
    {
        var inactive = new IntrospectResponse { Active = false };

        if (string.IsNullOrEmpty(token))
            return inactive;

        var tokenHash = HashToken(token);
        if (await _revokedTokenRepo.IsRevokedAsync(tokenHash))
            return inactive;

        var accessToken = await _accessTokenRepo.GetByTokenHashAsync(tokenHash);
        if (accessToken != null)
        {
            if (accessToken.ExpiresAt <= DateTime.UtcNow || accessToken.RevokedAt.HasValue)
                return inactive;

            return new IntrospectResponse
            {
                Active = true,
                ClientId = accessToken.ClientId.ToString(),
                Scope = string.Join(" ", accessToken.Scopes),
                Issuer = _issuer,
                Exp = new DateTimeOffset(accessToken.ExpiresAt).ToUnixTimeSeconds(),
                Iat = new DateTimeOffset(accessToken.IssuedAt).ToUnixTimeSeconds(),
                Jti = accessToken.Id.ToString(),
                TokenType = "Bearer",
                Sub = accessToken.UserId.HasValue ? accessToken.UserId.Value.ToString() : null
            };
        }

        var refreshToken = await _refreshTokenRepo.GetByTokenHashAsync(tokenHash);
        if (refreshToken != null)
        {
            if (refreshToken.ExpiresAt <= DateTime.UtcNow || refreshToken.RevokedAt.HasValue)
                return inactive;

            return new IntrospectResponse
            {
                Active = true,
                Sub = refreshToken.UserId.ToString(),
                ClientId = refreshToken.ClientId.ToString(),
                Scope = string.Join(" ", refreshToken.Scopes),
                Exp = new DateTimeOffset(refreshToken.ExpiresAt).ToUnixTimeSeconds(),
                Iat = new DateTimeOffset(refreshToken.IssuedAt).ToUnixTimeSeconds(),
                TokenType = "refresh_token"
            };
        }

        return inactive;
    }

    public async Task RevokeTokenAsync(string token, string? tokenTypeHint, Guid clientId, string? reason = "revoked")
    {
        var tokenHash = HashToken(token);
        var now = DateTime.UtcNow;
        var revokedAny = false;

        var accessToken = await _accessTokenRepo.GetByTokenHashAsync(tokenHash);
        if (accessToken != null && accessToken.ClientId == clientId && accessToken.RevokedAt == null)
        {
            accessToken.RevokedAt = now;
            _accessTokenRepo.Update(accessToken);
            revokedAny = true;
        }

        var refreshToken = await _refreshTokenRepo.GetByTokenHashAsync(tokenHash);
        if (refreshToken != null && refreshToken.ClientId == clientId && refreshToken.RevokedAt == null)
        {
            refreshToken.RevokedAt = now;
            _refreshTokenRepo.Update(refreshToken);
            revokedAny = true;
        }

        // Do not reveal token ownership or existence for another client.
        if (!revokedAny)
            return;

        var existing = await _revokedTokenRepo.GetByTokenHashAsync(tokenHash);
        if (existing == null)
        {
            _revokedTokenRepo.Insert(new RevokedToken
            {
                TokenHash = tokenHash,
                TokenType = tokenTypeHint ?? "access_token",
                ClientId = clientId,
                UserId = accessToken?.UserId ?? refreshToken?.UserId,
                Reason = reason,
                RevokedAt = now,
                ExpiresAt = accessToken?.ExpiresAt ?? refreshToken?.ExpiresAt
            });
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, Guid? sessionId = null, string reason = "logout")
    {
        var activeTokens = await _accessTokenRepo.GetActiveByUserIdAsync(userId);
        foreach (var token in activeTokens)
        {
            if (sessionId.HasValue && token.SessionId != sessionId)
                continue;

            token.RevokedAt = DateTime.UtcNow;
            _accessTokenRepo.Update(token);

            _revokedTokenRepo.Insert(new RevokedToken
            {
                TokenHash = token.TokenHash,
                TokenType = "access_token",
                ClientId = token.ClientId,
                UserId = userId,
                Reason = reason,
                RevokedAt = DateTime.UtcNow,
                ExpiresAt = token.ExpiresAt
            });
        }

        var activeRefreshTokens = await _refreshTokenRepo.GetActiveByUserIdAsync(userId);
        foreach (var token in activeRefreshTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            _refreshTokenRepo.Update(token);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public JwksResponse GetJwks()
    {
        var parameters = _signingKey.Rsa.ExportParameters(false);
        return new JwksResponse
        {
            Keys = new List<JwkKey>
            {
                new JwkKey
                {
                    Kty = "RSA",
                    Use = "sig",
                    Kid = _signingKey.KeyId,
                    Alg = "RS256",
                    N = Base64UrlEncoder.Encode(parameters.Modulus),
                    E = Base64UrlEncoder.Encode(parameters.Exponent)
                }
            }
        };
    }

    public string GetIssuer() => _issuer;

    public async Task<string> GenerateIdTokenAsync(Guid clientId, Guid userId, string[] scopes, string? nonce, DateTime authTime)
    {
        var now = DateTime.UtcNow;
        var expiry = now.AddMinutes(5);

        var user = await _userRepo.GetByIdAsync(userId);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Iss, _issuer),
            new Claim(JwtRegisteredClaimNames.Aud, clientId.ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim("auth_time", new DateTimeOffset(authTime).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        if (!string.IsNullOrEmpty(nonce))
            claims.Add(new Claim("nonce", nonce));

        if (user != null)
        {
            if (scopes.Contains("profile"))
            {
                if (!string.IsNullOrEmpty(user.DisplayName))
                    claims.Add(new Claim("name", user.DisplayName));
                if (!string.IsNullOrEmpty(user.LocaleCode))
                    claims.Add(new Claim("locale", user.LocaleCode));
                if (!string.IsNullOrEmpty(user.Zoneinfo))
                    claims.Add(new Claim("zoneinfo", user.Zoneinfo));
            }
            if (scopes.Contains("email"))
            {
                claims.Add(new Claim("email", user.Email));
                claims.Add(new Claim("email_verified", user.EmailVerified.ToString().ToLower(), ClaimValueTypes.Boolean));
            }
            if (scopes.Contains("phone") && !string.IsNullOrEmpty(user.PhoneNumber))
            {
                claims.Add(new Claim("phone_number", user.PhoneNumber));
                claims.Add(new Claim("phone_number_verified", user.PhoneVerified.ToString().ToLower(), ClaimValueTypes.Boolean));
            }
        }

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(claims: claims, notBefore: now, expires: expiry, signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task RevokeTokenFamilyAsync(Guid familyId, string reason = "refresh_token_reuse")
    {
        var familyTokens = await _refreshTokenRepo.GetByFamilyIdAsync(familyId);
        foreach (var token in familyTokens.Where(t => t.RevokedAt == null))
        {
            token.RevokedAt = DateTime.UtcNow;
            _refreshTokenRepo.Update(token);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    private static string ComputeKeyId(RSA rsa)
    {
        var parameters = rsa.ExportParameters(false);
        var combined = (parameters.Modulus ?? Array.Empty<byte>())
            .Concat(parameters.Exponent ?? Array.Empty<byte>()).ToArray();
        var hash = SHA256.HashData(combined);
        return Base64UrlEncoder.Encode(hash[..16]);
    }
}