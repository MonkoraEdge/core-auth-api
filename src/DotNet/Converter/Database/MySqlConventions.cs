using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.DotNet.Converter.Database
{
    public static class MySqlConventions
    {
        public static void Apply(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                entityType.SetTableName(entityType.GetTableName()!.ToLowerInvariant());

                foreach (var property in entityType.GetProperties())
                    property.SetColumnName(property.Name.ToLowerInvariant());

                foreach (var key in entityType.GetKeys())
                    key.SetName(key.GetName().ToLowerInvariant());

                foreach (var foreignKey in entityType.GetForeignKeys())
                    foreignKey.SetConstraintName(foreignKey.GetConstraintName()!.ToLowerInvariant());

                foreach (var index in entityType.GetIndexes())
                    index.SetDatabaseName(index.GetDatabaseName()!.ToLowerInvariant());
            }
        }
    }
}
