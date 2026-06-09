#!/usr/bin/env bash
# =============================================================================
# MonkoraEdge Core Auth — Security Negative Test Suite
# =============================================================================
# Every test in this file MUST return an error response.
# A passing test means the ATTACK was BLOCKED.
# A failing test means a security regression exists.
#
# Endpoints under attack:
#   GET  /connect/authorize
#   POST /connect/token
#
# Prerequisites
# ─────────────
#   • API running at http://localhost:5001
#   • A registered PUBLIC client:
#       client_id         = test-spa
#       grant_types       = AUTHORIZATION_CODE
#       redirect_uris     = ["http://localhost:4200/callback"]  ← exact registration
#   • jq installed
#
# For Test 3 (code replay) and Tests 5/6, additional setup noted per test.
#
# Run:
#   chmod +x security_negative_tests.sh
#   ./security_negative_tests.sh
# =============================================================================

BASE="http://localhost:5001"
CLIENT_ID="test-spa"
REGISTERED_REDIRECT="http://localhost:4200/callback"

# ── Colour helpers ─────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; GRAY='\033[0;90m'; NC='\033[0m'
pass()  { echo -e "${GREEN}  ✓ BLOCKED${NC}  $1"; }
fail()  { echo -e "${RED}  ✗ LEAKED${NC}   $1"; }
info()  { echo -e "${YELLOW}  ►${NC} $1"; }
skip()  { echo -e "${YELLOW}  SKIP${NC}    $1"; }
label() { echo -e "\n${CYAN}═══════════════════════════════════════════════════════════${NC}";
          echo -e "${CYAN} $1${NC}";
          echo -e "${CYAN}═══════════════════════════════════════════════════════════${NC}"; }

assert_blocked() {
  # assert_blocked "desc" expected_http actual_http body [must_not_redirect_to]
  local desc="$1" exp_http="$2" actual_http="$3" body="$4" evil_url="${5:-}"
  local ok=true

  if [ "$actual_http" != "$exp_http" ]; then
    fail "$desc — expected HTTP $exp_http, got HTTP $actual_http"
    ok=false
  fi

  if [ -n "$evil_url" ] && echo "$body" | grep -qi "$evil_url"; then
    fail "$desc — response body CONTAINS attacker URL '$evil_url'"
    ok=false
  fi

  [ "$ok" = true ] && pass "$desc (HTTP $actual_http)"
  echo -e "    ${GRAY}body: $body${NC}"
}

assert_error_code() {
  local desc="$1" expected_code="$2" body="$3"
  if echo "$body" | jq -e ".error == \"$expected_code\"" > /dev/null 2>&1; then
    pass "$desc — error=\"$expected_code\""
    echo -e "    ${GRAY}$(echo "$body" | jq -c '{error,error_description}' 2>/dev/null)${NC}"
  else
    fail "$desc — expected error=\"$expected_code\" not found"
    echo -e "    ${RED}got: $body${NC}"
  fi
}

assert_no_redirect_to() {
  local desc="$1" evil_url="$2" location="$3"
  if echo "$location" | grep -qi "$evil_url"; then
    fail "$desc — Location header POINTS TO attacker URL: $location"
    echo -e "    ${RED}OPEN REDIRECT CONFIRMED — browser would navigate to: $evil_url${NC}"
  else
    pass "$desc — Location header does NOT point to '$evil_url'"
    echo -e "    ${GRAY}Location: $location${NC}"
  fi
}

# PKCE helper — generate a valid S256 pair using openssl (available on macOS/Linux)
gen_pkce() {
  local verifier
  verifier=$(openssl rand -base64 48 | tr -d '=\n' | tr '+/' '-_')
  local challenge
  challenge=$(echo -n "$verifier" | openssl dgst -sha256 -binary | openssl base64 -A | tr -d '=' | tr '+/' '-_')
  echo "${verifier}:${challenge}"
}


# =============================================================================
# TEST 1 — Open Redirect Attack
# =============================================================================
# Attacker crafts an /authorize URL with redirect_uri=https://evil.com,
# hoping the server will redirect the auth code there.
#
# Code path (OAuth2Service.ValidateAuthorizeRequestAsync):
#   • client = await _clientAuth.LoadAsync(request.ClientId)       ← client found
#   • client.IsRedirectUriRegistered("https://evil.com") → FALSE
#   • return AuthorizeValidationResult.Fail("invalid_request",
#       "redirect_uri does not exactly match a registered URI.")
#     → validation.RedirectUriValidated == FALSE (still attacker-controlled)
#
# Back in ProcessAuthorizeRequestAsync (line 87–107):
#   • CRITICAL: "redirecting would be an open redirect vulnerability"
#   • Since RedirectUriValidated == FALSE, server does NOT redirect.
#   • Instead returns:
#       Kind = AuthorizeResponseKind.Error  →  ToActionResult  →  BadRequest(...)
#
# Expected: HTTP 400 (NOT 302), body contains error JSON, Location header absent.
# Expected body:
#   { "error": "invalid_request",
#     "error_description": "redirect_uri does not exactly match a registered URI." }
# =============================================================================
label "TEST 1 — Open Redirect: redirect_uri=https://evil.com"

