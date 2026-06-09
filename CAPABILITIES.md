# MonkoraEdge Core Auth API — ประเมินความสามารถ

> เวอร์ชัน 1.0.0 · .NET 8 · อัปเดต 2026-06-09

---

## สรุปภาพรวม

MonkoraEdge Core Auth API เป็น **Enterprise-grade OAuth 2.1 Authorization Server** ที่รองรับมาตรฐาน OAuth 2.0/2.1, OIDC Core 1.0 และ RFC สำคัญครบชุด พร้อมระบบ IAM (Identity & Access Management) หลายผู้เช่า (Multi-Tenant) และระบบ Security ระดับ Production

ระบบนี้ออกแบบมาเพื่อรองรับ Monkora Platform ที่มีหลาย Tenant และหลาย Application ใช้งานร่วมกัน โดย Authorization Server ทำหน้าที่เป็นศูนย์กลาง Identity สำหรับทุก Service ใน Ecosystem ไม่ว่าจะเป็น Web App, Mobile App, IoT Device, หรือ Machine-to-Machine (M2M) communication

---

## สรุปคะแนนรวม (ดูรายละเอียดแต่ละหมวดด้านล่าง)

| # | หมวด | คะแนน | สรุปสั้น |
|---|---|---|---|
| 1 | OAuth 2.1 / OIDC Core | ⭐⭐⭐⭐⭐ | ครบทุก RFC, DPoP, PAR, Device Grant |
| 2 | Authentication | ⭐⭐⭐⭐⭐ | Passkeys, Magic Link, TOTP, Social Login |
| 3 | SAML 2.0 SP | ⭐⭐⭐⭐ | SP side ครบ, ยังไม่รองรับ IdP side |
| 4 | IAM Multi-Tenant | ⭐⭐⭐⭐⭐ | RBAC granular, Agreement, Webhook |
| 5 | Security Architecture | ⭐⭐⭐⭐⭐ | OWASP Top 10 ครบ, theft detection |
| 6 | Infrastructure & Reliability | ⭐⭐⭐⭐ | Production-ready แต่ขาด OpenTelemetry |
| 7 | API & Developer Experience | ⭐⭐⭐⭐ | Postman 135 requests, Swagger |
| 8 | Standards & RFC Compliance | ⭐⭐⭐⭐⭐ | 14 RFC/Spec ครบ |
| | **คะแนนเฉลี่ยรวม** | **⭐⭐⭐⭐½ (4.6/5)** | |

---

## 1. OAuth 2.1 / OIDC Core ⭐⭐⭐⭐⭐

### 1.1 Authorization Code + PKCE (S256)

**คืออะไร:** Authorization Code Flow เป็น grant type หลักที่ใช้สำหรับ application ที่มี user interaction (Web App, SPA, Mobile App) PKCE (Proof Key for Code Exchange) เป็นกลไกป้องกัน authorization code interception attack โดยใช้ cryptographic challenge

**การ implement:**
- บังคับ `code_challenge_method=S256` เท่านั้น — ห้ามใช้ `plain` method (ตาม OAuth 2.1 draft §2.1.1)
- `code_challenge = BASE64URL(SHA256(ASCII(code_verifier)))` — verify ใน token endpoint
- Authorization code จัดเก็บในตาราง `tx_authorization_codes` พร้อม PKCE challenge, nonce, redirect_uri, scope ที่ approved
- Code มีอายุสั้น (default 5 นาที) และใช้ได้ครั้งเดียว — ป้องกัน replay attack

**ทำไมสำคัญ:** ป้องกัน CSRF, authorization code interception, และ redirect URI manipulation ซึ่งเป็น 3 attack vectors หลักของ OAuth 2.0 original

---

### 1.2 Client Credentials Grant

**คืออะไร:** Grant type สำหรับ Machine-to-Machine (M2M) communication ที่ไม่มี user interaction เช่น background service, microservice-to-microservice call, daemon

**การ implement:**
- Client authenticate ด้วย `client_id` + `client_secret` (HTTP Basic Auth หรือ POST body)
- **OIDC scope ถูก reject** (`openid`, `profile`, `email`, `phone`, `offline_access`) — เพราะไม่มี user context ตาม spec
- ออก access token เป็น `at+JWT` พร้อม `sub = client_id` (ไม่ใช่ user ID)
- รองรับ resource scope เช่น `api.access`, `reports.read` ที่ admin กำหนดใน OAuth client config

**ทำไมสำคัญ:** หลาย auth server ที่ implement ผิด allow OIDC scope ใน CC grant ทำให้ token มี claims ที่ไม่มีความหมาย ระบบนี้ reject อย่าง explicit พร้อม error message ที่ชัดเจน

---

### 1.3 Refresh Token Rotation + Theft Detection

**คืออะไร:** Refresh token ใช้สำหรับขอ access token ใหม่โดยไม่ต้องให้ user login ซ้ำ Rotation หมายถึงทุกครั้งที่ใช้ refresh token จะได้ token ใหม่และ token เก่าถูก revoke ทันที

**การ implement:**
- **Family-based chain:** ทุก refresh token มี `family_id` ที่ link กับ original authorization — ทำให้ track สาย rotation ได้ทั้งหมด
- **Theft Detection:** ถ้า refresh token ที่ถูก revoke ไปแล้วถูกนำมาใช้อีกครั้ง (reuse detection) ระบบ **revoke ทุก token ในสาย family เดิมทันที** — บังคับให้ user login ใหม่
- **Cascade Revocation:** `RefreshTokenProcessor` ใน Domain layer จัดการ logic นี้ทั้งหมด ทำให้ OAuth endpoint และ `/auth/refresh` endpoint มีพฤติกรรม identical
- Token เก็บใน `tx_authorization_refresh_tokens` พร้อม `family_id`, `replaced_by_id`, `revoked_at`, `revoke_reason`

