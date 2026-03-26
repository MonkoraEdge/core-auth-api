using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonkoraEdge.Core.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedMasterDataFromScriptSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
    {
        SeedMasterDataMigrationSupport.Apply(migrationBuilder);
    }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Seed data should not be deleted automatically because the SQL is idempotent.
    }
    }
}

