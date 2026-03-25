using MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Database.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.ValueObjects;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Database
{
    public class OracleProvider : IDatabaseProvider
    {
        public DatabaseType Type => DatabaseType.Oracle;

        public string BuildConnectionString(string host, string database, string user, string password, int? port = null)
        {
            return $"User Id={user};Password={password};Data Source={host}:{port ?? Type.DefaultPort}/{database}";
        }

        public string GetEfCoreProviderName() => Type.ProviderName;

        public bool IsRelational() => true;
    }
}