**ทำไมสำคัญ:** ป้องกัน token replay attack กรณีที่ refresh token รั่วไหล เพราะ attacker ที่ใช้ token จะ trigger revocation ของทุก session ในสาย นั่นคือ legitimate user ก็ถูก force logout พร้อมกัน — alert ว่า account ถูก compromise

---

### 1.4 Device Authorization Grant (RFC 8628)

**คืออะไร:** Grant type สำหรับ device ที่ input-constrained (Smart TV, IoT, CLI tool) ที่ไม่สามารถเปิด browser ได้ง่าย โดย device แสดง `user_code` ให้ user ไปกรอกใน browser บน device อื่น

**การ implement:**
- `POST /oauth2/device_authorization` — device ขอ `device_code` + `user_code` + `verification_uri`
- Device poll `POST /oauth2/token` ด้วย `grant_type=urn:ietf:params:oauth:grant-type:device_code` รอจนกว่า user authorize
- User ไปที่ `GET /device` เพื่อดู verification page, กรอก `user_code`, แล้ว approve ที่ `POST /device`
- Admin approve/deny ที่ `GET /oauth2/device_approval`
- Polling interval และ expiry config ได้ในแต่ละ client

**ทำไมสำคัญ:** รองรับ use case IoT และ Smart TV ซึ่งมีความต้องการเพิ่มขึ้นใน Enterprise — auth server ส่วนใหญ่ไม่รองรับ grant type นี้

---

### 1.5 Pushed Authorization Requests — PAR (RFC 9126)

**คืออะไร:** แทนที่จะส่ง authorization parameters ใน URL query string (ซึ่ง expose ใน browser history, logs, referrer headers) PAR ให้ client ส่ง parameters ผ่าน `POST` ไปก่อน แล้วได้ `request_uri` กลับมาใช้แทน

**การ implement:**
- `POST /oauth2/par` — client ส่ง authorization parameters ทั้งหมดรวมถึง PKCE challenge และ scope
- Server เก็บ parameters ชั่วคราว (30 วินาที) และคืน `request_uri = urn:ietf:params:oauth:request_uri:{uuid}`
- Client redirect user ไป `GET /oauth2/authorize?request_uri=urn:...&client_id=...` เท่านั้น
- Server fetch parameters จาก temporary store แทนที่จะอ่านจาก query string

**ทำไมสำคัญ:** ป้องกัน sensitive parameters (scope, redirect_uri, state, code_challenge) รั่วไหลใน URL ที่ browser อาจ cache หรือ log ไว้ เหมาะสำหรับ high-security application เช่น banking, healthcare

---

### 1.6 Dynamic Client Registration (RFC 7591)

**คืออะไร:** Client (application) สามารถ register ตัวเองกับ Authorization Server แบบ programmatic ได้โดยไม่ต้องให้ admin สร้างให้ด้วยมือ

**การ implement:**
- `POST /oauth2/register` — ส่ง metadata (client_name, redirect_uris, grant_types, response_types, token_endpoint_auth_method)
- Server validate metadata ตาม RFC 7591 และสร้าง `client_id` + `client_secret` (สำหรับ confidential client)
- Client secret ถูกส่งกลับครั้งเดียวเท่านั้น (ไม่ store ใน plaintext, เก็บ hash)
- รองรับ `software_statement` สำหรับ pre-authorized client identity

**ทำไมสำคัญ:** รองรับ SaaS model ที่ tenant ใหม่ต้องการ register OAuth client อัตโนมัติระหว่าง onboarding — ไม่ต้องมี human step ในกระบวนการ

---

### 1.7 Token Introspection (RFC 7662)

**คืออะไร:** Resource Server (API) ที่ได้รับ token สามารถ verify token validity โดยถาม Authorization Server แทนที่จะ validate token เอง — เหมาะสำหรับ opaque token หรือเมื่อต้องการ real-time revocation check

**การ implement:**
- `POST /oauth2/introspect` — ส่ง `token` + `token_type_hint`
- Response มี RFC-compliant fields: `active`, `sub`, `client_id`, `scope`, `exp`, `iat`, `jti`, `token_type`
- Check revocation list ใน DB ก่อน return `active: true`
- Protected ด้วย client authentication (ป้องกัน unauthorized token enumeration)

---

### 1.8 Token Revocation (RFC 7009)

**คืออะไร:** Client หรือ Resource Server สามารถ revoke token (access token หรือ refresh token) ได้ทันที เช่น เมื่อ user logout หรือ detect suspicious activity

**การ implement:**
- `POST /oauth2/revoke` — ส่ง `token` + optional `token_type_hint`
- ถ้า revoke refresh token → revoke access tokens ที่เกี่ยวข้องทั้งหมดในสายด้วย
- บันทึกใน `tx_revoked_tokens` พร้อม `revoke_reason`: `LOGOUT`, `ROTATION`, `ADMIN_REVOKE`, `SECURITY`
- `OnTokenValidated` JWT event hook check revocation list ทุก request — ทำให้ revocation มีผลทันที แม้ access token ยังไม่ expire

