using MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Cache.Interfaces;
using MonkoraEdge.Core.DotNet.Utilities;
using StackExchange.Redis;
using System.Text.Json;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Cache
{
    public class RedisCacheProvider : ICacheProvider
    {
        private readonly IDatabase _db;

        public RedisCacheProvider(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string cacheKey)
        {
            var value = await _db.StringGetAsync(cacheKey);
            if (value.IsNullOrEmpty)
                return default;

            return JsonSerializer.Deserialize<T>(value, JsonHelper.Options);
        }

        public async Task SetAsync<T>(string cacheKey, T value, TimeSpan? expiry = null)
        {
            var json = JsonSerializer.Serialize(value, JsonHelper.Options);
            await _db.StringSetAsync(cacheKey, json, expiry ?? TimeSpan.FromMinutes(30));
        }

        public async Task RemoveAsync(string cacheKey)
        {
            await _db.KeyDeleteAsync(cacheKey);
        }

        /// <summary>
        /// Flush the current Redis database. Requires the connected user to have FLUSHDB permission.
        /// Use with caution this clears ALL keys in the Redis database.
        /// </summary>
        public async Task RemoveAllAsync()
        {
            var server = _db.Multiplexer.GetServers().FirstOrDefault();
            if (server != null)
                await server.FlushDatabaseAsync(_db.Database);
        }

        /// <summary>Cache-aside: return cached value or invoke factory, store, and return.</summary>
        public async Task<T?> GetOrSetAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiry = null)
        {
            var cached = await GetAsync<T>(cacheKey);
            if (cached is not null)
                return cached;

            var value = await factory();
            if (value is not null)
                await SetAsync(cacheKey, value, expiry);

            return value;
        }

        public async Task<bool> ExistsAsync(string cacheKey)
        {
            return await _db.KeyExistsAsync(cacheKey);
        }
    }
}
