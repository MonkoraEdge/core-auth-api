namespace MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Cache.Interfaces
{
    public interface ICacheProvider
    {
        /// <summary>
        /// Get value from cache (or null if not exists)
        /// </summary>
        Task<T?> GetAsync<T>(string cacheKey);

        /// <summary>
        /// Set value to cache with optional expiration.
        /// </summary>
        Task SetAsync<T>(string cacheKey, T value, TimeSpan? expiry = null);

        /// <summary>
        /// Cache-aside pattern: return cached value if present; otherwise invoke
        /// <paramref name="factory"/>, store the result, and return it.
        /// </summary>
        Task<T?> GetOrSetAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiry = null);

        /// <summary>
        /// Remove a cacheKey from cache.
        /// </summary>
        Task RemoveAsync(string cacheKey);

        /// <summary>
        /// Remove all cached entries (implementation-specific — no-op for distributed stores
        /// that lack server-side SCAN/FLUSHDB permission).
        /// </summary>
        Task RemoveAllAsync();

        /// <summary>
        /// Check if cacheKey exists in cache.
        /// </summary>
        Task<bool> ExistsAsync(string cacheKey);
    }
}