---

### 1.9 DPoP Token Binding (RFC 9449)

**คืออะไร:** Demonstrating Proof-of-Possession (DPoP) ผูก access token กับ cryptographic key pair ของ client ทำให้ token ถูกขโมยไปใช้บน device อื่นไม่ได้ เพราะต้องมี private key ที่ตรงกันด้วย

**การ implement:**
- Client สร้าง ephemeral key pair และส่ง DPoP proof JWT ใน `DPoP` header ทุก request
- Server verify DPoP proof (signature, `htu`, `htm`, `iat`, `jti` replay prevention) ก่อน issue token
- Token มี `token_type: DPoP` แทน `Bearer` ใน response
- รองรับทั้ง Bearer fallback และ DPoP binding ขึ้นอยู่กับ client capability

**ทำไมสำคัญ:** แก้ปัญหา "Bearer token = whoever holds it can use it" โดยเพิ่ม cryptographic sender-binding ทำให้ token ถูก steal แล้วก็ใช้ไม่ได้

---

### 1.10 `at+JWT` Access Token (RFC 9068)

**คืออะไร:** กำหนด profile มาตรฐานสำหรับ access token ที่เป็น JWT รวมถึง `typ` header ที่ต้องเป็น `at+JWT`

**การ implement:**
- `TokenService` บังคับ `typ: at+JWT` ใน JOSE header ทุก access token
- Resource Server ที่ validate ต้อง reject token ที่ `typ != at+JWT` — ป้องกัน ID token ถูกใช้เป็น access token (confused deputy attack)
- RS256 signing ด้วย RSA key ≥ 2048 bits (validate ณ startup)

---

### 1.11 OIDC Discovery + JWKS

**คืออะไร:** Discovery document ช่วยให้ client ค้นหา endpoint URLs, signing keys, และ supported features ได้อัตโนมัติโดยไม่ต้อง hard-code

**การ implement:**
- `GET /.well-known/openid-configuration` — OIDC Discovery (RFC 8414)
- `GET /.well-known/oauth-authorization-server` — OAuth 2.0 AS Metadata (RFC 8414)
- `GET /.well-known/jwks.json` — JSON Web Key Set สำหรับ verify JWT signature
- JWKS ประกอบด้วย public key ในรูป `{ kty, use, alg, kid, n, e }` — ทุก JWT มี `kid` header ตรงกัน

---

### 1.12 UserInfo Endpoint

**คืออะไร:** Protected resource endpoint ที่ client ใช้ดึงข้อมูล user claims เพิ่มเติม หลังจากได้ access token ที่มี `openid` scope

**การ implement:**
- `GET /oauth2/userinfo` — Bearer access token required
- Return claims ตาม scope ที่ได้รับอนุญาต: `sub`, `name`, `email`, `email_verified`, `phone_number`, `picture`, `locale`, `zoneinfo`
- Enforce ว่า token ต้องมี `openid` scope — ไม่ใช่แค่ valid JWT

---

### 1.13 Consent Flow

**คืออะไร:** เมื่อ application ขอ scope ที่ user ยังไม่เคย consent มาก่อน ระบบจะแสดงหน้า consent ให้ user approve หรือ deny scope แต่ละตัว

**การ implement:**
- Authorization flow detect consent requirement โดยเปรียบเทียบ requested scope กับ `tx_authorization_consents` ของ user + client นั้น
- `POST /oauth2/authorize/consent` — user submit consent decision พร้อม approved/denied scope list
- Consent บันทึกพร้อม expiry และสถานะ revoked
- `offline_access` scope ต้องการ explicit consent (ตาม OIDC spec)

---

## 2. Authentication (การยืนยันตัวตน) ⭐⭐⭐⭐⭐

### 2.1 Username / Password Login

**คืออะไร:** Traditional credential-based authentication ที่ user ใช้ email/username + password เพื่อ login

**การ implement:**
- `POST /auth/login` — ส่ง `username`, `password`, optional `tenantId`
- Password hash ด้วย **bcrypt workFactor 12** — ทนทานต่อ brute force attack แม้ DB รั่วไหล
- **Timing-safe verify:** ทุก login attempt (รวม invalid username) ทำ `PasswordService.DummyVerify()` เพื่อให้ response time สม่ำเสมอ — ป้องกัน username enumeration ผ่าน timing side-channel
- ถ้า user มี 2FA enabled → response จะเป็น `MfaChallenge` พร้อม `twoFactorToken` (single-use, time-limited) แทน access token

---

### 2.2 Two-Factor Authentication (TOTP)

**คืออะไร:** TOTP (Time-based One-Time Password) เป็น 2FA method ที่ใช้ Authenticator app (Google Authenticator, Authy ฯลฯ) สร้าง 6-digit code ที่เปลี่ยนทุก 30 วินาที

**การ implement:**
- `GET /auth/2fa/setup` — สร้าง TOTP secret + QR code URI สำหรับ scan ด้วย Authenticator app
- `POST /auth/2fa/enable` — verify first OTP code เพื่อยืนยันว่า setup ถูกต้อง แล้ว enable 2FA
- `POST /auth/2fa/disable` — require current OTP code เพื่อ disable (ป้องกัน attacker disable 2FA)
- `POST /auth/2fa/verify` — หลัง login ปกติ, ส่ง `twoFactorToken` (จาก login response) + `code` เพื่อ complete authentication
- ใช้ **OtpNet library** — `OtpNet.Totp` ด้วย SHA1, 6 digits, 30-second window
- **Recovery Codes:** batch-generated (8-10 codes), single-use, stored as bcrypt hash, audited เมื่อใช้

