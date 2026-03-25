using MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Cache;
using MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Cache.Interfaces;
using MonkoraEdge.Core.DotNet.Extensions;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.FactoryAggregate
{
    public class CacheProviderFactory
    {
        private readonly IEnumerable<ICacheProvider> _providers;
        private readonly string _error_code="";

        public CacheProviderFactory(IEnumerable<ICacheProvider> providers)
        {
            _providers = providers;
        }
        
        public T GetProvider<T>() where T : ICacheProvider
        {
            var provider = _providers.FirstOrDefault(x => x is T);

            if (provider == null)
                throw ExceptionFactory.NotSupported(typeof(CacheProviderFactory).Name.ToSnakeCase(), errorMessage: $"Cache provider {typeof(T).Name} is not registered.");

            return (T)provider;
        }

        /// <summary>
        /// Resolve provider by type name e.g. "redis", "memory"
        /// </summary>
        public ICacheProvider GetProvider(string type)
        {
            type = type.ToLower();

            return type switch
            {
                "redis" => GetProvider<RedisCacheProvider>(),
                "distributed" => GetProvider<DistributedCacheProvider>(),
                "memory" => GetProvider<MemoryCacheProvider>(),
                _ => throw ExceptionFactory.NotSupported(typeof(CacheProviderFactory).Name.ToSnakeCase(), errorMessage: $"Unsupported cache type: '{type}'. Valid values: redis, distributed, memory.")
            };
        }
    }
}


//public class GetOrderHandler : IRequest<OrderDto>
//{
//    private readonly ICacheProvider _cache;
//    private readonly IOrderRepository _repo;

//    public GetOrderHandler(ICacheProvider cache, IOrderRepository repo)
//    {
//        _cache = cache;
//        _repo = repo;
//    }

//    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken cancellationToken)
//    {
//        string cacheKey = $"order:{request.OrderId}";

//        var cached = await _cache.GetAsync<OrderDto>(cacheKey);
//        if (cached != null)
//            return cached;

//        var order = await _repo.GetAsync(request.OrderId);

//        await _cache.SetAsync(cacheKey, order, TimeSpan.FromMinutes(15));

//        return order;
//    }
//}
