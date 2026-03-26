----------------------------------------------------------------------------------------------------------------------------------
--#seed 33
-- Bootstrap master/reference data for local and shared development environments.
-- Safe to run multiple times.
-- Intentionally excludes mt_users, mt_user_identities, and mt_api_keys because
-- those require environment-specific credentials or secret material.

BEGIN;

INSERT INTO public.mt_tenants (
    tenant_code,
    tenant_name,
    settings,
    is_active,
    created_by
)
SELECT
    'MONKORA_DEMO',
    '{"en":"Monkora Demo Tenant","th":"Monkora เดโม"}'::jsonb,
    '{
        "branding": {
            "display_name": {"en": "Monkora Demo", "th": "Monkora เดโม"},
            "primary_color": "#0f766e"
        },
        "features": {
            "mfa": true,
            "reports": {"enabled": true, "max_rows": 10000}
        },
        "locale": {
            "lang": "th",
            "tz": "Asia/Bangkok",
            "currency": "THB"
        },
        "security": {
            "password": {
                "min_length": 8,
                "require_upper": true,
                "require_numbers": true,
                "require_special": false
            },
            "session": {
                "timeout_minutes": 60,
                "persistent_sessions": false
            },
            "require_mfa_for_admins": true
        },
        "policies": {
            "consent_required": true,
            "allowed_email_domains": []
        }
    }'::jsonb,
    TRUE,
    'SYSTEM'
WHERE NOT EXISTS (
    SELECT 1
    FROM public.mt_tenants existing
    WHERE existing.tenant_code = 'MONKORA_DEMO'
      AND existing.deleted_at IS NULL
);

INSERT INTO public.mt_providers (
    provider_code,
    provider_name,
    protocol,
    client_id,
    client_secret_encrypt,
    scopes,
    issuer,
    authorization_url,
    jwks_uri,
    token_url,
    userinfo_url,
    discovery_url,
    end_session_endpoint,
    callback_url,
    pkce_supported,
    is_active,
    created_by
)
SELECT
    seed.provider_code,
    seed.provider_name,
    seed.protocol,
    seed.client_id,
    seed.client_secret_encrypt,
    seed.scopes,
    seed.issuer,
    seed.authorization_url,
    seed.jwks_uri,
    seed.token_url,
    seed.userinfo_url,
    seed.discovery_url,
    seed.end_session_endpoint,
    seed.callback_url,
    seed.pkce_supported,
    seed.is_active,
    'SYSTEM'
FROM (
    VALUES
        (
            'GOOGLE',
            '{"en":"Google","th":"Google"}'::jsonb,
            'OIDC',
            NULL,
            NULL,
            ARRAY['openid', 'profile', 'email']::text[],
            'https://accounts.google.com',
            'https://accounts.google.com/o/oauth2/v2/auth',
            'https://www.googleapis.com/oauth2/v3/certs',
            'https://oauth2.googleapis.com/token',
            'https://openidconnect.googleapis.com/v1/userinfo',
            'https://accounts.google.com/.well-known/openid-configuration',
            'https://accounts.google.com/logout',
            'http://localhost:5003/api/auth/external/callback',
            TRUE,
            FALSE,
            '{"en":"Pre-registered Google OIDC metadata. Keep inactive until a real client_id and client_secret are configured.","th":"Metadata ของ Google OIDC สำหรับตั้งต้น ปล่อย inactive ไว้จนกว่าจะตั้ง client_id และ client_secret จริง"}'::jsonb
        ),
        (
            'FACEBOOK',
            '{"en":"Facebook","th":"Facebook"}'::jsonb,
            'OAUTH2',
            NULL,
            NULL,
            ARRAY['public_profile', 'email']::text[],
            'https://www.facebook.com',
            'https://www.facebook.com/v20.0/dialog/oauth',
            NULL,
            'https://graph.facebook.com/v20.0/oauth/access_token',
            'https://graph.facebook.com/me?fields=id,name,email,picture',
            NULL,
            NULL,
            'http://localhost:5003/api/auth/external/callback',
            TRUE,
            FALSE,
            '{"en":"Pre-registered Facebook OAuth metadata. Keep inactive until app credentials are configured.","th":"Metadata ของ Facebook OAuth สำหรับตั้งต้น ปล่อย inactive ไว้จนกว่าจะตั้งค่า app credentials จริง"}'::jsonb
        ),
        (
            'APPLE',
            '{"en":"Apple","th":"Apple"}'::jsonb,
            'OIDC',
            NULL,
            NULL,
            ARRAY['openid', 'email', 'name']::text[],
            'https://appleid.apple.com',
            'https://appleid.apple.com/auth/authorize',
            'https://appleid.apple.com/auth/keys',
            'https://appleid.apple.com/auth/token',
            'https://appleid.apple.com/auth/userinfo',
            'https://appleid.apple.com/.well-known/openid-configuration',
            NULL,
            'http://localhost:5003/api/auth/external/callback',
            TRUE,
            FALSE,
            '{"en":"Pre-registered Apple Sign In metadata. Keep inactive until service credentials are configured.","th":"Metadata ของ Apple Sign In สำหรับตั้งต้น ปล่อย inactive ไว้จนกว่าจะตั้งค่า credentials จริง"}'::jsonb
        )
) AS seed(
    provider_code,
    provider_name,
    protocol,
    client_id,
    client_secret_encrypt,
    scopes,
    issuer,
    authorization_url,
    jwks_uri,
    token_url,
    userinfo_url,
    discovery_url,
    end_session_endpoint,
    callback_url,
    pkce_supported,
    is_active,
    description
)
LEFT JOIN public.mt_providers existing
    ON LOWER(existing.provider_code) = LOWER(seed.provider_code)
   AND existing.deleted_at IS NULL