---

### 2.3 Two-Factor Authentication (SMS / Email OTP)

**คืออะไร:** 2FA method ที่ส่ง OTP ผ่าน SMS หรือ Email แทน Authenticator app เหมาะสำหรับ user ที่ไม่คุ้นเคยกับ TOTP app

**การ implement:**
- Challenge token flow: login → ได้ `twoFactorToken` (single-use UUID) → system ส่ง OTP ไปยัง phone/email ของ user
- `twoFactorToken` เก็บใน **Redis** (in-memory, TTL สั้น) ป้องกัน brute force และ replay
- SMS/Email delivery ผ่าน notification service (pluggable adapter)
- OTP มีอายุ 5-10 นาที และใช้ได้ครั้งเดียว

---

### 2.4 Social Login (OAuth2 / OIDC Providers)

**คืออะไร:** User login ด้วย account จาก third-party provider เช่น Google, Facebook ทำให้ไม่ต้องสร้าง password ใหม่

**การ implement:**
- `GET /auth/social/{providerCode}` — redirect ไปยัง external IdP (Google, Facebook, Apple, GitHub, LINE)
- `GET /auth/social/{providerCode}/callback` — receive authorization code จาก provider, exchange เป็น profile, สร้างหรือ link กับ local user account
- External access/refresh token จาก provider เก็บใน `lnk_user_external_logins` แบบ **encrypted** (ไม่ใช่ plaintext)
- Provider config (client_id, secret, scopes, endpoints) เก็บใน `mt_providers` table — extensible ไม่ต้อง deploy ใหม่เมื่อเพิ่ม provider

---

### 2.5 Magic Link (Passwordless Email)

**คืออะไร:** Passwordless authentication ที่ส่ง link ไปทาง email ให้ user คลิกเพื่อ login โดยไม่ต้องใช้ password

**การ implement:**
- `POST /auth/magic-link` — ส่ง email เพื่อขอ magic link
- `GET /auth/magic-link/verify?token=...` — verify token แล้ว redirect ไปพร้อม authorization code หรือ access token
- Magic link token เป็น cryptographic random UUID, single-use, TTL 15 นาที
- หลัง verify → สร้าง session เหมือน login ปกติ รวมถึง trigger 2FA ถ้า user มี 2FA enabled

---

### 2.6 Passkeys / WebAuthn FIDO2

**คืออะไร:** มาตรฐาน W3C WebAuthn + FIDO2 ที่ใช้ public-key cryptography แทน password user สร้าง passkey บน device (Face ID, Touch ID, hardware security key) และ credential ไม่ถูกส่งออกจาก device เลย

**การ implement:**
- `POST /auth/passkey/register/begin` — สร้าง registration challenge (random bytes) + options สำหรับ WebAuthn API
- `POST /auth/passkey/register/complete` — verify attestation response จาก authenticator แล้วเก็บ public key
- `POST /auth/passkey/authenticate/begin` — สร้าง authentication challenge
- `POST /auth/passkey/authenticate/complete` — verify assertion (signature ด้วย private key บน device) → issue tokens
- `GET /auth/passkey/credentials` — list registered passkeys ของ user
- `DELETE /auth/passkey/credentials/{id}` — ลบ passkey เฉพาะตัว

**ทำไมสำคัญ:** Passkeys ทนทานต่อ phishing 100% เพราะ private key ไม่เคยออกจาก device และ challenge ผูกกับ origin (domain) ทำให้ fake login page ใช้ไม่ได้

---

### 2.7 Password Management

**คืออะไร:** ระบบ lifecycle ครบสำหรับ password — ตั้งแต่ forgot password ไปจนถึง enforce นโยบาย

**การ implement:**
- `POST /auth/forgot-password` — generate reset token (secure random), ส่ง email พร้อม link
- `POST /auth/reset-password` — verify token + set new password, invalidate reset token
- `POST /auth/change-password` — require current password ก่อน, then update (Authorized users only)
- **Password History:** ตาราง `tx_password_history` เก็บ bcrypt hash ของ passwords เก่า — ป้องกัน reuse
- Reset token เก็บใน `tx_password_resets` พร้อม `used_at`, `revoked_at` — single-use

---

### 2.8 Email Verification

**คืออะไร:** ยืนยันว่า user เป็นเจ้าของ email address จริง ป้องกัน account ปลอม

**การ implement:**
- `POST /auth/verify-email` — verify token ที่ส่งใน email registration link
- `POST /auth/resend-verification` — ขอส่ง email verification ใหม่
- Support 2 workflow types: `REGISTRATION` (สมัครใหม่) และ `EMAIL_CHANGE` (เปลี่ยน email)
- Token เก็บใน `tx_email_verifications` พร้อม expiry และ type

---

## 3. SAML 2.0 SP Support ⭐⭐⭐⭐

MonkoraEdge Core Auth ทำหน้าที่เป็น **SAML 2.0 Service Provider (SP)** — หมายความว่า system นี้ trust Identity Provider (IdP) ภายนอก (เช่น Azure AD, Okta, AD FS) สำหรับ enterprise SSO

### 3.1 SP Metadata

**คืออะไร:** XML document ที่อธิบาย SP's identity, endpoints, และ certificates ให้ IdP ใช้ configure trust relationship

