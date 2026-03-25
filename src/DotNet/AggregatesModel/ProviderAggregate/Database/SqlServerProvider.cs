using MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Database.Interfaces;
using MonkoraEdge.Core.DotNet.Domain.ValueObjects;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Database
{
    public class SqlServerProvider : IDatabaseProvider
    {
        public DatabaseType Type => DatabaseType.SqlServer;

        public string BuildConnectionString(string host, string database, string user, string password, int? port = null)
            => $"Server={host},{port ?? Type.DefaultPort};Database={database};User Id={user};Password={password};TrustServerCertificate=True;";

        public string GetEfCoreProviderName() => Type.ProviderName;

        public bool IsRelational() => true;
    }
}
