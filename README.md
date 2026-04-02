# MonkoraEdge.Core.Auth API

Core Authentication and Authorization Server for the MonkoraEdge Platform.

- Company: Monkora Co., Ltd.
- Author: Boonhome Wongsuwan
- Current version: 1.0.0
- Framework: .NET 9.0
- Last updated: 2026-04-02

## Current Status (2026-04)

- Full OAuth 2.1 Authorization Server implemented in `IOAuth2Service` (Domain layer).
- Authorization Code + PKCE (`S256`) is mandatory; `plain` method and implicit/password grants are explicitly rejected.
- `at+JWT` typ header enforced on all access tokens per RFC 9068 §2.1.
- RS256 signing with runtime key-size validation (≥ 2048 bits); algorithm restriction prevents `alg:none` and HS256 confusion.
- Refresh token rotation with family-based reuse detection and cascade revocation.
- JWT revocation check via `OnTokenValidated` event (DB-backed revocation list + access token soft-delete).
- Full OIDC Core support: `at_hash`, UserInfo endpoint, `prompt`, `max_age`, discovery documents.
- Two-factor authentication (TOTP) with challenge token flow and recovery codes.
- Per-IP rate limiting with two policies: `default` (60 req/min) and `auth` (10 req/min).
- Background token cleanup service (`TokenCleanupService`) runs periodically to purge expired rows.
- Structured logging via Serilog with `CorrelationId` enrichment on every request.
- This README is the source of truth for high-level architecture and maintenance workflow.

## Solution Structure

```text
core-auth-api/
|- README.md
|- MonkoraEdge.Core.Auth.sln
`- src/
   |- API/             # ASP.NET Core Web API — middleware pipeline, controllers, DI composition
   |- Domain/          # Business rules, OAuth2/Auth/2FA orchestration, domain exceptions
   |- Infrastructure/  # EF Core, repositories, TokenService, PasswordService, external adapters
   `- DotNet/          # Shared cross-cutting abstractions (UoW interfaces, base models, middlewares)
```

## Runtime Architecture

### API Layer (`src/API`)

- Hosts HTTP endpoints and composes the ASP.NET Core middleware pipeline.
- Binds request models, resolves auth context (user sub, IP address, user-agent), and maps domain results to HTTP responses.
- Domain exceptions surface as structured JSON via `DomainExceptionHandlingMiddleware`.
- Applies CORS policy from `Cors:AllowedOrigins` (deny-all when list is empty).
- Sets security headers on every response: CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy, HSTS (1 year + includeSubDomains).
- Exposes health checks at `/health/ready` (PostgreSQL + Redis) and `/health/live`.
- Requests a 64 KB max body size limit via Kestrel configuration.

### Domain Layer (`src/Domain`)

- Owns all OAuth 2.1, OIDC, and user authentication logic.
- Key services: `OAuth2Service`, `AuthService`, `ClientAuthenticator`, `RefreshTokenProcessor`.
- Defines repository and unit-of-work interfaces consumed by Infrastructure.
- `ClientAuthenticator` handles client verification, grant validation, and scope resolution.
- `RefreshTokenProcessor` centralizes refresh-token validation, rotation, and reuse detection so both the OAuth endpoint and legacy `/auth/refresh` are behaviorally identical.

### Infrastructure Layer (`src/Infrastructure`)

- EF Core (PostgreSQL via Npgsql) with DbContext pooling and retry-on-failure.
- `TokenService` — JWT issuance (RS256, `at+JWT`), JWKS, authorization codes, refresh tokens, ID tokens, token revocation and introspection.
- `PasswordService` — bcrypt (workFactor 12), PKCE S256 verification, TOTP via OtpNet, recovery code generation, timing-normalization dummy verify.
- `MemoryTwoFactorChallengeStore` — single-use in-process challenge token store backed by `IMemoryCache` (see deployment note below).
- Redis distributed cache via `StackExchange.Redis`.
- External API clients for Google, Facebook, Apple social login flows.
- AMQP (RabbitMQ) adapter for account deletion sync events.

### DotNet Layer (`src/DotNet`)