**การ implement:**
- `GET /auth/saml/{code}/metadata` — generate SAML SP metadata XML แบบ dynamic ตาม provider code
- ประกอบด้วย: `EntityID`, `AssertionConsumerService URL`, `SP certificate`, `NameID format`, `requested attributes`
- Admin ของ enterprise IdP ใช้ metadata URL นี้ register SP ในระบบ IdP ของตน

---

### 3.2 SP-Initiated SSO

**คืออะไร:** User ที่ไม่ได้ login เข้า application → application redirect ไปยัง IdP เพื่อ authenticate

**การ implement:**
- `GET /auth/saml/{code}/signin` — สร้าง `SAMLRequest` (AuthnRequest) แบบ XML ที่ signed แล้ว Base64+URL encode
- Redirect ไปยัง IdP's SSO endpoint ผ่าน HTTP Redirect Binding
- เก็บ `RelayState` เพื่อ redirect user กลับมายัง original URL หลัง authentication สำเร็จ

---

### 3.3 ACS (Assertion Consumer Service)

**คืออะไร:** Endpoint ที่รับ SAML Response จาก IdP หลัง user authenticate สำเร็จ

**การ implement:**
- `POST /auth/saml/{code}/acs` — รับ SAML Response (HTTP POST Binding)
- Validate: XML signature, certificate chain, `Conditions` (NotBefore/NotOnOrAfter), `AudienceRestriction`, `InResponseTo`
- Extract user attributes จาก Assertion → map กับ local user account หรือสร้าง user ใหม่ (JIT provisioning)
- ออก session token สำหรับ application

---

### 3.4 SAML → OAuth2 Token Exchange

**คืออะไร:** แปลง SAML assertion เป็น OAuth2 access token เพื่อให้ modern API ที่ expect Bearer token รองรับ SAML-based SSO ได้

**การ implement:**
- `POST /auth/saml/token` — ส่ง SAML assertion, validate, แล้วออก access token + refresh token
- สร้าง bridge ระหว่าง legacy SAML enterprise และ modern OAuth2 microservices

---

## 4. IAM — Multi-Tenant Management ⭐⭐⭐⭐⭐

### 4.1 Tenant Management

**คืออะไร:** Tenant คือ organizational unit ที่ isolate data, users, clients, และ config ไว้แยกกัน เหมาะสำหรับ SaaS ที่มีหลายลูกค้าใน database เดียวกัน

**การ implement:**
- `mt_tenants` table เก็บ settings, branding config (logo, colors, domain)
- CRUD + activate/deactivate — deactivated tenant ทำให้ login ไม่ได้ทุก user ในนั้น
- Tenant isolation enforce ใน query layer — ทุก resource query filter ด้วย `tenantId` อัตโนมัติ
- Console API (`/console/`) สำหรับ platform admin จัดการ tenant โดยตรง

---

### 4.2 User Management

**คืออะไร:** จัดการ lifecycle ของ user ทั้งหมดใน system

**การ implement:**
- `mt_users` — profile (display_name, email, phone, locale, zoneinfo, picture)
- `mt_user_identities` — auth provider separation: LOCAL (password), SSO (external)
- CRUD + soft-delete (ไม่ลบจริง — ป้องกัน FK constraint violations และเก็บ audit trail)
- Search/filter ด้วย status, tenant, keyword; paging + sorting รองรับ
- `tx_user_sessions_devices` — track device fingerprint, OS, browser, IP, location ทุก session

---

### 4.3 RBAC — Roles, Permissions

**คืออะไร:** Role-Based Access Control: user ได้รับ role → role มี permission → API check permission ก่อน execute

**การ implement:**
- **Permission:** resource + action model (เช่น `users:read`, `tenants:admin`)
- **Role:** กลุ่มของ permissions, hierarchical, scoped ต่อ platform หรือ tenant
- **Assignment:** user ↔ roles ผ่าน `lnk_user_roles` (มี expiry — รองรับ temporary access)
- Role ↔ Permissions ผ่าน `lnk_role_permissions`
- Controller `[Authorize]` attribute verify permission ผ่าน claims ใน access token

---

### 4.4 OAuth2 Client Management

**คืออะไร:** จัดการ OAuth2 clients (applications) ที่ใช้ Authorization Server นี้ ทั้ง CONFIDENTIAL (backend) และ PUBLIC (SPA, mobile)

**การ implement:**
- Config ต่อ client: allowed grant types, token lifetimes (access, refresh, id token), allowed scopes, redirect URIs
- `POST /clients/{id}/rotate-secret` — สร้าง client secret ใหม่, invalidate secret เก่าทันที
- Activate/deactivate client — deactivated client ออก token ไม่ได้
- `lnk_authorization_client_scopes` — กำหนด default scope และ required scope ต่อ client

---

### 4.5 API Key Management

**คืออะไร:** API key สำหรับ programmatic access ที่ไม่ต้องผ่าน OAuth flow เหมาะสำหรับ server-to-server, webhook receiver, หรือ legacy system integration

**การ implement:**
- Create API key พร้อมกำหนด: scope จำกัด, IP whitelist, expiry
- API key hash ด้วย SHA-256 ก่อนเก็บ DB — plaintext แสดงครั้งเดียวตอน create
- `POST /api-keys/validate` — endpoint สำหรับ Resource Server validate key แบบ real-time (AllowAnonymous — resource server เรียก)
- User-owned หรือ service-owned key แยกกันชัดเจน

---

