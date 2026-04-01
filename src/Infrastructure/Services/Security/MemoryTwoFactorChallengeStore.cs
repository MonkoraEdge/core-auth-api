using Microsoft.Extensions.Caching.Memory;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;

namespace MonkoraEdge.Core.Auth.Infrastructure.Services.Security;

/// <summary>
/// In-process implementation of <see cref="ITwoFactorChallengeStore"/> backed by
/// <see cref="IMemoryCache"/>. Each token is single-use and expires after 5 minutes —
/// the window for a user to complete the 2FA step after entering their password.
/// </summary>
public sealed class MemoryTwoFactorChallengeStore : ITwoFactorChallengeStore
{
    private readonly IMemoryCache _cache;

    public MemoryTwoFactorChallengeStore(IMemoryCache cache) => _cache = cache;

    public void Store(string token, Guid userId, TimeSpan expiry) =>
        _cache.Set(CacheKey(token), userId, expiry);

    public Guid? Consume(string token)
    {
        var key = CacheKey(token);
        if (_cache.TryGetValue<Guid>(key, out var userId))
        {
            _cache.Remove(key); // single-use: remove immediately on consume
            return userId;
        }
        return null;
    }

    private static string CacheKey(string token) => $"2fa_challenge:{token}";
}
