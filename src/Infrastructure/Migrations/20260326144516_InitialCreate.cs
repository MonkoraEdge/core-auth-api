using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonkoraEdge.Core.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_type = table.Column<string>(type: "text", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity_name = table.Column<string>(type: "text", nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    result = table.Column<string>(type: "text", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lnk_authorization_client_scopes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lnk_authorization_client_scopes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lnk_authorization_code_scopes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    authorization_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lnk_authorization_code_scopes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lnk_role_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lnk_role_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lnk_user_external_logins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_user_id = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    provider_email = table.Column<string>(type: "text", nullable: true),
                    access_token_encrypt = table.Column<string>(type: "text", nullable: true),
                    refresh_token_encrypt = table.Column<string>(type: "text", nullable: true),
                    token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    scopes = table.Column<string[]>(type: "text[]", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_lnk_user_external_logins", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lnk_user_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lnk_user_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mt_agreements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agreement_code = table.Column<string>(type: "text", nullable: false),
                    agreement_type = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "jsonb", nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: true),
                    summary = table.Column<string>(type: "jsonb", nullable: true),
                    version = table.Column<string>(type: "text", nullable: false),
                    effective_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    requires_explicit_action = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_mt_agreements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mt_api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    key_hash = table.Column<string>(type: "text", nullable: false),
                    key_prefix = table.Column<string>(type: "text", nullable: false),
                    key_name = table.Column<string>(type: "text", nullable: false),
                    scopes = table.Column<string[]>(type: "text[]", nullable: true),
                    allowed_ips = table.Column<string[]>(type: "text[]", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_mt_api_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mt_authorization_clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    client_id = table.Column<string>(type: "text", nullable: false),
                    client_secret_hash = table.Column<string>(type: "text", nullable: true),
                    client_name = table.Column<string>(type: "text", nullable: false),
                    client_type = table.Column<string>(type: "text", nullable: false, defaultValue: "CONFIDENTIAL"),
                    token_endpoint_auth_method = table.Column<string>(type: "text", nullable: false, defaultValue: "CLIENT_SECRET_BASIC"),
                    require_pkce = table.Column<bool>(type: "boolean", nullable: false),
                    pkce_code_challenge_method = table.Column<string>(type: "text", nullable: true),
                    require_consent = table.Column<bool>(type: "boolean", nullable: false),
                    redirect_uris = table.Column<string[]>(type: "text[]", nullable: false),
                    post_logout_redirect_uris = table.Column<string[]>(type: "text[]", nullable: true),
                    allowed_grant_types = table.Column<string[]>(type: "text[]", nullable: true),
                    allowed_response_types = table.Column<string[]>(type: "text[]", nullable: true),
                    access_token_lifetime = table.Column<int>(type: "integer", nullable: false, defaultValue: 3600),
                    refresh_token_lifetime = table.Column<int>(type: "integer", nullable: false, defaultValue: 2592000),
                    logo_uri = table.Column<string>(type: "text", nullable: true),
                    client_uri = table.Column<string>(type: "text", nullable: true),
                    jwks_uri = table.Column<string>(type: "text", nullable: true),
                    jwks = table.Column<string>(type: "text", nullable: true),
                    client_secret_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_mt_authorization_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mt_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    permission_code = table.Column<string>(type: "text", nullable: false),
                    permission_name = table.Column<string>(type: "jsonb", nullable: false),
                    resource = table.Column<string>(type: "text", nullable: true),
                    action = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_mt_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mt_providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    provider_code = table.Column<string>(type: "text", nullable: false),
                    provider_name = table.Column<string>(type: "jsonb", nullable: false),
                    protocol = table.Column<string>(type: "text", nullable: true),
                    client_id = table.Column<string>(type: "text", nullable: true),
                    client_secret_encrypt = table.Column<string>(type: "text", nullable: true),
                    scopes = table.Column<string[]>(type: "text[]", nullable: false),
                    issuer = table.Column<string>(type: "text", nullable: true),
                    authorization_url = table.Column<string>(type: "text", nullable: true),
                    jwks_uri = table.Column<string>(type: "text", nullable: true),
                    token_url = table.Column<string>(type: "text", nullable: true),
                    userinfo_url = table.Column<string>(type: "text", nullable: true),
                    discovery_url = table.Column<string>(type: "text", nullable: true),
                    end_session_endpoint = table.Column<string>(type: "text", nullable: true),
                    callback_url = table.Column<string>(type: "text", nullable: true),
                    pkce_supported = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_mt_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mt_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role_code = table.Column<string>(type: "text", nullable: false),
                    role_name = table.Column<string>(type: "jsonb", nullable: false),
                    parent_role_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_mt_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mt_scopes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    scope_name = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    scope_type = table.Column<string>(type: "text", nullable: false, defaultValue: "CUSTOM"),
                    claims = table.Column<string[]>(type: "text[]", nullable: true),
                    is_system_scope = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_mt_scopes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mt_tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_code = table.Column<string>(type: "text", nullable: false),
                    tenant_name = table.Column<string>(type: "jsonb", nullable: false),
                    settings = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_mt_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mt_user_identities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_type = table.Column<string>(type: "text", nullable: false, defaultValue: "LOCAL"),
                    username = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    hash_algorithm = table.Column<string>(type: "text", nullable: true),
                    password_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_mt_user_identities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mt_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    locale = table.Column<string>(type: "text", nullable: true),
                    zoneinfo = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_verified = table.Column<bool>(type: "boolean", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "INACTIVE"),
                    registration_source = table.Column<string>(type: "text", nullable: false, defaultValue: "LOCAL"),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_password_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_mt_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_agreement_accepts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    withdrawn_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    withdrawn_reason = table.Column<string>(type: "text", nullable: true),
                    acceptance_method = table.Column<string>(type: "text", nullable: false, defaultValue: "CHECKBOX"),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tx_agreement_accepts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_authorization_access_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    token_type = table.Column<string>(type: "text", nullable: false, defaultValue: "Bearer"),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scopes = table.Column<string[]>(type: "text[]", nullable: false),
                    grant_type = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tx_authorization_access_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_authorization_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scopes = table.Column<string[]>(type: "text[]", nullable: false),
                    redirect_uri = table.Column<string>(type: "text", nullable: false),
                    code_challenge = table.Column<string>(type: "text", nullable: true),
                    code_challenge_method = table.Column<string>(type: "text", nullable: true),
                    nonce = table.Column<string>(type: "text", nullable: true),
                    auth_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tx_authorization_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_authorization_consents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scopes = table.Column<string[]>(type: "text[]", nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tx_authorization_consents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_authorization_refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    refresh_token_hash = table.Column<string>(type: "text", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scopes = table.Column<string[]>(type: "text[]", nullable: false),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tx_authorization_refresh_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_email_verifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    verification_type = table.Column<string>(type: "text", nullable: false, defaultValue: "REGISTRATION"),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tx_email_verifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_login_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    username = table.Column<string>(type: "text", nullable: true),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: true),
                    login_method = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    device_fingerprint = table.Column<string>(type: "text", nullable: true),
                    country_code = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    failure_reason = table.Column<string>(type: "text", nullable: true),
                    risk_score = table.Column<int>(type: "integer", nullable: true),
                    latency_ms = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tx_login_attempts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_password_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    hash_algorithm = table.Column<string>(type: "text", nullable: true),
                    password_strength = table.Column<int>(type: "integer", nullable: true),
                    is_temporary = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tx_password_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_password_resets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tx_password_resets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_rate_limits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    identifier = table.Column<string>(type: "text", nullable: false),
                    endpoint = table.Column<string>(type: "text", nullable: false),
                    window_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    window_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    request_count = table.Column<int>(type: "integer", nullable: false),
                    limit_count = table.Column<int>(type: "integer", nullable: false),
                    blocked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    scope = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tx_rate_limits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_revoked_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    token_type = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tx_revoked_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_user_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_mime_type = table.Column<string>(type: "text", nullable: true),
                    file_name = table.Column<string>(type: "jsonb", nullable: false),
                    file_url = table.Column<string>(type: "text", nullable: false),
                    file_type = table.Column<string>(type: "text", nullable: false, defaultValue: "OTHER"),
                    file_size = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_tx_user_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_user_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_token_hash = table.Column<string>(type: "text", nullable: false),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tx_user_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_user_sessions_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_name = table.Column<string>(type: "text", nullable: true),
                    device_type = table.Column<string>(type: "text", nullable: false, defaultValue: "UNKNOWN"),
                    device_fingerprint = table.Column<string>(type: "text", nullable: true),
                    os_name = table.Column<string>(type: "text", nullable: true),
                    os_version = table.Column<string>(type: "text", nullable: true),
                    browser_name = table.Column<string>(type: "text", nullable: true),
                    browser_version = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    login_method = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    country = table.Column<string>(type: "text", nullable: true),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    is_trusted = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tx_user_sessions_devices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_user_two_factor_recovery_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    two_factor_setting_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    code_prefix = table.Column<string>(type: "text", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'SYSTEM'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tx_user_two_factor_recovery_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tx_user_two_factor_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_type = table.Column<string>(type: "text", nullable: false),
                    secret_key = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_tx_user_two_factor_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mt_api_keys_key_hash",
                table: "mt_api_keys",
                column: "key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mt_authorization_clients_client_id",
                table: "mt_authorization_clients",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mt_users_email",
                table: "mt_users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tx_authorization_access_tokens_expires_at",
                table: "tx_authorization_access_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_tx_authorization_access_tokens_token_hash",
                table: "tx_authorization_access_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tx_authorization_access_tokens_user_id",
                table: "tx_authorization_access_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tx_authorization_codes_code_hash",
                table: "tx_authorization_codes",
                column: "code_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tx_authorization_codes_expires_at",
                table: "tx_authorization_codes",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_tx_authorization_refresh_tokens_expires_at",
                table: "tx_authorization_refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_tx_authorization_refresh_tokens_family_id",
                table: "tx_authorization_refresh_tokens",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "ix_tx_authorization_refresh_tokens_refresh_token_hash",
                table: "tx_authorization_refresh_tokens",
                column: "refresh_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tx_authorization_refresh_tokens_user_id",
                table: "tx_authorization_refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tx_login_attempts_ip_address_success_created_at",
                table: "tx_login_attempts",
                columns: new[] { "ip_address", "success", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_tx_revoked_tokens_expires_at",
                table: "tx_revoked_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_tx_revoked_tokens_token_hash",
                table: "tx_revoked_tokens",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "lnk_authorization_client_scopes");

            migrationBuilder.DropTable(
                name: "lnk_authorization_code_scopes");

            migrationBuilder.DropTable(
                name: "lnk_role_permissions");

            migrationBuilder.DropTable(
                name: "lnk_user_external_logins");

            migrationBuilder.DropTable(
                name: "lnk_user_roles");

            migrationBuilder.DropTable(
                name: "mt_agreements");

            migrationBuilder.DropTable(
                name: "mt_api_keys");

            migrationBuilder.DropTable(
                name: "mt_authorization_clients");

            migrationBuilder.DropTable(
                name: "mt_permissions");

            migrationBuilder.DropTable(
                name: "mt_providers");

            migrationBuilder.DropTable(
                name: "mt_roles");

            migrationBuilder.DropTable(
                name: "mt_scopes");

            migrationBuilder.DropTable(
                name: "mt_tenants");

            migrationBuilder.DropTable(
                name: "mt_user_identities");

            migrationBuilder.DropTable(
                name: "mt_users");

            migrationBuilder.DropTable(
                name: "tx_agreement_accepts");

            migrationBuilder.DropTable(
                name: "tx_authorization_access_tokens");

            migrationBuilder.DropTable(
                name: "tx_authorization_codes");

            migrationBuilder.DropTable(
                name: "tx_authorization_consents");

            migrationBuilder.DropTable(
                name: "tx_authorization_refresh_tokens");

            migrationBuilder.DropTable(
                name: "tx_email_verifications");

            migrationBuilder.DropTable(
                name: "tx_login_attempts");

            migrationBuilder.DropTable(
                name: "tx_password_history");

            migrationBuilder.DropTable(
                name: "tx_password_resets");

            migrationBuilder.DropTable(
                name: "tx_rate_limits");

            migrationBuilder.DropTable(
                name: "tx_revoked_tokens");

            migrationBuilder.DropTable(
                name: "tx_user_files");

            migrationBuilder.DropTable(
                name: "tx_user_sessions");

            migrationBuilder.DropTable(
                name: "tx_user_sessions_devices");

            migrationBuilder.DropTable(
                name: "tx_user_two_factor_recovery_codes");

            migrationBuilder.DropTable(
                name: "tx_user_two_factor_settings");
        }
    }
}
