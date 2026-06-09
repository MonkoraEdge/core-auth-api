#!/usr/bin/env bash
# =============================================================================
# MonkoraEdge Core Auth — RFC 7009 Revocation + RFC 7662 Introspection Test Suite
# =============================================================================
#
# Client auth: Both endpoints require client authentication.
# ExtractClientCredentials() in OAuth2Controller accepts:
#   1. Authorization: Basic base64(client_id:client_secret)   ← preferred (CLIENT_SECRET_BASIC)
#   2. client_id + client_secret in the POST body             ← CLIENT_SECRET_POST
#
# For PUBLIC clients (no secret): send client_id in body only.
#
# Endpoints:
#   POST /connect/revoke      RFC 7009 §2
#   POST /connect/introspect  RFC 7662 §2
#
# Prerequisites
# ─────────────
# • API running at http://localhost:5001
# • A registered CONFIDENTIAL client:
#     client_id     = api-server
#     client_secret = super-secret-value
#     grant_types   = AUTHORIZATION_CODE, REFRESH_TOKEN, CLIENT_CREDENTIALS
# • A valid access_token from a completed flow. Paste into AT_VALID below.
# • jq installed (brew install jq / apt install jq) for field assertions.
#
# Run:
#   chmod +x revoke_introspect.sh
#   export AT_VALID="eyJhb..."
#   export RT_VALID="<opaque refresh token>"
#   ./revoke_introspect.sh
# =============================================================================

BASE="http://localhost:5001"

# ── Confidential client credentials ──────────────────────────────────────────
CLIENT_ID="api-server"
CLIENT_SECRET="super-secret-value"

# Authorization: Basic base64(client_id:client_secret)
# Pre-compute: echo -n "api-server:super-secret-value" | base64
BASIC_AUTH="Authorization: Basic $(echo -n "${CLIENT_ID}:${CLIENT_SECRET}" | base64)"

# ── Tokens — paste or export before running ───────────────────────────────────
AT_VALID="${AT_VALID:-PASTE_A_VALID_ACCESS_TOKEN_HERE}"
RT_VALID="${RT_VALID:-PASTE_A_VALID_REFRESH_TOKEN_HERE}"

# ── Colour helpers ─────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'
pass()  { echo -e "${GREEN}  ✓ PASS${NC}: $1"; }
fail()  { echo -e "${RED}  ✗ FAIL${NC}: $1"; }
info()  { echo -e "${YELLOW}  ►${NC} $1"; }
label() { echo -e "\n${CYAN}═══════════════════════════════════════════════════════════${NC}"; \
          echo -e "${CYAN} $1${NC}"; \
          echo -e "${CYAN}═══════════════════════════════════════════════════════════${NC}"; }

assert_http() {
  local desc="$1" expected="$2" actual="$3"
  [ "$actual" = "$expected" ] \
    && pass "$desc (HTTP $actual)" \
    || fail "$desc — expected HTTP $expected, got HTTP $actual"
}

assert_field_present() {
  local desc="$1" field="$2" body="$3"
  echo "$body" | jq -e "has(\"$field\")" > /dev/null 2>&1 \
    && pass "$desc — field '$field' present" \
    || fail "$desc — field '$field' MISSING in: $body"
}

assert_field_eq() {
  local desc="$1" field="$2" expected="$3" body="$4"
  local actual
  actual=$(echo "$body" | jq -r ".\"$field\" // empty" 2>/dev/null)
  [ "$actual" = "$expected" ] \
    && pass "$desc — $field=$actual" \
    || fail "$desc — expected $field='$expected', got '$actual'"
}

assert_field_bool() {
  local desc="$1" field="$2" expected="$3" body="$4"
  local actual
  actual=$(echo "$body" | jq -r ".\"$field\"" 2>/dev/null)
  [ "$actual" = "$expected" ] \
    && pass "$desc — $field=$actual" \
    || fail "$desc — expected $field=$expected, got $actual"
}