- Shared interfaces (`IUnitOfWork`, base aggregate models, constants), utilities, and middleware building blocks used by API and Infrastructure.

## Middleware Pipeline Order

```
HTTPS Redirect → HSTS → Cookie Policy → Error Handling → Domain Exception Handling
  → Correlation ID → Security Headers → Routing → CORS → Rate Limiter
  → Authentication → Authorization → Controllers
```

## Key Endpoints

### Discovery

| Method | Route                                     | Auth   | Notes                                                      |
| ------ | ----------------------------------------- | ------ | ---------------------------------------------------------- |
| `GET`  | `/.well-known/openid-configuration`       | Public | Full OIDC discovery document                               |
| `GET`  | `/.well-known/oauth-authorization-server` | Public | RFC 8414 AS metadata                                       |
| `GET`  | `/.well-known/jwks.json`                  | Public | Public signing keys; `Cache-Control: public, max-age=3600` |

### Health

| Method | Route           | Notes                           |
| ------ | --------------- | ------------------------------- |
| `GET`  | `/health/ready` | PostgreSQL + Redis checks       |
| `GET`  | `/health/live`  | Always healthy if process is up |

### OAuth 2 / OIDC (`/oauth2`)

| Method      | Route                                     | Auth         | Rate Limit | Notes                                                                             |
| ----------- | ----------------------------------------- | ------------ | ---------- | --------------------------------------------------------------------------------- |
| `GET\|POST` | `/oauth2/authorize` (also `/authorize`)   | Optional     | `default`  | Authorization Code flow; returns redirect, login-required, or consent-required    |
| `POST`      | `/oauth2/authorize/consent`               | Required     | `default`  | Consent submission; same-origin check enforced for browser contexts               |
| `POST`      | `/oauth2/token` (also `/token`)           | Client auth  | `auth`     | Supports `authorization_code`, `client_credentials`, `refresh_token`              |
| `POST`      | `/oauth2/revoke` (also `/revoke`)         | Client auth  | `auth`     | RFC 7009; always returns HTTP 200 for authenticated requests                      |
| `POST`      | `/oauth2/introspect` (also `/introspect`) | Client auth  | `auth`     | RFC 7662; returns `active: false` for invalid/revoked tokens                      |
| `GET`       | `/oauth2/userinfo`                        | Bearer token | —          | OIDC Core §5.3; scope-filtered claims; WWW-Authenticate on error                  |
| `GET\|POST` | `/oauth2/end-session`                     | Optional     | —          | RP-initiated logout; validates `post_logout_redirect_uri` against registered list |

### Authentication (`/auth`)

| Method | Route                       | Auth     | Rate Limit | Notes                                                                                                        |
| ------ | --------------------------- | -------- | ---------- | ------------------------------------------------------------------------------------------------------------ |
| `POST` | `/auth/login`               | Public   | `default`  | Local credential + optional TOTP/recovery code; issues 2FA challenge token when 2FA enrolled but code absent |
| `POST` | `/auth/register`            | Public   | `default`  | Creates local account; triggers email verification flow                                                      |
| `POST` | `/auth/logout`              | Required | —          | Revokes refresh tokens; supports single-device or all-device logout                                          |
| `POST` | `/auth/refresh`             | Public   | `default`  | Compatibility facade → delegates to `grant_type=refresh_token`                                               |
| `POST` | `/auth/forgot-password`     | Public   | `default`  | Initiates password reset; always returns 204 (no username enumeration)                                       |
| `POST` | `/auth/reset-password`      | Public   | `default`  | Completes with valid reset token                                                                             |
| `POST` | `/auth/change-password`     | Required | `default`  | Validates current password before updating                                                                   |
| `POST` | `/auth/verify-email`        | Public   | `default`  | Verifies ownership via token                                                                                 |
| `POST` | `/auth/resend-verification` | Required | `default`  | Re-sends verification email                                                                                  |
| `GET`  | `/auth/2fa/setup`           | Required | —          | Returns TOTP secret and QR URI; `?deviceType=TOTP`                                                           |
| `POST` | `/auth/2fa/enable`          | Required | —          | Activates 2FA after verifying code                                                                           |
| `POST` | `/auth/2fa/disable`         | Required | —          | Deactivates 2FA with proof                                                                                   |
| `POST` | `/auth/2fa/verify`          | Public   | —          | Completes login using challenge token + OTP/recovery code                                                    |

