using Microsoft.EntityFrameworkCore;

namespace MonkoraEdge.Core.DotNet.Converter.Database
{
    public static class MongoConventions
    {
        public static void Apply(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // MongoDB = Collection name only
                entityType.SetTableName(entityType.GetTableName()!.ToLowerInvariant());
            }
        }
    }
}
