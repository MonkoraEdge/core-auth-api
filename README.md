# MonkoraEdge.Core.Auth API

**Core Authentication API** สำหรับระบบ MonkoraEdge Platform

- **Company:** Monkora Co., Ltd.
- **Author:** Boonhome Wongsuwan
- **Version:** 1.0.0
- **Framework:** .NET 9.0
- **Architecture:** Clean Architecture (API / Domain / Infrastructure) + Shared `DotNet` Library
- **Current Focus:** OAuth2.1 Authorization Server + Authentication Platform

---

## Current Status

สถานะปัจจุบันของโค้ดใน repository นี้ ณ เวอร์ชันล่าสุด:

- OAuth2 authorization flow ถูก refactor ให้ controller บางลง และย้าย orchestration เข้า Domain Service
- มี endpoint metadata ครบทั้ง
  - `/.well-known/openid-configuration`
  - `/.well-known/oauth-authorization-server`
  - `/.well-known/jwks.json`
- Authorization Code Flow บังคับ `PKCE` แบบ `S256`
- Token endpoint orchestration อยู่ใน Domain (`ProcessTokenRequestAsync`)
- Refresh token rotation และ reuse detection ถูก implement ใน Domain service
- Domain เป็นเจ้าของทั้ง `IUnitOfWork` และ `IRepository<TEntity>` abstraction สำหรับการใช้งานของตัวเองแล้ว
- Domain โยน `DomainException` ของตัวเอง และให้ API translate เป็น HTTP response
- README นี้ต้องถือเป็น `source of truth` สำหรับภาพรวมของระบบและ workflow การแก้ไข

---

## Maintenance Workflow

กติกาการทำงานของ repository นี้:

1. ก่อนเริ่มแก้ไขโค้ดใด ๆ ให้ตรวจ README นี้ก่อนเสมอ
2. หากมีการเปลี่ยนสถาปัตยกรรม, endpoint, flow, dependency สำคัญ, security behavior หรือ workflow ต้องอัปเดต README นี้ในงานเดียวกันเสมอ
3. ห้ามถือว่า README ถูกต้องโดยอัตโนมัติหลังแก้โค้ด ต้อง sync ให้ตรงกับ code version ปัจจุบันทุกครั้ง

---

## โครงสร้างโปรเจค (Project Structure)

```
core-auth-api/
├── README.md
├── MonkoraEdge.Core.Auth.sln
├── src/
│   ├── API/                        # Presentation Layer (ASP.NET Core Web API)
│   ├── Domain/                     # Domain Logic, Use Cases, OAuth orchestration
│   ├── Infrastructure/             # Persistence, repositories, hosted services, EF Core
│   └── DotNet/                     # Shared building blocks / cross-cutting library
```

---

## Current OAuth2.1 Design

### API Layer

- รับ request / bind model / extract header / return HTTP result เท่านั้น
- `OAuth2Controller` ไม่ควรมี business rule สำคัญ
- legacy `/auth/refresh` เป็น compatibility facade ที่ map request body แล้วส่งต่อเข้า OAuth2 token flow ภายใน
- `WellKnownController` ใช้ Domain service เพื่อประกอบ metadata response
- `DomainExceptionHandlingMiddleware` รับผิดชอบ map Domain exception เป็น response schema เดิม

### Domain Layer

- `OAuth2Service` เป็น orchestration point หลักของ OAuth flow
- `RefreshTokenProcessor` เป็นจุดกลางสำหรับ validation, reuse detection และ rotation ของ refresh token
- Domain service ใช้ `IUnitOfWork` abstraction ของตัวเองแล้ว และให้ Infrastructure map implementation เข้ามาใน DI
- aggregate repository interfaces ใช้ `Domain.Repositories.IRepository<TEntity>` แทน shared repository contract ตรง ๆ
- Domain exception แยกจาก DotNet HTTP exception แล้ว
- methods สำคัญปัจจุบัน:
  - `ProcessAuthorizeRequestAsync`
  - `ProcessConsentAsync`
  - `ProcessTokenRequestAsync`
  - `ExchangeAuthorizationCodeAsync`
  - `RefreshTokenGrantAsync`
- endpoint-facing result models อยู่ใน `src/Domain/AggregatesModel/OAuth2Aggregate`

### Infrastructure Layer

- EF Core + PostgreSQL
- crypto/security implementation อยู่ใน Infrastructure
  - `TokenService` สำหรับ JWT/JWKS/signing
  - `PasswordService` สำหรับ BCrypt, PKCE, TOTP และ secure token generation
- `AuthRepositoryBase<TEntity>` ทำหน้าที่ bridge Domain repository contract ไปยัง EF repository base เดิม
- Repository implementations และ token cleanup background service
- project nullable context ถูกเปิดแล้วเพื่อลด warning กลุ่ม `CS8632`
- `AuthenticationDbContext` เปิดใช้ global soft-delete query filter และ apply snake_case naming convention สำหรับ PostgreSQL model metadata
- `AuthenticationContextDesignFactory` ใน Infrastructure สามารถ resolve connection string จาก `src/API/appsettings*.json` หรือ environment variables เพื่อให้ `dotnet ef` รันตรงจาก `src/Infrastructure` ได้
- EF model มี shared convention สำหรับ `Description -> jsonb` และ `IpAddress -> inet` เพื่อให้ตรงกับ DDL ใน `src/API/script_sql`
- `AuthenticationDbContext.ConfigureConventions(...)` register JSON value conversion สำหรับ `Locale` และ `TenantSettings` ทำให้ EF treat ค่าเหล่านี้เป็น scalar `jsonb` property ตั้งแต่ขั้น model discovery และสร้าง migration จาก `src/Infrastructure` ได้ตรง
- middleware `ErrorHandlingMiddleware` ถูกเรียกผ่าน DI แบบ `IMiddleware`; extension `UseErrorHandling(...)` จะไม่ส่ง explicit constructor arguments เข้าระบบ pipeline เพื่อหลีกเลี่ยง startup error ของ ASP.NET Core
- tables หลักสำหรับ OAuth:
  - authorization codes
  - access tokens
  - refresh tokens
  - revoked tokens
  - authorization consents

### Security Posture

- PKCE `S256` required
- redirect URI exact match
- confidential client ต้องมี `client_secret`
- refresh token rotation รองรับ family-based revocation
- legacy `/auth/refresh` path delegates internally to the OAuth2 `refresh_token` flow
- `/oauth2/userinfo` returns claims only for scopes granted to the access token
- JWKS endpoint สำหรับ public key discovery

---

## New API Template

- ใช้ [API_TEMPLATE.md](API_TEMPLATE.md) เป็น template สำหรับตั้งต้น API ใหม่ โดยอิง dependency direction, onboarding flow, config checklist, run/migration commands, observability, testing, CI/CD, และ release checklist จาก repository นี้