assert_header() {
  local desc="$1" header="$2" expected="$3" headers="$4"
  echo "$headers" | grep -qi "$header: $expected" \
    && pass "$desc — $header: $expected" \
    || fail "$desc — expected header '$header: $expected' not found"
}

# Helper: run curl, split headers from body, extract status code
do_curl() {
  # Usage: do_curl VAR_HTTP VAR_HEADERS VAR_BODY <curl args>
  local _http_var="$1" _hdr_var="$2" _body_var="$3"
  shift 3
  local raw
  raw=$(curl -s -D - -w "\n__DONE__" "$@")
  local _status _headers _body
  _status=$(echo "$raw" | grep -E "^HTTP/" | tail -1 | awk '{print $2}')
  _headers=$(echo "$raw" | awk '/^\r?$/{found=1; next} !found{print}' | tail -n +2)
  _body=$(echo "$raw" | awk '/^\r?$/{found++} found>=1{print}' | grep -v "__DONE__" | tail -n +2)
  eval "$_http_var='$_status'"
  # Use printf to avoid subshell issues with multiline
  printf -v "$_hdr_var" '%s' "$_headers"
  printf -v "$_body_var" '%s' "$_body"
}


# =============================================================================
# TEST A — Revoke a valid access_token
# RFC 7009 §2.1: revocation request MUST be authenticated
# RFC 7009 §2.2: server MUST respond with HTTP 200 on success
# =============================================================================
# Code path:
#   OAuth2Controller.Revoke → ExtractClientCredentials (Basic header)
#   → OAuth2Service.RevokeAsync → ClientAuthenticator.AuthenticateAsync (cache+bcrypt)
#   → TokenService.RevokeTokenAsync
#       → _accessTokenRepo.GetByTokenHashAsync → sets RevokedAt = now
#       → _revokedTokenRepo.Insert (RevokedToken row)
#       → _distributedCache.RemoveAsync("introspect:{hash}")  ← cache eviction
#       → _unitOfWork.SaveChangesAsync
#   → AuditLog.Insert (action="token_revoked")
#   → ApplyNoStoreHeaders()
#   → HTTP 200 (empty body)
#
# Expected HTTP 200, empty body, headers:
#   Cache-Control: no-store
#   Pragma: no-cache
# =============================================================================
label "TEST A — POST /connect/revoke  valid access_token"

info "REQUEST:"
cat <<EOF
  curl -X POST $BASE/connect/revoke \\
    -H "Content-Type: application/x-www-form-urlencoded" \\
    -H "$BASIC_AUTH" \\
    -d "token=<AT_VALID>&token_type_hint=access_token"
EOF

A_RESP=$(curl -s -D - -w "\n__DONE__" \
  -X POST "$BASE/connect/revoke" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -H "Authorization: Basic $(echo -n "${CLIENT_ID}:${CLIENT_SECRET}" | base64)" \
  -d "token=${AT_VALID}&token_type_hint=access_token")

A_HTTP=$(echo "$A_RESP" | grep -E "^HTTP/" | tail -1 | awk '{print $2}')
A_HDR=$(echo "$A_RESP" | awk '/^\r?$/{exit} NR>1{print}')

info "HTTP $A_HTTP"

assert_http    "A1 — HTTP status"              "200" "$A_HTTP"
assert_header  "A2 — Cache-Control: no-store"  "Cache-Control" "no-store" "$A_HDR"
assert_header  "A3 — Pragma: no-cache"         "Pragma" "no-cache" "$A_HDR"

# RFC 7009 §2.2: response body SHOULD be empty
A_BODY=$(echo "$A_RESP" | awk '/^\r?$/{found++} found>=1{print}' | grep -v "__DONE__" | tail -n +2 | tr -d '[:space:]')
[ -z "$A_BODY" ] \
  && pass "A4 — response body is empty (RFC 7009 §2.2)" \
  || info "A4 — note: non-empty body (acceptable): $A_BODY"


