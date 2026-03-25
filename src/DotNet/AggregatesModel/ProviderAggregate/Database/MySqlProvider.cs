using MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Database.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.ValueObjects;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Database
{
    public class MySqlProvider : IDatabaseProvider
    {
        public DatabaseType Type => DatabaseType.MySql;

        public string BuildConnectionString(string host, string database, string user, string password, int? port = null)
        {
            return $"Server={host};Port={port ?? Type.DefaultPort};Database={database};User={user};Password={password};";
        }

        public string GetEfCoreProviderName() => Type.ProviderName;

        public bool IsRelational() => true;
    }
}
