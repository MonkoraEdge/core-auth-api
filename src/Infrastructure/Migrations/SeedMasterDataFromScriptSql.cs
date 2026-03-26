using MonkoraEdge.Core.Auth.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Text.RegularExpressions;

namespace MonkoraEdge.Core.Auth.Infrastructure.Migrations;

[DbContext(typeof(AuthenticationDbContext))]
[Migration("20260326130000_SeedMasterDataFromScriptSql")]
public sealed class SeedMasterDataFromScriptSql : Migration
{
    private const string SeedScriptResourceName =
        "MonkoraEdge.Core.Auth.Infrastructure.Migrations.SeedScripts.seed_master_data.sql";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(LoadEmbeddedScript(SeedScriptResourceName));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Seed data should not be deleted automatically because the SQL is idempotent
        // and may already be referenced by live records.
    }

    private static string LoadEmbeddedScript(string resourceName)
    {
        var assembly = typeof(SeedMasterDataFromScriptSql).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded SQL resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return RemoveTransactionStatements(reader.ReadToEnd());
    }

    private static string RemoveTransactionStatements(string sql)
    {
        var normalized = sql.Replace("\r\n", "\n");
        var cleaned = Regex.Replace(
            normalized,
            @"^\s*(BEGIN|COMMIT)\s*;\s*$",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        return cleaned.Trim();
    }
}
