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

    public async Task StoreAsync(string token, Guid userId, string? clientId, TimeSpan expiry)
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry };
        // Format: "userId|clientId" — clientId may be an empty string for direct logins.
        var value = Encoding.UTF8.GetBytes($"{userId}|{clientId ?? string.Empty}");
        await _cache.SetAsync(CacheKey(token), value, options);
    }

    public async Task<TwoFactorChallengeData?> ConsumeAsync(string token)
    {
        var key = CacheKey(token);
        var value = await _cache.GetAsync(key);
        if (value == null)
            return null;

        // Remove before returning — if the caller throws after this point the token is
        // already consumed, preventing any window for reuse.
        await _cache.RemoveAsync(key);

        var raw = Encoding.UTF8.GetString(value);
        var parts = raw.Split('|', 2);
        if (!Guid.TryParse(parts[0], out var userId))
            return null;
        var clientId = parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) ? parts[1] : null;
        return new TwoFactorChallengeData(userId, clientId);
    }

    private static string CacheKey(string token) => $"2fa_challenge:{token}";
}
