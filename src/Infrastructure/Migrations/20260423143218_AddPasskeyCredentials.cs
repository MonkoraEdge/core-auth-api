using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonkoraEdge.Core.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPasskeyCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mt_passkey_credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_id = table.Column<byte[]>(type: "bytea", nullable: false),
                    credential_id_base64url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    public_key = table.Column<byte[]>(type: "bytea", nullable: false),
                    signature_counter = table.Column<long>(type: "bigint", nullable: false),
                    aa_guid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    transports = table.Column<string[]>(type: "jsonb", nullable: true),
                    is_backup_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    is_backed_up = table.Column<bool>(type: "boolean", nullable: false),
                    attestation_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    friendly_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mt_passkey_credentials", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mt_passkey_credentials_credential_id_base64url",
                table: "mt_passkey_credentials",
                column: "credential_id_base64url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mt_passkey_credentials_user_id",
                table: "mt_passkey_credentials",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mt_passkey_credentials");
        }
    }
}
