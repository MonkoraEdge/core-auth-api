using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonkoraEdge.Core.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSamlProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mt_saml_providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    provider_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    sp_entity_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    sp_certificate_pem = table.Column<string>(type: "text", nullable: true),
                    sp_private_key_encrypted = table.Column<string>(type: "text", nullable: true),
                    sign_auth_requests = table.Column<bool>(type: "boolean", nullable: false),
                    want_assertions_signed = table.Column<bool>(type: "boolean", nullable: false),
                    idp_entity_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    idp_sso_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    idp_slo_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    idp_certificate_pem = table.Column<string>(type: "text", nullable: false),
                    idp_metadata_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    name_id_format = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    attribute_mapping_json = table.Column<string>(type: "jsonb", nullable: true),
                    auto_provision_users = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mt_saml_providers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mt_saml_providers_provider_code",
                table: "mt_saml_providers",
                column: "provider_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mt_saml_providers");
        }
    }
}
