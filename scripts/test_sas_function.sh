#!/usr/bin/env bash
# Smoke-test the local SAS Function (functions/) without Azure credentials.
# Exercises the API-key and allowlist gates; a valid key still returns 500 at
# the Azure SAS mint step when no managed identity is available locally.
#
# Usage:
#   ./scripts/test_sas_function.sh
#   SAS_API_KEY=mykey ./scripts/test_sas_function.sh

set -euo pipefail

BASE="${SAS_BASE_URL:-http://localhost:7071/api/sas/restricted}"
KEY="${SAS_API_KEY:-local-dev-key}"
BUNDLE="${SAS_TEST_BUNDLE:-android/eastshorestructure-bundle}"

post() {
  local key="$1"
  curl -sS -w "\nHTTP %{http_code}\n" -X POST "$BASE" \
    -H "Content-Type: application/json" \
    ${key:+-H "X-API-Key: $key"} \
    -d "{\"bundle\":\"$BUNDLE\"}"
}

echo "== Missing API key (expect 401) =="
post "" | tail -1

echo "== Wrong API key (expect 401) =="
post "wrong-key" | tail -1

echo "== Valid key (expect 500 locally without Azure identity, or 200 when deployed) =="
post "$KEY" | tail -1

echo "== Disallowed bundle (expect 403) =="
curl -sS -w "\nHTTP %{http_code}\n" -X POST "$BASE" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: $KEY" \
  -d '{"bundle":"android/not-on-allowlist"}' | tail -1