### User Management (`/users`) — Requires JWT

| Method   | Route                      | Notes                                                                  |
| -------- | -------------------------- | ---------------------------------------------------------------------- |
| `GET`    | `/users`                   | List with datasource filters (search, status, tenant, paging, sorting) |
| `GET`    | `/users/{id}`              | Single user profile                                                    |
| `GET`    | `/users/tenant/{tenantId}` | Users by tenant                                                        |
| `POST`   | `/users`                   | Create user                                                            |
| `PUT`    | `/users/{id}`              | Update profile fields                                                  |
| `DELETE` | `/users/{id}`              | Soft-delete                                                            |
| `POST`   | `/users/{id}/activate`     | Allow authentication                                                   |
| `POST`   | `/users/{id}/deactivate`   | Block authentication                                                   |
| `POST`   | `/users/{id}/roles`        | Assign roles                                                           |
| `DELETE` | `/users/{id}/roles`        | Remove roles                                                           |

### Role Management (`/roles`) — Requires JWT

| Method   | Route                      | Notes                               |
| -------- | -------------------------- | ----------------------------------- |
| `GET`    | `/roles`                   | List with tenant and active filters |
| `GET`    | `/roles/{id}`              | Single role                         |
| `GET`    | `/roles/tenant/{tenantId}` | Roles by tenant                     |
| `POST`   | `/roles`                   | Create role                         |
| `PUT`    | `/roles/{id}`              | Update metadata                     |
| `DELETE` | `/roles/{id}`              | Delete role                         |
| `POST`   | `/roles/{id}/permissions`  | Assign permissions                  |
| `DELETE` | `/roles/{id}/permissions`  | Remove permissions                  |

### Permission Management (`/permissions`) — Requires JWT

| Method   | Route                            | Notes                               |
| -------- | -------------------------------- | ----------------------------------- |
| `GET`    | `/permissions`                   | List with tenant and active filters |
| `GET`    | `/permissions/{id}`              | Single permission                   |
| `GET`    | `/permissions/tenant/{tenantId}` | Permissions by tenant               |
| `POST`   | `/permissions`                   | Create                              |
| `PUT`    | `/permissions/{id}`              | Update                              |
| `DELETE` | `/permissions/{id}`              | Delete                              |

### OAuth Client Management (`/clients`) — Requires JWT

| Method   | Route                         | Notes                                                                        |
| -------- | ----------------------------- | ---------------------------------------------------------------------------- |
| `GET`    | `/clients`                    | List with optional tenant/search/paging                                      |
| `GET`    | `/clients/{id}`               | Single client                                                                |
| `GET`    | `/clients/tenant/{tenantId}`  | Clients by tenant                                                            |
| `POST`   | `/clients`                    | Register new client; returns plain-text secret once for confidential clients |
| `PUT`    | `/clients/{id}`               | Update metadata, grant types, redirect/logout URIs                           |
| `DELETE` | `/clients/{id}`               | Delete client                                                                |
| `POST`   | `/clients/{id}/rotate-secret` | Rotate confidential client secret                                            |
| `POST`   | `/clients/{id}/activate`      | Allow protocol usage                                                         |
| `POST`   | `/clients/{id}/deactivate`    | Block protocol usage                                                         |

### Scope Management (`/scopes`) — Requires JWT

| Method   | Route          | Notes                                         |
| -------- | -------------- | --------------------------------------------- |
| `GET`    | `/scopes`      | List with active filter                       |
| `GET`    | `/scopes/{id}` | Single scope                                  |
| `POST`   | `/scopes`      | Create scope                                  |
| `PUT`    | `/scopes/{id}` | Update scope                                  |
| `DELETE` | `/scopes/{id}` | Delete (domain rules protect built-in scopes) |

### API Key Management (`/api-keys`) — Requires JWT