---

## Bootstrap SQL Scripts

- โฟลเดอร์ `src/API/script_sql` เก็บทั้ง DDL และ bootstrap SQL สำหรับฐานข้อมูลของระบบ
- เพิ่มไฟล์ `#33 seed_master_data.sql` สำหรับ seed master/reference data แบบรันซ้ำได้ (idempotent)
- ฝั่ง `src/Infrastructure/Migrations/SeedMasterDataFromScriptSql.cs` ถูกใช้เป็น support code สำหรับ seed SQL และ migration จริงควรถูก scaffold ผ่าน EF เพื่อให้ timestamp ถูก generate อัตโนมัติ
- helper `src/Infrastructure/Migrations/MigrationSqlScriptLoader.cs` ใช้รวม logic โหลด embedded SQL และ strip transaction statement เพื่อให้ seed migration ตัวถัดไป reuse ได้
- ลำดับ migration สำหรับฐานข้อมูลใหม่คือ `InitialCreate` → `SeedMasterDataFromScriptSql`
- ไฟล์นี้ครอบคลุมตารางหลักต่อไปนี้:
  - `mt_tenants`
  - `mt_providers`
  - `mt_scopes`
  - `mt_authorization_clients`
  - `lnk_authorization_client_scopes`
  - `mt_roles`
  - `mt_permissions`
  - `lnk_role_permissions`
  - `mt_agreements`
- seed นี้จงใจไม่สร้างข้อมูลใน `mt_users`, `mt_user_identities`, และ `mt_api_keys` เพราะต้องใช้ credential/secret เฉพาะ environment
- development client ที่ถูก seed คือ `monkora-demo-spa` แบบ `PUBLIC` พร้อม `Authorization Code + PKCE` และ redirect URIs สำหรับ `localhost:3000` กับ `localhost:5173`

---

## Projects

### 1. `API` — MonkoraEdge.Core.Auth.API

ASP.NET Core Web API project ที่เป็น entry point หลักของระบบ

**Package Dependencies:**

| Package                                         | Version |
| ----------------------------------------------- | ------- |
| Microsoft.EntityFrameworkCore.Design            | 9.0.10  |
| Microsoft.AspNetCore.OpenApi                    | 9.0.2   |
| Microsoft.Extensions.Caching.StackExchangeRedis | 9.0.10  |
| Swashbuckle.AspNetCore                          | 9.0.6   |

**Middleware Pipeline (`Program.cs`):**

- HTTPS Redirection + HSTS
- Auto-migrate Database (`AuthenticationDbContext`)
- Swagger UI (Development เท่านั้น)
- Error Handling Middleware (`authentication`)
- Routing → Authentication → Authorization → CORS

**Development URL:** `http://localhost:5003`  
**Swagger:** `http://localhost:5003/swagger`  
**Health Checks:**

- `GET /health/ready` — Readiness probe
- `GET /health/live` — Liveness probe

---

### 2. `Domain` — MonkoraEdge.Core.Auth.Domain

Domain layer ประกอบด้วย business logic, entities, services และ interfaces

**Package Dependencies:**

| Package                      | Version |
| ---------------------------- | ------- |
| Azure.Identity               | 1.19.0  |
| Azure.Security.KeyVault.Keys | 4.9.0   |
| Duende.IdentityModel         | 8.0.1   |
| Isopoh.Cryptography.Argon2   | 2.0.0   |
| RabbitMQ.Client              | 7.2.1   |

**Localization Support:** `en`, `ja`, `th`, `zh`

---

### 3. `Infrastructure` — MonkoraEdge.Core.Auth.Infrastructure

Infrastructure layer จัดการ Database, Repositories และ HTTP Clients

**Package Dependencies:**

| Package                              | Version |
| ------------------------------------ | ------- |
| BCrypt.Net-Next                      | 4.1.0   |
| Microsoft.EntityFrameworkCore.Design | 9.0.10  |
| Microsoft.EntityFrameworkCore.Tools  | 9.0.10  |
| Microsoft.IdentityModel.Tokens       | 8.16.0  |
| Otp.NET                              | 1.4.0   |
| System.IdentityModel.Tokens.Jwt      | 8.16.0  |

**Database:** PostgreSQL ผ่าน EF Core (Npgsql)

---

### 4. `DotNet` — MonkoraEdge.Core.DotNet

Shared library สำหรับ cross-cutting concerns เช่น authentication options, exception models, security helpers และ infrastructure abstractions ที่ถูกใช้ข้าม layer

หมายเหตุ: เป้าหมายระยะถัดไปคือจำกัดการพึ่งพา shared library ใน Domain ให้เหลือเฉพาะ abstraction ที่จำเป็นจริง

---

## Entities

### `Tenant` → table: `mt_tenants`

Inherits `BaseEntity` (GUID PK) + `ISoftDelete`

| C# Property  | DB Column     | Type          | Description       |
| ------------ | ------------- | ------------- | ----------------- |
| `Id`         | `id`          | `uuid` PK     | จาก BaseEntity    |
| `TenantCode` | `tenant_code` | `varchar`     | รหัส Tenant       |
| `TenantName` | `tenant_name` | `jsonb`       | ชื่อ (Localized)  |
| `Settings`   | `settings`    | `jsonb`       | การตั้งค่าทั้งหมด |
| `IsActive`   | `is_active`   | `boolean`     | สถานะใช้งาน       |
| `DeletedAt`  | `deleted_at`  | `timestamptz` | Soft Delete       |
| `DeletedBy`  | `deleted_by`  | `varchar`     | ผู้ลบ             |

#### TenantSettings (stored as `jsonb`)

```json
{
  "branding": {
    "display_name": { "en": "", "th": "" },
    "logo_url": "",
    "primary_color": "",
    "favicon_url": null
  },
  "features": {
    "mfa": false,
    "file_upload": { "enabled": false, "max_size_mb": 1048576 },
    "reports": { "enabled": false, "max_rows": 0 }
  },
  "plan": {
    "tier": "",
    "user_limit": 0,
    "expires_at": ""
  },
  "locale": {
    "lang": "en",
    "tz": "UTC",
    "currency": "USD"
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
    "require_mfa_for_admins": false,
    "ip_restrictions": []
  },
  "email": {
    "sender_name": "",
    "sender_address": "",
    "reply_to": "",
    "templates": { "welcome": "", "reset": "" }
  },
  "integrations": {
    "google_oauth": {
      "enabled": false,
      "client_id": "",
      "redirect_uris": []
    },
    "webhooks": [{ "name": "", "url": "", "active": false }]
  },
  "policies": {
    "data_retention_days": 365,
    "consent_required": false,
    "allowed_email_domains": []
  },
  "limits": {
    "api_calls_per_minute": 100,
    "storage_mb": 1024
  },
  "custom": {
    "tags": [],
    "metadata": { "theme": "" }
  }
}
```

