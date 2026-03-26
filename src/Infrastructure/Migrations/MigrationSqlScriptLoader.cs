using Microsoft.EntityFrameworkCore.Migrations;
using System.Text.RegularExpressions;

namespace MonkoraEdge.Core.Auth.Infrastructure.Migrations
{
    internal static class MigrationSqlScriptLoader
    {
        public static string LoadEmbeddedScript(string resourceName)
        {
            var assembly = typeof(MigrationSqlScriptLoader).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded SQL resource '{resourceName}' was not found in assembly '{assembly.GetName().Name}'.");
            using var reader = new StreamReader(stream);
            return RemoveTransactionStatements(reader.ReadToEnd());
        }

        public static string RemoveTransactionStatements(string sql)
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
}