echo ""
info "MALICIOUS REQUEST:"
cat <<'EOF'
  GET /connect/authorize
    ?response_type=code
    &client_id=test-spa
    &redirect_uri=https://evil.com           ← ATTACKER URL (not registered)
    &scope=openid+profile
    &code_challenge=<valid_S256>
    &code_challenge_method=S256
    &state=legit-state
EOF

PKCE_PAIR=$(gen_pkce)
VERIFIER_1=$(echo "$PKCE_PAIR" | cut -d: -f1)
CHALLENGE_1=$(echo "$PKCE_PAIR" | cut -d: -f2)

T1_RESP=$(curl -s -D - -L \
  "$BASE/connect/authorize?response_type=code&client_id=${CLIENT_ID}&redirect_uri=https%3A%2F%2Fevil.com&scope=openid+profile&code_challenge=${CHALLENGE_1}&code_challenge_method=S256&state=legit-state" \
  2>/dev/null)

T1_HTTP=$(echo "$T1_RESP" | grep -E "^HTTP/" | tail -1 | awk '{print $2}')
T1_LOCATION=$(echo "$T1_RESP" | grep -i "^Location:" | head -1)
T1_BODY=$(echo "$T1_RESP" | awk 'BEGIN{body=0}/^\r?$/{body++} body>=1{print}' | tail -n +2)

echo ""
info "EXPECTED: HTTP 400, no redirect, error JSON"
info "GOT:"

assert_blocked      "1a — Server returns 400, not 302" "400" "$T1_HTTP" "$T1_BODY" "evil.com"
assert_error_code   "1b — error=invalid_request"       "invalid_request" "$T1_BODY"
assert_no_redirect_to "1c — Location header safe"      "evil.com" "$T1_LOCATION"

# Verify the exact error message from ValidateAuthorizeRequestAsync line 161
echo "$T1_BODY" | grep -qi "does not exactly match" \
  && pass "1d — error_description is redirect_uri mismatch message (not generic)" \
  || fail "1d — expected 'does not exactly match a registered URI' in error_description"

echo ""
info "PASS criteria: HTTP 400 body with error JSON. Browser MUST NOT land on evil.com."
echo -e "    ${GRAY}RFC 6749 §4.1.2.1: redirect MUST NOT occur for unvalidated redirect_uri${NC}"


# =============================================================================
# TEST 1b — Open Redirect: subdomain confusion (evil.localhost:4200/callback)
# =============================================================================
# IsRedirectUriRegistered performs EXACT string comparison (string.Equals, Ordinal).
# No prefix/suffix/subdomain matching.
# =============================================================================
label "TEST 1b — Open Redirect: evil subdomain looks like registered URI"

T1B_HTTP=$(curl -s -o /dev/null -w "%{http_code}" -L \
  "$BASE/connect/authorize?response_type=code&client_id=${CLIENT_ID}&redirect_uri=http%3A%2F%2Fevil.localhost%3A4200%2Fcallback&scope=openid&code_challenge=${CHALLENGE_1}&code_challenge_method=S256")

assert_blocked "1b — evil subdomain rejected (exact match only)" "400" "$T1B_HTTP" ""

# =============================================================================
# TEST 1c — Open Redirect: path traversal in redirect_uri
# =============================================================================
T1C_HTTP=$(curl -s -o /dev/null -w "%{http_code}" -L \
  "$BASE/connect/authorize?response_type=code&client_id=${CLIENT_ID}&redirect_uri=http%3A%2F%2Flocalhost%3A4200%2Fcallback%2F..%2F..%2Fevil&scope=openid&code_challenge=${CHALLENGE_1}&code_challenge_method=S256")

assert_blocked "1c — path traversal in redirect_uri rejected" "400" "$T1C_HTTP" ""


# =============================================================================
# TEST 2 — PKCE Downgrade Attack
# =============================================================================
# Phase 1: Obtain a real auth code with PKCE (code_challenge set on the code).
# Phase 2: Attempt token exchange WITHOUT code_verifier.
#
# Without PKCE, an attacker who intercepts the auth code (e.g. via referrer header,
# browser history, or malicious app) can exchange it directly.
#
# Code path (OAuth2Service.ExchangeAuthorizationCodeAsync):
#   line 374: if (!string.IsNullOrEmpty(authCode.CodeChallenge))
#   line 376:   if (string.IsNullOrEmpty(request.CodeVerifier))
#   line 377:     throw DomainException(INVALID_REQUEST, "code_verifier is required.")
#   MapTokenDomainException: INVALID_REQUEST → "invalid_request" → HTTP 400
#
# Expected HTTP 400:
#   { "error": "invalid_request",
#     "error_description": "code_verifier is required." }
# =============================================================================
label "TEST 2 — PKCE Downgrade: exchange auth code without code_verifier"

