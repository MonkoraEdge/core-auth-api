using Microsoft.Extensions.Caching.Memory;

namespace MonkoraEdge.Core.DotNet.Extensions
{
    public static class CacheExtensions
    {
        public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, MemoryCacheEntryOptions options)
        {
            using (var entry = cache.CreateEntry(key))
            {
                if (options != null)
                {
                    entry.SetOptions(options);
                }

                entry.Value = value;
            }

            return value;
        }
    }
}
