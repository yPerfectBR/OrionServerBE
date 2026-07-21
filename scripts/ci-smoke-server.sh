#!/usr/bin/env bash
# Boot OrionServer without plugins; fail on Fatal or missing listen line.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

mkdir -p config plugins worlds logs resource_packs
rm -rf worlds/default
bash scripts/first-run.sh -y >/dev/null

dotnet build src/Server/Server.csproj -c Release -v q

LOG="$(mktemp)"
trap 'rm -f "$LOG"' EXIT

set +e
timeout 25 dotnet run --project src/Server/Server.csproj -c Release --no-build >"$LOG" 2>&1
run_status=$?
set -e

if grep -qi "fatal" "$LOG"; then
  echo "::error::Server logged Fatal during smoke boot"
  cat "$LOG"
  exit 1
fi

if ! grep -q "Listening on" "$LOG"; then
  echo "::error::Server did not reach listening state (exit=$run_status)"
  cat "$LOG"
  exit 1
fi

echo "Smoke boot OK"
