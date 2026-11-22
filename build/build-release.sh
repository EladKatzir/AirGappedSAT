#!/usr/bin/env bash
# =============================================================================
# build-release.sh - builds the air-gapped Windows release zip from Linux
# (cloud sessions / CI). Equivalent of Build-Release.ps1 for Windows.
#
# Usage:  build/build-release.sh 0.1.0
# Output: packages/SatPrep-v<version>-win-x64.zip  (+ .sha256)
#
# The zip is fully self-contained (includes the .NET runtime), so the
# Windows Server 2022 machine needs nothing installed.
# =============================================================================
set -euo pipefail

VERSION="${1:?usage: build-release.sh <version>}"          # Release version, e.g. 0.1.0
ROOT="$(cd "$(dirname "$0")/.." && pwd)"                    # Repository root
PROJECT="$ROOT/src/SatPrep/SatPrep.csproj"                  # Web project
STAGE="$ROOT/build/out/SatPrep"                             # Staging folder that becomes the zip
PKG_DIR="$ROOT/packages"                                    # Where finished zips go
ZIP="$PKG_DIR/SatPrep-v$VERSION-win-x64.zip"                # Final zip path

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1         # No telemetry / banners

echo "[1/4] Running tests..."
dotnet test "$ROOT/SatPrep.sln" -c Release --nologo -v quiet  # Abort the release if anything fails

echo "[2/4] Publishing self-contained win-x64..."
rm -rf "$STAGE" && mkdir -p "$STAGE" "$PKG_DIR"
dotnet publish "$PROJECT" -c Release -r win-x64 --self-contained \
  -p:Version="$VERSION" -o "$STAGE" --nologo -v quiet

echo "[3/4] Adding deployment helpers..."
cp "$ROOT"/deploy/*.bat "$ROOT"/deploy/*.txt "$ROOT"/deploy/*.ps1 "$STAGE"/   # Installer, scripts, README
for f in "$STAGE"/*.bat "$STAGE"/*.ps1 "$STAGE"/*.txt; do sed -i 's/\r*$/\r/' "$f"; done      # Windows line endings (cmd.exe needs CRLF)
echo "$VERSION" > "$STAGE/VERSION.txt"
( cd "$STAGE" && find . -name '*.dll' -o -name '*.exe' | sort | xargs sha256sum > DLL_CHECKSUMS.txt )

echo "[4/4] Zipping..."
rm -f "$ZIP"
( cd "$ROOT/build/out" && zip -qr "$ZIP" SatPrep )
( cd "$PKG_DIR" && sha256sum "$(basename "$ZIP")" > "$(basename "$ZIP").sha256" )

echo "Done: $ZIP ($(du -h "$ZIP" | cut -f1))"
