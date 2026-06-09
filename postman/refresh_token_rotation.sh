#!/usr/bin/env bash
# =============================================================================
# MonkoraEdge Core Auth — Refresh Token Rotation Test Suite
# =============================================================================
# Tests A, B, C + variants for every branch in RefreshTokenProcessor.
#
# Prerequisites
# ─────────────
#   • API running at http://localhost:5001
#   • A registered PUBLIC client:
#       client_id    = test-spa
#       grant_types  = AUTHORIZATION_CODE, REFRESH_TOKEN
#   • A valid refresh_token from a completed PKCE flow (see pkce_flow.http).
#     Paste it into RT_ZERO below before running.
#
# How to run:
#   chmod +x refresh_token_rotation.sh
#   ./refresh_token_rotation.sh
#
# The script is also useful as a reference for Postman/Bruno — each REQUEST
# and EXPECTED block maps directly to one test case.
# =============================================================================

BASE="http://localhost:5001"
CLIENT="test-spa"

# ── Seed: paste a fresh refresh_token from a completed PKCE flow ─────────────
RT_ZERO="PASTE_REFRESH_TOKEN_FROM_PKCE_FLOW_HERE"

# Colour helpers
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
pass() { echo -e "${GREEN}  ✓ PASS${NC}: $1"; }
fail() { echo -e "${RED}  ✗ FAIL${NC}: $1"; }
info() { echo -e "${YELLOW}  ►${NC} $1"; }

assert_http() {
  local label="$1" expected="$2" actual="$3"
  if [ "$actual" = "$expected" ]; then pass "$label (HTTP $actual)";
  else fail "$label — expected HTTP $expected, got HTTP $actual"; fi
}

assert_field() {
  local label="$1" field="$2" body="$3"
  echo "$body" | grep -q "\"$field\"" \
    && pass "$label (field '$field' present)" \
    || fail "$label (field '$field' MISSING)"
}

assert_error_code() {
  local label="$1" expected="$2" body="$3"
  echo "$body" | grep -q "\"error\":\"$expected\"" \
    && pass "$label (error='$expected')" \
    || fail "$label — expected error='$expected' in: $body"
}


# =============================================================================
# TEST A — Happy path: refresh token rotation
# =============================================================================
# Code path:
#   RefreshTokenProcessor.ValidateActiveAsync  → token found, IsRevoked=false, IsExpired=false
#   RefreshTokenProcessor.RotateAsync          → GenerateAccessTokenAsync + GenerateRefreshTokenAsync
#   TryRevokeWithRotationAsync                 → UPDATE tx_refresh_tokens SET revoked_at=..., replaced_by_token_id=...
#   New TokenResponse returned
#
# Expected HTTP 200:
# {
#   "access_token":  "<new-jwt>",
#   "token_type":    "Bearer",
#   "expires_in":    <int, ≤ 900>,
#   "refresh_token": "<new-opaque-token>",   ← DIFFERENT from RT_ZERO
#   "id_token":      "<jwt>",                ← present when openid scope in original grant
#   "scope":         "openid profile email"
# }
# Response headers:
#   Cache-Control: no-store
#   Pragma: no-cache
# =============================================================================
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo " TEST A — Happy path: first rotation"
echo "═══════════════════════════════════════════════════════════════"

RESPONSE_A=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
  -X POST "$BASE/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token&refresh_token=${RT_ZERO}&client_id=${CLIENT}")

HTTP_A=$(echo "$RESPONSE_A" | grep "__HTTP_STATUS__" | sed 's/__HTTP_STATUS__//')
BODY_A=$(echo "$RESPONSE_A" | sed '/^__HTTP_STATUS__/d')

info "Response body: $BODY_A"