| Method   | Route                         | Notes                                                         |
| -------- | ----------------------------- | ------------------------------------------------------------- |
| `GET`    | `/api-keys`                   | Keys owned by current user                                    |
| `GET`    | `/api-keys/{id}`              | By identifier                                                 |
| `GET`    | `/api-keys/client/{clientId}` | Keys by client                                                |
| `POST`   | `/api-keys`                   | Create; plain-text key returned once                          |
| `DELETE` | `/api-keys/{id}`              | Revoke                                                        |
| `POST`   | `/api-keys/validate`          | **Anonymous.** Validate key string for service-to-service use |

### Console

| Method | Route                       | Auth     | Notes            |
| ------ | --------------------------- | -------- | ---------------- |
| `GET`  | `/console/api/console/{id}` | Required | Get tenant by ID |

## OAuth 2.1 Grant Support

| Grant Type           | Supported | Notes                                                                                     |
| -------------------- | --------- | ----------------------------------------------------------------------------------------- |
| `authorization_code` | ✅        | PKCE S256 mandatory for public clients; also enforced for confidential clients            |
| `client_credentials` | ✅        | Confidential clients only; no refresh token issued; OIDC/`offline_access` scopes rejected |
| `refresh_token`      | ✅        | Rotation on every use; family cascade-revocation on reuse detection                       |
| `password`           | ❌        | Explicitly rejected per OAuth 2.1 removal                                                 |
| `implicit`           | ❌        | `response_type=token` rejected before redirect URI is evaluated                           |
| `device_code`        | ❌        | Not implemented                                                                           |

## Security Posture

### Token Security

- RS256 (RSA-SHA256) signing; minimum 2048-bit key validated at startup.
- `typ: at+JWT` header on all access tokens (RFC 9068); `ValidTypes = ["at+JWT"]` on validation.
- Algorithm restriction to `RS256` — prevents `alg:none` and HS256 confusion attacks.
- `ClockSkew = TimeSpan.Zero` — no grace window extending effective token lifetime.
- Access token max lifetime capped at 900 seconds (15 minutes) regardless of client configuration.
- JWT revocation checked on every protected request via `OnTokenValidated` (revocation table + soft-delete column).
- JWKS `kid` derived from a deterministic hash of the public key material.

### PKCE and Authorization Code

- S256 is the only accepted method; `plain` is explicitly rejected.
- Authorization code lifetime: 60 seconds.
- Atomic one-time consume via `TryConsumeAsync` preventing replay under concurrency.
- `redirect_uri` exact-match against registered list using `StringComparison.Ordinal`.
- Error responses are only redirected after `redirect_uri` is validated (RFC 6749 §4.1.2.1).

### Client Authentication

- Public clients: no secret allowed (rejects secret if presented).
- Confidential clients: `client_secret_basic` or `client_secret_post`; bcrypt constant-time comparison.
- Client secret expiry enforced.

### Password and Account Security

- bcrypt with workFactor 12.
- Timing-normalization dummy verify for user-not-found and no-identity paths.
- Failed attempt tracking per IP (≥ 10 in window → rate-limit) and per username (lockout after 5 failures).
- Account lock applied to `LockedUntil` column, checked before password verification.

### Rate Limiting

- `default` policy: 60 requests/minute per TCP-level remote IP.
- `auth` policy: 10 requests/minute per IP — applied to token, revoke, and introspect endpoints.
- Partition key uses `RemoteIpAddress` (TCP level) to resist X-Forwarded-For spoofing from non-proxy sources.

### HTTP Security

- HSTS: `max-age=365d; includeSubDomains` (skipped in Development).
- `SameSite=Lax; HttpOnly=Always; Secure=Always` cookie policy.
- Security response headers: `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`, `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=(), usb=()`, `X-XSS-Protection: 0`.
- CORS deny-by-default when `Cors:AllowedOrigins` is empty; no wildcard origins.
- Consent submit endpoint (`POST /oauth2/authorize/consent`) enforces same-origin check for browser/cookie requests.
- Header injection prevention: `WWW-Authenticate` error descriptions are sanitized (CR/LF/quote stripped).

### Refresh Token