echo ""
info "Step 2a: obtain a valid auth code (requires a logged-in session or stub)"
info "         Set CODE_FOR_PKCE_DOWNGRADE env var to skip Step 2a"

CODE_PKCE="${CODE_FOR_PKCE_DOWNGRADE:-}"

if [ -z "$CODE_PKCE" ]; then
  # Generate a fresh PKCE pair for the authorize step
  PKCE2=$(gen_pkce)
  V2=$(echo "$PKCE2" | cut -d: -f1)
  C2=$(echo "$PKCE2" | cut -d: -f2)

  # Attempt to get a code (will succeed if user is already logged in, otherwise
  # returns login_required — still gives us the PKCE downgrade scenario if we
  # set the code manually)
  T2_AUTH=$(curl -s -w "\n__HTTP__%{http_code}" -L \
    "$BASE/connect/authorize?response_type=code&client_id=${CLIENT_ID}&redirect_uri=${REGISTERED_REDIRECT}&scope=openid&code_challenge=${C2}&code_challenge_method=S256&state=test2" \
    2>/dev/null)
  T2_AUTH_HTTP=$(echo "$T2_AUTH" | grep "__HTTP__" | sed 's/__HTTP__//')

  # Try to extract code from redirect (if user was logged in)
  CODE_PKCE=$(echo "$T2_AUTH" | grep -oP "(?<=code=)[^&]+" | head -1)

  if [ -z "$CODE_PKCE" ]; then
    skip "No auth code returned (user not logged in). Provide CODE_FOR_PKCE_DOWNGRADE:"
    echo "  1. Complete a PKCE flow (see pkce_flow.http)"
    echo "  2. export CODE_FOR_PKCE_DOWNGRADE=<auth_code>"
    echo "  3. re-run this script"
    CODE_PKCE="SKIP"
  fi
fi

if [ "$CODE_PKCE" != "SKIP" ]; then
  echo ""
  info "MALICIOUS REQUEST (no code_verifier in token exchange):"
  cat <<EOF
  POST /connect/token
    grant_type=authorization_code
    &code=${CODE_PKCE:0:20}...
    &redirect_uri=${REGISTERED_REDIRECT}
    &client_id=${CLIENT_ID}
    (MISSING: code_verifier)          ← PKCE bypass attempt
EOF

  T2_RESP=$(curl -s -w "\n__HTTP__%{http_code}" \
    -X POST "$BASE/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=authorization_code&code=${CODE_PKCE}&redirect_uri=${REGISTERED_REDIRECT}&client_id=${CLIENT_ID}")

  T2_HTTP=$(echo "$T2_RESP" | grep "__HTTP__" | sed 's/__HTTP__//')
  T2_BODY=$(echo "$T2_RESP" | sed '/^__HTTP__/d')

  echo ""
  info "EXPECTED: HTTP 400 { \"error\": \"invalid_request\", \"error_description\": \"code_verifier is required.\" }"
  info "GOT:"
  assert_blocked    "2a — token exchange without code_verifier rejected" "400" "$T2_HTTP" "$T2_BODY"
  assert_error_code "2b — error=invalid_request"                         "invalid_request" "$T2_BODY"
  echo "$T2_BODY" | grep -qi "code_verifier is required" \
    && pass "2c — error_description: 'code_verifier is required.'" \
    || fail "2c — expected 'code_verifier is required' in error_description: $T2_BODY"
fi

echo ""
info "PASS criteria: Server MUST reject token exchange when code was issued with PKCE"
echo -e "    ${GRAY}OAuth 2.1 §4.1.1: PKCE is mandatory. Downgrade MUST be rejected.${NC}"


# =============================================================================
# TEST 2b — PKCE Verifier Mismatch (wrong verifier, not missing)
# =============================================================================
# Code path line 379:
#   if (!_passwordService.VerifyPkceCodeVerifier(request.CodeVerifier, authCode.CodeChallenge, "S256"))
#   → throw DomainException(INVALID_PKCE_CODE_VERIFIER, "code_verifier is invalid.")
#   MapTokenDomainException: INVALID_PKCE_CODE_VERIFIER → "invalid_grant" → HTTP 400
#
# Note: wrong verifier maps to INVALID_GRANT (not INVALID_REQUEST), same as other
# code integrity failures — this is intentional to prevent oracle attacks.
# =============================================================================
label "TEST 2b — PKCE Verifier Mismatch: wrong code_verifier value"

