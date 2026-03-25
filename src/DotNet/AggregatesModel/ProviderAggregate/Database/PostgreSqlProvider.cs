using MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Database.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.ValueObjects;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Database
{
    public class PostgreSqlProvider : IDatabaseProvider
    {
        public DatabaseType Type => DatabaseType.PostgreSql;

        public string BuildConnectionString(string host, string database, string user, string password, int? port = null)
        {
            return $"Host={host};Port={port ?? Type.DefaultPort};Database={database};Username={user};Password={password};";
        }

        public string GetEfCoreProviderName() => Type.ProviderName;

        public bool IsRelational() => true;
    }
}
