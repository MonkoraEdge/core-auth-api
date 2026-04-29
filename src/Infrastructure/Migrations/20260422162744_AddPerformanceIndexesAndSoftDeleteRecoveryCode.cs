using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonkoraEdge.Core.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexesAndSoftDeleteRecoveryCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "tx_user_two_factor_recovery_codes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_by",
                table: "tx_user_two_factor_recovery_codes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tx_user_two_factor_settings_user_id",
                table: "tx_user_two_factor_settings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tx_user_two_factor_recovery_codes_user_id",
                table: "tx_user_two_factor_recovery_codes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tx_user_sessions_devices_user_id",
                table: "tx_user_sessions_devices",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tx_user_sessions_client_id",
                table: "tx_user_sessions",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_tx_user_sessions_user_id_is_active_expires_at",
                table: "tx_user_sessions",
                columns: new[] { "user_id", "is_active", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_tx_user_files_user_id",
                table: "tx_user_files",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tx_password_resets_token_hash",
                table: "tx_password_resets",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tx_password_resets_user_id",
                table: "tx_password_resets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tx_password_history_user_id_created_at",
                table: "tx_password_history",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_tx_login_attempts_username_success_created_at",
                table: "tx_login_attempts",
                columns: new[] { "username", "success", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_tx_email_verifications_token_hash",
                table: "tx_email_verifications",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tx_email_verifications_user_id",
                table: "tx_email_verifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tx_authorization_consents_user_id_client_id",
                table: "tx_authorization_consents",
                columns: new[] { "user_id", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_mt_user_identities_user_id",
                table: "mt_user_identities",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_mt_authorization_clients_is_active",
                table: "mt_authorization_clients",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_mt_authorization_clients_tenant_id",
                table: "mt_authorization_clients",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_lnk_user_roles_role_id",
                table: "lnk_user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_lnk_user_roles_user_id",
                table: "lnk_user_roles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_lnk_user_roles_user_id_role_id",
                table: "lnk_user_roles",
                columns: new[] { "user_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lnk_user_external_logins_provider_id_provider_user_id",
                table: "lnk_user_external_logins",
                columns: new[] { "provider_id", "provider_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_lnk_user_external_logins_user_id",
                table: "lnk_user_external_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_lnk_role_permissions_role_id",
                table: "lnk_role_permissions",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_lnk_role_permissions_role_id_permission_id",
                table: "lnk_role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lnk_authorization_client_scopes_client_id",
                table: "lnk_authorization_client_scopes",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_lnk_authorization_client_scopes_client_id_scope_id",
                table: "lnk_authorization_client_scopes",
                columns: new[] { "client_id", "scope_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_name_entity_id_created_at",
                table: "audit_logs",
                columns: new[] { "entity_name", "entity_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id_created_at",
                table: "audit_logs",
                columns: new[] { "user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tx_user_two_factor_settings_user_id",
                table: "tx_user_two_factor_settings");

            migrationBuilder.DropIndex(
                name: "ix_tx_user_two_factor_recovery_codes_user_id",
                table: "tx_user_two_factor_recovery_codes");

            migrationBuilder.DropIndex(
                name: "ix_tx_user_sessions_devices_user_id",
                table: "tx_user_sessions_devices");

            migrationBuilder.DropIndex(
                name: "ix_tx_user_sessions_client_id",
                table: "tx_user_sessions");

            migrationBuilder.DropIndex(
                name: "ix_tx_user_sessions_user_id_is_active_expires_at",
                table: "tx_user_sessions");

            migrationBuilder.DropIndex(
                name: "ix_tx_user_files_user_id",
                table: "tx_user_files");

            migrationBuilder.DropIndex(
                name: "ix_tx_password_resets_token_hash",
                table: "tx_password_resets");

            migrationBuilder.DropIndex(
                name: "ix_tx_password_resets_user_id",
                table: "tx_password_resets");

            migrationBuilder.DropIndex(
                name: "ix_tx_password_history_user_id_created_at",
                table: "tx_password_history");

            migrationBuilder.DropIndex(
                name: "ix_tx_login_attempts_username_success_created_at",
                table: "tx_login_attempts");

            migrationBuilder.DropIndex(
                name: "ix_tx_email_verifications_token_hash",
                table: "tx_email_verifications");

            migrationBuilder.DropIndex(
                name: "ix_tx_email_verifications_user_id",
                table: "tx_email_verifications");

            migrationBuilder.DropIndex(
                name: "ix_tx_authorization_consents_user_id_client_id",
                table: "tx_authorization_consents");

            migrationBuilder.DropIndex(
                name: "ix_mt_user_identities_user_id",
                table: "mt_user_identities");

            migrationBuilder.DropIndex(
                name: "ix_mt_authorization_clients_is_active",
                table: "mt_authorization_clients");

            migrationBuilder.DropIndex(
                name: "ix_mt_authorization_clients_tenant_id",
                table: "mt_authorization_clients");

            migrationBuilder.DropIndex(
                name: "ix_lnk_user_roles_role_id",
                table: "lnk_user_roles");

            migrationBuilder.DropIndex(
                name: "ix_lnk_user_roles_user_id",
                table: "lnk_user_roles");

            migrationBuilder.DropIndex(
                name: "ix_lnk_user_roles_user_id_role_id",
                table: "lnk_user_roles");

            migrationBuilder.DropIndex(
                name: "ix_lnk_user_external_logins_provider_id_provider_user_id",
                table: "lnk_user_external_logins");

            migrationBuilder.DropIndex(
                name: "ix_lnk_user_external_logins_user_id",
                table: "lnk_user_external_logins");

            migrationBuilder.DropIndex(
                name: "ix_lnk_role_permissions_role_id",
                table: "lnk_role_permissions");

            migrationBuilder.DropIndex(
                name: "ix_lnk_role_permissions_role_id_permission_id",
                table: "lnk_role_permissions");

            migrationBuilder.DropIndex(
                name: "ix_lnk_authorization_client_scopes_client_id",
                table: "lnk_authorization_client_scopes");

            migrationBuilder.DropIndex(
                name: "ix_lnk_authorization_client_scopes_client_id_scope_id",
                table: "lnk_authorization_client_scopes");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_entity_name_entity_id_created_at",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_user_id_created_at",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "tx_user_two_factor_recovery_codes");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                table: "tx_user_two_factor_recovery_codes");
        }
    }
}