---

## OAuth2 Database Tables (32 Tables)

All tables use snake*case naming. Prefix conventions: `mt*`= master data,`tx*`= transaction,`lnk*`= link/junction,`audit\_` = audit.

### Master Data Tables

#### `mt_users` — User

| Column                  | Type                  | Description   |
| ----------------------- | --------------------- | ------------- |
| id                      | uuid PK               |               |
| tenant_id               | uuid FK               | → mt_tenants  |
| email                   | varchar               | unique        |
| phone_number            | varchar               |               |
| first_name              | varchar               |               |
| last_name               | varchar               |               |
| display_name            | varchar               |               |
| locale                  | text                  | language code |
| avatar_url              | text                  |               |
| is_email_verified       | boolean               |               |
| is_phone_verified       | boolean               |               |
| is_active               | boolean               |               |
| last_login_at           | timestamptz           |               |
| deleted_at / deleted_by | timestamptz / varchar | soft delete   |

#### `mt_user_identities` — UserIdentity

| Column                  | Type    | Description          |
| ----------------------- | ------- | -------------------- |
| id                      | uuid PK |                      |
| user_id                 | uuid FK | → mt_users           |
| identity_type           | varchar | username/email/phone |
| username                | varchar | unique               |
| password_hash           | text    |                      |
| is_active               | boolean |                      |
| deleted_at / deleted_by |         | soft delete          |

#### `mt_providers` — Provider

| Column                  | Type    | Description               |
| ----------------------- | ------- | ------------------------- |
| id                      | uuid PK |                           |
| provider_code           | varchar | unique (google, facebook) |
| provider_name           | jsonb   | Locale                    |
| provider_type           | varchar | oauth2/saml/ldap          |
| client_id               | text    |                           |
| client_secret           | text    | encrypted                 |
| authorization_endpoint  | text    |                           |
| token_endpoint          | text    |                           |
| userinfo_endpoint       | text    |                           |
| jwks_uri                | text    |                           |
| scopes                  | text[]  |                           |
| is_active               | boolean |                           |
| deleted_at / deleted_by |         | soft delete               |

#### `mt_authorization_clients` — AuthorizationClient

| Column                     | Type    | Description             |
| -------------------------- | ------- | ----------------------- |
| id                         | uuid PK |                         |
| tenant_id                  | uuid FK |                         |
| client_id                  | varchar | unique OAuth2 client_id |
| client_secret_hash         | text    | hashed                  |
| client_name                | jsonb   | Locale                  |
| client_type                | varchar | public/confidential     |
| grant_types                | text[]  |                         |
| redirect_uris              | text[]  |                         |
| post_logout_redirect_uris  | text[]  |                         |
| token_endpoint_auth_method | varchar |                         |
| access_token_lifetime      | int     | seconds                 |
| refresh_token_lifetime     | int     | seconds                 |
| is_active                  | boolean |                         |
| deleted_at / deleted_by    |         | soft delete             |

#### `mt_scopes` — Scope

| Column                  | Type    | Description  |
| ----------------------- | ------- | ------------ |
| id                      | uuid PK |              |
| scope_name              | varchar | unique       |
| display_name            | jsonb   | Locale       |
| is_active               | boolean | default true |
| deleted_at / deleted_by |         | soft delete  |

#### `mt_roles` — Role

| Column                  | Type    | Description |
| ----------------------- | ------- | ----------- |
| id                      | uuid PK |             |
| tenant_id               | uuid FK |             |
| role_code               | varchar | unique      |
| role_name               | jsonb   | Locale      |
| is_active               | boolean |             |
| deleted_at / deleted_by |         | soft delete |

#### `mt_permissions` — Permission

| Column                  | Type    | Description |
| ----------------------- | ------- | ----------- |
| id                      | uuid PK |             |
| tenant_id               | uuid FK |             |
| permission_code         | varchar | unique      |
| permission_name         | jsonb   | Locale      |
| resource                | varchar |             |
| action                  | varchar |             |
| is_active               | boolean |             |
| deleted_at / deleted_by |         | soft delete |

#### `mt_api_keys` — ApiKey

| Column                  | Type        | Description    |
| ----------------------- | ----------- | -------------- |
| id                      | uuid PK     |                |
| tenant_id               | uuid FK     |                |
| client_id               | uuid FK     |                |
| user_id                 | uuid FK     |                |
| key_hash                | text        | unique         |
| key_prefix              | varchar     | visible prefix |
| name                    | varchar     |                |
| scopes                  | text[]      |                |
| expires_at              | timestamptz |                |
| last_used_at            | timestamptz |                |
| is_active               | boolean     | default true   |
| deleted_at / deleted_by |             | soft delete    |

#### `mt_agreements` — Agreement

| Column                  | Type    | Description          |
| ----------------------- | ------- | -------------------- |
| id                      | uuid PK |                      |
| tenant_id               | uuid FK |                      |
| agreement_code          | varchar | unique               |
| agreement_type          | varchar | terms/privacy/cookie |
| title                   | jsonb   | Locale               |
| content                 | jsonb   | Locale               |
| version                 | varchar |                      |
| is_required             | boolean |                      |
| effective_date          | date    |                      |
| is_active               | boolean |                      |
| deleted_at / deleted_by |         | soft delete          |

---

### Transaction Tables

#### `tx_user_files` — UserFile

| Column                  | Type    | Description      |
| ----------------------- | ------- | ---------------- |
| id                      | uuid PK |                  |
| user_id                 | uuid FK |                  |
| file_type               | varchar | profile/document |
| file_url                | text    |                  |
| file_name               | varchar |                  |
| mime_type               | varchar |                  |
| file_size               | bigint  | bytes            |
| is_active               | boolean |                  |
| deleted_at / deleted_by |         | soft delete      |

#### `lnk_user_external_logins` — UserExternalLogin

| Column                  | Type    | Description |
| ----------------------- | ------- | ----------- |
| id                      | uuid PK |             |
| user_id                 | uuid FK |             |
| provider_id             | uuid FK |             |
| provider_user_id        | varchar |             |
| provider_email          | varchar |             |
| provider_data           | jsonb   | raw profile |
| is_active               | boolean |             |
| deleted_at / deleted_by |         | soft delete |

#### `tx_user_sessions_devices` — UserSessionDevice

| Column             | Type        | Description           |
| ------------------ | ----------- | --------------------- |
| id                 | uuid PK     |                       |
| user_id            | uuid FK     |                       |
| device_fingerprint | varchar     |                       |
| device_name        | varchar     |                       |
| device_type        | varchar     | mobile/desktop/tablet |
| os                 | varchar     |                       |
| browser            | varchar     |                       |
| ip_address         | inet        |                       |
| is_active          | boolean     |                       |
| last_seen_at       | timestamptz |                       |

