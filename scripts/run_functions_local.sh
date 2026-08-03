#!/usr/bin/env bash
# Prepare and start the local Azure Functions host for GeoX auth backend.
#
# Creates functions/local.settings.json from the example when missing, ensures
# Azurite table connection is set, and runs `func start`.
#
# Usage:
#   ./scripts/run_functions_local.sh
#   ./scripts/start_azurite.sh --bg && ./scripts/run_functions_local.sh

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
FUNCS="$ROOT/functions"
SETTINGS="$FUNCS/local.settings.json"
EXAMPLE="$FUNCS/local.settings.json.example"
VENV="${ROOT}/.venv"

AZURITE_TABLE_CONN='DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;'

if [[ ! -f "$SETTINGS" ]]; then
  cp "$EXAMPLE" "$SETTINGS"
  echo "Created $SETTINGS from example."
fi

# Ensure Azurite table connection and local dev API key.
python3 <<PY
import json
from pathlib import Path
path = Path("$SETTINGS")
data = json.loads(path.read_text())
values = data.setdefault("Values", {})
if not values.get("ANCHOR_TABLE_CONNECTION"):
    values["ANCHOR_TABLE_CONNECTION"] = "$AZURITE_TABLE_CONN"
key = values.get("SAS_API_KEY", "")
if not key or key.startswith("<"):
    values["SAS_API_KEY"] = "local-dev-key"
path.write_text(json.dumps(data, indent=2) + "\n")
PY

if [[ -f "$VENV/bin/activate" ]]; then
  # shellcheck disable=SC1091
  source "$VENV/bin/activate"
elif command -v python3 >/dev/null 2>&1; then
  pip install -q -r "$FUNCS/requirements.txt" pytest 2>/dev/null || true
fi

export PATH="${ROOT}/.npm-tools/node_modules/.bin:${HOME}/.npm-global/bin:${PATH}"

if ! command -v func >/dev/null 2>&1; then
  if [[ ! -x "${ROOT}/.npm-tools/node_modules/.bin/func" ]]; then
    echo "Installing Azure Functions Core Tools to .npm-tools (one-time)..."
    npm install --prefix "${ROOT}/.npm-tools" azure-functions-core-tools@4
  fi
fi

if ! command -v func >/dev/null 2>&1; then
  echo "Azure Functions Core Tools (func) not found. Install: npm install -g azure-functions-core-tools@4" >&2
  exit 1
fi

cd "$FUNCS"
echo "Starting Function host at http://localhost:7071"
echo "  Anchor API: http://localhost:7071/api/anchors"
echo "  SAS API:    http://localhost:7071/api/sas/restricted"
exec func start
