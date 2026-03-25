using MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Cache.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Cache
{
    public class MemoryCacheProvider : ICacheProvider
    {
        private readonly IMemoryCache _memoryCache;

        // Thread-safe index of all keys stored by this provider instance.
        private static readonly HashSet<string> _cacheIndex = new();
        private static readonly object _lock = new();

        public MemoryCacheProvider(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public Task<T?> GetAsync<T>(string cacheKey)
        {
            if (_memoryCache.TryGetValue(cacheKey, out T? value))
                return Task.FromResult(value);

            return Task.FromResult(default(T?));
        }

        // ----------------------------------------------------------------------
        // SetCache
        // ----------------------------------------------------------------------
        public Task SetAsync<T>(string cacheKey, T value, TimeSpan? expiry = null)
        {
            var options = new MemoryCacheEntryOptions();

            if (expiry.HasValue)
                options.SetAbsoluteExpiration(expiry.Value);
            else
                options.SetSlidingExpiration(TimeSpan.FromMinutes(30));

            options.SetPriority(CacheItemPriority.High);

            lock (_lock)
            {
                _memoryCache.Set(cacheKey, value, options);
                _cacheIndex.Add(cacheKey);
            }

            return Task.CompletedTask;
        }

        // ----------------------------------------------------------------------
        // Clear Caches
        // ----------------------------------------------------------------------
        public Task RemoveAsync(string cacheKey)
        {
            var count = 0;

            lock (_lock)
            {
                var keysToRemove = _cacheIndex
                    .Where(k => k.StartsWith(cacheKey))
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _memoryCache.Remove(key);
                    _cacheIndex.Remove(key);
                    count++;
                }
            }

            return Task.FromResult(count);
        }

        // ----------------------------------------------------------------------
        // Clear all Caches
        // ----------------------------------------------------------------------
        public Task RemoveAllAsync()
        {
            var count = 0;

            lock (_lock)
            {
                foreach (var key in _cacheIndex.ToList())
                {
                    _memoryCache.Remove(key);
                    _cacheIndex.Remove(key);
                    count++;
                }
            }

            return Task.FromResult(count);
        }

        public Task<bool> ExistsAsync(string key)
            => Task.FromResult(_memoryCache.TryGetValue(key, out _));

        // ----------------------------------------------------------------------
        // GET-OR-SET (cache-aside)
        // ----------------------------------------------------------------------
        public async Task<T?> GetOrSetAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiry = null)
        {
            if (_memoryCache.TryGetValue(cacheKey, out T? cached))
                return cached;

            var value = await factory();
            if (value is not null)
                await SetAsync(cacheKey, value, expiry);

            return value;
        }
    }
}