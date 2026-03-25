using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.DotNet.Converter.Database
{
    public static class SqliteConventions
    {
        public static void Apply(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                entityType.SetTableName(entityType.GetTableName()!.ToLowerInvariant());

                foreach (var property in entityType.GetProperties())
                    property.SetColumnName(property.Name.ToLowerInvariant());
            }
        }
    }
}
