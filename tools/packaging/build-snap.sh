#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
snap_root="$repo_root/snap"
payload_root="$snap_root/payload/opt/rgaa-studio"
integration_root="$snap_root/integration"
version="$(sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' "$repo_root/Directory.Build.props")"

[[ -n "$version" ]] || { echo 'Could not read the project version from Directory.Build.props.' >&2; exit 1; }
command -v dotnet >/dev/null || { echo '.NET SDK 10 is required.' >&2; exit 1; }
command -v snapcraft >/dev/null || { echo 'Install Snapcraft on a native Ubuntu build machine.' >&2; exit 1; }
command -v rsvg-convert >/dev/null || { echo 'Install librsvg2-bin to generate the Snap Store icon.' >&2; exit 1; }
if [[ -r /etc/os-release ]]; then
  . /etc/os-release
  [[ "${ID:-}" == ubuntu && "${VERSION_ID:-}" == 24.04 ]] || {
    echo 'This destructive-mode prototype targets a native Ubuntu 24.04 build host (base core24).' >&2
    exit 1
  }
fi

rm -rf "$snap_root/payload" "$snap_root/integration/share"
mkdir -p "$payload_root" "$integration_root/bin" \
  "$integration_root/share/applications" \
  "$integration_root/share/icons/hicolor/scalable/apps" \
  "$integration_root/share/doc/rgaa-studio"

dotnet publish "$repo_root/src/RgaaStudio.Desktop/RgaaStudio.Desktop.csproj" \
  --configuration Release --runtime linux-x64 --self-contained true \
  -p:UseAppHost=true -p:DebugType=None -p:DebugSymbols=false -p:Version="$version" \
  -o "$payload_root"

install -m 0755 "$snap_root/launcher/rgaa-studio" "$integration_root/bin/rgaa-studio"
install -m 0644 "$snap_root/integration/rgaa-studio.desktop" "$integration_root/share/applications/rgaa-studio.desktop"
install -m 0644 "$repo_root/src/RgaaStudio.Desktop/Assets/rgaa-studio-app-icon.svg" \
  "$integration_root/share/icons/hicolor/scalable/apps/rgaa-studio.svg"
install -m 0644 "$repo_root/tools/packaging/DEBIAN-COPYRIGHT" \
  "$integration_root/share/doc/rgaa-studio/copyright"
install -m 0644 "$repo_root/tools/packaging/DISTRIBUTION-NOTICE.txt" \
  "$integration_root/share/doc/rgaa-studio/DISTRIBUTION-NOTICE.txt"

rsvg-convert --width=512 --height=512 \
  --output "$snap_root/gui/icon.png" \
  "$repo_root/src/RgaaStudio.Desktop/Assets/rgaa-studio-app-icon.svg"

(
  cd "$snap_root"
  snapcraft --destructive-mode
)

snap_file="$snap_root/rgaa-studio_${version}_amd64.snap"
[[ -s "$snap_file" ]] || { echo "Snapcraft did not produce $snap_file." >&2; exit 1; }
output_dir="$repo_root/artifacts/packages/$version"
mkdir -p "$output_dir"
cp -f "$snap_file" "$output_dir/"
sha256sum "$output_dir/$(basename "$snap_file")" > "$output_dir/$(basename "$snap_file").sha256"
echo "Created experimental Snap: $output_dir/$(basename "$snap_file")"