assert_http      "A1 — HTTP status"              "200"  "$HTTP_A"
assert_field     "A2 — access_token present"     "access_token"  "$BODY_A"
assert_field     "A3 — refresh_token present"    "refresh_token" "$BODY_A"
assert_field     "A4 — token_type present"       "token_type"    "$BODY_A"
assert_field     "A5 — expires_in present"       "expires_in"    "$BODY_A"

# Capture the new rotation-1 refresh token for subsequent tests.
# Requires jq (brew install jq / apt install jq).
RT_ONE=$(echo "$BODY_A" | jq -r '.refresh_token // empty' 2>/dev/null)
AT_ONE=$(echo "$BODY_A" | jq -r '.access_token  // empty' 2>/dev/null)

if [ -n "$RT_ONE" ]; then
  pass "A6 — new refresh_token captured ($( echo "$RT_ONE" | cut -c1-20 )...)"
else
  fail "A6 — could not capture new refresh_token (is jq installed?)"
fi

# Verify expires_in ≤ 900 (MaxAccessTokenLifetimeSeconds)
EXPIRES_A=$(echo "$BODY_A" | jq -r '.expires_in // 0' 2>/dev/null)
[ "$EXPIRES_A" -le 900 ] 2>/dev/null \
  && pass "A7 — expires_in=$EXPIRES_A ≤ 900 (server cap)" \
  || fail "A7 — expires_in=$EXPIRES_A exceeds 900-second server cap"

# Verify old token (RT_ZERO) is NOT the same as the new token
[ "$RT_ONE" != "$RT_ZERO" ] \
  && pass "A8 — new refresh_token differs from old (rotation confirmed)" \
  || fail "A8 — refresh_token unchanged — rotation NOT applied"


# =============================================================================
# TEST B — Replay attack: present OLD refresh_token after rotation
# =============================================================================
# Code path:
#   ValidateActiveAsync: GetByTokenHashAsync(RT_ZERO) → found
#   storedToken.IsRevoked = true  (TryRevokeWithRotationAsync set revoked_at in Test A)
#   → RevokeTokenFamilyAsync(familyId, "refresh_token_reuse_detected")
#   → throws DomainException(TOKEN_REVOKED, "The refresh token has already been used…")
#   MapTokenDomainException: TOKEN_REVOKED → "invalid_grant" → HTTP 400
#
# SIDE EFFECT (verified in B2):
#   RevokeTokenFamilyAsync cascades to ALL tokens in the family:
#     - tx_refresh_tokens: all rows with same family_id → revoked_at = now
#     - tx_access_tokens:  all rows for same user/client → revoked_at = now
#   This means RT_ONE (issued in Test A) is NOW ALSO invalid.
#
# Expected HTTP 400:
# {
#   "error":             "invalid_grant",
#   "error_description": "The refresh token has already been used. All sessions in this chain have been revoked for security."
# }
# =============================================================================
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo " TEST B — Replay attack: reuse revoked refresh_token (RT_ZERO)"
echo "═══════════════════════════════════════════════════════════════"

RESPONSE_B=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
  -X POST "$BASE/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token&refresh_token=${RT_ZERO}&client_id=${CLIENT}")

HTTP_B=$(echo "$RESPONSE_B" | grep "__HTTP_STATUS__" | sed 's/__HTTP_STATUS__//')
BODY_B=$(echo "$RESPONSE_B" | sed '/^__HTTP_STATUS__/d')

info "Response body: $BODY_B"

assert_http       "B1 — HTTP status"             "400"          "$HTTP_B"
assert_error_code "B2 — error=invalid_grant"     "invalid_grant" "$BODY_B"
assert_field      "B3 — error_description field" "error_description" "$BODY_B"

# Confirm the exact message from RefreshTokenProcessor line 47
echo "$BODY_B" | grep -q "already been used" \
  && pass "B4 — error_description contains 'already been used'" \
  || fail "B4 — expected 'already been used' in error_description: $BODY_B"


