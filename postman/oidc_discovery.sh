#!/usr/bin/env bash
# =============================================================================
# MonkoraEdge Core Auth — OIDC Discovery + JWKS Completeness Test Suite
# =============================================================================
# Verifies RFC 8414 / OIDC Core §3 discovery document and RFC 7517 JWKS output.
#
# Endpoints under test:
#   GET /.well-known/openid-configuration
#   GET /.well-known/oauth-authorization-server   (RFC 8414)
#   GET /.well-known/jwks.json
#
# Prerequisites
# ─────────────
#   • API running at http://localhost:5001
#   • AUTH_ISSUER configured as http://localhost:5003 (matches launchSettings.json)
#   • jq installed (brew install jq / apt install jq)
#   • A valid JWT access_token for kid-correlation test in Test B3.
#     Obtain one via client_credentials:
#       curl -X POST .../connect/token \
#         -d "grant_type=client_credentials&client_id=api-server&client_secret=..."
#     Export as: export AT_FOR_KID_CHECK="eyJhb..."
#
# Run:
#   chmod +x oidc_discovery.sh
#   ./oidc_discovery.sh
# =============================================================================

BASE="http://localhost:5001"
# AUTH_ISSUER from launchSettings.json — the value INSIDE tokens and discovery doc.
# The API listens on $BASE but issues tokens with iss=$EXPECTED_ISSUER.
EXPECTED_ISSUER="${EXPECTED_ISSUER:-http://localhost:5003}"

AT_FOR_KID="${AT_FOR_KID_CHECK:-}"

# ── Colour helpers ─────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'
GRAY='\033[0;90m'; NC='\033[0m'
pass()  { echo -e "${GREEN}  ✓ PASS${NC}  $1"; }
fail()  { echo -e "${RED}  ✗ FAIL${NC}  $1"; }
info()  { echo -e "${YELLOW}  ►${NC} $1"; }
skip()  { echo -e "${YELLOW}  SKIP${NC}  $1"; }
label() { echo -e "\n${CYAN}═══════════════════════════════════════════════════════════${NC}";
          echo -e "${CYAN} $1${NC}";
          echo -e "${CYAN}═══════════════════════════════════════════════════════════${NC}"; }
show_pass_fail() {
  # show_pass_fail "description" "expected_value" "actual_value"
  local desc="$1" expected="$2" actual="$3"
  if [ "$actual" = "$expected" ]; then
    pass "$desc"
    echo -e "    ${GRAY}expected : $expected${NC}"
    echo -e "    ${GRAY}got      : $actual${NC}"
  else
    fail "$desc"
    echo -e "    ${RED}expected : $expected${NC}"
    echo -e "    ${RED}got      : $actual${NC}"
  fi
}

assert_http() {
  local desc="$1" expected="$2" actual="$3"
  show_pass_fail "$desc" "$expected" "$actual"
}

assert_field_present() {
  local desc="$1" field="$2" body="$3"
  local val
  val=$(echo "$body" | jq -r ".\"$field\" // empty" 2>/dev/null)
  if [ -n "$val" ]; then
    pass "$desc — '$field' present"
    echo -e "    ${GRAY}value: $val${NC}"
  else
    fail "$desc — '$field' MISSING or null"
    echo -e "    ${RED}expected: non-empty string or array${NC}"
    echo -e "    ${RED}got     : (absent)${NC}"
  fi
}

assert_array_not_empty() {
  local desc="$1" field="$2" body="$3"
  local len
  len=$(echo "$body" | jq -r ".\"$field\" | length" 2>/dev/null)
  if [ "$len" -gt 0 ] 2>/dev/null; then
    pass "$desc — '$field' is non-empty array ($len items)"
    echo -e "    ${GRAY}value: $(echo "$body" | jq -c ".\"$field\"")${NC}"
  else
    fail "$desc — '$field' is missing or empty array"
    echo -e "    ${RED}expected: array with ≥1 items${NC}"
    echo -e "    ${RED}got     : $len items${NC}"
  fi
}

