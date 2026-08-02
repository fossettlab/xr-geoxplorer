#!/usr/bin/env bash
# Smoke-test anchor persistence endpoints on local func host.
#
# Usage:
#   ./scripts/start_azurite.sh --bg
#   ./scripts/run_functions_local.sh   # separate terminal
#   ./scripts/test_anchor_function.sh
#
# With Azurite + ANCHOR_TABLE_CONNECTION configured, expects 200/201 on list/create/get.
# Without table storage, list/create return 503 (auth/validation tests still run).

set -euo pipefail

BASE="${ANCHOR_BASE:-http://localhost:7071/api/anchors}"
KEY="${SAS_API_KEY:-local-dev-key}"

extract_http_code() {
  tail -1 | sed -n 's/^HTTP \([0-9]*\)$/\1/p'
}

extract_body() {
  sed '$d'
}

echo "== List anchors (expect 200 [] with Azurite, or 503 without) =="
LIST_OUT="$(curl -sS -w "\nHTTP %{http_code}\n" -X GET "$BASE" -H "X-API-Key: $KEY")"
LIST_CODE="$(echo "$LIST_OUT" | extract_http_code)"
echo "$LIST_OUT" | tail -3

echo "== Create anchor (expect 201 with Azurite, or 503 without) =="
CREATE_OUT="$(curl -sS -w "\nHTTP %{http_code}\n" -X POST "$BASE" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: $KEY" \
  -d '{"name":"Cloud Test Room","identifier":"test-asa-id","dateExpired":"2026-12-31T00:00:00Z"}')"
CREATE_CODE="$(echo "$CREATE_OUT" | extract_http_code)"
CREATE_BODY="$(echo "$CREATE_OUT" | extract_body)"
echo "$CREATE_OUT" | tail -3

if [[ "$CREATE_CODE" == "201" ]]; then
  ANCHOR_ID="$(echo "$CREATE_BODY" | python3 -c "import json,sys; print(json.load(sys.stdin).get('id',''))" 2>/dev/null || true)"
  if [[ -n "$ANCHOR_ID" ]]; then
    echo "== Get anchor by id (expect 200) =="
    curl -sS -w "\nHTTP %{http_code}\n" -X GET "$BASE/$ANCHOR_ID" \
      -H "X-API-Key: $KEY" | tail -3

    echo "== List anchors again (expect 200 with >=1 entry) =="
    curl -sS -w "\nHTTP %{http_code}\n" -X GET "$BASE" \
      -H "X-API-Key: $KEY" | tail -3
  fi
fi

echo "== Missing API key (expect 401) =="
curl -sS -w "\nHTTP %{http_code}\n" -X POST "$BASE" \
  -H "Content-Type: application/json" \
  -d '{"name":"x","identifier":"y","dateExpired":"2026-01-01T00:00:00Z"}' | tail -1

echo "== Invalid body (expect 400) =="
curl -sS -w "\nHTTP %{http_code}\n" -X POST "$BASE" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: $KEY" \
  -d '{"name":""}' | tail -1

echo "== Get unknown id (expect 404 when table configured, else 503) =="
UNKNOWN="00000000000000000000000000000000"
curl -sS -w "\nHTTP %{http_code}\n" -X GET "$BASE/$UNKNOWN" \
  -H "X-API-Key: $KEY" | tail -1