- Rotation on every use; new token issues atomically via `TryRevokeWithRotationAsync`.
- Family-based cascade revocation on reuse detection.
- Issued only when `offline_access` scope is requested.
- Scope downscoping permitted on refresh (RFC 6749 §6); scope expansion rejected.

## Known Pre-Production Issues

The following items were identified in the April 2026 security audit and must be resolved before deployment:

| Priority    | Issue                                                                                                                                                                                                                                                                                        |
| ----------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 🔴 Critical | `appsettings.Development.json` and `launchSettings.json` contain hardcoded production database credentials. Rotate the password and provision via `dotnet user-secrets` or environment variables. Add both files to `.gitignore`.                                                            |
| 🔴 Critical | `AllowedHosts: "localhost"` in `appsettings.json` causes HTTP 400 for all production requests. Override with the real hostname or `"*"` in the production config.                                                                                                                            |
| 🟠 High     | `UseForwardedHeaders()` is not registered. Behind any reverse proxy (nginx, AWS ALB, Azure App Gateway), rate limiting and discovery document URLs will use the proxy's internal IP/scheme instead of the real client IP and external URL.                                                   |
| 🟠 High     | `MemoryTwoFactorChallengeStore` is in-process memory. On multi-instance deployments (Kubernetes, ECS, etc.), 2FA challenge tokens are not shared across pods. Replace with a Redis-backed distributed implementation.                                                                        |
| 🟡 Medium   | `/auth/login`, `/auth/register`, `/auth/forgot-password`, `/auth/reset-password`, and `/auth/refresh` use the `"default"` rate limit policy (60/min) instead of `"auth"` (10/min). These endpoints are at least as sensitive as the OAuth token endpoint.                                    |
| 🟡 Medium   | Discovery document endpoint URLs are built from `Request.Scheme`/`Request.Host`. Without `UseForwardedHeaders()`, these will be `http://internal-host` instead of the configured `AUTH_ISSUER` URL. Use `AUTH_ISSUER` as the base URL for all endpoint URL fields in the discovery document. |
| 🟡 Medium   | `IssueAuthorizationCodeAsync` passes `null` for `sessionId`, breaking per-session token revocation linkage.                                                                                                                                                                                  |
| 🔵 Low      | `Cache-Control: no-store` is missing on successful UserInfo, end-session, and authorize flow responses.                                                                                                                                                                                      |
| 🔵 Low      | `ARGON2_SECRET`, `HASH_SECRET_KEY`, `HASH_SECRET_IV` are `[Required]` in `EnvironmentOptions` but unused in the codebase. Remove or document.                                                                                                                                                |
| 🔵 Low      | Swagger is gated only by `IsDevelopment()`. Add an explicit `SwaggerOptions:Enabled` config flag that defaults to `false`.                                                                                                                                                                   |

## Configuration

All configuration is bound from environment variables via `EnvironmentOptions` (`src/Infrastructure/Configurations/EnvironmentOptions.cs`). Missing required values will cause startup to fail.

### Required Variables