#### `tx_user_two_factor_settings` — UserTwoFactorSetting

| Column                  | Type    | Description    |
| ----------------------- | ------- | -------------- |
| id                      | uuid PK |                |
| user_id                 | uuid FK |                |
| device_type             | varchar | totp/sms/email |
| secret_key              | text    | encrypted      |
| phone_number            | varchar |                |
| is_active               | boolean |                |
| deleted_at / deleted_by |         | soft delete    |

#### `tx_user_two_factor_recovery_codes` — UserTwoFactorRecoveryCode

| Column    | Type        | Description     |
| --------- | ----------- | --------------- |
| id        | uuid PK     |                 |
| user_id   | uuid FK     |                 |
| code_hash | text        |                 |
| used_at   | timestamptz | null = not used |
| is_active | boolean     | default true    |

#### `tx_user_sessions` — UserSession

| Column     | Type        | Description |
| ---------- | ----------- | ----------- |
| id         | uuid PK     |             |
| user_id    | uuid FK     |             |
| client_id  | uuid FK     |             |
| device_id  | uuid FK     |             |
| token_hash | text        | unique      |
| ip_address | inet        |             |
| user_agent | text        |             |
| expires_at | timestamptz |             |
| is_active  | boolean     |             |

#### `tx_authorization_codes` — AuthorizationCode

| Column                | Type        | Description |
| --------------------- | ----------- | ----------- |
| id                    | uuid PK     |             |
| client_id             | uuid FK     |             |
| user_id               | uuid FK     |             |
| code_hash             | text        | unique      |
| redirect_uri          | text        |             |
| code_challenge        | text        | PKCE        |
| code_challenge_method | varchar     | S256/plain  |
| expires_at            | timestamptz |             |
| used_at               | timestamptz |             |

#### `tx_authorization_access_tokens` — AccessToken

| Column         | Type        | Description |
| -------------- | ----------- | ----------- |
| id             | uuid PK     |             |
| client_id      | uuid FK     |             |
| user_id        | uuid FK     |             |
| session_id     | uuid FK     |             |
| token_hash     | text        | unique      |
| token_type     | varchar     | Bearer      |
| scopes         | text[]      |             |
| expires_at     | timestamptz |             |
| revoked_at     | timestamptz |             |
| revoked_reason | varchar     |             |

#### `tx_authorization_refresh_tokens` — RefreshToken

| Column          | Type        | Description    |
| --------------- | ----------- | -------------- |
| id              | uuid PK     |                |
| access_token_id | uuid FK     |                |
| client_id       | uuid FK     |                |
| user_id         | uuid FK     |                |
| token_hash      | text        | unique         |
| expires_at      | timestamptz |                |
| revoked_at      | timestamptz |                |
| rotated_to_id   | uuid        | token rotation |

#### `tx_authorization_consents` — AuthorizationConsent

| Column     | Type        | Description      |
| ---------- | ----------- | ---------------- |
| id         | uuid PK     |                  |
| user_id    | uuid FK     |                  |
| client_id  | uuid FK     |                  |
| scopes     | text[]      | consented scopes |
| expires_at | timestamptz |                  |

#### `tx_login_attempts` — LoginAttempt

| Column         | Type    | Description |
| -------------- | ------- | ----------- |
| id             | uuid PK |             |
| user_id        | uuid FK | nullable    |
| username       | varchar | attempted   |
| ip_address     | inet    |             |
| user_agent     | text    |             |
| is_success     | boolean |             |
| failure_reason | varchar |             |

#### `tx_rate_limits` — RateLimit

| Column        | Type        | Description        |
| ------------- | ----------- | ------------------ |
| id            | uuid PK     |                    |
| identifier    | varchar     | IP or user_id      |
| endpoint      | varchar     |                    |
| request_count | int         |                    |
| window_start  | timestamptz |                    |
| blocked_until | timestamptz | null = not blocked |

#### `tx_password_history` — PasswordHistory

| Column        | Type    | Description |
| ------------- | ------- | ----------- |
| id            | uuid PK |             |
| user_id       | uuid FK |             |
| identity_id   | uuid FK |             |
| password_hash | text    |             |

#### `tx_email_verifications` — EmailVerification

| Column      | Type        | Description |
| ----------- | ----------- | ----------- |
| id          | uuid PK     |             |
| user_id     | uuid FK     |             |
| email       | varchar     |             |
| token_hash  | text        |             |
| expires_at  | timestamptz |             |
| verified_at | timestamptz |             |

#### `tx_password_resets` — PasswordReset

| Column      | Type        | Description |
| ----------- | ----------- | ----------- |
| id          | uuid PK     |             |
| user_id     | uuid FK     |             |
| identity_id | uuid FK     |             |
| token_hash  | text        |             |
| expires_at  | timestamptz |             |
| used_at     | timestamptz |             |

#### `tx_revoked_tokens` — RevokedToken

| Column         | Type        | Description    |
| -------------- | ----------- | -------------- |
| id             | uuid PK     |                |
| token_hash     | text        | unique         |
| token_type     | varchar     | access/refresh |
| client_id      | uuid FK     |                |
| user_id        | uuid FK     |                |
| revoked_reason | varchar     |                |
| expires_at     | timestamptz |                |

#### `tx_agreement_accepts` — AgreementAccept

| Column            | Type        | Description  |
| ----------------- | ----------- | ------------ |
| id                | uuid PK     |              |
| user_id           | uuid FK     |              |
| agreement_id      | uuid FK     |              |
| agreement_version | varchar     |              |
| ip_address        | inet        |              |
| user_agent        | text        |              |
| is_active         | boolean     | default true |
| revoked_at        | timestamptz |              |

---

### Audit Table

#### `audit_logs` — AuditLog

| Column      | Type    | Description                     |
| ----------- | ------- | ------------------------------- |
| id          | uuid PK |                                 |
| tenant_id   | uuid FK |                                 |
| user_id     | uuid FK | nullable                        |
| action      | varchar | CREATE/UPDATE/DELETE/LOGIN etc. |
| entity_type | varchar |                                 |
| entity_id   | uuid    |                                 |
| old_values  | jsonb   | before state                    |
| new_values  | jsonb   | after state                     |
| ip_address  | inet    |                                 |
| user_agent  | text    |                                 |

---

### Link / Junction Tables

#### `lnk_authorization_client_scopes` — AuthorizationClientScope

| Column    | Type    | Description                |
| --------- | ------- | -------------------------- |
| id        | uuid PK |                            |
| client_id | uuid FK | → mt_authorization_clients |
| scope_id  | uuid FK | → mt_scopes                |