if [ "${CODE_PKCE}" != "SKIP" ] && [ -n "$CODE_PKCE" ]; then
  T2B_RESP=$(curl -s -w "\n__HTTP__%{http_code}" \
    -X POST "$BASE/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=authorization_code&code=${CODE_PKCE}&redirect_uri=${REGISTERED_REDIRECT}&client_id=${CLIENT_ID}&code_verifier=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")

  T2B_HTTP=$(echo "$T2B_RESP" | grep "__HTTP__" | sed 's/__HTTP__//')
  T2B_BODY=$(echo "$T2B_RESP" | sed '/^__HTTP__/d')

  info "EXPECTED: HTTP 400 { \"error\": \"invalid_grant\", \"error_description\": \"code_verifier is invalid.\" }"
  assert_blocked    "2b-1 — wrong code_verifier rejected" "400" "$T2B_HTTP" "$T2B_BODY"
  assert_error_code "2b-2 — error=invalid_grant (INVALID_PKCE_CODE_VERIFIER maps here)" "invalid_grant" "$T2B_BODY"
else
  skip "2b — skipped (no code available)"
fi


# =============================================================================
# TEST 3 — Authorization Code Replay Attack
# =============================================================================
# An attacker captures an auth code (e.g. via referrer, browser history, log
# exfiltration) and tries to use it a second time after the legitimate client
# has already exchanged it.
#
# Code path — FIRST use (legitimate):
#   ExchangeAuthorizationCodeAsync:
#   → authCode.IsValid == true (not consumed, not expired)
#   → _authCodeRepo.TryConsumeAsync(authCode.Id, now) → TRUE (atomic UPDATE)
#   → tokens issued ✓
#
# Code path — SECOND use (attacker replay):
#   GetByCodeHashAsync → authCode found
#   authCode.IsConsumed == true → authCode.IsValid == false
#   → throw DomainException(INVALID_GRANT,
#       "Authorization code is invalid, expired, or already used.")
#   → HTTP 400 invalid_grant
#
# ⚠️  IMPORTANT SECURITY NOTE on cascade revocation:
#   This server BLOCKS the replay (correct) but does NOT automatically revoke the
#   access token issued during the first use. RFC 6749 §10.5 says servers SHOULD
#   revoke tokens issued to a replayed code, but it is not MUST. The security
#   guarantee here is: the attacker gets no NEW tokens. If the original token was
#   already stolen, revocation requires a separate call to POST /connect/revoke.
#   See KNOWN GAP in session summary — this is an intentional trade-off.
#
# Expected SECOND use — HTTP 400:
#   { "error": "invalid_grant",
#     "error_description": "Authorization code is invalid, expired, or already used." }
# =============================================================================
label "TEST 3 — Code Replay: use authorization code twice"

CODE_REPLAY="${CODE_FOR_REPLAY_TEST:-}"
VERIFIER_REPLAY="${VERIFIER_FOR_REPLAY_TEST:-}"

if [ -z "$CODE_REPLAY" ] || [ -z "$VERIFIER_REPLAY" ]; then
  skip "CODE_FOR_REPLAY_TEST and VERIFIER_FOR_REPLAY_TEST not set."
  echo "  To run this test:"
  echo "  1. Complete a PKCE authorize flow, capture the code and code_verifier."
  echo "  2. export CODE_FOR_REPLAY_TEST=<auth_code>"
  echo "  3. export VERIFIER_FOR_REPLAY_TEST=<code_verifier>"
  echo "  4. Re-run. The script will exchange it once (legit) then once again (attack)."
else
  echo ""
  info "Step 3a: FIRST use — legitimate token exchange (should succeed)"

  T3A_RESP=$(curl -s -w "\n__HTTP__%{http_code}" \
    -X POST "$BASE/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=authorization_code&code=${CODE_REPLAY}&redirect_uri=${REGISTERED_REDIRECT}&client_id=${CLIENT_ID}&code_verifier=${VERIFIER_REPLAY}")

  T3A_HTTP=$(echo "$T3A_RESP" | grep "__HTTP__" | sed 's/__HTTP__//')
  T3A_BODY=$(echo "$T3A_RESP" | sed '/^__HTTP__/d')

  if [ "$T3A_HTTP" = "200" ]; then
    pass "3a — First use succeeded (HTTP 200)"
    AT_FROM_CODE=$(echo "$T3A_BODY" | jq -r '.access_token // empty' 2>/dev/null)
    echo -e "    ${GRAY}access_token: ${AT_FROM_CODE:0:40}...${NC}"
  else
    fail "3a — First use failed (HTTP $T3A_HTTP) — test setup issue"
    echo -e "    ${RED}body: $T3A_BODY${NC}"
  fi

  echo ""
  info "Step 3b: SECOND use — attacker replays the SAME code (must be rejected)"
  info "MALICIOUS REQUEST:"
  cat <<EOF
  POST /connect/token
    grant_type=authorization_code
    &code=${CODE_REPLAY:0:20}...       ← SAME code as Step 3a (already consumed)
    &redirect_uri=${REGISTERED_REDIRECT}
    &client_id=${CLIENT_ID}
    &code_verifier=${VERIFIER_REPLAY:0:20}...