| Variable                                | Description                                                                     |
| --------------------------------------- | ------------------------------------------------------------------------------- |
| `POSTGRES_CONNECTIONSTRING`             | PostgreSQL connection string                                                    |
| `REDIS_CONNECTIONSTRING`                | Redis connection string                                                         |
| `AUTH_ISSUER`                           | Issuer URL embedded in tokens (e.g. `https://auth.example.com`)                 |
| `OAUTH2_SIGNED_PRIVATE_KEY`             | PEM-encoded RSA private key (≥ 2048 bits)                                       |
| `TOKEN_EXPIRES_IN_MINUTES`              | Default access token lifetime in minutes (capped to 15 min / 900 s)             |
| `OAUTH2_AUDIENCE`                       | Token `aud` claim for resource servers (falls back to `AUTH_ISSUER` if not set) |
| `BASIC_AUTHENTICATION_USERNAME`         | Admin basic auth username                                                       |
| `BASIC_AUTHENTICATION_PASSWORD`         | Admin basic auth password                                                       |
| `SIGNIN_FAILED_IN_MINUTES`              | Sliding window for failed login attempt counting and account lock duration      |
| `BLOCK_IP_ADDRESS_IN_MINUTES`           | IP block duration after threshold exceeded                                      |
| `ARGON2_SECRET`                         | Legacy — currently unused; keep to avoid startup failure                        |
| `HASH_SECRET_KEY`                       | Legacy — currently unused; keep to avoid startup failure                        |
| `HASH_SECRET_IV`                        | Legacy — currently unused; keep to avoid startup failure                        |
| `NOTIFICATION_ENDPOINT`                 | Internal notification service URL                                               |
| `BP_API_ENDPOINT`                       | Business process API URL                                                        |
| `RESOURCE_API_ENDPOINT`                 | Resource API URL                                                                |
| `AUTH_JWKS_ENDPOINT`                    | JWKS endpoint URL advertised to downstream services                             |
| `ACCOUNT_TOYO_ENDPOINT`                 | Account service URL                                                             |
| `CLIENT_TOYO_FORGOT_PASSWORD_ENDPOINT`  | Forgot-password redirect path                                                   |
| `ACCOUNT_DELETION_GRACE_PERIOD_DAYS`    | Grace period before permanent deletion                                          |
| `GOOGLE_API_AUTH_KEY`                   | Google API key                                                                  |
| `GOOGLE_API_AUTH_ENDPOINT`              | Google API base URL                                                             |
| `GOOGLE_OAUTH2_API_AUTH_ENDPOINT`       | Google OAuth2 token endpoint                                                    |
| `GOOGLE_APPLICATION_CREDENTIALS_AUTH`   | Google service account credentials                                              |
| `FACEBOOK_API_ENDPOINT`                 | Facebook Graph API URL                                                          |
| `APPLE_API_ENDPOINT`                    | Apple ID endpoint                                                               |
| `AMQP_HOST`                             | RabbitMQ host                                                                   |
| `AMQP_PORT`                             | RabbitMQ port                                                                   |
| `AMQP_USERNAME`                         | RabbitMQ username                                                               |
| `AMQP_PASSWORD`                         | RabbitMQ password                                                               |
| `AMQP_SYNC_DELETE_ACCOUNT_EXCHANGE_KEY` | Exchange key for delete-account sync events                                     |

**Never commit sensitive values.** Use `dotnet user-secrets` locally or container/cloud secret management in all other environments.

### Optional Variables

| Variable          | Default       | Description                                  |
| ----------------- | ------------- | -------------------------------------------- |
| `OAUTH2_AUDIENCE` | `AUTH_ISSUER` | Separate audience claim for resource servers |

### CORS

Add allowed origins under `Cors:AllowedOrigins` in the configuration file or as an array environment variable:

```json
{
  "Cors": {
    "AllowedOrigins": ["https://app.example.com"]
  }
}
```

## Local Development

### Prerequisites

- .NET SDK 9.x
- PostgreSQL (or Docker)
- Redis (or Docker)
- PowerShell 7+ (for migration helper script)

### Run API

```bash
dotnet restore
dotnet build MonkoraEdge.Core.Auth.sln
dotnet run --project src/API/API.csproj --launch-profile API
```

Default URL: `http://localhost:5001`

### Swagger

Swagger UI is available at `http://localhost:5001/swagger` when `ASPNETCORE_ENVIRONMENT=Development`.

### Set Secrets Locally (Recommended)

```bash
cd src/API
dotnet user-secrets set "POSTGRES_CONNECTIONSTRING" "Host=localhost;Port=5432;User ID=...;Password=...;Database=auth-dev;"
dotnet user-secrets set "OAUTH2_SIGNED_PRIVATE_KEY" "-----BEGIN RSA PRIVATE KEY-----\n..."
dotnet user-secrets set "REDIS_CONNECTIONSTRING" "localhost:6379"
```

## Database and EF Core

Migrations are in `src/Infrastructure/Migrations`.

### Apply migrations

```bash
dotnet ef database update \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/API/API.csproj
```

### Create a migration

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/API/API.csproj \
  --output-dir Migrations
