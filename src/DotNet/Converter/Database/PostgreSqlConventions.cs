using MonkoraEdge.Core.DotNet.Extensions;
using Microsoft.EntityFrameworkCore;
using Npgsql.NameTranslation;

namespace MonkoraEdge.Core.DotNet.Converter.Database
{
    public static class PostgreSqlConventions
    {
        public static void Apply(
            ModelBuilder modelBuilder,
            NpgsqlSnakeCaseNameTranslator caseNameTranslator)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                Convert(entityType, caseNameTranslator);

                foreach (var property in entityType.GetProperties())
                    Convert(property, caseNameTranslator);

                foreach (var key in entityType.GetKeys())
                    Convert(key, caseNameTranslator);

                foreach (var foreignKey in entityType.GetForeignKeys())
                    Convert(foreignKey, caseNameTranslator);

                foreach (var index in entityType.GetIndexes())
                    Convert(index, caseNameTranslator);
            }
        }

        private static void Convert(object target, NpgsqlSnakeCaseNameTranslator translator)
        {
            ModelBuilderExtensions.PostgreSQLConvertToSnake(translator, target);
        }
    }
}