assert_array_contains() {
  local desc="$1" field="$2" expected_item="$3" body="$4"
  local found
  found=$(echo "$body" | jq -r ".\"$field\" // [] | map(select(. == \"$expected_item\")) | length" 2>/dev/null)
  if [ "$found" -gt 0 ]; then
    pass "$desc — '$field' contains \"$expected_item\""
    echo -e "    ${GRAY}full array: $(echo "$body" | jq -c ".\"$field\"")${NC}"
  else
    fail "$desc — '$field' does NOT contain \"$expected_item\""
    echo -e "    ${RED}expected: array containing \"$expected_item\"${NC}"
    echo -e "    ${RED}got     : $(echo "$body" | jq -c ".\"$field\"")${NC}"
  fi
}

assert_eq() {
  local desc="$1" field="$2" expected="$3" body="$4"
  local actual
  actual=$(echo "$body" | jq -r ".\"$field\" // empty" 2>/dev/null)
  show_pass_fail "$desc — '$field'" "$expected" "$actual"
}

assert_url_reachable() {
  local desc="$1" url="$2"
  # Use OPTIONS/HEAD to avoid triggering business logic; fall back to GET
  local http
  http=$(curl -s -o /dev/null -w "%{http_code}" -X GET "$url" --max-time 3 2>/dev/null)
  # Anything except connection refused (000) counts as reachable
  if [ "$http" != "000" ]; then
    pass "$desc — $url reachable (HTTP $http)"
    echo -e "    ${GRAY}Note: non-200 is expected for authenticated endpoints${NC}"
  else
    fail "$desc — $url unreachable (connection refused)"
    echo -e "    ${RED}got: HTTP $http (000 = connection refused)${NC}"
  fi
}

assert_header_contains() {
  local desc="$1" header="$2" expected_fragment="$3" headers="$4"
  if echo "$headers" | grep -qi "${header}.*${expected_fragment}"; then
    pass "$desc — $header contains '$expected_fragment'"
    echo -e "    ${GRAY}$(echo "$headers" | grep -i "$header" | head -1)${NC}"
  else
    fail "$desc — $header missing '$expected_fragment'"
    echo -e "    ${RED}expected: header $header to contain '$expected_fragment'${NC}"
    echo -e "    ${RED}got     : $(echo "$headers" | grep -i "$header" | head -1 || echo '(header absent)')${NC}"
  fi
}


# =============================================================================
# TEST A — GET /.well-known/openid-configuration
# =============================================================================
# Source: WellKnownController.GetOpenIdConfiguration()
#         → OAuth2Service.GetOpenIdConfiguration(baseUrl)
#         → new OpenIdConfigurationResponse { ... }
#
# Cache header set by controller: Cache-Control: public, max-age=3600
#
# OIDC Core §3 REQUIRED fields:
#   issuer, authorization_endpoint, token_endpoint, userinfo_endpoint,
#   jwks_uri, response_types_supported, subject_types_supported,
#   id_token_signing_alg_values_supported
#
# OAuth2.1 / RFC 8414 RECOMMENDED (de facto required for compliant clients):
#   revocation_endpoint, introspection_endpoint, grant_types_supported,
#   scopes_supported, code_challenge_methods_supported (S256 mandatory per OAuth2.1)
#
# Server-specific additions (advertised to clients):
#   token_endpoint_auth_methods_supported, claims_supported,
#   response_modes_supported, require_pkce, registration_endpoint,
#   pushed_authorization_request_endpoint, device_authorization_endpoint,
#   dpop_signing_alg_values_supported, authorization_response_iss_parameter_supported
# =============================================================================
label "TEST A — GET /.well-known/openid-configuration"

info "REQUEST:  curl -s $BASE/.well-known/openid-configuration"

A_RESP=$(curl -s -D - "$BASE/.well-known/openid-configuration")
A_HTTP=$(echo "$A_RESP" | grep -E "^HTTP/" | awk '{print $2}')
A_HDR=$(echo "$A_RESP" | awk '/^\r?$/{exit} NR>1{print}')
A_BODY=$(echo "$A_RESP" | awk 'BEGIN{body=0} /^\r?$/{body++} body>=1{print}' | tail -n +2)

echo ""
info "── HTTP + caching ──────────────────────────────────────────"
assert_http "A1" "200" "$A_HTTP"
assert_header_contains "A2 — discovery doc is publicly cacheable (RFC 8414 §3)" \
  "Cache-Control" "public" "$A_HDR"
assert_header_contains "A3 — max-age=3600 (1 hour cache per controller)" \
  "Cache-Control" "max-age=3600" "$A_HDR"
