#!/usr/bin/env bash
# Publica o app desktop (WPF) como um release no GitHub.
#
# Uso:  scripts/publish-desktop.sh v0.1.1
#
# Requisitos: dotnet SDK, gh (GitHub CLI). O token é reaproveitado do
# git credential manager automaticamente (ou defina GH_TOKEN no ambiente).
set -euo pipefail

VERSION="${1:?informe a tag da versão, ex.: v0.1.1}"
REPO="alephdias/delta_apont_app"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
GH="${GH:-/c/Program Files/GitHub CLI/gh.exe}"
OUT="$ROOT/artifacts/desktop"
ASSET="$OUT/DeltaDecisao-Desktop.exe"

echo "==> Publicando desktop $VERSION (self-contained, arquivo único)..."
dotnet publish "$ROOT/desktop/DeltaApp.Desktop" -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$OUT"
cp "$OUT/DeltaApp.Desktop.exe" "$ASSET"

export GH_TOKEN="${GH_TOKEN:-$(printf 'protocol=https\nhost=github.com\n\n' | git credential fill 2>/dev/null | grep '^password=' | cut -d= -f2-)}"

# Cria o release; se a tag já existir, apenas substitui o binário.
if "$GH" release view "$VERSION" --repo "$REPO" >/dev/null 2>&1; then
  "$GH" release upload "$VERSION" "$ASSET" --repo "$REPO" --clobber
else
  "$GH" release create "$VERSION" "$ASSET" --repo "$REPO" \
    --title "Delta Decisão Desktop $VERSION" \
    --notes "Aplicativo desktop — caderno + cronômetro de apontamentos. Windows 10/11, arquivo único (não precisa instalar o .NET). Entre com a conta da empresa."
fi

echo "==> OK: https://github.com/$REPO/releases/tag/$VERSION"
