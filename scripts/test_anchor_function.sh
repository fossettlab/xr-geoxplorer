#!/usr/bin/env bash
# Smoke-test anchor persistence endpoints on local func host.
#
# Usage:
#   ./scripts/test_anchor_function.sh
#   SAS_API_KEY=mykey ./scripts/test_anchor_function.sh

set -euo pipefail

BASE="${ANCHOR_BASE:-http://localhost:7071/api/anchors}"
KEY="${SAS_API_KEY:-local-dev-key}"

post_create() {
  curl -sS -w "\nHTTP %{http_code}\n" -X POST "$BASE" \
    -H "Content-Type: application/json" \
    -H "X-API-Key: $KEY" \
    -d '{"name":"Cloud Test Room","identifier":"test-asa-id","dateExpired":"2026-12-31T00:00:00Z"}'
}

echo "== Create anchor (expect 503 without table storage, or 201 when configured) =="
post_create | tail -3

echo "== Missing API key (expect 401) =="
curl -sS -w "\nHTTP %{http_code}\n" -X POST "$BASE" \
  -H "Content-Type: application/json" \
  -d '{"name":"x","identifier":"y","dateExpired":"2026-01-01T00:00:00Z"}' | tail -1

echo "== Invalid body (expect 400) =="
curl -sS -w "\nHTTP %{http_code}\n" -X POST "$BASE" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: $KEY" \
  -d '{"name":""}' | tail -1