WHERE existing.id IS NULL;

INSERT INTO public.mt_scopes (
    scope_name,
    display_name,
    scope_type,
    claims,
    is_system_scope,
    is_active,
    created_by
)
SELECT
    seed.scope_name,
    seed.display_name,
    seed.scope_type,
    seed.claims,
    seed.is_system_scope,
    seed.is_active,
    'SYSTEM'
FROM (
    VALUES
        ('openid', 'OpenID Connect subject access', 'OPENID', ARRAY['sub']::text[], TRUE, TRUE, '{"en":"Required baseline scope for OpenID Connect.","th":"Baseline scope ที่จำเป็นสำหรับ OpenID Connect"}'::jsonb),
        ('profile', 'Basic profile information', 'PROFILE', ARRAY['name', 'preferred_username', 'locale', 'zoneinfo']::text[], TRUE, TRUE, '{"en":"Profile claims used by user-facing clients.","th":"Claims โปรไฟล์พื้นฐานสำหรับ client ฝั่งผู้ใช้"}'::jsonb),
        ('email', 'Email address information', 'EMAIL', ARRAY['email', 'email_verified']::text[], TRUE, TRUE, '{"en":"Email claims for sign-in and contact flows.","th":"Claims อีเมลสำหรับการเข้าสู่ระบบและการติดต่อ"}'::jsonb),
        ('phone', 'Phone number information', 'PHONE', ARRAY['phone_number', 'phone_number_verified']::text[], TRUE, TRUE, '{"en":"Phone claims when phone login or verification is enabled.","th":"Claims เบอร์โทรเมื่อเปิดใช้การเข้าสู่ระบบหรือยืนยันเบอร์"}'::jsonb),
        ('address', 'Address information', 'ADDRESS', ARRAY['address']::text[], TRUE, TRUE, '{"en":"Address claim bundle for profile enrichment.","th":"ชุด claim ที่อยู่สำหรับขยายข้อมูลโปรไฟล์"}'::jsonb),
        ('offline_access', 'Refresh token access', 'OFFLINE_ACCESS', ARRAY[]::text[], TRUE, TRUE, '{"en":"Allows refresh token issuance for long-lived sessions.","th":"อนุญาตให้ออก refresh token สำหรับ session ระยะยาว"}'::jsonb),
        ('tenant', 'Tenant context claims', 'CUSTOM', ARRAY['tenant_id', 'tenant_code']::text[], FALSE, TRUE, '{"en":"Adds tenant context to tokens for multi-tenant APIs.","th":"เพิ่ม tenant context ลงใน token สำหรับ multi-tenant APIs"}'::jsonb),
        ('roles', 'Role claims', 'CUSTOM', ARRAY['role']::text[], FALSE, TRUE, '{"en":"Exposes role membership claims to client applications.","th":"เปิดเผย role membership ให้ client applications"}'::jsonb),
        ('permissions', 'Permission claims', 'CUSTOM', ARRAY['permissions']::text[], FALSE, TRUE, '{"en":"Exposes fine-grained permission claims to client applications.","th":"เปิดเผย permission แบบละเอียดให้ client applications"}'::jsonb),
        ('api.read', 'Read access to protected APIs', 'CUSTOM', ARRAY['resource_access']::text[], FALSE, TRUE, '{"en":"Used by resource APIs for read-only access.","th":"ใช้กับ resource APIs สำหรับสิทธิ์อ่านอย่างเดียว"}'::jsonb),
        ('api.write', 'Write access to protected APIs', 'CUSTOM', ARRAY['resource_access']::text[], FALSE, TRUE, '{"en":"Used by resource APIs for write operations.","th":"ใช้กับ resource APIs สำหรับสิทธิ์เขียนหรือแก้ไขข้อมูล"}'::jsonb)
) AS seed(scope_name, display_name, scope_type, claims, is_system_scope, is_active, description)
LEFT JOIN public.mt_scopes existing
    ON existing.scope_name = seed.scope_name
   AND existing.deleted_at IS NULL
