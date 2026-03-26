using Microsoft.EntityFrameworkCore.Migrations;

namespace MonkoraEdge.Core.Auth.Infrastructure.Migrations;

internal static class SeedMasterDataMigrationSupport
{
    private const string SeedScriptResourceName =
        "MonkoraEdge.Core.Auth.Infrastructure.Migrations.SeedScripts.seed_master_data.sql";

    public static void Apply(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(MigrationSqlScriptLoader.LoadEmbeddedScript(SeedScriptResourceName));
    }
}
