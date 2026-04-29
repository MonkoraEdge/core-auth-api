using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonkoraEdge.Core.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mt_webhook_endpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    events = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    secret = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mt_webhook_endpoints", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_webhook_delivery_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    webhook_endpoint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    response_status_code = table.Column<int>(type: "integer", nullable: true),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tx_webhook_delivery_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mt_webhook_endpoints_client_id",
                table: "mt_webhook_endpoints",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_mt_webhook_endpoints_is_active_is_deleted",
                table: "mt_webhook_endpoints",
                columns: new[] { "is_active", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_tx_webhook_delivery_logs_success_created_at",
                table: "tx_webhook_delivery_logs",
                columns: new[] { "success", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_tx_webhook_delivery_logs_webhook_endpoint_id_created_at",
                table: "tx_webhook_delivery_logs",
                columns: new[] { "webhook_endpoint_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mt_webhook_endpoints");

            migrationBuilder.DropTable(
                name: "tx_webhook_delivery_logs");
        }
    }
}