assert_header_contains "A4 — Content-Type: application/json" \
  "Content-Type" "application/json" "$A_HDR"

echo ""
info "── OIDC Core §3 REQUIRED fields ────────────────────────────"

# A5 — issuer: exact value must match AUTH_ISSUER env var (same value embedded in JWT iss claim)
# OAuth2Service line 622: Issuer = _tokenService.GetIssuer()  where _issuer = opts.AUTH_ISSUER
ACTUAL_ISSUER=$(echo "$A_BODY" | jq -r '.issuer // empty')
show_pass_fail "A5 — issuer exact match (must equal AUTH_ISSUER / JWT iss claim)" \
  "$EXPECTED_ISSUER" "$ACTUAL_ISSUER"

# A6 — all URL-valued required fields
for field in authorization_endpoint token_endpoint userinfo_endpoint jwks_uri; do
  assert_field_present "A6 — REQUIRED" "$field" "$A_BODY"
done

# A7 — revocation and introspection (RFC 7009 / 7662 — registered by discovery)
for field in revocation_endpoint introspection_endpoint; do
  assert_field_present "A7 — RFC 7009/7662 endpoint" "$field" "$A_BODY"
done

# A8 — arrays required by OIDC Core §3
for field in response_types_supported subject_types_supported id_token_signing_alg_values_supported; do
  assert_array_not_empty "A8 — OIDC REQUIRED array" "$field" "$A_BODY"
done

echo ""
info "── OAuth2.1 / RFC 8414 fields ──────────────────────────────"

assert_array_not_empty "A9  — grant_types_supported"              "grant_types_supported"     "$A_BODY"
assert_array_not_empty "A10 — scopes_supported"                   "scopes_supported"           "$A_BODY"
assert_array_not_empty "A11 — token_endpoint_auth_methods_supported" "token_endpoint_auth_methods_supported" "$A_BODY"

# A12 — code_challenge_methods_supported MUST include "S256" (OAuth2.1 §4.1.1 — plain is PROHIBITED)
assert_array_not_empty "A12 — code_challenge_methods_supported present" \
  "code_challenge_methods_supported" "$A_BODY"
assert_array_contains  "A13 — code_challenge_methods_supported includes S256 (OAuth2.1 §4.1.1)" \
  "code_challenge_methods_supported" "S256" "$A_BODY"

# A14 — "plain" MUST NOT appear (OAuth2.1 §7.6 — plain is prohibited)
PLAIN_COUNT=$(echo "$A_BODY" | jq '.code_challenge_methods_supported // [] | map(select(. == "plain")) | length' 2>/dev/null)
if [ "$PLAIN_COUNT" = "0" ]; then
  pass "A14 — code_challenge_methods_supported does NOT include 'plain' (OAuth2.1 §7.6 — PROHIBITED)"
  echo -e "    ${GRAY}full array: $(echo "$A_BODY" | jq -c '.code_challenge_methods_supported')${NC}"
else
  fail "A14 — 'plain' MUST NOT appear in code_challenge_methods_supported (OAuth2.1 §7.6)"
  echo -e "    ${RED}got: $(echo "$A_BODY" | jq -c '.code_challenge_methods_supported')${NC}"
fi

echo ""
info "── Grant type enumeration ──────────────────────────────────"
for grant in "authorization_code" "client_credentials" "refresh_token"; do
  assert_array_contains "A15 — grant_types_supported" "grant_types_supported" "$grant" "$A_BODY"
done

echo ""
info "── Scope enumeration ───────────────────────────────────────"
for scope in "openid" "profile" "email" "offline_access"; do
  assert_array_contains "A16 — scopes_supported" "scopes_supported" "$scope" "$A_BODY"
done

echo ""
info "── Client auth methods ─────────────────────────────────────"
for method in "none" "client_secret_basic" "client_secret_post"; do
  assert_array_contains "A17 — token_endpoint_auth_methods_supported" \
    "token_endpoint_auth_methods_supported" "$method" "$A_BODY"
done

echo ""
info "── PKCE enforcement flag ───────────────────────────────────"
REQUIRE_PKCE=$(echo "$A_BODY" | jq -r '.require_pkce // empty')
show_pass_fail "A18 — require_pkce=true (all public clients must use PKCE per OAuth2.1 §4.1)" \
  "true" "$REQUIRE_PKCE"