WHERE existing.id IS NULL;

INSERT INTO public.mt_authorization_clients (
    tenant_id,
    client_id,
    client_secret_hash,
    client_name,
    client_type,
    token_endpoint_auth_method,
    require_pkce,
    pkce_code_challenge_method,
    require_consent,
    redirect_uris,
    post_logout_redirect_uris,
    allowed_grant_types,
    allowed_response_types,
    access_token_lifetime,
    refresh_token_lifetime,
    logo_uri,
    client_uri,
    jwks_uri,
    jwks,
    client_secret_expires_at,
    is_active,
    created_by
)
SELECT
    tenant.id,
    'monkora-demo-spa',
    NULL,
    'Monkora Demo SPA',
    'PUBLIC',
    'CLIENT_SECRET_BASIC',
    TRUE,
    'S256',
    TRUE,
    ARRAY[
        'http://localhost:3000/auth/callback',
        'http://localhost:5173/auth/callback'
    ]::text[],
    ARRAY[
        'http://localhost:3000/',
        'http://localhost:5173/'
    ]::text[],
    ARRAY['AUTHORIZATION_CODE', 'REFRESH_TOKEN']::text[],
    ARRAY['code']::text[],
    3600,
    2592000,
    NULL,
    'http://localhost:3000',
    NULL,
    NULL,
    NULL,
    TRUE,
    'SYSTEM'
FROM public.mt_tenants tenant
WHERE tenant.tenant_code = 'MONKORA_DEMO'
  AND tenant.deleted_at IS NULL
  AND NOT EXISTS (
      SELECT 1
      FROM public.mt_authorization_clients existing
      WHERE existing.client_id = 'monkora-demo-spa'
        AND existing.deleted_at IS NULL
  );

INSERT INTO public.lnk_authorization_client_scopes (
    client_id,
    scope_id,
    is_default,
    is_required,
    created_by
)
SELECT
    client.id,
    scope.id,
    seed.is_default,
    seed.is_required,
    'SYSTEM'
FROM (
    VALUES
        ('tenant', TRUE, TRUE, '{"en":"Always include tenant context in demo client tokens.","th":"ใส่ tenant context ใน token ของ demo client เสมอ"}'::jsonb),
        ('roles', TRUE, FALSE, '{"en":"Expose role claims by default for application authorization.","th":"เปิด role claims เป็นค่าเริ่มต้นสำหรับ authorization ฝั่งแอป"}'::jsonb),
        ('permissions', FALSE, FALSE, '{"en":"Optional fine-grained permission claims for advanced clients.","th":"Permission claims แบบละเอียดสำหรับ client ที่ต้องการสิทธิ์ขั้นสูง"}'::jsonb),
        ('api.read', TRUE, FALSE, '{"en":"Default read scope for protected resource APIs.","th":"Scope อ่านข้อมูลเริ่มต้นสำหรับ protected resource APIs"}'::jsonb),
        ('api.write', FALSE, FALSE, '{"en":"Optional write scope that should be explicitly requested.","th":"Scope เขียนข้อมูลที่ควรถูก request แบบ explicit"}'::jsonb)
    ) AS seed(scope_name, is_default, is_required, description)
JOIN public.mt_authorization_clients client
    ON client.client_id = 'monkora-demo-spa'
   AND client.deleted_at IS NULL