# =============================================================================
# TEST B2 — Cascade verification: RT_ONE is also revoked after replay
# =============================================================================
# After the replay in Test B, RevokeTokenFamilyAsync revoked the entire family.
# RT_ONE (sibling in same chain) MUST also be rejected.
#
# Code path:
#   ValidateActiveAsync: GetByTokenHashAsync(RT_ONE) → found
#   storedToken.IsRevoked = true  (cascade from Test B)
#   → RevokeTokenFamilyAsync again (idempotent — already revoked)
#   → throws DomainException(TOKEN_REVOKED)
#
# Expected HTTP 400: same shape as Test B
# =============================================================================
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo " TEST B2 — Cascade: sibling token RT_ONE also revoked"
echo "═══════════════════════════════════════════════════════════════"

if [ -z "$RT_ONE" ]; then
  echo -e "${YELLOW}  SKIP${NC}: RT_ONE not captured (jq not available or Test A failed)"
else
  RESPONSE_B2=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
    -X POST "$BASE/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=refresh_token&refresh_token=${RT_ONE}&client_id=${CLIENT}")

  HTTP_B2=$(echo "$RESPONSE_B2" | grep "__HTTP_STATUS__" | sed 's/__HTTP_STATUS__//')
  BODY_B2=$(echo "$RESPONSE_B2" | sed '/^__HTTP_STATUS__/d')

  info "Response body: $BODY_B2"

  assert_http       "B2a — RT_ONE rejected (HTTP)"  "400"           "$HTTP_B2"
  assert_error_code "B2b — error=invalid_grant"     "invalid_grant" "$BODY_B2"
fi


# =============================================================================
# TEST B3 — Access token from Test A is also revoked (cascade to access tokens)
# =============================================================================
# RevokeTokenFamilyAsync calls _accessTokenRepo.GetActiveByUserIdAsync and marks
# all access tokens for the same (userId, clientId) pair as revoked.
# The JwtBearer OnTokenValidated hook will reject AT_ONE on the next request.
#
# Expected HTTP 401 on a protected endpoint (userinfo).
# =============================================================================
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo " TEST B3 — Cascade: access_token from Test A is also revoked"
echo "═══════════════════════════════════════════════════════════════"

if [ -z "$AT_ONE" ]; then
  echo -e "${YELLOW}  SKIP${NC}: AT_ONE not captured (jq not available or Test A failed)"
else
  RESPONSE_B3=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
    -X GET "$BASE/connect/userinfo" \
    -H "Authorization: Bearer $AT_ONE")

  HTTP_B3=$(echo "$RESPONSE_B3" | grep "__HTTP_STATUS__" | sed 's/__HTTP_STATUS__//')
  BODY_B3=$(echo "$RESPONSE_B3" | sed '/^__HTTP_STATUS__/d')

  info "Response body: $BODY_B3"
  assert_http "B3 — access_token from compromised chain rejected" "401" "$HTTP_B3"
fi


# =============================================================================
# TEST C — Expired: present refresh_token after ExpiresAt OR AbsoluteExpiresAt
# =============================================================================
# IsExpired = ExpiresAt <= UtcNow || AbsoluteExpiresAt <= UtcNow
#
# Code path (from RefreshTokenProcessor.ValidateActiveAsync):
#   storedToken.IsRevoked = false
#   storedToken.IsExpired = true
#   → throws DomainException(TOKEN_EXPIRED, "The refresh token has expired.")
#   MapTokenDomainException: TOKEN_EXPIRED → "invalid_grant" → HTTP 400
#
# To trigger in tests you need a token that has already passed ExpiresAt.
# The easiest way is to use a known-expired token from your DB:
#   SELECT refresh_token_hash, expires_at, absolute_expires_at
#   FROM tx_refresh_tokens
#   WHERE expires_at < now() AND revoked_at IS NULL
#   LIMIT 1;
#
# Alternatively: spin up a client with AccessTokenLifetime=1 and wait 2 seconds.
# The RT_EXPIRED variable below must be the raw token (not the hash).
#
# Expected HTTP 400:
# {
#   "error":             "invalid_grant",
#   "error_description": "The refresh token has expired."
# }
# =============================================================================
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo " TEST C — Expired refresh_token (past ExpiresAt or AbsoluteExpiresAt)"
echo "═══════════════════════════════════════════════════════════════"