echo ""
info "── ID token signing algorithm ──────────────────────────────"
assert_array_contains "A19 — id_token_signing_alg_values_supported includes RS256" \
  "id_token_signing_alg_values_supported" "RS256" "$A_BODY"

# RS256 none-alg MUST NOT appear (OIDC Core §3.1.7)
NONE_ALG=$(echo "$A_BODY" | jq '.id_token_signing_alg_values_supported // [] | map(select(. == "none")) | length' 2>/dev/null)
if [ "$NONE_ALG" = "0" ]; then
  pass "A20 — id_token_signing_alg_values_supported does NOT include 'none' (OIDC Core §3.1.7)"
else
  fail "A20 — 'none' MUST NOT appear in id_token_signing_alg_values_supported"
fi

echo ""
info "── RFC 9207: iss parameter in authorization responses ───────"
ISS_PARAM=$(echo "$A_BODY" | jq -r '.authorization_response_iss_parameter_supported // empty')
show_pass_fail "A21 — authorization_response_iss_parameter_supported=true (RFC 9207)" \
  "true" "$ISS_PARAM"

echo ""
info "── Endpoint URL consistency: all endpoints use the issuer base ──"
# All URL fields must start with EXPECTED_ISSUER (not the API port, not a different host)
for field in authorization_endpoint token_endpoint userinfo_endpoint jwks_uri revocation_endpoint introspection_endpoint; do
  val=$(echo "$A_BODY" | jq -r ".\"$field\" // empty")
  if [[ "$val" == "${EXPECTED_ISSUER}"* ]]; then
    pass "A22 — $field prefix matches issuer"
    echo -e "    ${GRAY}$val${NC}"
  else
    fail "A22 — $field prefix mismatch"
    echo -e "    ${RED}expected prefix: $EXPECTED_ISSUER${NC}"
    echo -e "    ${RED}got            : $val${NC}"
  fi
done

echo ""
info "── Reachability spot-check ─────────────────────────────────"
# Confirm advertised endpoints actually respond (any non-000 HTTP status)
JWKS_URI=$(echo "$A_BODY" | jq -r '.jwks_uri // empty')
TOKEN_EP=$(echo "$A_BODY" | jq -r '.token_endpoint // empty')
if [ -n "$JWKS_URI" ]; then
  assert_url_reachable "A23 — jwks_uri is reachable" "$JWKS_URI"
fi
if [ -n "$TOKEN_EP" ]; then
  assert_url_reachable "A24 — token_endpoint is reachable (expect 400/405 without body)" "$TOKEN_EP"
fi


# =============================================================================
# TEST A2 — GET /.well-known/oauth-authorization-server  (RFC 8414)
# =============================================================================
# Second discovery doc — same issuer and endpoint values, slightly different shape.
# Verified separately because RFC 8414 §2 defines its own REQUIRED field set.
# =============================================================================
label "TEST A2 — GET /.well-known/oauth-authorization-server  (RFC 8414)"

info "REQUEST:  curl -s $BASE/.well-known/oauth-authorization-server"

A2_BODY=$(curl -s "$BASE/.well-known/oauth-authorization-server")
A2_HTTP=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/.well-known/oauth-authorization-server")

assert_http "A2-1 — HTTP 200" "200" "$A2_HTTP"

# RFC 8414 §2 REQUIRED: issuer, authorization_endpoint, token_endpoint, jwks_uri
for field in issuer authorization_endpoint token_endpoint jwks_uri; do
  assert_field_present "A2-2 — RFC 8414 REQUIRED" "$field" "$A2_BODY"
done

# issuer must match
A2_ISSUER=$(echo "$A2_BODY" | jq -r '.issuer // empty')
show_pass_fail "A2-3 — issuer matches AUTH_ISSUER" "$EXPECTED_ISSUER" "$A2_ISSUER"

# Both discovery docs must return the SAME issuer
show_pass_fail "A2-4 — OIDC and RFC 8414 discovery docs report same issuer (mix-up prevention)" \
  "$ACTUAL_ISSUER" "$A2_ISSUER"


