using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace MonkoraEdge.Core.DotNet.Utilities
{
    public static class CacheHelper
    {
        public static IMemoryCache CreateMemoryCache()
        {
            var services = new ServiceCollection();
            services.AddMemoryCache();

            var serviceProvider = services.BuildServiceProvider();

            var memoryCache = serviceProvider.GetService<IMemoryCache>();

            return memoryCache;
        }
    }
}