RT_EXPIRED="${RT_EXPIRED_OVERRIDE:-PASTE_AN_EXPIRED_REFRESH_TOKEN_HERE}"

if [[ "$RT_EXPIRED" == PASTE* ]]; then
  echo -e "${YELLOW}  SKIP${NC}: set RT_EXPIRED_OVERRIDE env var to a known-expired refresh_token"
  echo "  Example: export RT_EXPIRED_OVERRIDE=<token>  then re-run the script."
else
  RESPONSE_C=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
    -X POST "$BASE/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=refresh_token&refresh_token=${RT_EXPIRED}&client_id=${CLIENT}")

  HTTP_C=$(echo "$RESPONSE_C" | grep "__HTTP_STATUS__" | sed 's/__HTTP_STATUS__//')
  BODY_C=$(echo "$RESPONSE_C" | sed '/^__HTTP_STATUS__/d')

  info "Response body: $BODY_C"

  assert_http       "C1 — HTTP status"             "400"           "$HTTP_C"
  assert_error_code "C2 — error=invalid_grant"     "invalid_grant" "$BODY_C"

  echo "$BODY_C" | grep -q "expired" \
    && pass "C3 — error_description contains 'expired'" \
    || fail "C3 — expected 'expired' in error_description: $BODY_C"
fi


# =============================================================================
# TEST C2 — AbsoluteExpiresAt boundary: token is not sliding-window expired
#           but has passed the chain's hard deadline
# =============================================================================
# IsExpired checks BOTH ExpiresAt and AbsoluteExpiresAt. A token that was
# rotated many times may have ExpiresAt in the future but AbsoluteExpiresAt
# already passed (chain lifetime exceeded).
#
# Same code path as Test C — same HTTP 400 / invalid_grant response.
# The field that fires: AbsoluteExpiresAt <= DateTime.UtcNow.
#
# To generate: issue a token with RefreshTokenLifetime large but  
#              AbsoluteExpiresAt manually set in the past via DB update:
#   UPDATE tx_refresh_tokens
#   SET absolute_expires_at = now() - interval '1 second'
#   WHERE id = '<id>';
#
# Then use the raw token string as RT_ABSOLUTE_EXPIRED_OVERRIDE.
# =============================================================================
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo " TEST C2 — AbsoluteExpiresAt chain hard deadline exceeded"
echo "═══════════════════════════════════════════════════════════════"

RT_ABS_EXPIRED="${RT_ABSOLUTE_EXPIRED_OVERRIDE:-PASTE_ABS_EXPIRED_TOKEN_HERE}"

if [[ "$RT_ABS_EXPIRED" == PASTE* ]]; then
  echo -e "${YELLOW}  SKIP${NC}: set RT_ABSOLUTE_EXPIRED_OVERRIDE env var to a token whose absolute_expires_at is in the past"
else
  RESPONSE_C2=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
    -X POST "$BASE/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=refresh_token&refresh_token=${RT_ABS_EXPIRED}&client_id=${CLIENT}")

  HTTP_C2=$(echo "$RESPONSE_C2" | grep "__HTTP_STATUS__" | sed 's/__HTTP_STATUS__//')
  BODY_C2=$(echo "$RESPONSE_C2" | sed '/^__HTTP_STATUS__/d')

  info "Response body: $BODY_C2"

  assert_http       "C2a — HTTP status"         "400"           "$HTTP_C2"
  assert_error_code "C2b — error=invalid_grant" "invalid_grant" "$BODY_C2"
fi