# =============================================================================
# TEST B — Revoke an INVALID / unknown token
# RFC 7009 §2.2: "The authorization server responds with HTTP status code 200
#  if the token has been revoked successfully or if the client submitted an
#  invalid token."  Server MUST NOT return an error for an unknown token.
# =============================================================================
# Code path:
#   OAuth2Service.RevokeAsync → TokenService.RevokeTokenAsync
#   → _accessTokenRepo.GetByTokenHashAsync → null
#   → _refreshTokenRepo.GetByTokenHashAsync → null
#   → revokedAny = false → early return (no SaveChanges, no error)
#   → ApplyNoStoreHeaders()
#   → HTTP 200 (empty body)
# =============================================================================
label "TEST B — POST /connect/revoke  invalid/unknown token (RFC 7009 §2.2 — MUST return 200)"

info "REQUEST:"
cat <<EOF
  curl -X POST $BASE/connect/revoke \\
    -H "Content-Type: application/x-www-form-urlencoded" \\
    -H "$BASIC_AUTH" \\
    -d "token=this-token-was-never-issued-aaaaaaaaaaaaa&token_type_hint=access_token"
EOF

B_HTTP=$(curl -s -o /dev/null -w "%{http_code}" \
  -X POST "$BASE/connect/revoke" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -H "Authorization: Basic $(echo -n "${CLIENT_ID}:${CLIENT_SECRET}" | base64)" \
  -d "token=this-token-was-never-issued-aaaaaaaaaaaaa&token_type_hint=access_token")

info "HTTP $B_HTTP"
assert_http "B1 — HTTP 200 for unknown token (RFC 7009 §2.2)"  "200" "$B_HTTP"


# =============================================================================
# TEST B2 — Revoke with unsupported token_type_hint
# Controller guard: IsSupportedTokenTypeHint — only "access_token" | "refresh_token"
# Expected HTTP 400 (the one carve-out where revoke doesn't return 200)
# =============================================================================
label "TEST B2 — POST /connect/revoke  unsupported token_type_hint (HTTP 400)"

B2_RESP=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
  -X POST "$BASE/connect/revoke" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -H "Authorization: Basic $(echo -n "${CLIENT_ID}:${CLIENT_SECRET}" | base64)" \
  -d "token=${AT_VALID}&token_type_hint=id_token")

B2_HTTP=$(echo "$B2_RESP" | grep "__HTTP_STATUS__" | sed 's/__HTTP_STATUS__//')
B2_BODY=$(echo "$B2_RESP" | sed '/^__HTTP_STATUS__/d')

info "HTTP $B2_HTTP  body: $B2_BODY"
assert_http "B2a — HTTP 400 for unsupported hint"       "400" "$B2_HTTP"
echo "$B2_BODY" | jq -e '.error == "unsupported_token_type"' > /dev/null 2>&1 \
  && pass "B2b — error=unsupported_token_type" \
  || fail "B2b — expected error=unsupported_token_type in: $B2_BODY"


# =============================================================================
# TEST C — Introspect the SAME token after revocation (must return active:false)
# =============================================================================
# Code path:
#   IntrospectTokenAsync:
#   1. Check Redis cache (key="introspect:{hash}") → MISS (evicted by RevokeTokenAsync in Test A)
#   2. _revokedTokenRepo.IsRevokedAsync(hash) → TRUE (row exists from Test A)
#   → return { active: false }
#   → HTTP 200 { "active": false }   ← RFC 7662 §2.2: inactive response has only "active"
# =============================================================================
label "TEST C — POST /connect/introspect  revoked token (active:false)"

info "REQUEST:"
cat <<EOF
  curl -X POST $BASE/connect/introspect \\
    -H "Content-Type: application/x-www-form-urlencoded" \\
    -H "$BASIC_AUTH" \\
    -d "token=<AT_VALID (revoked in Test A)>&token_type_hint=access_token"
EOF