### 4.6 Agreement Management (PDPA/Consent)

**คืออะไร:** จัดการ Terms of Service, Privacy Policy, PDPA consent documents พร้อม audit trail ว่า user แต่ละคน accept/withdraw consent เมื่อไหร่ version ไหน

**การ implement:**
- `mt_agreements` — versioned documents (v1, v2, ...), multi-language (th, en, zh, ja), type: TERMS/PRIVACY/PDPA/CONSENT
- `tx_agreement_accepts` — บันทึก user acceptance: `user_id`, `agreement_id`, `version`, `accepted_at`, `ip_address`, `user_agent`, `withdrawn_at`
- `POST /agreements/{id}/accept` — user accept agreement
- Filter by `tenantId` และ `activeOnly` — แต่ละ tenant มี agreement set ของตัวเอง
- **PDPA compliance:** เก็บหลักฐานครบว่า user consent อะไร เมื่อไหร่ จาก IP อะไร และ withdraw เมื่อไหร่

---

### 4.7 External Provider Management

**คืออะไร:** Admin จัดการ external Identity Providers (Google, Facebook, SAML IdP ฯลฯ) แบบ dynamic ไม่ต้อง deploy code ใหม่

**การ implement:**
- `mt_providers` — config: type (OAUTH2/OIDC/SAML), client_id, client_secret (encrypted), scopes, endpoints
- CRUD + activate/deactivate provider
- Provider code (`samlProviderCode`, `socialProviderCode`) ใช้อ้างอิงใน auth endpoints

---

### 4.8 Webhook Management

**คืออะไร:** Notify external systems เมื่อ event เกิดขึ้นใน Auth Server (user registered, password changed, token revoked ฯลฯ)

**การ implement:**
- Create webhook พร้อม endpoint URL + event types ที่ subscribe
- **Signing Secret:** generate ครั้งเดียว (HMAC-SHA256 key) ให้ receiver verify authenticity ของ webhook payload
- `PATCH /webhooks/{id}` — toggle active status
- `GET /webhooks/{id}/deliveries` — delivery logs พร้อม response status, retry count, error message
- ป้องกัน SSRF โดย validate URL format และ restrict internal IP ranges

---

## 5. Security Architecture ⭐⭐⭐⭐⭐

### 5.1 Security Headers

**คืออะไร:** HTTP response headers ที่บอก browser ว่าจะ enforce security policy อย่างไร

**การ implement (ทุก response):**
| Header | ค่า | ป้องกัน |
|---|---|---|
| `Content-Security-Policy` | strict source whitelist | XSS, data injection |
| `X-Frame-Options` | DENY | Clickjacking |
| `X-Content-Type-Options` | nosniff | MIME sniffing |
| `Referrer-Policy` | strict-origin-when-cross-origin | Information leakage |
| `Permissions-Policy` | disable unused features | Feature abuse |
| `Strict-Transport-Security` | max-age=31536000; includeSubDomains | SSL stripping (1 ปี) |

---

### 5.2 Rate Limiting

**คืออะไร:** จำกัดจำนวน request ต่อ IP ต่อหน่วยเวลา ป้องกัน brute force, credential stuffing, DoS

**การ implement:**
- **default policy:** 60 request / นาที ต่อ IP — ใช้กับ endpoints ทั่วไป
- **auth policy:** 10 request / นาที ต่อ IP — ใช้กับ `/oauth2/token`, `/oauth2/revoke`, `/auth/login`, `/auth/register`, `/auth/forgot-password`
- Rate limit state เก็บใน Redis (distributed) — ทำงานถูกต้องแม้ deploy หลาย instance
- Response `429 Too Many Requests` พร้อม `Retry-After` header

---

### 5.3 JWT Algorithm Restriction

**คืออะไร:** ป้องกัน algorithm confusion attacks ที่ attacker ปรับ `alg` header ใน JWT เพื่อ bypass signature verification

**การ implement:**
- Accept เฉพาะ `RS256` เท่านั้น — reject `none`, `HS256`, `HS384`, `RS512` ฯลฯ
- RSA key size ≥ 2048 bits validate ณ application startup — fail-fast ถ้า config ผิด
- Singleton `RsaSecurityKey` ป้องกัน key rotation race condition

---

### 5.4 JWT Revocation (Real-time)

**คืออะไร:** แม้ access token จะยังไม่ expire แต่ถ้าถูก revoke ต้องถือว่า invalid ทันที

**การ implement:**
- `OnTokenValidated` event hook ใน ASP.NET Core JWT middleware — ทุก request ที่ผ่าน `[Authorize]` จะ check revocation list ใน DB
- Access token soft-delete ใน `tx_authorization_access_tokens` (เพิ่ม `revoked_at` column)
- `tx_revoked_tokens` table เป็น audit trail แยกต่างหาก
- Performance: check ด้วย indexed `jti` (JWT ID) — O(1) lookup

---

### 5.5 Audit Logging

**คืออะไร:** บันทึกทุก security-relevant action สำหรับ compliance (PDPA, ISO 27001) และ forensic investigation

**การ implement:**
- `audit_logs` table: `actor_id`, `actor_type` (USER/SERVICE/ADMIN), `action` (LOGIN, LOGOUT, TOKEN_ISSUED, PASSWORD_CHANGED ฯลฯ), `entity_type`, `entity_id`, `ip_address`, `user_agent`, `result` (SUCCESS/FAILURE), `detail` (JSON), `created_at`
- `tx_login_attempts`: `risk_score`, `latency_ms`, `failure_reason`, `geo_location` — สำหรับ anomaly detection