JOIN public.mt_scopes scope
    ON scope.scope_name = seed.scope_name
   AND scope.deleted_at IS NULL
LEFT JOIN public.lnk_authorization_client_scopes existing
    ON existing.client_id = client.id
   AND existing.scope_id = scope.id
WHERE existing.id IS NULL;

INSERT INTO public.mt_permissions (
    tenant_id,
    permission_code,
    permission_name,
    resource,
    action,
    is_active,
    created_by
)
SELECT
    NULL,
    seed.permission_code,
    seed.permission_name,
    seed.resource,
    seed.action,
    TRUE,
    'SYSTEM'
FROM (
    VALUES
        ('users.read', '{"en":"View users","th":"ดูข้อมูลผู้ใช้"}'::jsonb, 'users', 'READ', '{"en":"Read user profiles and account state.","th":"อ่านข้อมูลโปรไฟล์และสถานะบัญชีผู้ใช้"}'::jsonb),
        ('users.write', '{"en":"Manage users","th":"จัดการผู้ใช้"}'::jsonb, 'users', 'WRITE', '{"en":"Create, update, suspend, or unlock user accounts.","th":"สร้าง แก้ไข ระงับ หรือปลดล็อกบัญชีผู้ใช้"}'::jsonb),
        ('roles.read', '{"en":"View roles","th":"ดูข้อมูล role"}'::jsonb, 'roles', 'READ', '{"en":"Read role definitions and assignments.","th":"อ่านข้อมูล role และการผูก role"}'::jsonb),
        ('roles.write', '{"en":"Manage roles","th":"จัดการ role"}'::jsonb, 'roles', 'WRITE', '{"en":"Create and update role definitions.","th":"สร้างและแก้ไขข้อมูล role"}'::jsonb),
        ('permissions.read', '{"en":"View permissions","th":"ดูข้อมูล permission"}'::jsonb, 'permissions', 'READ', '{"en":"Read permission catalogue and mappings.","th":"อ่านรายการ permission และการผูกสิทธิ์"}'::jsonb),
        ('clients.read', '{"en":"View clients","th":"ดูข้อมูล client"}'::jsonb, 'clients', 'READ', '{"en":"Read OAuth client configuration.","th":"อ่านการตั้งค่า OAuth client"}'::jsonb),
        ('clients.write', '{"en":"Manage clients","th":"จัดการ client"}'::jsonb, 'clients', 'WRITE', '{"en":"Create or update OAuth clients and redirect URIs.","th":"สร้างหรือแก้ไข OAuth client และ redirect URIs"}'::jsonb),
        ('scopes.read', '{"en":"View scopes","th":"ดูข้อมูล scope"}'::jsonb, 'scopes', 'READ', '{"en":"Read available OAuth and custom scopes.","th":"อ่านรายการ OAuth scope และ custom scope"}'::jsonb),
        ('providers.read', '{"en":"View providers","th":"ดูข้อมูล provider"}'::jsonb, 'providers', 'READ', '{"en":"Read external identity provider configuration.","th":"อ่านการตั้งค่า external identity provider"}'::jsonb),
        ('providers.write', '{"en":"Manage providers","th":"จัดการ provider"}'::jsonb, 'providers', 'WRITE', '{"en":"Create or update external identity provider settings.","th":"สร้างหรือแก้ไขการตั้งค่า external identity provider"}'::jsonb),
        ('agreements.read', '{"en":"View agreements","th":"ดูข้อมูลข้อตกลง"}'::jsonb, 'agreements', 'READ', '{"en":"Read platform agreements and consent documents.","th":"อ่านเอกสารข้อตกลงและความยินยอมของระบบ"}'::jsonb),
        ('agreements.write', '{"en":"Manage agreements","th":"จัดการข้อตกลง"}'::jsonb, 'agreements', 'WRITE', '{"en":"Create or publish agreement versions.","th":"สร้างหรือเผยแพร่เวอร์ชันของเอกสารข้อตกลง"}'::jsonb),
        ('consents.read', '{"en":"View consents","th":"ดูข้อมูล consent"}'::jsonb, 'consents', 'READ', '{"en":"Read user consent history and active grants.","th":"อ่านประวัติ consent และสิทธิ์ที่ยัง active"}'::jsonb),
        ('consents.write', '{"en":"Manage consents","th":"จัดการ consent"}'::jsonb, 'consents', 'WRITE', '{"en":"Revoke or manage consent records when required.","th":"เพิกถอนหรือจัดการ consent records เมื่อจำเป็น"}'::jsonb),
        ('audit.read', '{"en":"View audit logs","th":"ดูข้อมูล audit log"}'::jsonb, 'audit_logs', 'READ', '{"en":"Read security and operational audit trails.","th":"อ่าน audit trail ด้านความปลอดภัยและการปฏิบัติการ"}'::jsonb)
) AS seed(permission_code, permission_name, resource, action, description)
LEFT JOIN public.mt_permissions existing
    ON existing.permission_code = seed.permission_code
   AND existing.tenant_id IS NULL
   AND existing.deleted_at IS NULL
