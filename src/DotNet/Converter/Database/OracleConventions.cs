using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.DotNet.Converter.Database
{
    public static class OracleConventions
    {
        public static void Apply(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                entityType.SetTableName(ToOracleName(entityType.GetTableName()));

                foreach (var property in entityType.GetProperties())
                    property.SetColumnName(ToOracleName(property.Name));

                foreach (var key in entityType.GetKeys())
                    key.SetName(ToOracleName(key.GetName()));

                foreach (var fk in entityType.GetForeignKeys())
                    fk.SetConstraintName(ToOracleName(fk.GetConstraintName()));

                foreach (var index in entityType.GetIndexes())
                    index.SetDatabaseName(ToOracleName(index.GetDatabaseName()));
            }
        }

        private static string ToOracleName(string name)
        {
            var upper = name.ToUpperInvariant();
            return upper.Length <= 30 ? upper : upper.Substring(0, 30);
        }
    }
}
