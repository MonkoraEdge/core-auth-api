using MonkoraEdge.Core.DotNet.Domain.SeedWork;

namespace MonkoraEdge.Core.DotNet.Domain.ValueObjects
{
    public sealed class DatabaseType : Enumeration
    {
        public string ProviderName { get; }
        public int DefaultPort { get; }

        private DatabaseType(int value, string name, string providerName, int defaultPort)
            : base(value, name)
        {
            ProviderName = providerName;
            DefaultPort = defaultPort;
        }

        // ---------------------------------------------
        // Static Instances
        // ---------------------------------------------
        public static readonly DatabaseType SqlServer =
            new(1, "SqlServer", "Microsoft.EntityFrameworkCore.SqlServer", 1433);

        public static readonly DatabaseType PostgreSql =
            new(2, "PostgreSql", "Npgsql.EntityFrameworkCore.PostgreSQL", 5432);

        public static readonly DatabaseType MySql =
            new(3, "MySql", "Pomelo.EntityFrameworkCore.MySql", 3306);

        public static readonly DatabaseType Oracle =
            new(4, "Oracle", "Oracle.EntityFrameworkCore", 1521);

        public static readonly DatabaseType Sqlite =
            new(5, "Sqlite", "Microsoft.EntityFrameworkCore.Sqlite", 0);

        public static readonly DatabaseType MongoDb =
            new(6, "MongoDb", "MongoDB.Driver", 27017);

        // ---------------------------------------------
        // Helpers / Behavior
        // ---------------------------------------------
        public bool IsRelational() =>
            this != MongoDb;

        public bool IsDocumentDatabase() =>
            this == MongoDb;

        public static DatabaseType FromProvider(string provider)
            => GetAll<DatabaseType>()
                .First(x => x.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));
    }
}



//var dbType = DatabaseType.SqlServer;

//Console.WriteLine(dbType.Name);          // SqlServer
//Console.WriteLine(dbType.ProviderName);  // Microsoft.EntityFrameworkCore.SqlServer
//Console.WriteLine(dbType.DefaultPort);   // 1433
//Console.WriteLine(dbType.IsRelational()); // true
