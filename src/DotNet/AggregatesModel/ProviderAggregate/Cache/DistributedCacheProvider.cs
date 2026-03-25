using MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Cache.Interfaces;
using MonkoraEdge.Core.DotNet.Utilities;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Cache
{

    public class DistributedCacheProvider : ICacheProvider
    {
        private readonly IDistributedCache _cache;

        public DistributedCacheProvider(IDistributedCache cache)
            => _cache = cache;

        // --------------------------------------------------------------
        // GET
        // --------------------------------------------------------------
        public async Task<T?> GetAsync<T>(string key)
        {
            byte[] bytes = await _cache.GetAsync(key);

            if (bytes is null)
                return default;

            string json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(json, JsonHelper.Options);
        }

        // --------------------------------------------------------------
        // SET
        // --------------------------------------------------------------
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var json = JsonSerializer.Serialize(value, JsonHelper.Options);
            var bytes = Encoding.UTF8.GetBytes(json);

            var options = new DistributedCacheEntryOptions();

            if (expiry.HasValue)
                options.AbsoluteExpirationRelativeToNow = expiry.Value;
            else
                options.SlidingExpiration = TimeSpan.FromMinutes(30); // default sliding

            await _cache.SetAsync(key, bytes, options);
        }

        // --------------------------------------------------------------
        // REMOVE
        // --------------------------------------------------------------
        public async Task RemoveAsync(string key)
            => await _cache.RemoveAsync(key);

        // --------------------------------------------------------------
        // REMOVE ALL (IDistributedCache has no built-in SCAN/FLUSH)
        // --------------------------------------------------------------
        public Task RemoveAllAsync()
        {
            // IDistributedCache does not expose a "flush all" API.
            // Override in a derived class if your provider supports it (e.g. Redis FLUSHDB).
            return Task.CompletedTask;
        }

        // --------------------------------------------------------------
        // GET-OR-SET (cache-aside)
        // --------------------------------------------------------------
        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
        {
            var cached = await GetAsync<T>(key);
            if (cached is not null)
                return cached;

            var value = await factory();
            if (value is not null)
                await SetAsync(key, value, expiry);

            return value;
        }
        public async Task<bool> ExistsAsync(string key)
        {
            var bytes = await _cache.GetAsync(key);
            return bytes != null;
        }
    }
}