---

### 5.6 CORS Strict Policy

**คืออะไร:** Cross-Origin Resource Sharing policy ควบคุมว่า browser ใดสามารถเรียก API ได้บ้าง

**การ implement:**
- Whitelist-based: `Cors:AllowedOrigins` config array — ไม่มีการ allow wildcard `*`
- Deny-all ถ้า AllowedOrigins list ว่าง
- Explicit allowed methods และ headers — ไม่ใช้ `AllowAnyHeader()` หรือ `AllowAnyMethod()`
- Credentials allowed สำหรับ same-origin requests เท่านั้น

---

### 5.7 Request Body Size Limit

**คืออะไร:** จำกัดขนาด request body ป้องกัน large payload DoS attack

**การ implement:**
- Kestrel config: max request body = **64 KB**
- Return `413 Payload Too Large` ถ้าเกิน — ก่อนถึง controller layer

---

## 6. Infrastructure & Reliability ⭐⭐⭐⭐

### 6.1 PostgreSQL + EF Core

**คืออะไร:** Relational database ผ่าน Entity Framework Core ORM

**การ implement:**
- `Npgsql.EntityFrameworkCore.PostgreSQL` driver
- **DbContext Pooling** — reuse connection objects แทนสร้างใหม่ทุก request (performance สำคัญ)
- **Retry-on-failure policy** — handle transient errors (network blip, DB restart) โดยอัตโนมัติ สูงสุด N retries ด้วย exponential backoff
- 33 tables ครอบคลุม entity ทั้งหมด

---

### 6.2 Redis Distributed Cache

**คืออะไร:** In-memory data store ที่ใช้ share state ระหว่าง application instances

**การ implement (3 use cases):**
- **2FA Challenge Store:** `twoFactorToken` เก็บใน Redis พร้อม TTL สั้น (5-15 นาที) — Redis-backed ทำให้ทนต่อ app restart (เดิมใช้ IMemoryCache ซึ่งหายเมื่อ restart)
- **Session/Cache:** distributed cache สำหรับ frequently accessed data
- **Rate Limit State:** per-IP request count window — share ระหว่าง instances ใน load balancer setup

---

### 6.3 Background Token Cleanup

**คืออะไร:** Service ที่รัน background ลบ token ที่ expire แล้วออกจาก DB เพื่อป้องกัน table ใหญ่เกินไป

**การ implement:**
- `TokenCleanupService` — IHostedService รัน periodic cleanup
- ลบ expired records จาก: `tx_authorization_codes`, `tx_authorization_access_tokens`, `tx_authorization_refresh_tokens`, `tx_email_verifications`, `tx_password_resets`
- Config interval ได้ผ่าน appsettings

---

### 6.4 Health Checks

**คืออะไร:** Endpoint สำหรับ Kubernetes/load balancer ใช้ check ว่า instance พร้อมรับ traffic หรือไม่

**การ implement:**
- `GET /health/ready` — check PostgreSQL connection + Redis connection (unhealthy ถ้า DB หรือ Redis down)
- `GET /health/live` — เช็ค application process ยังรันอยู่ (ไม่ deadlock)
- ใช้ ASP.NET Core Health Checks framework — extensible สำหรับ custom check

---

### 6.5 Structured Logging (Serilog)

**คืออะไร:** Log ในรูป JSON structure แทน plain text ทำให้ query และ aggregate ใน log management system (ELK, Seq, Datadog) ได้ง่าย

**การ implement:**
- Serilog ด้วย configuration-driven sinks (Console, File, Seq ฯลฯ ตาม appsettings)
- **CorrelationId enrichment:** ทุก log entry มี `CorrelationId` header จาก request — ทำให้ trace log ทุกบรรทัดของ request เดียวกันได้

---

### 6.6 RabbitMQ AMQP Integration

**คืออะไร:** Message broker integration สำหรับ async event publishing

**การ implement:**
- AMQP adapter สำหรับ publish `AccountDeleted` event ไปยัง RabbitMQ queue
- Downstream services (เช่น data cleanup worker) consume event นี้เพื่อ sync การลบ user data

---

## 7. API & Developer Experience ⭐⭐⭐⭐

### 7.1 Postman Collection (135 Requests / 17 Folders)

**คืออะไร:** Pre-built Postman collection ช่วยให้ developer ทดสอบทุก endpoint ได้ทันทีโดยไม่ต้องเขียน request เอง

**รายละเอียด:**
- **17 Folders:** Health & Discovery, Auth, OAuth2/OIDC, Tenants, Agreements, Users, Clients, Roles, Permissions, Scopes, API Keys, Providers, Webhooks, Passkeys, SAML 2.0, Console, Security Negative Tests
- **PKCE Pre-request Script:** auto-generate `code_verifier` (32 random bytes) + `code_challenge` (S256 SHA256 + Base64url) ก่อนทุก Authorize request
- **Auto-save Tokens:** test scripts save `bearerToken`, `refreshToken`, `idToken` ลงใน collection variables อัตโนมัติ
- **Test Assertions:** ทุก request มี assertion check status code — ทราบทันทีถ้า regression เกิด
- **Negative Tests Folder:** 9 test cases สำหรับ security boundary (rate limit, plain PKCE, invalid credentials, OIDC scope in CC grant ฯลฯ)
- **RFC-correct Parameters:** `response_type`, `client_id`, `redirect_uri`, `code_challenge`, `code_challenge_method` เป็น snake_case ถูกต้องตาม RFC 6749