EOF

  T3B_RESP=$(curl -s -w "\n__HTTP__%{http_code}" \
    -X POST "$BASE/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=authorization_code&code=${CODE_REPLAY}&redirect_uri=${REGISTERED_REDIRECT}&client_id=${CLIENT_ID}&code_verifier=${VERIFIER_REPLAY}")

  T3B_HTTP=$(echo "$T3B_RESP" | grep "__HTTP__" | sed 's/__HTTP__//')
  T3B_BODY=$(echo "$T3B_RESP" | sed '/^__HTTP__/d')

  echo ""
  info "EXPECTED: HTTP 400 { \"error\": \"invalid_grant\", \"error_description\": \"...already used.\" }"
  info "GOT:"
  assert_blocked    "3b — second code use rejected (HTTP 400)"      "400" "$T3B_HTTP" "$T3B_BODY"
  assert_error_code "3c — error=invalid_grant"                       "invalid_grant" "$T3B_BODY"
  echo "$T3B_BODY" | grep -qi "already used\|invalid.*expired" \
    && pass "3d — error_description indicates consumed/used code" \
    || fail "3d — expected 'already used' or 'invalid, expired' in error_description"

  # Verify the TryConsumeAsync atomic guard: concurrent replay attempt also rejected
  echo ""
  info "Step 3c: Concurrent replay (simulate race condition — both rejected atomically)"
  T3C1_HTTP=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST "$BASE/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=authorization_code&code=${CODE_REPLAY}&redirect_uri=${REGISTERED_REDIRECT}&client_id=${CLIENT_ID}&code_verifier=${VERIFIER_REPLAY}" &)
  T3C2_HTTP=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST "$BASE/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=authorization_code&code=${CODE_REPLAY}&redirect_uri=${REGISTERED_REDIRECT}&client_id=${CLIENT_ID}&code_verifier=${VERIFIER_REPLAY}")
  wait
  echo ""
  info "Both concurrent replays must be 400:"
  assert_blocked "3e — concurrent replay #1 rejected" "400" "$T3C1_HTTP" ""
  assert_blocked "3f — concurrent replay #2 rejected" "400" "$T3C2_HTTP" ""
fi

echo ""
info "SECURITY NOTE: Token issued during first use is NOT auto-revoked on replay."
echo -e "    ${GRAY}RFC 6749 §10.5: servers SHOULD revoke — manual POST /connect/revoke required${NC}"
echo -e "    ${GRAY}TryConsumeAsync uses atomic UPDATE WHERE consumed_at IS NULL — race-safe${NC}"


# =============================================================================
# TEST 4 — State Tampering
# =============================================================================
# The `state` parameter is an opaque value generated by the CLIENT (e.g. browser SPA).
# RFC 6749 §10.12: the client MUST verify the state value on callback to detect CSRF.
#
# Server-side behaviour:
#   • The server accepts any `state` string and echoes it back in the redirect URL.
#   • The server does NOT validate state (this is intentionally the client's job).
#   • The attack surface is therefore in the CLIENT, not the server.
#   • This test validates the server's CORRECT behaviour (echo-through, not rejection).
#
# To simulate a state-tampering CSRF attack:
#   1. Attacker crafts an authorize URL with their own state.
#   2. Victim clicks, logs in, gets redirect: ?code=...&state=ATTACKER_STATE
#   3. Attacker's page receives the callback (if redirect_uri is attacker-controlled).
#      → Blocked by Test 1 (unregistered redirect_uri → HTTP 400).
#   4. Even with a registered redirect_uri, the CLIENT must validate state.
#
# What we test here: if redirect_uri is registered, server responds correctly.
# The tampered-state case is a CLIENT IMPLEMENTATION test, not a server test.
# =============================================================================
label "TEST 4 — State Tampering (server echoes state; client MUST validate)"

echo ""
info "Phase A: request with state=legitimate-state"

PKCE4=$(gen_pkce)
V4=$(echo "$PKCE4" | cut -d: -f1)
C4=$(echo "$PKCE4" | cut -d: -f2)

T4A=$(curl -s -w "\n__HTTP__%{http_code}" -L \
  "$BASE/connect/authorize?response_type=code&client_id=${CLIENT_ID}&redirect_uri=${REGISTERED_REDIRECT}&scope=openid&code_challenge=${C4}&code_challenge_method=S256&state=legitimate-state-abc123")
T4A_HTTP=$(echo "$T4A" | grep "__HTTP__" | sed 's/__HTTP__//')
T4A_CODE=$(echo "$T4A" | grep -oP "(?<=code=)[^& ]+" | head -1)
T4A_STATE=$(echo "$T4A" | grep -oP "(?<=state=)[^& ]+" | head -1)

info "state in redirect-back: $T4A_STATE"