# =============================================================================
# TEST B — GET /.well-known/jwks.json
# =============================================================================
# Source: WellKnownController.GetJwks() → TokenService.GetJwks()
#         Cached in IMemoryCache 24h (key="jwks:v1").
#
# RFC 7517 §4 REQUIRED fields per key: kty, use, alg, kid, n, e (for RSA)
# Server uses RSA-2048 + RS256. kid = SHA-1 thumbprint of the public key (x5t).
#
# Response shape (from OidcModels.cs JwksResponse / JwkKey):
# {
#   "keys": [
#     {
#       "kty": "RSA",
#       "use": "sig",
#       "kid": "<thumbprint>",
#       "alg": "RS256",
#       "n":   "<base64url modulus>",
#       "e":   "<base64url exponent>",
#       "x5t": "<sha1 thumbprint>"
#     }
#   ]
# }
# =============================================================================
label "TEST B — GET /.well-known/jwks.json"

info "REQUEST:  curl -s $BASE/.well-known/jwks.json"

B_RESP=$(curl -s -D - "$BASE/.well-known/jwks.json")
B_HTTP=$(echo "$B_RESP" | grep -E "^HTTP/" | awk '{print $2}')
B_HDR=$(echo "$B_RESP" | awk '/^\r?$/{exit} NR>1{print}')
B_BODY=$(echo "$B_RESP" | awk 'BEGIN{body=0} /^\r?$/{body++} body>=1{print}' | tail -n +2)

echo ""
info "── HTTP + caching ──────────────────────────────────────────"
assert_http "B1 — HTTP 200" "200" "$B_HTTP"
assert_header_contains "B2 — JWKS is publicly cacheable"    "Cache-Control" "public"       "$B_HDR"
assert_header_contains "B3 — max-age=3600"                  "Cache-Control" "max-age=3600" "$B_HDR"
assert_header_contains "B4 — Content-Type: application/json" "Content-Type" "application/json" "$B_HDR"

echo ""
info "── keys array ──────────────────────────────────────────────"
KEY_COUNT=$(echo "$B_BODY" | jq '.keys | length' 2>/dev/null)
if [ "$KEY_COUNT" -gt 0 ] 2>/dev/null; then
  pass "B5 — keys array is non-empty ($KEY_COUNT key(s))"
  echo -e "    ${GRAY}keys: $(echo "$B_BODY" | jq -c '[.keys[].kid]')${NC}"
else
  fail "B5 — keys array is EMPTY or missing"
  echo -e "    ${RED}expected: array with ≥1 JWK entries${NC}"
  echo -e "    ${RED}got     : $B_BODY${NC}"
fi

