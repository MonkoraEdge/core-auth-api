using MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Database.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.ValueObjects;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Database
{
    public class SqliteProvider : IDatabaseProvider
    {
        public DatabaseType Type => DatabaseType.Sqlite;

        public string BuildConnectionString(string host, string database, string user, string password, int? port = null)
        {
            return $"Data Source={database}.db";
        }

        public string GetEfCoreProviderName() => Type.ProviderName;

        public bool IsRelational() => true;
    }
}
