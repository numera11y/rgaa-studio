#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Usage: bash install-rpm.sh <rgaa-studio-rpm-file>" >&2
  exit 2
fi

rpm_file="$(realpath "$1")"
[[ -f "$rpm_file" ]] || { echo "RPM file not found: $rpm_file" >&2; exit 1; }
command -v rpm >/dev/null || { echo 'The rpm command is required.' >&2; exit 1; }
command -v dnf >/dev/null || { echo 'DNF is required on Fedora and CentOS Stream.' >&2; exit 1; }

[[ "$(rpm -qp --qf '%{NAME}' "$rpm_file")" == 'rgaa-studio' ]] || {
  echo 'The selected RPM is not an RGAA Studio package.' >&2
  exit 1
}
[[ "$(rpm -qp --qf '%{ARCH}' "$rpm_file")" == 'x86_64' ]] || {
  echo 'This preview RPM supports x86_64 only.' >&2
  exit 1
}

if (( EUID == 0 )); then
  dnf_cmd=(dnf)
else
  command -v sudo >/dev/null || { echo 'Run as root or install sudo.' >&2; exit 1; }
  dnf_cmd=(sudo dnf)
fi

[[ -r /etc/os-release ]] || { echo 'Cannot identify the RPM distribution: /etc/os-release is missing.' >&2; exit 1; }
. /etc/os-release
version_major="${VERSION_ID:-}"
version_major="${version_major%%.*}"
case "${ID:-}:$version_major" in
  fedora:*)
    echo 'Installing RGAA Studio and its GTK/WebKitGTK runtime from Fedora repositories.'
    ;;
  centos:10)
    if [[ "${NAME:-}" != 'CentOS Stream' ]]; then
      echo 'The CentOS RPM helper currently targets CentOS Stream 10.' >&2
      exit 1
    fi
    echo 'Enabling CRB and EPEL repositories to obtain WebKitGTK 4.1 on CentOS Stream 10.'
    "${dnf_cmd[@]}" install -y dnf5-plugins
    "${dnf_cmd[@]}" config-manager enable crb
    "${dnf_cmd[@]}" install -y epel-release epel-next-release
    "${dnf_cmd[@]}" makecache --refresh
    ;;
  *)
    echo "Unsupported RPM distribution: ${PRETTY_NAME:-${ID:-unknown}}. Supported targets are Fedora and CentOS Stream 10." >&2
    exit 1
    ;;
esac

"${dnf_cmd[@]}" install -y gtk3 webkit2gtk4.1 libsoup3 "$rpm_file"
