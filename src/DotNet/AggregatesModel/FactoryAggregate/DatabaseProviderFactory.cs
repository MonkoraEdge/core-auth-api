using MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Database.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.ValueObjects;
using MonkoraEdge.Core.DotNet.Extensions;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.FactoryAggregate
{
    public class DatabaseProviderFactory
    {
        private readonly Dictionary<DatabaseType, IDatabaseProvider> _providers;

        public DatabaseProviderFactory(IEnumerable<IDatabaseProvider> providers)
            => _providers = providers.ToDictionary(x => x.Type, x => x);

        public IDatabaseProvider GetProvider(DatabaseType type)
        {
            if (_providers.TryGetValue(type, out var provider))
                return provider;

            throw ExceptionFactory.NotSupported(typeof(DatabaseProviderFactory).Name.ToSnakeCase(), errorMessage: $"Database provider not registered: {type.Name}");           
        }
    }
}


//var provider = _factory.GetProvider(DatabaseType.PostgreSql);
//string conn = provider.BuildConnectionString("localhost", "appdb", "admin", "1234");