C_RESP=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
  -X POST "$BASE/connect/introspect" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -H "Authorization: Basic $(echo -n "${CLIENT_ID}:${CLIENT_SECRET}" | base64)" \
  -d "token=${AT_VALID}&token_type_hint=access_token")

C_HTTP=$(echo "$C_RESP" | grep "__HTTP_STATUS__" | sed 's/__HTTP_STATUS__//')
C_BODY=$(echo "$C_RESP" | sed '/^__HTTP_STATUS__/d')

info "HTTP $C_HTTP  body: $C_BODY"

assert_http        "C1 — HTTP 200 (RFC 7662 §2.3: always 200, never 4xx for inactive)" "200" "$C_HTTP"
assert_field_bool  "C2 — active=false"  "active" "false" "$C_BODY"

# RFC 7662 §2.2: inactive response MUST NOT include token metadata (sub, scope, exp, etc.)
for field in sub scope exp iat jti client_id token_type; do
  val=$(echo "$C_BODY" | jq -r ".\"$field\" // empty" 2>/dev/null)
  [ -z "$val" ] \
    && pass "C3 — inactive response omits '$field' (RFC 7662 §2.2)" \
    || fail "C3 — inactive response MUST NOT include '$field', got: $val"
done


# =============================================================================
# TEST D — Introspect a VALID active access_token
# =============================================================================
# Requires a FRESH access_token (not AT_VALID which was revoked above).
# Set AT_FRESH_OVERRIDE before running, or obtain one via:
#   ./pkce_flow.http Step 3, or client_credentials grant:
#     curl -X POST .../connect/token -d "grant_type=client_credentials&client_id=api-server&client_secret=..."
#
# Code path:
#   IntrospectTokenAsync:
#   1. Redis cache MISS (first call on fresh token)
#   2. _revokedTokenRepo.IsRevokedAsync → false
#   3. _accessTokenRepo.GetByTokenHashAsync → found, ExpiresAt > now, RevokedAt = null
#   4. Build IntrospectResponse, write to Redis (TTL = min(remaining, 15min))
#   5. Return { active: true, sub, client_id, scope, iss, aud, exp, iat, jti, token_type }
#
# Expected HTTP 200:
# {
#   "active":     true,
#   "sub":        "<user-guid>",
#   "client_id":  "api-server",
#   "scope":      "openid profile email",
#   "iss":        "http://localhost:5003",
#   "aud":        "https://api.example.com",
#   "exp":        <unix-timestamp>,
#   "iat":        <unix-timestamp>,
#   "jti":        "<guid>",
#   "token_type": "Bearer"
# }
# =============================================================================
label "TEST D — POST /connect/introspect  valid active access_token (active:true + all RFC 7662 §2.2 fields)"

AT_FRESH="${AT_FRESH_OVERRIDE:-${AT_VALID}}"

info "REQUEST:"
cat <<EOF
  curl -X POST $BASE/connect/introspect \\
    -H "Content-Type: application/x-www-form-urlencoded" \\
    -H "$BASIC_AUTH" \\
    -d "token=<AT_FRESH>&token_type_hint=access_token"
EOF

D_RESP=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
  -X POST "$BASE/connect/introspect" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -H "Authorization: Basic $(echo -n "${CLIENT_ID}:${CLIENT_SECRET}" | base64)" \
  -d "token=${AT_FRESH}&token_type_hint=access_token")

D_HTTP=$(echo "$D_RESP" | grep "__HTTP_STATUS__" | sed 's/__HTTP_STATUS__//')
D_BODY=$(echo "$D_RESP" | sed '/^__HTTP_STATUS__/d')

info "HTTP $D_HTTP  body: $D_BODY"