---

### 7.2 Postman Environment (34 Variables)

**คืออะไร:** Pre-configured environment variables สำหรับ local development

**รายละเอียด:**
- `baseUrl`, `clientId`, `redirectUri`, `tenantId`, `userId` และ IDs อื่นๆ ตั้งค่า default พร้อมใช้
- Sensitive values (`bearerToken`, `clientSecret`, `refreshToken`) ตั้งเป็น type `secret` — ไม่แสดงใน UI
- 34 variables ครอบคลุมทุก parameter ที่ collection ใช้

---

### 7.3 Swagger / OpenAPI

**คืออะไร:** Interactive API documentation ใน browser

**การ implement:**
- Swagger UI available ใน Development environment
- **Bearer JWT support** — กรอก `bearerToken` เพื่อทดสอบ `[Authorize]` endpoints ใน Swagger UI โดยตรง

---

### 7.4 Structured Error Responses

**คืออะไร:** Error response format สม่ำเสมอทุก endpoint ทำให้ client handle error ได้ง่าย

**การ implement (`DomainExceptionHandlingMiddleware`):**

| Error Category | HTTP Status |
|---|---|
| Validation | 400 Bad Request |
| Auth (invalid token, OAuth2 error, API key, MFA) | 401 Unauthorized |
| Permission | 403 Forbidden |
| NotFound | 404 Not Found |
| Conflict / Domain / Business Rule | 409 Conflict |
| RateLimit | 429 Too Many Requests |
| APIGateway / External Service | 502 Bad Gateway |
| NotSupported | 501 Not Implemented |
| System / Infrastructure | 500 Internal Server Error |

---

## 8. Standards & RFC Compliance ⭐⭐⭐⭐⭐

| RFC / Spec | คำอธิบาย | สถานะ |
|---|---|---|
| RFC 6749 — OAuth 2.0 | Authorization Code, Client Credentials, Refresh Token grants | ✅ ครบ |
| RFC 7009 — Token Revocation | Revoke access/refresh tokens ทันที | ✅ ครบ |
| RFC 7591 — Dynamic Client Registration | Client register ตัวเองได้ผ่าน API | ✅ ครบ |
| RFC 7662 — Token Introspection | Resource server verify token กับ AS | ✅ ครบ |
| RFC 8414 — AS Metadata | Discovery document for OAuth 2.0 AS | ✅ ครบ |
| RFC 8628 — Device Authorization Grant | IoT / input-constrained device flow | ✅ ครบ |
| RFC 9068 — JWT Profile for Access Tokens | `at+JWT` typ header enforced | ✅ ครบ |
| RFC 9126 — Pushed Authorization Requests | PAR endpoint, `request_uri` flow | ✅ ครบ |
| RFC 9449 — DPoP | Cryptographic sender-binding ของ token | ✅ ครบ |
| OIDC Core 1.0 | ID Token, UserInfo, `at_hash`, Discovery, prompt, max_age | ✅ ครบ |
| OAuth 2.1 Draft | PKCE mandatory, implicit/password rejected | ✅ ครบ |
| SAML 2.0 (SP Profile) | SP metadata, SP-SSO initiate, ACS, token exchange | ✅ ครบ |
| WebAuthn / FIDO2 | W3C WebAuthn Level 2, FIDO2 attestation + assertion | ✅ ครบ |
| PDPA / Consent Management | Versioned consent + audit trail | ✅ ครบ |

---

## จุดเด่นสูงสุด

- **OAuth 2.1 strict mode** — เป็นหนึ่งใน auth server ไม่กี่ตัวที่บังคับ PKCE, ปฏิเสธ implicit/password, และ DPoP binding ครบ
- **Refresh token theft detection** — family-based cascade revocation ป้องกัน token replay attack: token ถูกขโมยแล้ว attacker ใช้ → revoke ทุก session ในสาย
- **Passkeys (FIDO2)** — รองรับ passwordless authentication มาตรฐานอนาคต ทนทานต่อ phishing 100%
- **PAR + Device Grant + Dynamic Client Registration** — RFC สำคัญที่ OSS auth server ส่วนใหญ่ยังไม่รองรับ
- **PDPA-ready** — Agreement management พร้อม versioning และ audit trail acceptance/withdrawal ครบ compliance
- **Real-time JWT revocation** — `OnTokenValidated` hook ทำให้ revocation มีผลทันที ไม่ต้องรอ token expire

---

## จุดที่อาจปรับปรุงต่อไป

- เพิ่ม **OpenTelemetry** distributed tracing สำหรับ observability ในระบบ microservices (trace ข้าม service ได้)
- เพิ่ม **Circuit breaker** pattern (Polly) สำหรับ external IdP calls ป้องกัน cascade failure เมื่อ Google/Facebook API down
- Export **OpenAPI spec** เป็น JSON/YAML เพื่อ SDK generation อัตโนมัติ (NSwag, Kiota)
- เพิ่ม **mTLS client authentication (RFC 8705)** สำหรับ high-security M2M use case (financial services, government)
- พิจารณา **IdP mode สำหรับ SAML** เพื่อรองรับ enterprise federation ทั้งสองทิศทาง (เป็นทั้ง SP และ IdP)
- เพิ่ม **step-up authentication** — require stronger auth เมื่อ access sensitive resource โดยไม่ต้อง logout
