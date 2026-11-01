#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 4 ]]; then
  echo "Usage: $0 <rgaa-studio.deb> <repository-root> <armored-private-key> <passphrase-file>" >&2
  exit 2
fi

deb_file="$(realpath "$1")"
repository_root="$(realpath -m "$2")"
private_key_file="$(realpath "$3")"
passphrase_file="$(realpath "$4")"
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
work="$(mktemp -d)"
gnupg_home="$work/gnupg"
trap 'rm -rf "$work"' EXIT
mkdir -m 0700 "$gnupg_home"
export GNUPGHOME="$gnupg_home"

for command in apt-ftparchive appstreamcli dpkg-deb gpg gzip tar; do
  command -v "$command" >/dev/null || { echo "$command is required to assemble the signed APT repository." >&2; exit 1; }
done
[[ -f "$deb_file" ]] || { echo "Debian package not found: $deb_file" >&2; exit 1; }
[[ -s "$private_key_file" ]] || { echo "Armored APT signing key is missing: $private_key_file" >&2; exit 1; }
[[ -f "$passphrase_file" ]] || { echo "APT key passphrase file is missing: $passphrase_file" >&2; exit 1; }

package_name="$(dpkg-deb -f "$deb_file" Package)"
[[ "$package_name" == "rgaa-studio" ]] || { echo "Unexpected Debian package: $package_name" >&2; exit 1; }

mkdir -p "$repository_root/pool/main/r/rgaa-studio" \
  "$repository_root/dists/stable/main/binary-amd64" \
  "$repository_root/dists/stable/main/dep11"
install -m 0644 "$deb_file" "$repository_root/pool/main/r/rgaa-studio/"

dpkg-deb -x "$deb_file" "$work/package-root"
appstreamcli compose --no-net --origin=numera11y --result-root="$work/appstream" "$work/package-root"
appstream_xml="$work/appstream/usr/share/swcatalog/xml/numera11y.xml.gz"
[[ -s "$appstream_xml" ]] || { echo "AppStream compose did not create a metadata catalog." >&2; exit 1; }
gzip -dc "$appstream_xml" > "$work/components.xml"
appstreamcli convert --format=yaml "$work/components.xml" "$repository_root/dists/stable/main/dep11/Components-amd64.yml"
gzip -n -9 "$repository_root/dists/stable/main/dep11/Components-amd64.yml"

icons_root="$work/appstream/usr/share/swcatalog/icons"
for icon_size in 48x48 64x64 64x64@2 128x128 128x128@2; do
  if [[ -d "$icons_root/numera11y/$icon_size" ]]; then
    tar -C "$icons_root" -cf "$repository_root/dists/stable/main/dep11/icons-$icon_size.tar" "numera11y/$icon_size"
  fi
done

(
  cd "$repository_root"
  apt-ftparchive packages pool > dists/stable/main/binary-amd64/Packages
  gzip -n -9 -c dists/stable/main/binary-amd64/Packages > dists/stable/main/binary-amd64/Packages.gz
  apt-ftparchive \
    -o APT::FTPArchive::Release::Origin="numera11y" \
    -o APT::FTPArchive::Release::Label="RGAA Studio" \
    -o APT::FTPArchive::Release::Suite="stable" \
    -o APT::FTPArchive::Release::Codename="stable" \
    -o APT::FTPArchive::Release::Architectures="amd64" \
    -o APT::FTPArchive::Release::Components="main" \
    -o APT::FTPArchive::Release::Description="Signed APT repository for RGAA Studio" \
    release dists/stable > dists/stable/Release
)

gpg --batch --import "$private_key_file" >/dev/null
fingerprint="$(gpg --batch --with-colons --list-secret-keys | awk -F: '$1 == "fpr" { print $10; exit }')"
[[ -n "$fingerprint" ]] || { echo "No signing key found in $private_key_file" >&2; exit 1; }
gpg --batch --yes --pinentry-mode loopback --passphrase-file "$passphrase_file" \
  --local-user "$fingerprint" --digest-algo SHA256 --clearsign \
  --output "$repository_root/dists/stable/InRelease" "$repository_root/dists/stable/Release"
gpg --batch --yes --pinentry-mode loopback --passphrase-file "$passphrase_file" \
  --local-user "$fingerprint" --digest-algo SHA256 --armor --detach-sign \
  --output "$repository_root/dists/stable/Release.gpg" "$repository_root/dists/stable/Release"
gpg --armor --export "$fingerprint" > "$repository_root/numera11y-archive-key.asc"
printf '%s\n' "$fingerprint" > "$repository_root/KEY-FINGERPRINT.txt"
gpg --verify "$repository_root/dists/stable/InRelease" >/dev/null
gpg --verify "$repository_root/dists/stable/Release.gpg" "$repository_root/dists/stable/Release" >/dev/null

echo "Signed APT repository assembled for $package_name. Signing-key fingerprint: $fingerprint"
