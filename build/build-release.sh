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

echo "[1/5] Running tests..."
dotnet test "$ROOT/SatPrep.sln" -c Release --nologo -v quiet  # Abort the release if anything fails

echo "[2/5] Publishing self-contained win-x64..."
rm -rf "$STAGE" && mkdir -p "$STAGE" "$PKG_DIR"
dotnet publish "$PROJECT" -c Release -r win-x64 --self-contained \
  -p:Version="$VERSION" -o "$STAGE" --nologo -v quiet

echo "[3/5] Adding deployment helpers..."
cp "$ROOT"/deploy/*.bat "$ROOT"/deploy/*.txt "$ROOT"/deploy/*.ps1 "$STAGE"/   # Installer, scripts, README
for f in "$STAGE"/*.bat "$STAGE"/*.ps1 "$STAGE"/*.txt; do sed -i 's/\r*$/\r/' "$f"; done      # Windows line endings (cmd.exe needs CRLF)
echo "$VERSION" > "$STAGE/VERSION.txt"
( cd "$STAGE" && find . -name '*.dll' -o -name '*.exe' | sort | xargs sha256sum > DLL_CHECKSUMS.txt )

echo "[4/5] Zipping..."
rm -f "$ZIP"
( cd "$ROOT/build/out" && zip -qr "$ZIP" SatPrep )
( cd "$PKG_DIR" && sha256sum "$(basename "$ZIP")" > "$(basename "$ZIP").sha256" )

echo "[5/5] Assembling release/ folder (committed to GitHub; every file < 95 MB)..."
REL="$ROOT/release"
BUNDLE_VER="8.0.34"                                               # ASP.NET Core Hosting Bundle (IIS mode only)
BUNDLE="$ROOT/build/prereqs/dotnet-hosting-$BUNDLE_VER-win.exe"
BUNDLE_SHA512="$(curl -s https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/8.0/releases.json 2>/dev/null \
  | python3 -c "import json,sys;d=json.load(sys.stdin);print(next(f['hash'] for r in d['releases'] if r['release-version']=='$BUNDLE_VER' for f in r['aspnetcore-runtime']['files'] if f['name']=='dotnet-hosting-win.exe'))" 2>/dev/null || true)"
if [ ! -f "$BUNDLE" ]; then
  mkdir -p "$(dirname "$BUNDLE")"
  curl -sL "https://builds.dotnet.microsoft.com/dotnet/aspnetcore/Runtime/$BUNDLE_VER/dotnet-hosting-$BUNDLE_VER-win.exe" -o "$BUNDLE"
fi
if [ -n "$BUNDLE_SHA512" ]; then                                  # Verify Microsoft's published hash when online
  echo "$BUNDLE_SHA512  $BUNDLE" | sha512sum -c --quiet - || { echo "Hosting Bundle hash mismatch"; exit 1; }
fi
rm -rf "$REL" && mkdir -p "$REL/prereqs"
cp "$ROOT"/setup/* "$REL"/
for f in "$REL"/*.bat "$REL"/*.ps1 "$REL"/*.txt; do sed -i 's/\r*$/\r/' "$f"; done
cp "$ZIP" "$REL"/
cp "$BUNDLE" "$REL/prereqs/"
( cd "$REL" && sha256sum "$(basename "$ZIP")" "prereqs/$(basename "$BUNDLE")" > SHA256SUMS.txt )
for big in $(find "$REL" -type f -size +95M); do                  # GitHub rejects files over 100 MB: split, SETUP re-joins
  split -b 50m -d -a 3 --numeric-suffixes=1 "$big" "$big."
  rm "$big"
done
sed -i 's/\r*$/\r/' "$REL/SHA256SUMS.txt"
echo "Done: $ZIP ($(du -h "$ZIP" | cut -f1)); release folder: $REL ($(du -sh "$REL" | cut -f1))"