# =============================================================================
# TEST D — Unknown token: token never existed in the database
# =============================================================================
# Code path:
#   GetByTokenHashAsync(hash) → null
#   → throws DomainException(INVALID_GRANT, "The refresh token is invalid.")
#   → HTTP 400 invalid_grant
#
# Expected HTTP 400:
# {
#   "error":             "invalid_grant",
#   "error_description": "The refresh token is invalid."
# }
# =============================================================================
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo " TEST D — Unknown token: token not in the database"
echo "═══════════════════════════════════════════════════════════════"

RESPONSE_D=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
  -X POST "$BASE/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token&refresh_token=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA&client_id=${CLIENT}")

HTTP_D=$(echo "$RESPONSE_D" | grep "__HTTP_STATUS__" | sed 's/__HTTP_STATUS__//')
BODY_D=$(echo "$RESPONSE_D" | sed '/^__HTTP_STATUS__/d')

info "Response body: $BODY_D"

assert_http       "D1 — HTTP status"         "400"           "$HTTP_D"
assert_error_code "D2 — error=invalid_grant" "invalid_grant" "$BODY_D"


# =============================================================================
# TEST E — Missing refresh_token field entirely
# =============================================================================
# Code path:
#   OAuth2Service.RefreshTokenGrantAsync: string.IsNullOrEmpty(request.RefreshToken)
#   → throws DomainException(INVALID_REQUEST, "refresh_token is required.")
#   → HTTP 400 invalid_request
#
# Expected HTTP 400:
# {
#   "error":             "invalid_request",
#   "error_description": "refresh_token is required."
# }
# =============================================================================
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo " TEST E — Missing refresh_token field"
echo "═══════════════════════════════════════════════════════════════"

RESPONSE_E=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
  -X POST "$BASE/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token&client_id=${CLIENT}")

HTTP_E=$(echo "$RESPONSE_E" | grep "__HTTP_STATUS__" | sed 's/__HTTP_STATUS__//')
BODY_E=$(echo "$RESPONSE_E" | sed '/^__HTTP_STATUS__/d')

info "Response body: $BODY_E"

assert_http       "E1 — HTTP status"           "400"            "$HTTP_E"
assert_error_code "E2 — error=invalid_request" "invalid_request" "$BODY_E"


# =============================================================================
# TEST F — Scope narrowing on refresh (RFC 6749 §6)
# =============================================================================
# A client MAY request a subset of the original scopes on refresh.
# Example: original grant was "openid profile email", request only "openid email".
# Server MUST NOT expand — exceed original scopes must return invalid_scope.
# =============================================================================
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo " TEST F — Scope expansion beyond original grant (must fail)"
echo "═══════════════════════════════════════════════════════════════"
echo "  (Requires a fresh valid refresh_token — set RT_SCOPE_TEST_OVERRIDE)"

RT_SCOPE="${RT_SCOPE_TEST_OVERRIDE:-$RT_ZERO}"

RESPONSE_F=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
  -X POST "$BASE/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token&refresh_token=${RT_SCOPE}&client_id=${CLIENT}&scope=openid+profile+email+admin")

HTTP_F=$(echo "$RESPONSE_F" | grep "__HTTP_STATUS__" | sed 's/__HTTP_STATUS__//')
BODY_F=$(echo "$RESPONSE_F" | sed '/^__HTTP_STATUS__/d')

info "Response body: $BODY_F"

assert_http       "F1 — HTTP status"         "400"          "$HTTP_F"
assert_error_code "F2 — error=invalid_scope" "invalid_scope" "$BODY_F"


# =============================================================================
# Summary
# =============================================================================
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo " Test run complete."
echo " Review ✓ PASS / ✗ FAIL lines above."
echo " Skipped tests require RT_EXPIRED_OVERRIDE / RT_ABSOLUTE_EXPIRED_OVERRIDE"
echo " environment variables — see comments in each TEST C block."
echo "═══════════════════════════════════════════════════════════════"