#### `lnk_authorization_code_scopes` — AuthorizationCodeScope

| Column                | Type    | Description              |
| --------------------- | ------- | ------------------------ |
| id                    | uuid PK |                          |
| authorization_code_id | uuid FK | → tx_authorization_codes |
| scope_id              | uuid FK | → mt_scopes              |

#### `lnk_role_permissions` — RolePermission

| Column        | Type    | Description      |
| ------------- | ------- | ---------------- |
| id            | uuid PK |                  |
| role_id       | uuid FK | → mt_roles       |
| permission_id | uuid FK | → mt_permissions |

#### `lnk_user_roles` — UserRole

| Column     | Type        | Description |
| ---------- | ----------- | ----------- |
| id         | uuid PK     |             |
| user_id    | uuid FK     | → mt_users  |
| role_id    | uuid FK     | → mt_roles  |
| tenant_id  | uuid FK     |             |
| expires_at | timestamptz |             |

---

## Repositories (OAuth2 System)

All repository implementations follow the `BaseRepository<AuthenticationDbContext, TEntity>` pattern.

| Interface                              | Implementation                        | Aggregate              |
| -------------------------------------- | ------------------------------------- | ---------------------- |
| `IUserRepository`                      | `UserRepository`                      | UserAggregate          |
| `IUserFileRepository`                  | `UserFileRepository`                  | UserAggregate          |
| `IUserIdentityRepository`              | `UserIdentityRepository`              | UserAggregate          |
| `IUserExternalLoginRepository`         | `UserExternalLoginRepository`         | UserAggregate          |
| `IUserSessionDeviceRepository`         | `UserSessionDeviceRepository`         | UserAggregate          |
| `IUserTwoFactorSettingRepository`      | `UserTwoFactorSettingRepository`      | UserAggregate          |
| `IUserTwoFactorRecoveryCodeRepository` | `UserTwoFactorRecoveryCodeRepository` | UserAggregate          |
| `IUserSessionRepository`               | `UserSessionRepository`               | UserAggregate          |
| `IPasswordHistoryRepository`           | `PasswordHistoryRepository`           | UserAggregate          |
| `IEmailVerificationRepository`         | `EmailVerificationRepository`         | UserAggregate          |
| `IPasswordResetRepository`             | `PasswordResetRepository`             | UserAggregate          |
| `IProviderRepository`                  | `ProviderRepository`                  | ProviderAggregate      |
| `IAuthorizationClientRepository`       | `AuthorizationClientRepository`       | AuthorizationAggregate |
| `IScopeRepository`                     | `ScopeRepository`                     | AuthorizationAggregate |
| `IAuthorizationClientScopeRepository`  | `AuthorizationClientScopeRepository`  | AuthorizationAggregate |
| `IAuthorizationCodeRepository`         | `AuthorizationCodeRepository`         | AuthorizationAggregate |
| `IAuthorizationCodeScopeRepository`    | `AuthorizationCodeScopeRepository`    | AuthorizationAggregate |
| `IAccessTokenRepository`               | `AccessTokenRepository`               | AuthorizationAggregate |
| `IRefreshTokenRepository`              | `RefreshTokenRepository`              | AuthorizationAggregate |
| `IAuthorizationConsentRepository`      | `AuthorizationConsentRepository`      | AuthorizationAggregate |
| `IRevokedTokenRepository`              | `RevokedTokenRepository`              | AuthorizationAggregate |
| `IRoleRepository`                      | `RoleRepository`                      | RoleAggregate          |
| `IPermissionRepository`                | `PermissionRepository`                | RoleAggregate          |
| `IRolePermissionRepository`            | `RolePermissionRepository`            | RoleAggregate          |
| `IUserRoleRepository`                  | `UserRoleRepository`                  | RoleAggregate          |
| `IAuditLogRepository`                  | `AuditLogRepository`                  | AuditAggregate         |
| `ILoginAttemptRepository`              | `LoginAttemptRepository`              | AuditAggregate         |
| `IRateLimitRepository`                 | `RateLimitRepository`                 | AuditAggregate         |
| `IApiKeyRepository`                    | `ApiKeyRepository`                    | ApiKeyAggregate        |
| `IAgreementRepository`                 | `AgreementRepository`                 | AgreementAggregate     |
| `IAgreementAcceptRepository`           | `AgreementAcceptRepository`           | AgreementAggregate     |

---

## Domain Services

All services are registered in `ServiceCollectionExtensions` and injected via constructor.

| Interface            | Implementation      | Description                                                   |
| -------------------- | ------------------- | ------------------------------------------------------------- |
| `IPasswordService`   | `PasswordService`   | BCrypt hashing (factor 12), PKCE, SHA-256, TOTP, policy check |
| `ITokenService`      | `TokenService`      | RS256 JWT generation, refresh rotation, JWKS, introspection   |
| `IAuthService`       | `AuthService`       | Login, register, logout, refresh, 2FA, email verify, password |
| `IOAuth2Service`     | `OAuth2Service`     | RFC 6749 authorize + all grant types, revoke, introspect      |
| `IUserService`       | `UserService`       | User CRUD + role assignment                                   |
| `IClientService`     | `ClientService`     | OAuth2 client CRUD + secret rotation                          |
| `IScopeService`      | `ScopeService`      | Scope CRUD (system scopes protected)                          |
| `IRoleService`       | `RoleService`       | Role CRUD + permission assignment                             |
| `IPermissionService` | `PermissionService` | Permission CRUD                                               |
| `IApiKeyService`     | `ApiKeyService`     | API key CRUD + validation                                     |

---

## API Endpoints

### OpenID Connect Discovery

| Method | Path                                | Auth | Description              |
| ------ | ----------------------------------- | ---- | ------------------------ |
| GET    | `/.well-known/openid-configuration` | None | OIDC discovery document  |
| GET    | `/.well-known/jwks.json`            | None | JSON Web Key Set (RS256) |

### OAuth2 Endpoints (RFC 6749)

| Method   | Path                        | Auth       | Description                                    |
| -------- | --------------------------- | ---------- | ---------------------------------------------- |
| GET/POST | `/oauth2/authorize`         | Optional   | Authorization endpoint — PKCE + consent flow   |
| POST     | `/oauth2/authorize/consent` | Bearer     | Submit consent decision, receive auth code     |
| POST     | `/oauth2/token`             | Basic/Body | Token endpoint (all grant types, form-encoded) |
| POST     | `/oauth2/revoke`            | Basic/Body | Token revocation (RFC 7009)                    |
| POST     | `/oauth2/introspect`        | Basic/Body | Token introspection (RFC 7662)                 |
| GET      | `/oauth2/userinfo`          | Bearer     | UserInfo endpoint (OIDC)                       |