WHERE existing.id IS NULL;

INSERT INTO public.mt_roles (
    tenant_id,
    role_code,
    role_name,
    parent_role_id,
    is_active,
    created_by
)
SELECT
    seed.tenant_id,
    seed.role_code,
    seed.role_name,
    NULL,
    TRUE,
    'SYSTEM'
FROM (
    SELECT
        NULL::uuid AS tenant_id,
        'PLATFORM_ADMIN' AS role_code,
        '{"en":"Platform Administrator","th":"ผู้ดูแลระบบส่วนกลาง"}'::jsonb AS role_name,
        '{"en":"Full platform-level access across auth administration, providers, clients, and agreements.","th":"สิทธิ์เต็มระดับ platform สำหรับการดูแล auth, providers, clients และ agreements"}'::jsonb AS description
    UNION ALL
    SELECT
        NULL::uuid,
        'SECURITY_AUDITOR',
        '{"en":"Security Auditor","th":"ผู้ตรวจสอบความปลอดภัย"}'::jsonb,
        '{"en":"Read-only oversight role for audits, consent history, and configuration review.","th":"Role แบบอ่านอย่างเดียวสำหรับตรวจสอบ audit, consent history และการตั้งค่า"}'::jsonb
    UNION ALL
    SELECT
        tenant.id,
        'TENANT_ADMIN',
        '{"en":"Tenant Administrator","th":"ผู้ดูแล tenant"}'::jsonb,
        '{"en":"Tenant-scoped administrative role for daily operations in the demo tenant.","th":"Role ผู้ดูแลระดับ tenant สำหรับงานประจำวันใน demo tenant"}'::jsonb
    FROM public.mt_tenants tenant
    WHERE tenant.tenant_code = 'MONKORA_DEMO'
      AND tenant.deleted_at IS NULL
) AS seed
LEFT JOIN public.mt_roles existing
    ON existing.role_code = seed.role_code
   AND ((existing.tenant_id IS NULL AND seed.tenant_id IS NULL) OR existing.tenant_id = seed.tenant_id)
   AND existing.deleted_at IS NULL
WHERE existing.id IS NULL;

INSERT INTO public.lnk_role_permissions (
    role_id,
    permission_id,
    created_by
)
SELECT
    role_map.role_id,
    role_map.permission_id,
    'SYSTEM'