```

### Seed migration (PowerShell helper)

```powershell
pwsh ./src/Infrastructure/Add-SeedMigration.ps1 -MigrationName SeedMasterDataFromScriptSql -EnvironmentName Development
```

### Bootstrap SQL

Reference SQL scripts are under `src/API/script_sql/` and are numbered in dependency order:

```
#01 mt_tenants.sql
#02 mt_users.sql
#03 tx_user_files.sql
...
#10 mt_authorization_clients.sql
#11 tx_user_sessions.sql
#12 mt_scopes.sql
...
```

## Postman

- Collection: `postman/MonkoraEdge.Core.Auth.postman_collection.json`
- Local environment: `postman/MonkoraEdge.Core.Auth.local.postman_environment.json`

### Collection Variables

| Variable | Description | Auto-saved by |
|----------|-------------|---------------|
| `baseUrl` | API base URL | — |
| `bearerToken` | Current access token | Login, Token requests |
| `refreshToken` | Current refresh token | Login, Token requests |
| `idToken` | OIDC ID token | Token - Authorization Code |
| `twoFactorToken` | 2FA challenge token from Login | Login (when 2FA required) |
| `basicAuth` | Base64 `clientId:clientSecret` | Revoke, Introspect (pre-request) |
| `codeVerifier` | PKCE code verifier | Authorize GET/POST (pre-request) |
| `codeChallenge` | PKCE S256 challenge | Authorize GET/POST (pre-request) |
| `clientId` | OAuth client_id string | — |
| `clientSecret` | OAuth client secret | — |
| `authorizationCode` | Authorization code to exchange | — |
| `redirectUri` | Client callback URL | — |
| `oauthScopeWithOfflineAccess` | Scope for code flow | — |
| `clientCredentialsScope` | Scope for client_credentials | — |
| `tenantId`, `userId`, `roleId`, etc. | ID placeholders for management APIs | — |

### Key Notes

- **PKCE auto-generation**: `Authorize GET` and `Authorize POST` run a pre-request script that generates a fresh `codeVerifier`/`codeChallenge` (S256) on every send.
- **Token auto-save**: All token-issuing requests (Login, Token - Authorization Code, Refresh Grant, Client Credentials) save `bearerToken` and `refreshToken` via test scripts.
- **2FA flow**: When Login returns `requires_two_factor: true`, the `twoFactorToken` is auto-saved. Pass it in `POST /auth/2fa/verify` to complete login. Do not supply `userId` — the server resolves the user from the opaque challenge token.
- **client_credentials scope**: OIDC scopes (`openid`, `profile`, `email`, `phone`) and `offline_access` are explicitly rejected for the `client_credentials` grant by the server. Set `clientCredentialsScope` to a resource-level scope registered for the client (e.g. `api.access`, `orders.read`).
- **Revoke / Introspect**: Pre-request scripts build `Authorization: Basic` from `clientId`/`clientSecret` automatically.

## Packages (Key Dependencies)

| Package                                           | Version          | Purpose                                |
| ------------------------------------------------- | ---------------- | -------------------------------------- |
| `Asp.Versioning.Http`                             | 8.1.0            | API versioning via header/query-string |
| `AspNetCore.HealthChecks.NpgSql`                  | 8.0.2            | PostgreSQL health check                |
| `AspNetCore.HealthChecks.Redis`                   | 8.0.1            | Redis health check                     |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | 9.0.10           | Redis distributed cache                |
| `Serilog.AspNetCore`                              | 9.0.0            | Structured logging                     |
| `Swashbuckle.AspNetCore`                          | 9.0.6            | Swagger/OpenAPI                        |
| `BCrypt.Net-Next`                                 | (Infrastructure) | Password hashing (workFactor 12)       |
| `OtpNet`                                          | (Infrastructure) | TOTP generation and verification       |
| `Microsoft.IdentityModel.Tokens`                  | (Infrastructure) | JWT signing, RS256                     |

## Maintenance Workflow

1. Check this README before making architectural or behavioral changes.
2. **After every code change — endpoint routes, flow behavior, security posture, configuration keys, infrastructure dependencies, Postman — update this README in the same task.**
3. Do not assume this README is accurate after code changes — keep it synchronized with the live implementation.

## Related Docs

- `API_TEMPLATE.md`
- `PROMPT_BP_API.md`
- `PROMPT_NOTIFICATION_API.md`