assert_http       "D1 — HTTP 200"                  "200"  "$D_HTTP"
assert_field_bool "D2 — active=true"               "active" "true" "$D_BODY"
assert_field_present "D3 — sub present"            "sub"        "$D_BODY"
assert_field_present "D4 — client_id present"      "client_id"  "$D_BODY"
assert_field_present "D5 — scope present"          "scope"      "$D_BODY"
assert_field_present "D6 — iss present"            "iss"        "$D_BODY"
assert_field_present "D7 — aud present"            "aud"        "$D_BODY"
assert_field_present "D8 — exp present"            "exp"        "$D_BODY"
assert_field_present "D9 — iat present"            "iat"        "$D_BODY"
assert_field_present "D10 — jti present"           "jti"        "$D_BODY"
assert_field_eq   "D11 — token_type=Bearer"        "token_type" "Bearer" "$D_BODY"
assert_field_eq   "D12 — client_id=api-server"     "client_id"  "$CLIENT_ID" "$D_BODY"

# exp must be ≤ iat + 900 (MaxAccessTokenLifetimeSeconds)
EXP_D=$(echo "$D_BODY" | jq -r '.exp // 0')
IAT_D=$(echo "$D_BODY" | jq -r '.iat // 0')
MAX_LIFETIME=$(( IAT_D + 900 ))
[ "$EXP_D" -le "$MAX_LIFETIME" ] 2>/dev/null \
  && pass "D13 — exp ($EXP_D) ≤ iat+900 ($MAX_LIFETIME) — server cap respected" \
  || fail "D13 — exp=$EXP_D exceeds iat+900=$MAX_LIFETIME"

# D14 — Second call should be served from Redis cache (no extra assertion possible
#        without tracing, but note it here for performance verification)
info "D14 — Making second introspect call (should hit Redis cache, no DB round-trip)..."
D2_HTTP=$(curl -s -o /dev/null -w "%{http_code}" \
  -X POST "$BASE/connect/introspect" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -H "Authorization: Basic $(echo -n "${CLIENT_ID}:${CLIENT_SECRET}" | base64)" \
  -d "token=${AT_FRESH}&token_type_hint=access_token")
assert_http "D14 — cached introspect also returns 200" "200" "$D2_HTTP"


# =============================================================================
# TEST E — Introspect WITHOUT client authentication
# =============================================================================
# Code path:
#   ExtractClientCredentials: no Authorization header, no client_id in body
#   → clientId = "" (empty string)
#   → OAuth2Service.IntrospectAsync → ClientAuthenticator.AuthenticateAsync("")
#   → string.IsNullOrEmpty(clientId) → throws DomainException(INVALID_CLIENT)
#   → MapTokenDomainException → IsInvalidClientError → HTTP 401
#
# Expected HTTP 401:
# {
#   "error":             "invalid_client",
#   "error_description": "Client authentication failed."
# }
# Response header:
#   WWW-Authenticate: Basic realm="oauth2/introspect", error="invalid_client"
# =============================================================================
label "TEST E — POST /connect/introspect  NO client authentication (HTTP 401)"

info "REQUEST:"
cat <<EOF
  curl -X POST $BASE/connect/introspect \\
    -H "Content-Type: application/x-www-form-urlencoded" \\
    -d "token=<AT_FRESH>"
  # No Authorization header. No client_id in body.
EOF

E_RESP=$(curl -s -D - -w "\n__DONE__" \
  -X POST "$BASE/connect/introspect" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "token=${AT_FRESH}")

E_HTTP=$(echo "$E_RESP" | grep -E "^HTTP/" | tail -1 | awk '{print $2}')
E_HDR=$(echo "$E_RESP" | awk '/^\r?$/{exit} NR>1{print}')
E_BODY=$(echo "$E_RESP" | awk '/^\r?$/{found++} found>=1{print}' | grep -v "__DONE__" | tail -n +2)

info "HTTP $E_HTTP  body: $E_BODY"

assert_http "E1 — HTTP 401 (no auth)" "401" "$E_HTTP"
echo "$E_BODY" | jq -e '.error == "invalid_client"' > /dev/null 2>&1 \
  && pass "E2 — error=invalid_client" \
  || fail "E2 — expected error=invalid_client in: $E_BODY"

