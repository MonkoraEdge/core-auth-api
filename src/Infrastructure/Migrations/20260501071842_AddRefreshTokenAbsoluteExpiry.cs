using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonkoraEdge.Core.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenAbsoluteExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add column with a temporary server-side default so the NOT NULL constraint is satisfied
            // for existing rows. We immediately backfill from expires_at, then drop the default.
            migrationBuilder.AddColumn<DateTime>(
                name: "absolute_expires_at",
                table: "tx_authorization_refresh_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");      // placeholder — overwritten by the UPDATE below

            // Backfill: for all existing tokens, anchor the absolute expiry to their existing
            // sliding-window expiry. This is conservative (no token is immediately revoked) while
            // still enforcing the hard deadline for any future rotation from this point forward.
            migrationBuilder.Sql(
                "UPDATE tx_authorization_refresh_tokens SET absolute_expires_at = expires_at;");

            // Remove the column default — new rows must supply the value explicitly.
            migrationBuilder.AlterColumn<DateTime>(
                name: "absolute_expires_at",
                table: "tx_authorization_refresh_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.CreateIndex(
                name: "ix_tx_authorization_refresh_tokens_absolute_expires_at",
                table: "tx_authorization_refresh_tokens",
                column: "absolute_expires_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tx_authorization_refresh_tokens_absolute_expires_at",
                table: "tx_authorization_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "absolute_expires_at",
                table: "tx_authorization_refresh_tokens");
        }
    }
}
