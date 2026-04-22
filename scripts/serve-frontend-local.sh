#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PORT="${1:-5196}"
STAGING_DIR="${TMPDIR:-/tmp}/jig-inventory-wwwroot"

cd "$ROOT_DIR"

npm --prefix frontend run tw:build >/dev/null
dotnet build frontend/frontend.csproj --no-restore >/dev/null

mkdir -p "$STAGING_DIR" "$STAGING_DIR/_framework"
rsync -a --delete frontend/wwwroot/ "$STAGING_DIR/"
rsync -a --delete frontend/bin/Debug/net9.0/wwwroot/_framework/ "$STAGING_DIR/_framework/"

echo "Serving merged frontend assets on http://localhost:${PORT}"
exec python3 -m http.server "$PORT" -d "$STAGING_DIR"
