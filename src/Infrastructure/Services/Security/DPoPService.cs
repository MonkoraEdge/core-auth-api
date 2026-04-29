using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using MonkoraEdge.Core.Auth.Domain.Exceptions;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;

namespace MonkoraEdge.Core.Auth.Infrastructure.Services.Security;

/// <summary>
/// RFC 9449 DPoP proof validator.
/// Parses the DPoP JWT, verifies the embedded public key, validates htm/htu/iat claims,
/// enforces jti replay protection via distributed cache, and returns the JWK thumbprint.
/// </summary>
public sealed class DPoPService : IDPoPService
{
    private readonly IDistributedCache _cache;

    private static readonly HashSet<string> SupportedAlgorithms = new(StringComparer.OrdinalIgnoreCase)
        { "RS256", "RS384", "RS512", "ES256", "ES384", "ES512", "PS256", "PS384", "PS512" };

    private const int ClockSkewSeconds = 60;
    private const int JtiTtlSeconds   = 120; // Must cover 2× clock skew

    public DPoPService(IDistributedCache cache) => _cache = cache;

    public async Task<string> ValidateProofAsync(string dpopHeader, string httpMethod, string httpUrl)
    {
        var parts = dpopHeader.Split('.');
        if (parts.Length != 3)
            throw new DomainException("dpop", "DPoP proof is not a valid JWT.");

        // ── Step 1: decode JOSE header (unauthenticated — signature checked below) ───
        JsonElement joseHeader;
        try
        {
            var headerBytes = Base64UrlDecode(parts[0]);
            joseHeader = JsonDocument.Parse(headerBytes).RootElement;
        }
        catch
        {
            throw new DomainException("dpop", "DPoP JOSE header is malformed.");
        }

        // ── Step 2: typ MUST be "dpop+jwt" (RFC 9449 §4.2) ─────────────────────────
        if (!joseHeader.TryGetProperty("typ", out var typ) || typ.GetString() != "dpop+jwt")
            throw new DomainException("dpop", "DPoP proof must have typ=dpop+jwt.");

        // ── Step 3: verify alg is one of our supported algorithms ───────────────────
        if (!joseHeader.TryGetProperty("alg", out var algEl) || string.IsNullOrEmpty(algEl.GetString()))
            throw new DomainException("dpop", "DPoP proof alg is missing.");
        var alg = algEl.GetString()!;
        if (!SupportedAlgorithms.Contains(alg))
            throw new DomainException("dpop", $"DPoP alg '{alg}' is not supported.");

        // ── Step 4: extract embedded public key ─────────────────────────────────────
        if (!joseHeader.TryGetProperty("jwk", out var jwkEl))
            throw new DomainException("dpop", "DPoP proof must embed jwk in the JOSE header.");

        var signingKey = BuildSecurityKey(jwkEl);

        // ── Step 5: verify signature ─────────────────────────────────────────────────
        var tokenHandler = new JwtSecurityTokenHandler();
        JwtSecurityToken jwt;
        try
        {
            tokenHandler.ValidateToken(dpopHeader, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false, // iat validated manually
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);
            jwt = (JwtSecurityToken)validatedToken;
        }
        catch (Exception ex)
        {
            throw new DomainException("dpop", $"DPoP proof signature verification failed: {ex.Message}");
        }

        // ── Step 6: validate htm ─────────────────────────────────────────────────────
        var htm = jwt.Claims.FirstOrDefault(c => c.Type == "htm")?.Value;
        if (!string.Equals(htm, httpMethod, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("dpop", $"DPoP htm '{htm}' does not match the request method '{httpMethod}'.");

        // ── Step 7: validate htu ─────────────────────────────────────────────────────
        var htu = jwt.Claims.FirstOrDefault(c => c.Type == "htu")?.Value;
        if (!string.Equals(htu, httpUrl, StringComparison.Ordinal))
            throw new DomainException("dpop", "DPoP htu does not match the request URI.");

        // ── Step 8: validate iat (within ±60 s of server clock) ─────────────────────
        var iatClaim = jwt.Claims.FirstOrDefault(c => c.Type == "iat")?.Value;
        if (!long.TryParse(iatClaim, out var iatUnix))
            throw new DomainException("dpop", "DPoP iat is missing or not a numeric value.");
        var iatUtc = DateTimeOffset.FromUnixTimeSeconds(iatUnix).UtcDateTime;
        if (Math.Abs((DateTime.UtcNow - iatUtc).TotalSeconds) > ClockSkewSeconds)
            throw new DomainException("dpop", "DPoP iat is outside the acceptable clock-skew window (±60 s).");

        // ── Step 9: require jti and enforce replay protection ────────────────────────
        var jti = jwt.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;
        if (string.IsNullOrEmpty(jti))
            throw new DomainException("dpop", "DPoP jti claim is required.");

        var replayKey = $"dpop:jti:{jti}";
        if (await _cache.GetStringAsync(replayKey) != null)
            throw new DomainException("dpop", "DPoP proof jti has already been used (replay detected).");

        await _cache.SetStringAsync(replayKey, "1", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(JtiTtlSeconds)
        });

        // ── Step 10: compute and return JWK thumbprint (RFC 7638) ────────────────────
        return ComputeJwkThumbprint(jwkEl);
    }

    // ─── Private helpers ─────────────────────────────────────────────────────────

    private static SecurityKey BuildSecurityKey(JsonElement jwk)
    {
        if (!jwk.TryGetProperty("kty", out var ktyEl))
            throw new DomainException("dpop", "DPoP jwk is missing kty.");

        return ktyEl.GetString() switch
        {
            "RSA" => BuildRsaKey(jwk),
            "EC"  => BuildEcKey(jwk),
            var kty => throw new DomainException("dpop", $"Unsupported jwk kty: {kty}")
        };
    }

    private static RsaSecurityKey BuildRsaKey(JsonElement jwk)
    {
        if (!jwk.TryGetProperty("n", out var n) || !jwk.TryGetProperty("e", out var e))
            throw new DomainException("dpop", "RSA jwk must contain n and e.");
        var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Modulus  = Base64UrlDecode(n.GetString()!),
            Exponent = Base64UrlDecode(e.GetString()!)
        });
        return new RsaSecurityKey(rsa);
    }

    private static ECDsaSecurityKey BuildEcKey(JsonElement jwk)
    {
        if (!jwk.TryGetProperty("crv", out var crv) ||
            !jwk.TryGetProperty("x",   out var x)   ||
            !jwk.TryGetProperty("y",   out var y))
            throw new DomainException("dpop", "EC jwk must contain crv, x, and y.");

        var curve = crv.GetString() switch
        {
            "P-256" => ECCurve.NamedCurves.nistP256,
            "P-384" => ECCurve.NamedCurves.nistP384,
            "P-521" => ECCurve.NamedCurves.nistP521,
            var c   => throw new DomainException("dpop", $"Unsupported EC curve: {c}")
        };

        var ec = ECDsa.Create(new ECParameters
        {
            Curve = curve,
            Q = new ECPoint
            {
                X = Base64UrlDecode(x.GetString()!),
                Y = Base64UrlDecode(y.GetString()!)
            }
        });
        return new ECDsaSecurityKey(ec);
    }

    /// <summary>
    /// Compute SHA-256 JWK thumbprint per RFC 7638.
    /// Only the required JWK members are included, sorted lexicographically.
    /// </summary>
    private static string ComputeJwkThumbprint(JsonElement jwk)
    {
        if (!jwk.TryGetProperty("kty", out var ktyEl))
            throw new DomainException("dpop", "Cannot compute JWK thumbprint: kty is missing.");

        // RFC 7638 §3.1: canonical JSON uses only the required members, sorted lexicographically.
        var canonical = ktyEl.GetString() switch
        {
            "RSA" => $"{{\"e\":\"{jwk.GetProperty("e").GetString()}\",\"kty\":\"RSA\",\"n\":\"{jwk.GetProperty("n").GetString()}\"}}",
            "EC"  => $"{{\"crv\":\"{jwk.GetProperty("crv").GetString()}\",\"kty\":\"EC\",\"x\":\"{jwk.GetProperty("x").GetString()}\",\"y\":\"{jwk.GetProperty("y").GetString()}\"}}",
            var kty => throw new DomainException("dpop", $"Cannot compute thumbprint for kty={kty}")
        };

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Base64UrlEncode(hash);
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => string.Empty };
        return Convert.FromBase64String(padded);
    }

    private static string Base64UrlEncode(byte[] input)
        => Convert.ToBase64String(input).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