**Supported Grant Types:**

- `authorization_code` — with PKCE (S256 or plain)
- `client_credentials` — for machine-to-machine
- `refresh_token` — token rotation (single-use)
- `password` — resource owner (legacy, internal use)

**Client Authentication:**

- `Authorization: Basic base64(client_id:client_secret)` (client_secret_basic)
- Form body `client_id` + `client_secret` (client_secret_post)

### Authentication Endpoints

| Method | Path                        | Auth   | Description                               |
| ------ | --------------------------- | ------ | ----------------------------------------- |
| POST   | `/auth/login`               | None   | Username/password login, returns tokens   |
| POST   | `/auth/register`            | None   | Register new account                      |
| POST   | `/auth/logout`              | Bearer | Revoke tokens, end session                |
| POST   | `/auth/refresh`             | None   | Refresh access token                      |
| POST   | `/auth/forgot-password`     | None   | Send password reset email                 |
| POST   | `/auth/reset-password`      | None   | Reset password with token                 |
| POST   | `/auth/change-password`     | Bearer | Change password while authenticated       |
| POST   | `/auth/verify-email`        | None   | Verify email with token                   |
| POST   | `/auth/resend-verification` | Bearer | Resend verification email                 |
| GET    | `/auth/2fa/setup`           | Bearer | Get TOTP setup details (secret + QR code) |
| POST   | `/auth/2fa/enable`          | Bearer | Enable 2FA after verifying TOTP code      |
| POST   | `/auth/2fa/disable`         | Bearer | Disable 2FA with password confirmation    |
| POST   | `/auth/2fa/verify`          | None   | Complete 2FA login challenge              |

### User Management

| Method | Path                       | Auth   | Description                  |
| ------ | -------------------------- | ------ | ---------------------------- |
| GET    | `/users`                   | Bearer | List users (paged, filtered) |
| GET    | `/users/{id}`              | Bearer | Get user by ID               |
| GET    | `/users/tenant/{tenantId}` | Bearer | List users by tenant         |
| POST   | `/users`                   | Bearer | Create user                  |
| PUT    | `/users/{id}`              | Bearer | Update user                  |
| DELETE | `/users/{id}`              | Bearer | Soft-delete user             |
| POST   | `/users/{id}/activate`     | Bearer | Activate user                |
| POST   | `/users/{id}/deactivate`   | Bearer | Deactivate user              |
| POST   | `/users/{id}/roles`        | Bearer | Assign roles to user         |
| DELETE | `/users/{id}/roles`        | Bearer | Remove roles from user       |

### Client Management

| Method | Path                          | Auth   | Description                             |
| ------ | ----------------------------- | ------ | --------------------------------------- |
| GET    | `/clients`                    | Bearer | List clients (paged, filtered)          |
| GET    | `/clients/{id}`               | Bearer | Get client by ID                        |
| GET    | `/clients/tenant/{tenantId}`  | Bearer | List clients by tenant                  |
| POST   | `/clients`                    | Bearer | Register new client (secret shown once) |
| PUT    | `/clients/{id}`               | Bearer | Update client                           |
| DELETE | `/clients/{id}`               | Bearer | Delete client                           |
| POST   | `/clients/{id}/rotate-secret` | Bearer | Rotate client secret                    |
| POST   | `/clients/{id}/activate`      | Bearer | Activate client                         |
| POST   | `/clients/{id}/deactivate`    | Bearer | Deactivate client                       |

### Scope Management

| Method | Path           | Auth   | Description                         |
| ------ | -------------- | ------ | ----------------------------------- |
| GET    | `/scopes`      | Bearer | List scopes                         |
| GET    | `/scopes/{id}` | Bearer | Get scope by ID                     |
| POST   | `/scopes`      | Bearer | Create scope                        |
| PUT    | `/scopes/{id}` | Bearer | Update scope (system scopes locked) |
| DELETE | `/scopes/{id}` | Bearer | Delete scope (system scopes locked) |

### Role Management

| Method | Path                       | Auth   | Description          |
| ------ | -------------------------- | ------ | -------------------- |
| GET    | `/roles`                   | Bearer | List roles           |
| GET    | `/roles/{id}`              | Bearer | Get role by ID       |
| GET    | `/roles/tenant/{tenantId}` | Bearer | List roles by tenant |
| POST   | `/roles`                   | Bearer | Create role          |
| PUT    | `/roles/{id}`              | Bearer | Update role          |
| DELETE | `/roles/{id}`              | Bearer | Delete role          |
| POST   | `/roles/{id}/permissions`  | Bearer | Assign permissions   |
| DELETE | `/roles/{id}/permissions`  | Bearer | Remove permissions   |

### Permission Management

| Method | Path                             | Auth   | Description                |
| ------ | -------------------------------- | ------ | -------------------------- |
| GET    | `/permissions`                   | Bearer | List permissions           |
| GET    | `/permissions/{id}`              | Bearer | Get permission by ID       |
| GET    | `/permissions/tenant/{tenantId}` | Bearer | List permissions by tenant |
| POST   | `/permissions`                   | Bearer | Create permission          |
| PUT    | `/permissions/{id}`              | Bearer | Update permission          |
| DELETE | `/permissions/{id}`              | Bearer | Delete permission          |

### API Key Management

| Method | Path                          | Auth   | Description                           |
| ------ | ----------------------------- | ------ | ------------------------------------- |
| GET    | `/api-keys`                   | Bearer | List API keys for current user        |
| GET    | `/api-keys/{id}`              | Bearer | Get API key by ID                     |
| GET    | `/api-keys/client/{clientId}` | Bearer | List API keys by client               |
| POST   | `/api-keys`                   | Bearer | Create API key (plain key shown once) |
| DELETE | `/api-keys/{id}`              | Bearer | Revoke API key                        |
| POST   | `/api-keys/validate`          | None   | Validate API key (internal/service)   |

---

## Environment Variables

