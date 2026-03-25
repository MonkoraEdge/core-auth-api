using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.FactoryAggregate;
using MonkoraEdge.Core.DotNet.Converter.Database;
using MonkoraEdge.Core.DotNet.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Npgsql;
using Npgsql.NameTranslation;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace MonkoraEdge.Core.DotNet.Extensions
{
    public static class ModelBuilderExtensions
    {
        private static readonly Regex KeysRegex = new Regex("^(PK|FK|IX)_", RegexOptions.Compiled);

        public static void ApplyGlobalFiltersSoftDeleted(this ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var foundProperty = entityType.FindProperty("DeletedAt");
                if (foundProperty != null && foundProperty.ClrType == typeof(DateTime?))
                {
                    var parameter = Expression.Parameter(entityType.ClrType);
                    var propertyAccess = foundProperty.PropertyInfo != null
                        ? Expression.Property(parameter, foundProperty.PropertyInfo)
                        : Expression.Property(parameter, "DeletedAt");

                    // Must specify type DateTime? to avoid runtime EF expression tree error
                    var nullConstant = Expression.Constant(null, typeof(DateTime?));
                    var filter = Expression.Lambda(
                        Expression.Equal(propertyAccess, nullConstant),
                        parameter
                    );
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
                }
            }
        }

        /// <summary>
        /// Apply a global query filter that limits all queries to entities belonging to the given tenant.
        /// Entities must have a <c>string TenantId</c> property to be filtered.
        ///
        /// Call inside <c>OnModelCreating</c> after your other configurations:
        /// <code>
        /// protected override void OnModelCreating(ModelBuilder modelBuilder)
        /// {
        ///     base.OnModelCreating(modelBuilder);
        ///     modelBuilder.ApplyGlobalFiltersSoftDeleted();
        ///     modelBuilder.ApplyGlobalFiltersTenanted(_tenantService.TenantId);
        ///     modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        /// }
        /// </code>
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="tenantId">The current tenant identifier (from <c>ITenantService.TenantId</c>).</param>
        public static void ApplyGlobalFiltersTenanted(this ModelBuilder modelBuilder, string tenantId)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var foundProperty = entityType.FindProperty("TenantId");
                if (foundProperty != null && foundProperty.ClrType == typeof(string))
                {
                    var parameter = Expression.Parameter(entityType.ClrType);
                    var propertyAccess = foundProperty.PropertyInfo != null
                        ? Expression.Property(parameter, foundProperty.PropertyInfo)
                        : Expression.Property(parameter, "TenantId");

                    var tenantConstant = Expression.Constant(tenantId, typeof(string));
                    var filter = Expression.Lambda(
                        Expression.Equal(propertyAccess, tenantConstant),
                        parameter
                    );
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
                }
            }
        }

        public static void UseSnakeCaseNames(this ModelBuilder modelBuilder, DatabaseType dbType)
        {
            var caseNameTranslator = new NpgsqlSnakeCaseNameTranslator();

            switch (dbType)
            {
                case var _ when dbType == DatabaseType.SqlServer:
                    SqlServerConventions.Apply(modelBuilder);
                    break;

                case var _ when dbType == DatabaseType.PostgreSql:
                    PostgreSqlConventions.Apply(modelBuilder, caseNameTranslator);
                    break;

                case var _ when dbType == DatabaseType.MySql:
                    MySqlConventions.Apply(modelBuilder);
                    break;

                case var _ when dbType == DatabaseType.Oracle:
                    OracleConventions.Apply(modelBuilder);
                    break;

                case var _ when dbType == DatabaseType.Sqlite:
                    SqliteConventions.Apply(modelBuilder);
                    break;

                case var _ when dbType == DatabaseType.MongoDb:
                    MongoConventions.Apply(modelBuilder);
                    break;

                default:
                    throw ExceptionFactory.Create("Database", ErrorCodeType.OPERATION_NOT_SUPPORTED);
            }
        }

        /// <summary>
        /// Convert entity, property, key, index, foreign key names into snake_case for PostgreSQL.
        /// </summary>
        public static void PostgreSQLConvertToSnake(INpgsqlNameTranslator translator, object target)
        {
            switch (target)
            {
                case IMutableEntityType entityType:
                    ConvertEntityType(entityType, translator);
                    break;

                case IMutableProperty property:
                    ConvertProperty(property, translator);
                    break;

                case IMutableKey key:
                    ConvertKey(key, translator);
                    break;

                case IMutableForeignKey foreignKey:
                    ConvertForeignKey(foreignKey, translator);
                    break;

                case IMutableIndex index:
                    ConvertIndex(index, translator);
                    break;
            }
        }

        // ---------------------------------------------------------------------
        // Entity
        // ---------------------------------------------------------------------
        private static void ConvertEntityType(IMutableEntityType entityType, INpgsqlNameTranslator translator)
        {
            var originalName = entityType.GetTableName();
            if (originalName != null)
            {
                var newName = translator.TranslateTypeName(originalName);
                entityType.SetTableName(newName);
            }

            var originalSchema = entityType.GetSchema();
            if (originalSchema != null)
            {
                var newSchema = translator.TranslateTypeName(originalSchema);
                entityType.SetSchema(newSchema);
            }
        }

        // ---------------------------------------------------------------------
        // Property
        // ---------------------------------------------------------------------
        private static void ConvertProperty(IMutableProperty property, INpgsqlNameTranslator translator)
        {
            var newName = translator.TranslateMemberName(property.GetColumnName());
            property.SetColumnName(newName);
        }

        // ---------------------------------------------------------------------
        // Primary Key / Unique Key
        // ---------------------------------------------------------------------
        private static void ConvertKey(IMutableKey key, INpgsqlNameTranslator translator)
        {
            var newName = translator.TranslateMemberName(key.GetName());
            key.SetName(newName);
        }

        // ---------------------------------------------------------------------
        // Foreign Key
        // ---------------------------------------------------------------------
        private static void ConvertForeignKey(IMutableForeignKey foreignKey, INpgsqlNameTranslator translator)
        {
            var newName = translator.TranslateMemberName(foreignKey.GetConstraintName());
            foreignKey.SetConstraintName(newName);
        }

        // ---------------------------------------------------------------------
        // Index
        // ---------------------------------------------------------------------
        private static void ConvertIndex(IMutableIndex index, INpgsqlNameTranslator translator)
        {
            var newName = translator.TranslateMemberName(index.GetDatabaseName());
            index.SetDatabaseName(newName);
        }
    }
}