FROM (
    SELECT
        role_entity.id AS role_id,
        permission_entity.id AS permission_id
    FROM (
        VALUES
            ('PLATFORM_ADMIN', NULL, 'users.read', '{"en":"Platform admins can view users.","th":"Platform admin สามารถดูข้อมูลผู้ใช้ได้"}'::jsonb),
            ('PLATFORM_ADMIN', NULL, 'users.write', '{"en":"Platform admins can manage users.","th":"Platform admin สามารถจัดการผู้ใช้ได้"}'::jsonb),
            ('PLATFORM_ADMIN', NULL, 'roles.read', '{"en":"Platform admins can view roles.","th":"Platform admin สามารถดู role ได้"}'::jsonb),
            ('PLATFORM_ADMIN', NULL, 'roles.write', '{"en":"Platform admins can manage roles.","th":"Platform admin สามารถจัดการ role ได้"}'::jsonb),
            ('PLATFORM_ADMIN', NULL, 'permissions.read', '{"en":"Platform admins can view permissions.","th":"Platform admin สามารถดู permission ได้"}'::jsonb),
            ('PLATFORM_ADMIN', NULL, 'clients.read', '{"en":"Platform admins can view clients.","th":"Platform admin สามารถดู client ได้"}'::jsonb),
            ('PLATFORM_ADMIN', NULL, 'clients.write', '{"en":"Platform admins can manage clients.","th":"Platform admin สามารถจัดการ client ได้"}'::jsonb),
            ('PLATFORM_ADMIN', NULL, 'scopes.read', '{"en":"Platform admins can view scopes.","th":"Platform admin สามารถดู scope ได้"}'::jsonb),
            ('PLATFORM_ADMIN', NULL, 'providers.read', '{"en":"Platform admins can view providers.","th":"Platform admin สามารถดู provider ได้"}'::jsonb),
            ('PLATFORM_ADMIN', NULL, 'providers.write', '{"en":"Platform admins can manage providers.","th":"Platform admin สามารถจัดการ provider ได้"}'::jsonb),
            ('PLATFORM_ADMIN', NULL, 'agreements.read', '{"en":"Platform admins can view agreements.","th":"Platform admin สามารถดู agreement ได้"}'::jsonb),
            ('PLATFORM_ADMIN', NULL, 'agreements.write', '{"en":"Platform admins can manage agreements.","th":"Platform admin สามารถจัดการ agreement ได้"}'::jsonb),
            ('PLATFORM_ADMIN', NULL, 'consents.read', '{"en":"Platform admins can review consent history.","th":"Platform admin สามารถตรวจสอบประวัติ consent ได้"}'::jsonb),
            ('PLATFORM_ADMIN', NULL, 'consents.write', '{"en":"Platform admins can revoke or repair consent records.","th":"Platform admin สามารถเพิกถอนหรือแก้ไข consent records ได้"}'::jsonb),
            ('PLATFORM_ADMIN', NULL, 'audit.read', '{"en":"Platform admins can review audit trails.","th":"Platform admin สามารถดู audit trail ได้"}'::jsonb),
            ('SECURITY_AUDITOR', NULL, 'users.read', '{"en":"Security auditors can view users.","th":"Security auditor สามารถดูข้อมูลผู้ใช้ได้"}'::jsonb),
            ('SECURITY_AUDITOR', NULL, 'roles.read', '{"en":"Security auditors can view roles.","th":"Security auditor สามารถดู role ได้"}'::jsonb),
            ('SECURITY_AUDITOR', NULL, 'permissions.read', '{"en":"Security auditors can view permissions.","th":"Security auditor สามารถดู permission ได้"}'::jsonb),
            ('SECURITY_AUDITOR', NULL, 'clients.read', '{"en":"Security auditors can view clients.","th":"Security auditor สามารถดู client ได้"}'::jsonb),
            ('SECURITY_AUDITOR', NULL, 'scopes.read', '{"en":"Security auditors can view scopes.","th":"Security auditor สามารถดู scope ได้"}'::jsonb),
            ('SECURITY_AUDITOR', NULL, 'providers.read', '{"en":"Security auditors can view providers.","th":"Security auditor สามารถดู provider ได้"}'::jsonb),
            ('SECURITY_AUDITOR', NULL, 'agreements.read', '{"en":"Security auditors can view agreements.","th":"Security auditor สามารถดู agreement ได้"}'::jsonb),
            ('SECURITY_AUDITOR', NULL, 'consents.read', '{"en":"Security auditors can review consent history.","th":"Security auditor สามารถตรวจสอบประวัติ consent ได้"}'::jsonb),
            ('SECURITY_AUDITOR', NULL, 'audit.read', '{"en":"Security auditors can review audit trails.","th":"Security auditor สามารถดู audit trail ได้"}'::jsonb),
            ('TENANT_ADMIN', 'MONKORA_DEMO', 'users.read', '{"en":"Tenant admins can view users in their tenant.","th":"Tenant admin สามารถดูผู้ใช้ใน tenant ของตัวเองได้"}'::jsonb),
            ('TENANT_ADMIN', 'MONKORA_DEMO', 'users.write', '{"en":"Tenant admins can manage users in their tenant.","th":"Tenant admin สามารถจัดการผู้ใช้ใน tenant ของตัวเองได้"}'::jsonb),
            ('TENANT_ADMIN', 'MONKORA_DEMO', 'roles.read', '{"en":"Tenant admins can view tenant role assignments.","th":"Tenant admin สามารถดูการกำหนด role ของ tenant ได้"}'::jsonb),
            ('TENANT_ADMIN', 'MONKORA_DEMO', 'roles.write', '{"en":"Tenant admins can manage tenant role assignments.","th":"Tenant admin สามารถจัดการการกำหนด role ของ tenant ได้"}'::jsonb),
            ('TENANT_ADMIN', 'MONKORA_DEMO', 'permissions.read', '{"en":"Tenant admins can view available permissions.","th":"Tenant admin สามารถดูรายการ permission ที่ใช้งานได้"}'::jsonb),
            ('TENANT_ADMIN', 'MONKORA_DEMO', 'clients.read', '{"en":"Tenant admins can view tenant clients.","th":"Tenant admin สามารถดู client ของ tenant ได้"}'::jsonb),
            ('TENANT_ADMIN', 'MONKORA_DEMO', 'clients.write', '{"en":"Tenant admins can manage tenant clients.","th":"Tenant admin สามารถจัดการ client ของ tenant ได้"}'::jsonb),
            ('TENANT_ADMIN', 'MONKORA_DEMO', 'scopes.read', '{"en":"Tenant admins can view available scopes.","th":"Tenant admin สามารถดู scope ที่ใช้งานได้"}'::jsonb),
            ('TENANT_ADMIN', 'MONKORA_DEMO', 'agreements.read', '{"en":"Tenant admins can view agreement versions.","th":"Tenant admin สามารถดูเวอร์ชันของ agreement ได้"}'::jsonb),
            ('TENANT_ADMIN', 'MONKORA_DEMO', 'agreements.write', '{"en":"Tenant admins can publish tenant agreement versions.","th":"Tenant admin สามารถเผยแพร่เวอร์ชันของ agreement ได้"}'::jsonb),
            ('TENANT_ADMIN', 'MONKORA_DEMO', 'consents.read', '{"en":"Tenant admins can review consent history.","th":"Tenant admin สามารถตรวจสอบประวัติ consent ได้"}'::jsonb),
            ('TENANT_ADMIN', 'MONKORA_DEMO', 'consents.write', '{"en":"Tenant admins can manage consent records when needed.","th":"Tenant admin สามารถจัดการ consent records เมื่อจำเป็นได้"}'::jsonb)
    ) AS seed(role_code, tenant_code, permission_code, description)
    JOIN public.mt_permissions permission_entity
        ON permission_entity.permission_code = seed.permission_code
       AND permission_entity.tenant_id IS NULL
       AND permission_entity.deleted_at IS NULL
    LEFT JOIN public.mt_tenants tenant_entity
        ON tenant_entity.tenant_code = seed.tenant_code
       AND tenant_entity.deleted_at IS NULL
    JOIN public.mt_roles role_entity
        ON role_entity.role_code = seed.role_code
       AND role_entity.deleted_at IS NULL
       AND ((seed.tenant_code IS NULL AND role_entity.tenant_id IS NULL) OR role_entity.tenant_id = tenant_entity.id)
) AS role_map
LEFT JOIN public.lnk_role_permissions existing
    ON existing.role_id = role_map.role_id
   AND existing.permission_id = role_map.permission_id