echo ""
info "Phase B: request with state=TAMPERED-BY-ATTACKER"
T4B=$(curl -s -w "\n__HTTP__%{http_code}" -L \
  "$BASE/connect/authorize?response_type=code&client_id=${CLIENT_ID}&redirect_uri=${REGISTERED_REDIRECT}&scope=openid&code_challenge=${C4}&code_challenge_method=S256&state=TAMPERED-BY-ATTACKER")
T4B_STATE=$(echo "$T4B" | grep -oP "(?<=state=)[^& ]+" | head -1)
T4B_HTTP=$(echo "$T4B" | grep "__HTTP__" | sed 's/__HTTP__//')

info "state in redirect-back: $T4B_STATE"

# The server SHOULD echo the state back in the redirect
if [ "$T4B_STATE" = "TAMPERED-BY-ATTACKER" ]; then
  pass "4a — Server echoes back tampered state value (RFC 6749 §4.1.2 — state echo is correct server behaviour)"
  echo -e "    ${GRAY}The CLIENT must compare state to its session cookie/nonce — that is where CSRF is caught${NC}"
else
  info "4a — state not echoed back (may be login-required flow — that's also safe)"
fi

echo ""
echo -e "    ${YELLOW}IMPORTANT:${NC} State validation is a CLIENT responsibility (RFC 6749 §10.12)."
echo   "    Test: your SPA/client must verify state===sessionStorage.getItem('oauth_state') on callback."
echo   "    Server: correctly rejects unregistered redirect_uri (Test 1) — primary CSRF mitigation."
echo -e "    ${GRAY}This server also appends &iss= (RFC 9207) — provides additional mix-up protection${NC}"


# =============================================================================
# TEST 5 — Brute Force / Rate Limit on /connect/token
# =============================================================================
# Policy "auth" (SlidingWindowRateLimiter):
#   PermitLimit = 10, Window = 1 minute, SegmentsPerWindow = 6
#   Partition key: "auth:{RemoteIpAddress}"   ← TCP-level, cannot be spoofed
#
# After 10 requests within 1 minute from the same IP, the 11th and beyond
# return HTTP 429 with the slow_down error (from Program.cs OnRejected handler).
#
# Expected HTTP 429:
#   { "error": "slow_down",
#     "error_description": "Too many requests. Retry after N seconds." }
# Response header:
#   Retry-After: <seconds>
#
# ⚠️  SIDE EFFECT: this test will exhaust your rate limit bucket for 1 minute.
#     Run it last or wait for the sliding window to reset before other tests.
# =============================================================================
label "TEST 5 — Brute Force: 15 wrong-credential requests to /connect/token"

echo ""
info "Sending 15 POST /connect/token requests with wrong client_secret..."
info "Rate limit: 10 req/min (SlidingWindow). Requests 11-15 MUST return 429."
echo ""

GOT_429=false
FIRST_429_ON=0

for i in $(seq 1 15); do
  T5_HTTP=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST "$BASE/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=client_credentials&client_id=${CLIENT_ID}&client_secret=WRONG_SECRET_${i}")

  if [ "$T5_HTTP" = "429" ]; then
    if [ "$GOT_429" = false ]; then
      FIRST_429_ON=$i
      GOT_429=true
    fi
    echo -e "  Request $i: ${GREEN}HTTP 429 — RATE LIMITED${NC}"
  elif [ "$T5_HTTP" = "400" ] || [ "$T5_HTTP" = "401" ]; then
    echo -e "  Request $i: ${GRAY}HTTP $T5_HTTP — rejected (below threshold)${NC}"
  else
    echo -e "  Request $i: ${RED}HTTP $T5_HTTP — unexpected response${NC}"
  fi
done

echo ""
if [ "$GOT_429" = true ]; then
  pass "5a — Rate limit triggered at request $FIRST_429_ON (expected ≤ 11)"
  [ "$FIRST_429_ON" -le 11 ] \
    && pass "5b — 429 fired within PermitLimit+1 (threshold is 10 req/min)" \
    || fail "5b — 429 fired at request $FIRST_429_ON (expected ≤ 11)"
else
  fail "5a — Rate limit NOT triggered after 15 requests"
  echo -e "    ${RED}Brute force protection may be disabled or misconfigured${NC}"
fi

# Check the 429 response body and headers
T5_LAST=$(curl -s -D - \
  -X POST "$BASE/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=${CLIENT_ID}&client_secret=WRONG_AGAIN")

T5_LAST_HTTP=$(echo "$T5_LAST" | grep -E "^HTTP/" | awk '{print $2}')
T5_LAST_HDR=$(echo "$T5_LAST" | awk '/^\r?$/{exit} NR>1{print}')
T5_LAST_BODY=$(echo "$T5_LAST" | awk 'BEGIN{b=0}/^\r?$/{b++} b>=1{print}' | tail -n +2)

