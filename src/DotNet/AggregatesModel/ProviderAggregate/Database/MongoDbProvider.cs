using MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Database.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.ValueObjects;


namespace MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Database
{
    public class MongoDbProvider : IDatabaseProvider
    {
        public DatabaseType Type => DatabaseType.MongoDb;

        public string BuildConnectionString(string host, string database, string user, string password, int? port = null)
        {
            return $"mongodb://{user}:{password}@{host}:{port ?? Type.DefaultPort}/{database}";
        }

        public string GetEfCoreProviderName() => Type.ProviderName;

        public bool IsRelational() => false;
    }
}