# RFC 6750 §3 / RFC 7235 §4.1: 401 MUST include WWW-Authenticate
echo "$E_HDR" | grep -qi "WWW-Authenticate" \
  && pass "E3 — WWW-Authenticate header present" \
  || fail "E3 — WWW-Authenticate header MISSING on 401"

echo "$E_HDR" | grep -qi 'Basic realm="oauth2/introspect"' \
  && pass "E4 — WWW-Authenticate realm=oauth2/introspect" \
  || fail "E4 — WWW-Authenticate missing correct realm: $(echo "$E_HDR" | grep -i WWW)"


# =============================================================================
# TEST E2 — Introspect with WRONG client secret
# Same error shape as E, but triggered by bcrypt mismatch instead of empty clientId.
# =============================================================================
label "TEST E2 — POST /connect/introspect  wrong client_secret (HTTP 401)"

E2_HTTP=$(curl -s -o /dev/null -w "%{http_code}" \
  -X POST "$BASE/connect/introspect" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -H "Authorization: Basic $(echo -n "${CLIENT_ID}:WRONG_SECRET" | base64)" \
  -d "token=${AT_FRESH}")

info "HTTP $E2_HTTP"
assert_http "E2 — HTTP 401 for wrong client_secret" "401" "$E2_HTTP"


# =============================================================================
# TEST F — Introspect an EXPIRED token
# =============================================================================
# To obtain an expired access_token:
#   Option 1: Configure a client with AccessTokenLifetime=1 (second), wait 2 seconds.
#   Option 2: Directly set expires_at in the past in tx_access_tokens:
#     UPDATE tx_access_tokens SET expires_at = now() - interval '1 second' WHERE id='<id>';
#   Then use the raw token string as AT_EXPIRED_OVERRIDE.
#
# Code path:
#   IntrospectTokenAsync:
#   1. Redis cache MISS (expired tokens are never cached)
#   2. _revokedTokenRepo.IsRevokedAsync → false (not explicitly revoked)
#   3. _accessTokenRepo.GetByTokenHashAsync → found
#   4. accessToken.ExpiresAt <= DateTime.UtcNow → TRUE
#   → return { active: false }
#
# Expected HTTP 200:
# { "active": false }
# =============================================================================
label "TEST F — POST /connect/introspect  expired token (active:false)"

AT_EXPIRED="${AT_EXPIRED_OVERRIDE:-PASTE_AN_EXPIRED_ACCESS_TOKEN_HERE}"

if [[ "$AT_EXPIRED" == PASTE* ]]; then
  echo -e "${YELLOW}  SKIP${NC}: set AT_EXPIRED_OVERRIDE env var to a known-expired access_token."
  echo "  Generate: UPDATE tx_access_tokens SET expires_at = now() - interval '1 second' WHERE id='<id>';"
  echo "  Then re-run with: export AT_EXPIRED_OVERRIDE=<raw token>"
else
  info "REQUEST:"
  cat <<EOF
  curl -X POST $BASE/connect/introspect \\
    -H "Content-Type: application/x-www-form-urlencoded" \\
    -H "$BASIC_AUTH" \\
    -d "token=<AT_EXPIRED>&token_type_hint=access_token"
EOF

  F_RESP=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
    -X POST "$BASE/connect/introspect" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -H "Authorization: Basic $(echo -n "${CLIENT_ID}:${CLIENT_SECRET}" | base64)" \
    -d "token=${AT_EXPIRED}&token_type_hint=access_token")

  F_HTTP=$(echo "$F_RESP" | grep "__HTTP_STATUS__" | sed 's/__HTTP_STATUS__//')
  F_BODY=$(echo "$F_RESP" | sed '/^__HTTP_STATUS__/d')

  info "HTTP $F_HTTP  body: $F_BODY"

  assert_http       "F1 — HTTP 200 (RFC 7662: expired = inactive, not an error)" "200" "$F_HTTP"
  assert_field_bool "F2 — active=false" "active" "false" "$F_BODY"