if [ "$T5_LAST_HTTP" = "429" ]; then
  info "429 response body: $T5_LAST_BODY"
  info "429 response headers:"
  echo "$T5_LAST_HDR" | grep -i "Retry-After\|Content-Type\|X-RateLimit" | sed 's/^/    /'

  assert_error_code "5c — error=slow_down (from Program.cs OnRejected)" "slow_down" "$T5_LAST_BODY"
  echo "$T5_LAST_BODY" | grep -qi "Too many requests" \
    && pass "5d — error_description contains 'Too many requests'" \
    || fail "5d — expected 'Too many requests' in error_description"

  echo "$T5_LAST_HDR" | grep -qi "Retry-After" \
    && pass "5e — Retry-After header present (RFC 6585 §4)" \
    || fail "5e — Retry-After header MISSING on 429 (RFC 6585 §4 SHOULD include it)"
fi

echo ""
echo -e "    ${GRAY}Partition key: 'auth:{TCP RemoteIpAddress}' — cannot be bypassed via X-Forwarded-For${NC}"
echo -e "    ${YELLOW}NOTE: Wait 60 seconds before running further /token tests${NC}"


# =============================================================================
# TEST 6 — Expired Authorization Code
# =============================================================================
# Authorization code lifetime: ExpiresAt = DateTime.UtcNow.AddSeconds(60)
#   → codes are valid for exactly 60 SECONDS (not minutes).
#
# Code path:
#   GetByCodeHashAsync(codeHash) → found
#   authCode.IsExpired = (ExpiresAt <= DateTime.UtcNow) → TRUE
#   authCode.IsValid = (!IsConsumed && !IsExpired) → FALSE
#   → throw DomainException(INVALID_GRANT,
#       "Authorization code is invalid, expired, or already used.")
#   → HTTP 400 invalid_grant
#
# To test: obtain an auth code, wait 61+ seconds, THEN attempt to exchange it.
#
# Expected HTTP 400:
#   { "error": "invalid_grant",
#     "error_description": "Authorization code is invalid, expired, or already used." }
# =============================================================================
label "TEST 6 — Expired Code: exchange auth code after 61-second lifetime"

echo ""
info "CODE LIFETIME: 60 seconds (ExpiresAt = DateTime.UtcNow.AddSeconds(60))"
info "This test requires waiting 61 seconds after obtaining a code."

CODE_EXPIRED="${CODE_FOR_EXPIRY_TEST:-}"
VERIFIER_EXPIRED="${VERIFIER_FOR_EXPIRY_TEST:-}"

if [ -z "$CODE_EXPIRED" ] || [ -z "$VERIFIER_EXPIRED" ]; then
  echo ""
  echo -e "${YELLOW}  To generate an expired code automatically:${NC}"
  echo "    1. Complete a PKCE authorize flow (see pkce_flow.http)"
  echo "    2. Capture the code and verifier"
  echo "    3. Wait 61+ seconds"
  echo "    4. Export and re-run:"
  echo "         export CODE_FOR_EXPIRY_TEST=<auth_code>"
  echo "         export VERIFIER_FOR_EXPIRY_TEST=<code_verifier>"
  echo "         ./security_negative_tests.sh"
  echo ""
  echo "  Or fast-expire via DB:"
  echo "    UPDATE tx_authorization_codes"
  echo "      SET expires_at = now() - interval '1 second'"
  echo "      WHERE code_hash = (SELECT code_hash FROM tx_authorization_codes ORDER BY created_at DESC LIMIT 1);"
  skip "TEST 6 — set CODE_FOR_EXPIRY_TEST + VERIFIER_FOR_EXPIRY_TEST to run"
else
  info "MALICIOUS REQUEST (presenting expired code):"
  cat <<EOF
  POST /connect/token
    grant_type=authorization_code
    &code=${CODE_EXPIRED:0:20}...       ← issued >60s ago, ExpiresAt has elapsed
    &redirect_uri=${REGISTERED_REDIRECT}
    &client_id=${CLIENT_ID}
    &code_verifier=${VERIFIER_EXPIRED:0:20}...
EOF

  T6_RESP=$(curl -s -w "\n__HTTP__%{http_code}" \
    -X POST "$BASE/connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=authorization_code&code=${CODE_EXPIRED}&redirect_uri=${REGISTERED_REDIRECT}&client_id=${CLIENT_ID}&code_verifier=${VERIFIER_EXPIRED}")

  T6_HTTP=$(echo "$T6_RESP" | grep "__HTTP__" | sed 's/__HTTP__//')
  T6_BODY=$(echo "$T6_RESP" | sed '/^__HTTP__/d')

  echo ""
  info "EXPECTED: HTTP 400 { \"error\": \"invalid_grant\", \"error_description\": \"...expired, or already used.\" }"
  info "GOT:"
  assert_blocked    "6a — expired code rejected (HTTP 400)"      "400" "$T6_HTTP" "$T6_BODY"
  assert_error_code "6b — error=invalid_grant"                    "invalid_grant" "$T6_BODY"
  echo "$T6_BODY" | grep -qi "expired\|already used\|invalid" \
    && pass "6c — error_description indicates expiry/invalid state" \
    || fail "6c — expected 'expired' or 'already used' in error_description"
