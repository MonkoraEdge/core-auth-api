using Microsoft.Extensions.Caching.Memory;
using MonkoraEdge.Core.Auth.Domain.Services.Interface;

namespace MonkoraEdge.Core.Auth.Infrastructure.Services.Security;

/// <summary>
/// In-process implementation of <see cref="ITwoFactorChallengeStore"/> backed by
/// <see cref="IMemoryCache"/>. Suitable for single-instance deployments only.
/// For multi-instance deployments use <see cref="RedisTwoFactorChallengeStore"/> instead.
/// </summary>
public sealed class MemoryTwoFactorChallengeStore : ITwoFactorChallengeStore
{
    private readonly IMemoryCache _cache;

    public MemoryTwoFactorChallengeStore(IMemoryCache cache) => _cache = cache;

    public Task StoreAsync(string token, Guid userId, TimeSpan expiry)
    {
        _cache.Set(CacheKey(token), userId, expiry);
        return Task.CompletedTask;
    }

    public Task<Guid?> ConsumeAsync(string token)
    {
        var key = CacheKey(token);
        if (_cache.TryGetValue<Guid>(key, out var userId))
        {
            _cache.Remove(key); // single-use: remove immediately on consume
            return Task.FromResult<Guid?>(userId);
        }
        return Task.FromResult<Guid?>(null);
    }

    private static string CacheKey(string token) => $"2fa_challenge:{token}";
}