| Variable                      | Type   | Description                                      |
| ----------------------------- | ------ | ------------------------------------------------ |
| `POSTGRES_CONNECTIONSTRING`   | string | PostgreSQL connection string                     |
| `AUTH_ISSUER`                 | string | Issuer base URL (e.g. https://auth.example.com)  |
| `OAUTH2_SIGNED_PRIVATE_KEY`   | string | RSA private key PEM for RS256 signing            |
| `AUTH_JWKS_ENDPOINT`          | string | Public JWKS endpoint URL                         |
| `TOKEN_EXPIRES_IN_MINUTES`    | int    | Access token lifetime (default: 60)              |
| `SIGNIN_FAILED_IN_MINUTES`    | int    | Window for lock-out counting (default: 15)       |
| `BLOCK_IP_ADDRESS_IN_MINUTES` | int    | IP block duration after too many failed attempts |
| `ARGON2_SECRET`               | string | Secret for Argon2 password hashing               |
| `REDIS_CONNECTIONSTRING`      | string | Redis for distributed cache                      |

---

## Security Design

- **Password hashing:** BCrypt work factor 12 (upgrade path to Argon2 via `ARGON2_SECRET`)
- **Token signing:** RSA-2048 RS256 (private key from environment — never in code)
- **Client secrets:** SHA-256 hashed at storage, generated as 48-byte base64url random
- **API keys:** SHA-256 hashed, format `dk_<base64url>`, stored with visible prefix for lookup
- **PKCE:** S256 (SHA-256 code challenge) enforced for public clients; plain supported for legacy
- **Token rotation:** Refresh tokens are single-use; each use issues a new token and revokes old
- **Revocation list:** `tx_revoked_tokens` table indexed for fast lookup at introspection time
- **Account lockout:** Consecutive failures within `SIGNIN_FAILED_IN_MINUTES` window trigger lock
- **Soft delete:** All master data uses `deleted_at` / `deleted_by` — no hard deletes
- **IP forwarding:** `X-Forwarded-For` header respected for real client IP behind proxies

---

## Development

```bash
# Restore and build
dotnet restore
dotnet build

# Apply database migrations
dotnet ef database update --project src/Infrastructure --startup-project src/API

# Run API (Development)
dotnet run --project src/API
```

Swagger UI: `http://localhost:5003/swagger`

| `IAuthorizationCodeRepository` | `AuthorizationCodeRepository` | AuthorizationAggregate |
| `IAuthorizationCodeScopeRepository` | `AuthorizationCodeScopeRepository` | AuthorizationAggregate |
| `IAccessTokenRepository` | `AccessTokenRepository` | AuthorizationAggregate |
| `IRefreshTokenRepository` | `RefreshTokenRepository` | AuthorizationAggregate |
| `IAuthorizationConsentRepository` | `AuthorizationConsentRepository` | AuthorizationAggregate |
| `IRevokedTokenRepository` | `RevokedTokenRepository` | AuthorizationAggregate |
| `IRoleRepository` | `RoleRepository` | RoleAggregate |
| `IPermissionRepository` | `PermissionRepository` | RoleAggregate |
| `IRolePermissionRepository` | `RolePermissionRepository` | RoleAggregate |
| `IUserRoleRepository` | `UserRoleRepository` | RoleAggregate |
| `IAuditLogRepository` | `AuditLogRepository` | AuditAggregate |
| `ILoginAttemptRepository` | `LoginAttemptRepository` | AuditAggregate |
| `IRateLimitRepository` | `RateLimitRepository` | AuditAggregate |
| `IApiKeyRepository` | `ApiKeyRepository` | ApiKeyAggregate |
| `IAgreementRepository` | `AgreementRepository` | AgreementAggregate |
| `IAgreementAcceptRepository` | `AgreementAcceptRepository` | AgreementAggregate |

### Console API (`/console`)

| Method   | Route               | Auth     | Description              | Status             |
| -------- | ------------------- | -------- | ------------------------ | ------------------ |
| `GET`    | `/api/console/{id}` | Required | ดึงข้อมูล Tenant ตาม ID  | ✅ Implemented     |
| `POST`   | Create Tenant       | —        | สร้าง Tenant ใหม่        | 🚧 In Progress     |
| `PUT`    | Update Tenant       | —        | แก้ไขข้อมูล Tenant       | ⏳ Not Implemented |
| `DELETE` | Delete Tenant       | —        | ลบ Tenant                | ⏳ Not Implemented |
| `GET`    | DataSource Tenants  | —        | ดึงรายการ Tenant (paged) | ⏳ Not Implemented |

---

## Services

### `ITenantService` / `TenantService`

| Method                           | Return Type                                    | Description         |
| -------------------------------- | ---------------------------------------------- | ------------------- |
| `CreateTenantAsync(request)`     | `CreateResponse`                               | สร้าง Tenant ใหม่   |
| `GetTenantByIdAsync(id)`         | `TenantResponse`                               | ดึง Tenant ตาม GUID |
| `GetTenantsAsync(request)`       | `DataSourceResponse<TenantDataSourceResponse>` | ดึงรายการ Tenant    |
| `UpdateTenantAsync(id, request)` | `UpdateResponse`                               | แก้ไข Tenant        |
| `DeleteTenantAsync(id)`          | `DeleteResponse`                               | ลบ Tenant           |

---

## Repositories

### `ITenanttRepository` / `TenanttRepository`

| Method                        | Description                                 |
| ----------------------------- | ------------------------------------------- |
| `GetTenantByIdAsync(Guid id)` | ดึง Tenant entity จาก DB ตาม ID             |
| CRUD inherited                | Insert, Update, Delete จาก `BaseRepository` |

---

## HTTP Clients

| Client Key          | Base URL (from config)            | Usage                     |
| ------------------- | --------------------------------- | ------------------------- |
| `GOOGLE_API`        | `GOOGLE_API_AUTH_ENDPOINT`        | Google API Authentication |
| `GOOGLE_AOUTH2_API` | `GOOGLE_OAUTH2_API_AUTH_ENDPOINT` | Google OAuth2             |
| `NOTIFICATION_API`  | `NOTIFICATION_ENDPOINT`           | Notification Service      |
| `RESOURCES_API`     | `RESOURCE_API_ENDPOINT`           | Resources Service         |
| `FACEBOOK`          | `FACEBOOK_API_ENDPOINT`           | Facebook API              |
| `APPLE`             | `APPLE_API_ENDPOINT`              | Apple API                 |

---

## Environment Variables (`EnvironmentOptions`)

| Variable                                | Required | Description                                      |
| --------------------------------------- | -------- | ------------------------------------------------ |
| `POSTGRES_CONNECTIONSTRING`             | ✅       | PostgreSQL connection string                     |
| `REDIS_CONNECTIONSTRING`                | ✅       | Redis connection string                          |
| `BASIC_AUTHENTICATION_USERNAME`         | ✅       | Basic Auth username                              |
| `BASIC_AUTHENTICATION_PASSWORD`         | ✅       | Basic Auth password                              |
| `API_KEYS`                              | ✅       | API Key(s) สำหรับ authorization                  |
| `TOKEN_EXPIRES_IN_MINUTES`              | ✅       | อายุ token (นาที)                                |
| `OAUTH2_SIGNED_PRIVATE_KEY`             | ✅       | Private key สำหรับ OAuth2 JWT signing            |
| `ARGON2_SECRET`                         | ✅       | Secret สำหรับ Argon2 hashing                     |
| `HASH_SECRET_KEY`                       | ✅       | AES encryption key                               |
| `HASH_SECRET_IV`                        | ✅       | AES encryption IV                                |
| `SIGNIN_FAILED_IN_MINUTES`              | ✅       | ระยะเวลา lock หลัง sign-in ล้มเหลว (นาที)        |
| `BLOCK_IP_ADDRESS_IN_MINUTES`           | ✅       | ระยะเวลา block IP address (นาที)                 |
| `ACCOUNT_DELETION_GRACE_PERIOD_DAYS`    | ✅       | ระยะเวลา grace period ก่อนลบบัญชี (วัน)          |
| `GOOGLE_API_AUTH_KEY`                   | ✅       | Google API Key                                   |
| `GOOGLE_API_AUTH_ENDPOINT`              | ✅       | Google API endpoint                              |
| `GOOGLE_OAUTH2_API_AUTH_ENDPOINT`       | ✅       | Google OAuth2 endpoint                           |
| `GOOGLE_APPLICATION_CREDENTIALS_AUTH`   | ✅       | Google Service Account JSON                      |
| `NOTIFICATION_ENDPOINT`                 | ✅       | Notification service URL                         |
| `RESOURCE_API_ENDPOINT`                 | ✅       | Resources API URL                                |
| `FACEBOOK_API_ENDPOINT`                 | ✅       | Facebook Graph API URL                           |
| `APPLE_API_ENDPOINT`                    | ✅       | Apple ID API URL                                 |
| `AUTH_ISSUER`                           | ✅       | Issuer base URL สำหรับ token claims และ metadata |
| `AUTH_JWKS_ENDPOINT`                    | ✅       | JWKS endpoint สำหรับ JWT verification            |
| `ACCOUNT_TOYO_ENDPOINT`                 | ✅       | Account (TOYO) service URL                       |
| `CLIENT_TOYO_FORGOT_PASSWORD_ENDPOINT`  | ✅       | Forgot password path                             |
| `AMQP_HOST`                             | ✅       | RabbitMQ host                                    |
| `AMQP_PORT`                             | ✅       | RabbitMQ port                                    |
| `AMQP_USERNAME`                         | ✅       | RabbitMQ username                                |
| `AMQP_PASSWORD`                         | ✅       | RabbitMQ password                                |
| `AMQP_SYNC_DELETE_ACCOUNT_EXCHANGE_KEY` | ✅       | RabbitMQ exchange key สำหรับ sync delete account |

---

## External Dependencies

- **[core-dotnet](../core-dotnet)** — Shared base library (`MonkoraEdge.Core.DotNet`) ประกอบด้วย `BaseEntity`, `BaseRepository`, `UnitOfWork`, middleware และ common utilities

---

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- PostgreSQL
- Redis

### Run (Development)

```bash
cd src/API
dotnet run
```

### Postman

- Postman collection อยู่ที่ `postman/MonkoraEdge.Core.Auth.postman_collection.json`
- Postman environment สำหรับ local อยู่ที่ `postman/MonkoraEdge.Core.Auth.local.postman_environment.json`
- collection ครอบคลุม health checks, discovery endpoints, auth, OAuth2, users, clients, roles, permissions, scopes, API keys และ console tenant endpoint
- ก่อนใช้งาน protected endpoints ให้ตั้งค่า `bearerToken`; สำหรับ `oauth2/revoke` และ `oauth2/introspect` ให้ตั้งค่า `basicAuth` เป็น Base64 ของ `clientId:clientSecret`
- ค่าเริ่มต้นของ `baseUrl` ถูกตั้งไว้เป็น `http://localhost:5000` ตาม workflow `dotnet run --no-launch-profile`; หากรันด้วยพอร์ตอื่นให้แก้ใน environment ก่อน import หรือหลัง import

### Database Migration

```bash
cd src/Infrastructure
set ASPNETCORE_ENVIRONMENT=Development
dotnet ef migrations add <MigrationName> --context AuthenticationDbContext --output-dir Migrations
dotnet ef database update --context AuthenticationDbContext
```

สำหรับ seed migration ใหม่ที่ไม่อยากชนชื่อเดิม ให้ใช้:

```powershell
cd src/Infrastructure
.\Add-SeedMigration.ps1
```

หมายเหตุ:

- EF Core ไม่รองรับการสร้าง `[Migration("yyyyMMddHHmmss_Name")]` แบบ dynamic ภายในไฟล์ C# เพราะค่าใน attribute ต้องเป็น compile-time constant
- สคริปต์ `Add-SeedMigration.ps1` จะใช้ EF CLI สร้าง migration ใหม่พร้อม timestamp อัตโนมัติ แล้ว patch `Up()` ให้เรียก `SeedMasterDataMigrationSupport.Apply(migrationBuilder)` ให้อัตโนมัติ
- ถ้าชื่อ `SeedMasterDataFromScriptSql` มีอยู่แล้ว สคริปต์จะเติม suffix เวลาเข้าไปในชื่อ migration ใหม่ให้อัตโนมัติ
- ต้องมี migration schema อย่างน้อยหนึ่งตัวก่อน เช่น `InitialCreate`; ถ้าโฟลเดอร์ `Migrations` ยังไม่มี migration แบบ `yyyyMMddHHmmss_Name.cs` สคริปต์จะหยุดเพื่อกันการ scaffold schema ทั้งก้อนออกมาเป็น seed migration โดยผิดลำดับ

หมายเหตุ:

- ถ้าต้องการ override connection string ให้ตั้ง `POSTGRES_CONNECTIONSTRING` ใน environment variables ก่อนรัน `dotnet ef`
- ถ้าต้องการยังใช้ API เป็น startup project ก็ยังทำได้ผ่าน `--startup-project ../API` แต่ไม่จำเป็นแล้วสำหรับ workflow ปกติ
- migration `SeedMasterDataFromScriptSql` จะรัน master-data seed จาก `src/API/script_sql/#33 seed_master_data.sql` อัตโนมัติหลัง update schema สำเร็จ

# Create Migrations Step

1. สร้าง Migrations
   dotnet ef migrations add InitialCreate --context AuthenticationDbContext --output-dir Migrations

2. สร้าง .\Add-SeedMigration.ps1

3. dotnet ef database update --context AuthenticationDbContext

--

---

## Changelog

| Date       | Description                                                                                                                                                                                                                                                                                                                                      |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 2026-03-14 | Initial README documentation — documented full project structure, entities (Tenant + all TenantSettings), services, endpoints, and environment variables                                                                                                                                                                                         |
| 2026-03-14 | Implemented full OAuth2 system: 31 entities, 31 EF EntityTypeConfigurations, 31 repository interfaces (across 6 aggregates), 31 repository implementations, updated AuthenticationDbContext (32 DbSets + 32 ApplyConfiguration calls), updated ServiceCollectionExtensions (31 DI registrations), updated README with all 32 table documentation |
