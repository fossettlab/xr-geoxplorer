#!/usr/bin/env bash
# Start Azurite for local Table Storage (anchor API smoke tests).
#
# Usage:
#   ./scripts/start_azurite.sh          # foreground
#   ./scripts/start_azurite.sh --bg     # background (pid file in /tmp/azurite.pid)

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DATA_DIR="${AZURITE_DATA_DIR:-$ROOT/.azurite}"
PID_FILE="/tmp/azurite-geox.pid"
BG=false

for arg in "$@"; do
  case "$arg" in
    --bg) BG=true ;;
  esac
done

mkdir -p "$DATA_DIR"

if ! command -v azurite >/dev/null 2>&1; then
  if [[ -x "$ROOT/.npm-tools/node_modules/.bin/azurite" ]]; then
    AZURITE="$ROOT/.npm-tools/node_modules/.bin/azurite"
  else
    echo "Installing Azurite to $ROOT/.npm-tools (one-time)..."
    npm install --prefix "$ROOT/.npm-tools" azurite
    AZURITE="$ROOT/.npm-tools/node_modules/.bin/azurite"
  fi
else
  AZURITE="azurite"
fi

if [[ "$BG" == true ]]; then
  if [[ -f "$PID_FILE" ]] && kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
    echo "Azurite already running (pid $(cat "$PID_FILE"))"
    exit 0
  fi
  "$AZURITE" --silent --location "$DATA_DIR" --debug "$DATA_DIR/debug.log" &
  echo $! > "$PID_FILE"
  sleep 1
  echo "Azurite started in background (pid $(cat "$PID_FILE"))"
  echo "  Table: http://127.0.0.1:10002/devstoreaccount1"
  exit 0
fi

echo "Starting Azurite (Ctrl+C to stop)..."
echo "  Table endpoint: http://127.0.0.1:10002/devstoreaccount1"
exec "$AZURITE" --location "$DATA_DIR" --debug "$DATA_DIR/debug.log"