fi


# =============================================================================
# TEST G — Revoke a refresh_token (verifies token_type_hint routing)
# =============================================================================
# Code path:
#   RevokeTokenAsync with token_type_hint="refresh_token"
#   → _accessTokenRepo.GetByTokenHashAsync → null (it is a refresh token hash)
#   → _refreshTokenRepo.GetByTokenHashAsync → found
#   → refreshToken.RevokedAt = now
#   → _revokedTokenRepo.Insert
#   → refreshToken.FamilyId != Guid.Empty → RevokeTokenFamilyAsync cascade
# =============================================================================
label "TEST G — POST /connect/revoke  refresh_token with token_type_hint"

info "REQUEST:"
cat <<EOF
  curl -X POST $BASE/connect/revoke \\
    -H "Content-Type: application/x-www-form-urlencoded" \\
    -H "$BASIC_AUTH" \\
    -d "token=<RT_VALID>&token_type_hint=refresh_token"
EOF

G_HTTP=$(curl -s -o /dev/null -w "%{http_code}" \
  -X POST "$BASE/connect/revoke" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -H "Authorization: Basic $(echo -n "${CLIENT_ID}:${CLIENT_SECRET}" | base64)" \
  -d "token=${RT_VALID}&token_type_hint=refresh_token")

info "HTTP $G_HTTP"
assert_http "G1 — HTTP 200" "200" "$G_HTTP"


# =============================================================================
# TEST G2 — Introspect the refresh_token after revocation
# =============================================================================
label "TEST G2 — POST /connect/introspect  revoked refresh_token → active:false"

G2_RESP=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
  -X POST "$BASE/connect/introspect" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -H "Authorization: Basic $(echo -n "${CLIENT_ID}:${CLIENT_SECRET}" | base64)" \
  -d "token=${RT_VALID}&token_type_hint=refresh_token")

G2_HTTP=$(echo "$G2_RESP" | grep "__HTTP_STATUS__" | sed 's/__HTTP_STATUS__//')
G2_BODY=$(echo "$G2_RESP" | sed '/^__HTTP_STATUS__/d')

info "HTTP $G2_HTTP  body: $G2_BODY"
assert_http       "G2a — HTTP 200"       "200"   "$G2_HTTP"
assert_field_bool "G2b — active=false"   "active" "false" "$G2_BODY"


# =============================================================================
# TEST H — Revoke without token field (must be 400, not 200)
# Controller guard fires before RFC 7009 "always 200" rule
# =============================================================================
label "TEST H — POST /connect/revoke  missing token field (HTTP 400)"

H_RESP=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
  -X POST "$BASE/connect/revoke" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -H "Authorization: Basic $(echo -n "${CLIENT_ID}:${CLIENT_SECRET}" | base64)" \
  -d "token_type_hint=access_token")

H_HTTP=$(echo "$H_RESP" | grep "__HTTP_STATUS__" | sed 's/__HTTP_STATUS__//')
H_BODY=$(echo "$H_RESP" | sed '/^__HTTP_STATUS__/d')

info "HTTP $H_HTTP  body: $H_BODY"
assert_http "H1 — HTTP 400"               "400" "$H_HTTP"
echo "$H_BODY" | jq -e '.error == "invalid_request"' > /dev/null 2>&1 \
  && pass "H2 — error=invalid_request" \
  || fail "H2 — expected error=invalid_request in: $H_BODY"


# =============================================================================
# Summary
# =============================================================================
echo ""
echo -e "${CYAN}═══════════════════════════════════════════════════════════${NC}"
echo  " Test run complete."
echo  " Tests A B C D E G H run automatically."
echo  " Tests F C2 require expired tokens — see SKIP messages above."
echo  " Set AT_EXPIRED_OVERRIDE env var to enable Test F."
echo -e "${CYAN}═══════════════════════════════════════════════════════════${NC}"
