using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;

namespace MonkoraEdge.Core.Auth.Infrastructure.Services.Security;

/// <summary>
/// Redis-backed implementation of <see cref="ITwoFactorChallengeStore"/>.
/// Safe for multi-instance / horizontally-scaled deployments — challenges survive
/// node restarts and are visible across all instances in the same Redis cluster.
/// Each token is single-use: <see cref="ConsumeAsync"/> removes it immediately after
/// returning the userId, preventing replay of a stolen 2FA token.
/// </summary>
public sealed class RedisTwoFactorChallengeStore : ITwoFactorChallengeStore
{
    private readonly IDistributedCache _cache;

    public RedisTwoFactorChallengeStore(IDistributedCache cache) => _cache = cache;

    public async Task StoreAsync(string token, Guid userId, TimeSpan expiry)
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry };
        var value = Encoding.UTF8.GetBytes(userId.ToString());
        await _cache.SetAsync(CacheKey(token), value, options);
    }

    public async Task<Guid?> ConsumeAsync(string token)
    {
        var key = CacheKey(token);
        var value = await _cache.GetAsync(key);
        if (value == null)
            return null;

        // Remove before returning — if the caller throws after this point the token is
        // already consumed, preventing any window for reuse.
        await _cache.RemoveAsync(key);

        var raw = Encoding.UTF8.GetString(value);
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    private static string CacheKey(string token) => $"2fa_challenge:{token}";
}