echo ""
info "── Per-key field validation (RFC 7517 §4) ──────────────────"
# Validate every key in the array
KEY_INDEX=0
while IFS= read -r key_json; do
  KEY_INDEX=$((KEY_INDEX + 1))
  echo ""
  info "  Key $KEY_INDEX: $(echo "$key_json" | jq -r '.kid // "(no kid)"')"

  # kty — REQUIRED (RFC 7517 §4.1)
  KTY=$(echo "$key_json" | jq -r '.kty // empty')
  if [ -n "$KTY" ]; then pass "B6.$KEY_INDEX — kty present: $KTY"
  else fail "B6.$KEY_INDEX — kty MISSING (RFC 7517 §4.1 REQUIRED)"; fi

  # use — REQUIRED for keys used for signature verification (RFC 7517 §4.2)
  USE=$(echo "$key_json" | jq -r '.use // empty')
  show_pass_fail "B7.$KEY_INDEX — use=sig (key is for signature, not encryption)" "sig" "$USE"

  # alg — SHOULD be present (RFC 7517 §4.4)
  ALG=$(echo "$key_json" | jq -r '.alg // empty')
  if [ "$ALG" = "RS256" ] || [ "$ALG" = "ES256" ] || [ "$ALG" = "RS384" ] || [ "$ALG" = "ES384" ]; then
    pass "B8.$KEY_INDEX — alg=$ALG (RS256 or EC variant)"
    echo -e "    ${GRAY}value: $ALG${NC}"
  else
    fail "B8.$KEY_INDEX — alg unexpected or missing"
    echo -e "    ${RED}expected: RS256 or ES256 (server uses RSA-2048 + RS256)${NC}"
    echo -e "    ${RED}got     : '$ALG'${NC}"
  fi

  # kid — REQUIRED for key rotation (RFC 7517 §4.5)
  KID=$(echo "$key_json" | jq -r '.kid // empty')
  if [ -n "$KID" ]; then
    pass "B9.$KEY_INDEX — kid present: $KID"
  else
    fail "B9.$KEY_INDEX — kid MISSING (required for JWT header kid matching)"
    echo -e "    ${RED}expected: non-empty string (SHA-1 thumbprint / x5t)${NC}"
  fi

  # RSA-specific: n and e must be present when kty=RSA
  if [ "$KTY" = "RSA" ]; then
    N_VAL=$(echo "$key_json" | jq -r '.n // empty')
    E_VAL=$(echo "$key_json" | jq -r '.e // empty')
    if [ -n "$N_VAL" ]; then
      pass "B10.$KEY_INDEX — RSA modulus (n) present (length ${#N_VAL} chars)"
    else
      fail "B10.$KEY_INDEX — RSA modulus (n) MISSING for kty=RSA"
    fi
    if [ -n "$E_VAL" ]; then
      pass "B11.$KEY_INDEX — RSA exponent (e) present: $E_VAL"
      # e=AQAB → 65537 (standard safe exponent)
      [ "$E_VAL" = "AQAB" ] \
        && pass "B12.$KEY_INDEX — exponent is 65537 (AQAB — standard safe public exponent)" \
        || info  "B12.$KEY_INDEX — exponent is $E_VAL (non-standard, verify intent)"
    else
      fail "B11.$KEY_INDEX — RSA exponent (e) MISSING for kty=RSA"
    fi
    # Verify key size ≥ 2048 bits: base64url(n) length ≥ 342 chars (2048 bits → 256 bytes → 342 base64 chars)
    if [ ${#N_VAL} -ge 342 ]; then
      pass "B13.$KEY_INDEX — RSA key size ≥ 2048 bits (modulus length ${#N_VAL} chars)"
    else
      fail "B13.$KEY_INDEX — RSA key appears to be < 2048 bits (modulus length ${#N_VAL} chars)"
      echo -e "    ${RED}expected: ≥342 base64url chars for 2048-bit key${NC}"
    fi
  fi

done < <(echo "$B_BODY" | jq -c '.keys[]' 2>/dev/null)

# Capture first key's kid for correlation tests
JWKS_KID=$(echo "$B_BODY" | jq -r '.keys[0].kid // empty')


# =============================================================================
# TEST B2 — kid in JWKS matches kid in JWT header
# =============================================================================
# When a client receives a JWT, it extracts kid from the JWT header and looks
# it up in JWKS to find the correct public key for signature verification.
# If kid values don't match, ALL clients will fail to validate tokens.
#
# JWT header structure (from TokenService.GenerateAccessTokenAsync):
#   { "alg": "RS256", "typ": "at+JWT", "kid": "<same as JwkKey.Kid>" }
# =============================================================================
label "TEST B2 — kid in JWKS matches kid in JWT header of issued access_token"

if [ -z "$AT_FOR_KID" ]; then
  skip "AT_FOR_KID_CHECK not set — obtain a token and re-run:"
  echo "  export AT_FOR_KID_CHECK=\"\$(curl -s -X POST $BASE/connect/token \\"
  echo "    -d 'grant_type=client_credentials&client_id=api-server&client_secret=...' \\"
  echo "    | jq -r '.access_token')\""
  echo "  ./oidc_discovery.sh"
else
  # Decode JWT header (part before first dot), base64url → base64 → JSON
  JWT_HEADER_B64=$(echo "$AT_FOR_KID" | cut -d. -f1)
  # Add padding if needed
  PADDED="${JWT_HEADER_B64}$(printf '=%.0s' $(seq 1 $((4 - ${#JWT_HEADER_B64} % 4 )) | head -$((4 - ${#JWT_HEADER_B64} % 4))))"
  # tr: base64url → standard base64
  JWT_HEADER_JSON=$(echo "$PADDED" | tr '_-' '/+' | base64 --decode 2>/dev/null)

  info "JWT header decoded: $JWT_HEADER_JSON"

  JWT_KID=$(echo "$JWT_HEADER_JSON" | jq -r '.kid // empty' 2>/dev/null)
  JWT_ALG=$(echo "$JWT_HEADER_JSON" | jq -r '.alg // empty' 2>/dev/null)
  JWT_TYP=$(echo "$JWT_HEADER_JSON" | jq -r '.typ // empty' 2>/dev/null)

  echo ""
  show_pass_fail "B2-1 — JWT alg=RS256 (matches id_token_signing_alg_values_supported)" \
    "RS256" "$JWT_ALG"

  # RFC 9068 §2.1: access tokens MUST use typ: at+JWT
  show_pass_fail "B2-2 — JWT typ=at+JWT (RFC 9068 §2.1 — application/at+jwt)" \
    "at+JWT" "$JWT_TYP"

  if [ -n "$JWT_KID" ] && [ -n "$JWKS_KID" ]; then
    show_pass_fail "B2-3 — JWT header kid matches first key in JWKS (clients can resolve signing key)" \
      "$JWKS_KID" "$JWT_KID"
    # Also verify the kid is in the keys array at all (supports multiple keys during rotation)
    KID_IN_JWKS=$(echo "$B_BODY" | jq -r --arg kid "$JWT_KID" '.keys[] | select(.kid == $kid) | .kid // empty' 2>/dev/null)
    if [ -n "$KID_IN_JWKS" ]; then
      pass "B2-4 — JWT kid found in JWKS keys array (key rotation safe — kid=$JWT_KID)"
    else
      fail "B2-4 — JWT kid '$JWT_KID' NOT FOUND in JWKS keys array"
      echo -e "    ${RED}JWKS kids: $(echo "$B_BODY" | jq -c '[.keys[].kid]')${NC}"
      echo -e "    ${RED}Clients WILL FAIL to verify this token's signature${NC}"
    fi
  else
    fail "B2-3 — Cannot compare — JWT kid='$JWT_KID', JWKS first kid='$JWKS_KID'"
  fi
fi


# =============================================================================
# TEST B3 — JWKS caching: second call served from IMemoryCache (no re-computation)
# =============================================================================
# TokenService.GetJwks() uses IMemoryCache with 24h TTL (key="jwks:v1").
# Cannot assert caching directly from HTTP without tracing, but we can verify
# that two identical requests return identical responses (no key re-generation).
# =============================================================================
label "TEST B3 — JWKS is stable across calls (IMemoryCache 24h — no key re-computation)"

B3_BODY_1=$(curl -s "$BASE/.well-known/jwks.json")
B3_BODY_2=$(curl -s "$BASE/.well-known/jwks.json")

KID_1=$(echo "$B3_BODY_1" | jq -r '.keys[0].kid // empty')
KID_2=$(echo "$B3_BODY_2" | jq -r '.keys[0].kid // empty')
N_1=$(echo "$B3_BODY_1" | jq -r '.keys[0].n // empty' | cut -c1-20)
N_2=$(echo "$B3_BODY_2" | jq -r '.keys[0].n // empty' | cut -c1-20)

show_pass_fail "B3-1 — kid identical across two calls"         "$KID_1"  "$KID_2"
show_pass_fail "B3-2 — modulus prefix identical across calls"  "$N_1"    "$N_2"
if [ "$B3_BODY_1" = "$B3_BODY_2" ]; then
  pass "B3-3 — full JWKS response byte-for-byte identical (cache is working)"
else
  fail "B3-3 — JWKS response differs between calls (cache miss or key rotation in progress?)"
fi


# =============================================================================
# TEST C — issuer in discovery doc matches iss in issued JWT
# =============================================================================
# OIDC Core §2: "The iss Claim value is a case sensitive URL using the https
#  scheme that contains scheme, host, and optionally, port number and path
#  components and no query or fragment components."
# RFC 8414 §2: issuer in discovery doc MUST match iss in all issued tokens.
#
# TokenService lines: issuer: _issuer (set from opts.AUTH_ISSUER)
# OAuth2Service line 622: Issuer = _tokenService.GetIssuer()  ← same source
# → Both must equal EXPECTED_ISSUER.
# =============================================================================
label "TEST C — issuer in discovery doc == iss claim in issued JWT"

if [ -z "$AT_FOR_KID" ]; then
  skip "AT_FOR_KID_CHECK not set — cannot decode JWT iss claim. See Test B2 SKIP message."
else
  JWT_PAYLOAD_B64=$(echo "$AT_FOR_KID" | cut -d. -f2)
  PADDED_P="${JWT_PAYLOAD_B64}$(printf '=%.0s' $(seq 1 $((4 - ${#JWT_PAYLOAD_B64} % 4)) | head -$((4 - ${#JWT_PAYLOAD_B64} % 4))))"
  JWT_PAYLOAD_JSON=$(echo "$PADDED_P" | tr '_-' '/+' | base64 --decode 2>/dev/null)

  info "JWT payload (selected claims): $(echo "$JWT_PAYLOAD_JSON" | jq -c '{iss,aud,sub,exp,jti}' 2>/dev/null)"

  JWT_ISS=$(echo "$JWT_PAYLOAD_JSON" | jq -r '.iss // empty')
  JWT_AUD=$(echo "$JWT_PAYLOAD_JSON" | jq -r '.aud // empty')
  JWT_JTI=$(echo "$JWT_PAYLOAD_JSON" | jq -r '.jti // empty')
  JWT_EXP=$(echo "$JWT_PAYLOAD_JSON" | jq -r '.exp // empty')
  JWT_IAT=$(echo "$JWT_PAYLOAD_JSON" | jq -r '.iat // empty')
  JWT_SUB=$(echo "$JWT_PAYLOAD_JSON" | jq -r '.sub // empty')

  echo ""
  show_pass_fail "C1 — JWT iss matches discovery doc issuer (RFC 8414 §2 — MUST be identical)" \
    "$EXPECTED_ISSUER" "$JWT_ISS"
  show_pass_fail "C2 — JWT iss matches OIDC discovery issuer field" \
    "$ACTUAL_ISSUER" "$JWT_ISS"

  # Check all RFC 9068 §2.2 REQUIRED claims are present in access token
  echo ""
  info "── RFC 9068 §2.2 access token required claims ──────────────"
  for claim in iss sub aud exp iat jti; do
    val=$(echo "$JWT_PAYLOAD_JSON" | jq -r ".\"$claim\" // empty")
    if [ -n "$val" ]; then
      pass "C3 — JWT claim '$claim' present: $val"
    else
      fail "C3 — JWT claim '$claim' MISSING (RFC 9068 §2.2 REQUIRED)"
    fi
  done

  # exp sanity: must be in the future
  NOW=$(date +%s)
  if [ -n "$JWT_EXP" ] && [ "$JWT_EXP" -gt "$NOW" ] 2>/dev/null; then
    REMAINING=$(( JWT_EXP - NOW ))
    pass "C4 — JWT exp is in the future (token valid for ${REMAINING}s)"
    echo -e "    ${GRAY}exp=$JWT_EXP  now=$NOW  remaining=${REMAINING}s${NC}"
  else
    fail "C4 — JWT exp=$JWT_EXP is in the past or missing (token is already expired)"
  fi

  # client_id / scope claims — present in access tokens per TokenService.GenerateAccessTokenAsync
  for claim in client_id scope; do
    val=$(echo "$JWT_PAYLOAD_JSON" | jq -r ".\"$claim\" // empty")
    if [ -n "$val" ]; then
      pass "C5 — JWT claim '$claim' present: $val"
    else
      info "C5 — JWT claim '$claim' absent (optional for non-OIDC access tokens)"
    fi
  done
fi


# =============================================================================
# Summary
# =============================================================================
echo ""
echo -e "${CYAN}═══════════════════════════════════════════════════════════${NC}"
echo  " Test run complete."
echo  " "
echo  " Tests A / A2 / B / B3 / C run automatically."
echo  " Tests B2 / C (JWT decode) require AT_FOR_KID_CHECK."
echo  " "
echo  " To run with JWT kid correlation:"
echo  "   export AT_FOR_KID_CHECK=\"\$(curl -s -X POST $BASE/connect/token \\"
echo  "     -H 'Content-Type: application/x-www-form-urlencoded' \\"
echo  "     -d 'grant_type=client_credentials&client_id=api-server&client_secret=...' \\"
echo  "     | jq -r '.access_token')\""
echo  "   ./oidc_discovery.sh"
echo  " "
echo  " To override expected issuer:"
echo  "   export EXPECTED_ISSUER=https://auth.yourdomain.com"
echo  "   ./oidc_discovery.sh"
echo -e "${CYAN}═══════════════════════════════════════════════════════════${NC}"
