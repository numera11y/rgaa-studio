# Community preview packages

The packaging script reads the default version from the root `Directory.Build.props` and packages the Windows x64, Debian/Ubuntu amd64 and Linux x86_64 builds as:

- a self-contained .NET Windows x64 ZIP with per-user install/uninstall PowerShell scripts;
- a branded Windows setup executable that installs or updates the current release per user;
- an amd64 Debian package;
- an x86_64 RPM package.

Run from the repository root on Windows with .NET SDK 10, NSIS 3.13 and WSL enabled:

```powershell
.\tools\packaging\Build-Release.ps1
```

Install the Windows setup compiler with `winget install --id NSIS.NSIS --exact`. The default WSL distribution is `Ubuntu-24.04`; it needs `dpkg-deb` and `rpmbuild` (`sudo apt install rpm`). To select another installed WSL distribution or override the shared version:

```powershell
.\tools\packaging\Build-Release.ps1 -LinuxDistro Ubuntu-24.04 -Version 1.0.0-preview.1
```

Output is written under `artifacts/packages/<version>/`, with `SHA256SUMS.txt`. The setup EXE carries Windows file-version metadata and numera11y publisher metadata, provides a branded setup/splash, and supports per-user install/update/uninstall while preserving audit data. It bundles the Microsoft Edge WebView2 Evergreen Standalone Installer (x64), installs it only when missing, and leaves Evergreen updates enabled. The app and setup icons are generated from `src/RgaaStudio.Desktop/Assets/rgaa-studio-app-icon.svg` by `tools/packaging/RgaaStudio.IconBuilder`; the separate Windows ZIP is a portable/manual-install alternative and requires WebView2 to be present. The Linux packages depend on GTK 3, WebKitGTK 4.1 and libsoup 3; the apphost and .NET runtime are bundled.

The Linux package files are assembled on Ubuntu 24.04 WSL, not installed or runtime-tested on Ubuntu 26.04 or Fedora 44. Their Debian/RPM metadata and payload contents are validated during packaging; test installation and WebKit rendering on the target distributions before general distribution. The WSL package-building toolchain is not part of the deliverables.

The application is licensed under MIT (`LICENSE`). `DISTRIBUTION-NOTICE.txt` and third-party notices accompany the packages. The setup is unsigned. Linux package metadata and dependency resolution were checked on Ubuntu 24.04 WSL, but installation and rendering on Ubuntu 26.04/Fedora 44 still require validation.
