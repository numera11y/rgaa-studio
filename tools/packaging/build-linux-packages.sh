#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 3 ]]; then
  echo "Usage: $0 <published-linux-x64-dir> <output-dir> <version>" >&2
  exit 2
fi

payload_dir="$(realpath "$1")"
output_dir="$(realpath -m "$2")"
version="$3"
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

command -v dpkg-deb >/dev/null || { echo 'dpkg-deb is required (Ubuntu/Debian: dpkg).' >&2; exit 1; }
command -v rpmbuild >/dev/null || { echo 'rpmbuild is required (Ubuntu WSL: sudo apt install rpm).' >&2; exit 1; }
[[ -x "$payload_dir/RgaaStudio.Desktop" ]] || { echo "Linux apphost not found in $payload_dir" >&2; exit 1; }

mkdir -p "$output_dir"
stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT

deb_version="${version/-/\~}"
deb_root="$stage/deb-root"
install -d "$deb_root/opt/rgaa-studio" "$deb_root/usr/bin" "$deb_root/usr/share/applications" "$deb_root/usr/share/icons/hicolor/scalable/apps" "$deb_root/usr/share/doc/rgaa-studio/Assets" "$deb_root/DEBIAN"
cp -a "$payload_dir/." "$deb_root/opt/rgaa-studio/"
find "$deb_root/opt/rgaa-studio" -type d -exec chmod 0755 {} +
find "$deb_root/opt/rgaa-studio" -type f -exec chmod 0644 {} +
chmod 0755 "$deb_root/opt/rgaa-studio/RgaaStudio.Desktop"
install -m 0755 "$script_dir/rgaa-studio-launcher" "$deb_root/usr/bin/rgaa-studio"
install -m 0644 "$script_dir/rgaa-studio.desktop" "$deb_root/usr/share/applications/rgaa-studio.desktop"
install -m 0644 "$payload_dir/Assets/rgaa-studio-app-icon.svg" "$deb_root/usr/share/icons/hicolor/scalable/apps/rgaa-studio.svg"
install -m 0644 "$script_dir/README-Linux.txt" "$deb_root/usr/share/doc/rgaa-studio/README-Linux.txt"
install -m 0644 "$script_dir/DISTRIBUTION-NOTICE.txt" "$deb_root/usr/share/doc/rgaa-studio/DISTRIBUTION-NOTICE.txt"
install -m 0644 "$payload_dir/LICENSE.txt" "$deb_root/usr/share/doc/rgaa-studio/copyright"
install -m 0644 "$payload_dir/Assets/LUCIDE-LICENSE.txt" "$deb_root/usr/share/doc/rgaa-studio/Assets/LUCIDE-LICENSE.txt"
install -m 0644 "$payload_dir/Assets/AXE-CORE-LICENSE.txt" "$deb_root/usr/share/doc/rgaa-studio/Assets/AXE-CORE-LICENSE.txt"

cat > "$deb_root/DEBIAN/control" <<CONTROL
Package: rgaa-studio
Version: $deb_version
Section: utils
Priority: optional
Architecture: amd64
Maintainer: numera11y
Homepage: https://www.numera11y.fr
Depends: libgtk-3-0, libwebkit2gtk-4.1-0, libsoup-3.0-0
Description: Espace de travail d’audit d’accessibilité RGAA/WCAG
 Application desktop Avalonia pour accompagner les audits d’accessibilité.
 Cette préversion inclut le runtime .NET et utilise le WebKitGTK du système.
CONTROL

deb_file="$output_dir/rgaa-studio_${deb_version}_amd64.deb"
dpkg-deb --root-owner-group --build "$deb_root" "$deb_file" >/dev/null
dpkg-deb --info "$deb_file" >/dev/null

rpm_version="${version%%-*}"
rpm_release="0.${version#*-}"
rpm_root="$stage/rpmbuild"
mkdir -p "$rpm_root/BUILD" "$rpm_root/BUILDROOT" "$rpm_root/RPMS" "$rpm_root/SOURCES" "$rpm_root/SPECS" "$rpm_root/SRPMS"
payload_source="$stage/rgaa-studio"
mkdir -p "$payload_source"
cp -a "$payload_dir/." "$payload_source/"
find "$payload_source" -type d -exec chmod 0755 {} +
find "$payload_source" -type f -exec chmod 0644 {} +
chmod 0755 "$payload_source/RgaaStudio.Desktop"
tar -C "$stage" -czf "$rpm_root/SOURCES/rgaa-studio-payload.tar.gz" rgaa-studio
cp "$script_dir/rgaa-studio-launcher" "$rpm_root/SOURCES/"
cp "$script_dir/rgaa-studio.desktop" "$rpm_root/SOURCES/"
cp "$script_dir/README-Linux.txt" "$rpm_root/SOURCES/"
cp "$script_dir/DISTRIBUTION-NOTICE.txt" "$rpm_root/SOURCES/"
sed -e "s/@RPM_VERSION@/$rpm_version/g" -e "s/@RPM_RELEASE@/$rpm_release/g" "$script_dir/rgaa-studio.spec.in" > "$rpm_root/SPECS/rgaa-studio.spec"
rpmbuild -bb "$rpm_root/SPECS/rgaa-studio.spec" --define "_topdir $rpm_root" >/dev/null
rpm_file="$(find "$rpm_root/RPMS/x86_64" -maxdepth 1 -name '*.rpm' -print -quit)"
[[ -n "$rpm_file" ]] || { echo 'rpmbuild completed without producing an RPM.' >&2; exit 1; }
cp "$rpm_file" "$output_dir/"
rpm --query --package --info "$output_dir/$(basename "$rpm_file")" >/dev/null

echo "Created: $deb_file"
echo "Created: $output_dir/$(basename "$rpm_file")"