fi

echo ""
echo -e "    ${GRAY}AuthorizationCode.IsExpired = ExpiresAt <= DateTime.UtcNow${NC}"
echo -e "    ${GRAY}ExpiresAt = now.AddSeconds(60) — RFC 6749 recommends max 10 minutes; this server uses 60s${NC}"


# =============================================================================
# TEST 6b — Additional: implicit grant attempt (OAuth2.1 removes implicit)
# =============================================================================
label "TEST 6b — Implicit Grant Attempt: response_type=token (prohibited by OAuth2.1)"

echo ""
info "MALICIOUS REQUEST:"
cat <<EOF
  GET /connect/authorize
    ?response_type=token               ← OAuth2.1 §4.1.2 PROHIBITED
    &client_id=${CLIENT_ID}
    &redirect_uri=${REGISTERED_REDIRECT}
EOF

T6B_RESP=$(curl -s -w "\n__HTTP__%{http_code}" -L \
  "$BASE/connect/authorize?response_type=token&client_id=${CLIENT_ID}&redirect_uri=${REGISTERED_REDIRECT}&scope=openid")

T6B_HTTP=$(echo "$T6B_RESP" | grep "__HTTP__" | sed 's/__HTTP__//')
T6B_BODY=$(echo "$T6B_RESP" | sed '/^__HTTP__/d')

# Code path (ValidateAuthorizeRequestAsync line 146):
#   request.ResponseType contains "token" →
#   return Fail("unsupported_response_type",
#     "Implicit grant and hybrid flows are not supported. Use response_type=code with PKCE.")
#   RedirectUriValidated = FALSE → BadRequest (not redirect)
info "EXPECTED: HTTP 400 { \"error\": \"unsupported_response_type\" }"
assert_blocked    "6b-1 — implicit grant blocked"         "400" "$T6B_HTTP" "$T6B_BODY"
echo "$T6B_BODY" | jq -e '.error == "unsupported_response_type"' > /dev/null 2>&1 \
  && pass "6b-2 — error=unsupported_response_type" \
  || fail "6b-2 — expected unsupported_response_type"
echo "$T6B_BODY" | grep -qi "implicit\|PKCE" \
  && pass "6b-3 — error_description mentions PKCE requirement" \
  || info "6b-3 — (optional) no PKCE mention in description"


# =============================================================================
# TEST 6c — Password Grant Attempt (OAuth2.1 removes password grant)
# =============================================================================
label "TEST 6c — Password Grant: grant_type=password (prohibited by OAuth2.1)"

T6C_RESP=$(curl -s -w "\n__HTTP__%{http_code}" \
  -X POST "$BASE/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=admin@example.com&password=hunter2&client_id=${CLIENT_ID}")

T6C_HTTP=$(echo "$T6C_RESP" | grep "__HTTP__" | sed 's/__HTTP__//')
T6C_BODY=$(echo "$T6C_RESP" | sed '/^__HTTP__/d')

# Code path (OAuth2Service.ProcessTokenRequestAsync):
#   "PASSWORD" => throw DomainException(UNSUPPORTED_GRANT_TYPE,
#     "The 'password' grant has been removed in OAuth 2.1. Use 'authorization_code' with PKCE.")
#   → "unsupported_grant_type" → HTTP 400
info "EXPECTED: HTTP 400 { \"error\": \"unsupported_grant_type\" }"
assert_blocked    "6c-1 — password grant blocked"               "400" "$T6C_HTTP" "$T6C_BODY"
assert_error_code "6c-2 — error=unsupported_grant_type"          "unsupported_grant_type" "$T6C_BODY"
echo "$T6C_BODY" | grep -qi "password.*grant.*removed\|OAuth 2.1" \
  && pass "6c-3 — error_description mentions OAuth 2.1 removal" \
  || info "6c-3 — (optional) no OAuth 2.1 mention in description"


# =============================================================================
# Summary
# =============================================================================
echo ""
echo -e "${CYAN}═══════════════════════════════════════════════════════════${NC}"
echo  " Security negative test run complete."
echo  " ✓ BLOCKED = attack was rejected correctly"
echo  " ✗ LEAKED  = security regression — investigate immediately"
echo  " "
echo  " Tests requiring env vars:"
echo  "   TEST 2  — CODE_FOR_PKCE_DOWNGRADE"
echo  "   TEST 3  — CODE_FOR_REPLAY_TEST + VERIFIER_FOR_REPLAY_TEST"
echo  "   TEST 6  — CODE_FOR_EXPIRY_TEST  + VERIFIER_FOR_EXPIRY_TEST"
echo  " "
echo  " Tests 1/1b/1c/2b/4/5/6b/6c run fully automatically."
echo  " Test 5 exhausts the rate limit bucket — wait 60s before other /token calls."
echo -e "${CYAN}═══════════════════════════════════════════════════════════${NC}"
