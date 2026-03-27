# MonkoraEdge.Core.Auth API

Core Authentication API for MonkoraEdge Platform.

- Company: Monkora Co., Ltd.
- Author: Boonhome Wongsuwan
- Current version: 1.0.0
- Framework: .NET 9.0
- Last updated: 2026-03-27

## Current Status (2026-03)

- OAuth 2.1 Authorization Server flow is implemented in Domain orchestration service (`IOAuth2Service`).
- Authorization Code + PKCE (`S256`) is enforced.
- Refresh token rotation and token reuse detection are implemented.
- OpenID Connect / OAuth metadata endpoints are available.
- API layer is focused on transport concerns (bind request, auth context, HTTP response).
- Domain exceptions are translated by middleware into HTTP response shape.
- This README is the source of truth for high-level architecture and maintenance workflow.

## Solution Structure

```text
core-auth-api/
|- README.md
|- MonkoraEdge.Core.Auth.sln
`- src/
   |- API/             # ASP.NET Core Web API entry point
   |- Domain/          # Business rules and use-case orchestration
   |- Infrastructure/  # EF Core, repositories, cryptography, external adapters
   `- DotNet/          # Shared cross-cutting building blocks
```

## Runtime Architecture

- API
  - Hosts HTTP endpoints and middleware pipeline.
  - Applies CORS policy from `Cors:AllowedOrigins`.
  - Exposes health checks at `/health/ready` and `/health/live`.
- Domain
  - Owns authentication and OAuth behaviors.
  - Uses repository and unit-of-work abstractions defined for domain usage.
- Infrastructure
  - Implements persistence with EF Core + PostgreSQL.
  - Implements JWT/JWKS/token and password/PKCE security services.
- DotNet
  - Shared abstractions and utilities used by multiple projects.

## Key Endpoints (High Level)

### Discovery and Health

- `GET /.well-known/openid-configuration`
- `GET /.well-known/oauth-authorization-server`
- `GET /.well-known/jwks.json`
- `GET /health/ready`
- `GET /health/live`

### OAuth2 / OIDC

- `GET|POST /oauth2/authorize`
- `POST /oauth2/authorize/consent`
- `POST /oauth2/token`
- `POST /oauth2/revoke`
- `POST /oauth2/introspect`
- `GET /oauth2/userinfo`
- `GET|POST /oauth2/end-session`

### Authentication

- `POST /auth/login`
- `POST /auth/register`
- `POST /auth/logout`
- `POST /auth/refresh` (compatibility facade to `grant_type=refresh_token`)
- `POST /auth/forgot-password`
- `POST /auth/reset-password`
- `POST /auth/change-password`
- `POST /auth/verify-email`
- `POST /auth/resend-verification`
- `GET /auth/2fa/setup`
- `POST /auth/2fa/enable`
- `POST /auth/2fa/disable`
- `POST /auth/2fa/verify`

### Management APIs (Authorized)

- `/users`
- `/roles`
- `/permissions`
- `/clients`
- `/scopes`
- `/api-keys`

See controller files in `src/API/Controllers` for full route details.

## Security Posture

- PKCE `S256` required for Authorization Code flow.
- Redirect URI exact-match validation.
- Confidential clients require `client_secret`.
- Refresh token rotation with family-aware revocation support.
- Refresh token issuance on OAuth token endpoint now requires `offline_access` scope.
- Atomic one-time consume checks are applied for authorization codes and refresh token rotation.
- Token revocation is restricted to tokens owned by the authenticated client.
- Runtime rate limiting is enabled and enforced on high-risk OAuth endpoints.
- Consent submit endpoint enforces same-origin checks for browser/cookie contexts.
- Security headers are set in API middleware pipeline.
- CORS is deny-by-default when `Cors:AllowedOrigins` is empty.

## Configuration

`EnvironmentOptions` is bound from configuration root. Missing required values can fail startup.

Minimum important keys:

- `POSTGRES_CONNECTIONSTRING`
- `REDIS_CONNECTIONSTRING`
- `AUTH_ISSUER`
- `OAUTH2_SIGNED_PRIVATE_KEY`
- `TOKEN_EXPIRES_IN_MINUTES`
- `BASIC_AUTHENTICATION_USERNAME`
- `BASIC_AUTHENTICATION_PASSWORD`
- `ARGON2_SECRET`
- `HASH_SECRET_KEY`
- `HASH_SECRET_IV`

Additional external integration keys are also required depending on environment (Google, notification, BP API, AMQP, etc.) and are defined in:

- `src/Infrastructure/Configurations/EnvironmentOptions.cs`

Use environment variables or secret store for sensitive values.

## Local Development

### Prerequisites

- .NET SDK 9.x
- PostgreSQL
- Redis
- PowerShell (for helper migration script)

### Run API

From repository root:

```bash
dotnet restore
dotnet build MonkoraEdge.Core.Auth.sln
dotnet run --project src/API/API.csproj --launch-profile API
```

Default launch profile URL is configured in `src/API/Properties/launchSettings.json`.

### Swagger

- `/swagger` is enabled in Development environment.

## Database and EF Core

Migrations are in `src/Infrastructure/Migrations`.

Run database update:

```bash
dotnet ef database update --project src/Infrastructure/Infrastructure.csproj --startup-project src/API/API.csproj
```

Create a migration:

```bash
dotnet ef migrations add <MigrationName> --project src/Infrastructure/Infrastructure.csproj --startup-project src/API/API.csproj --output-dir Migrations
```

Seed migration helper script (PowerShell):

```powershell
pwsh ./src/Infrastructure/Add-SeedMigration.ps1 -MigrationName SeedMasterDataFromScriptSql -EnvironmentName Development
```

Bootstrap SQL is under:

- `src/API/script_sql`

## Postman

- Collection: `postman/MonkoraEdge.Core.Auth.postman_collection.json`
- Local env: `postman/MonkoraEdge.Core.Auth.local.postman_environment.json`

## Maintenance Workflow

1. Check this README before making architectural or behavior changes.
2. When endpoint, flow, security behavior, dependency, or run/migration workflow changes, update README in the same task.
3. Do not assume README is accurate after code changes. Keep it synchronized with current implementation.

## Related Docs

- `API_TEMPLATE.md`
- `PROMPT_BP_API.md`
- `PROMPT_NOTIFICATION_API.md`
