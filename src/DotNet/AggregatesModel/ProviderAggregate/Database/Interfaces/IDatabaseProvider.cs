using MonkoraEdge.Core.DotNet.Domain.ValueObjects;

namespace MonkoraEdge.Core.DotNet.AggregatesModel.ProviderAggregate.Database.Interfaces
{
    public interface IDatabaseProvider
    {
        DatabaseType Type { get; }

        string BuildConnectionString(
            string host,
            string database,
            string user,
            string password,
            int? port = null);

        string GetEfCoreProviderName();

        bool IsRelational();
    }
}