WHERE existing.id IS NULL;

INSERT INTO public.mt_agreements (
    tenant_id,
    agreement_code,
    agreement_type,
    title,
    content,
    summary,
    version,
    effective_at,
    expires_at,
    is_required,
    requires_explicit_action,
    is_active,
    created_by
)
SELECT
    NULL,
    seed.agreement_code,
    seed.agreement_type,
    seed.title,
    seed.content,
    seed.summary,
    seed.version,
    seed.effective_at,
    NULL,
    seed.is_required,
    seed.requires_explicit_action,
    TRUE,
    'SYSTEM'
FROM (
    VALUES
        (
            'TERMS_OF_SERVICE_v1',
            'TERMS_OF_SERVICE',
            '{"en":"Terms of Service","th":"ข้อกำหนดการใช้งาน"}'::jsonb,
            '{"en":"By using MonkoraEdge services you agree to follow platform usage rules, protect credentials, and comply with applicable law.","th":"เมื่อใช้บริการ MonkoraEdge ถือว่าคุณยอมรับกติกาการใช้งานของแพลตฟอร์ม ปกป้องข้อมูลเข้าสู่ระบบ และปฏิบัติตามกฎหมายที่เกี่ยวข้อง"}'::jsonb,
            '{"en":"Platform usage terms for all end users.","th":"ข้อกำหนดการใช้งานของแพลตฟอร์มสำหรับผู้ใช้ทุกคน"}'::jsonb,
            '1.0.0',
            TIMESTAMPTZ '2026-01-01 00:00:00+00',
            TRUE,
            TRUE,
            '{"en":"Required legal terms for platform access.","th":"ข้อกำหนดทางกฎหมายที่ต้องยอมรับก่อนใช้งานแพลตฟอร์ม"}'::jsonb
        ),
        (
            'PRIVACY_POLICY_v1',
            'PRIVACY_POLICY',
            '{"en":"Privacy Policy","th":"นโยบายความเป็นส่วนตัว"}'::jsonb,
            '{"en":"MonkoraEdge collects only the data required to authenticate users, secure sessions, and meet legal obligations.","th":"MonkoraEdge จะเก็บเฉพาะข้อมูลที่จำเป็นต่อการยืนยันตัวตน ป้องกันความปลอดภัยของ session และปฏิบัติตามข้อกฎหมาย"}'::jsonb,
            '{"en":"Explains how identity and security data is collected and used.","th":"อธิบายวิธีการเก็บและใช้ข้อมูลด้านตัวตนและความปลอดภัย"}'::jsonb,
            '1.0.0',
            TIMESTAMPTZ '2026-01-01 00:00:00+00',
            TRUE,
            TRUE,
            '{"en":"Required privacy notice for all sign-in journeys.","th":"ประกาศความเป็นส่วนตัวที่ต้องยอมรับในทุกเส้นทางการเข้าสู่ระบบ"}'::jsonb
        ),
        (
            'PDPA_CONSENT_v1',
            'PDPA_CONSENT',
            '{"en":"PDPA Consent","th":"ความยินยอมตาม PDPA"}'::jsonb,
            '{"en":"You explicitly consent to the collection, use, and disclosure of personal data for authentication, fraud prevention, and account recovery.","th":"คุณยินยอมโดยชัดแจ้งให้เก็บ ใช้ และเปิดเผยข้อมูลส่วนบุคคลเพื่อการยืนยันตัวตน ป้องกันการทุจริต และกู้คืนบัญชี"}'::jsonb,
            '{"en":"Explicit consent for processing personal data in identity flows.","th":"ความยินยอมแบบ explicit สำหรับการประมวลผลข้อมูลส่วนบุคคลในขั้นตอนยืนยันตัวตน"}'::jsonb,
            '1.0.0',
            TIMESTAMPTZ '2026-01-01 00:00:00+00',
            TRUE,
            TRUE,
            '{"en":"PDPA-specific consent document for identity operations.","th":"เอกสารความยินยอมตาม PDPA สำหรับการดำเนินงานด้านตัวตน"}'::jsonb
        ),
        (
            'MARKETING_CONSENT_v1',
            'MARKETING_CONSENT',
            '{"en":"Marketing Consent","th":"ความยินยอมรับการตลาด"}'::jsonb,
            '{"en":"You may optionally opt in to receive product updates, release notes, and service announcements that are not strictly transactional.","th":"คุณสามารถเลือกยินยอมเพื่อรับข้อมูลอัปเดตผลิตภัณฑ์ release notes และประกาศบริการที่ไม่ใช่ข้อความเชิงธุรกรรมได้"}'::jsonb,
            '{"en":"Optional consent for product and campaign communications.","th":"ความยินยอมแบบไม่บังคับสำหรับการสื่อสารด้านผลิตภัณฑ์และแคมเปญ"}'::jsonb,
            '1.0.0',
            TIMESTAMPTZ '2026-01-01 00:00:00+00',
            FALSE,
            TRUE,
            '{"en":"Optional consent kept separate from mandatory legal agreements.","th":"ความยินยอมแบบไม่บังคับที่แยกจากข้อตกลงทางกฎหมายที่จำเป็น"}'::jsonb
        )
) AS seed(
    agreement_code,
    agreement_type,
    title,
    content,
    summary,
    version,
    effective_at,
    is_required,
    requires_explicit_action,
    description
)
LEFT JOIN public.mt_agreements existing
    ON existing.agreement_code = seed.agreement_code
   AND existing.deleted_at IS NULL
WHERE existing.id IS NULL;

COMMIT;