# Prototype Snap (not published)

This is an experimental strict-confinement Snap for Linux x86-64. It is intended to test App Center/Snap Store delivery, WebKitGTK inside Snap confinement, VMware rendering, and persistence of audit data. It is not uploaded to the Snap Store and is not a replacement for the signed APT repository or `.deb`/`.rpm` releases.

## Build on Ubuntu

Build on a native Ubuntu 24.04 machine with .NET SDK 10 and Snapcraft installed (`sudo snap install snapcraft --classic`). The script publishes the self-contained Linux app, assembles the Snap project and runs Snapcraft in destructive mode on the host:

```bash
bash tools/packaging/build-snap.sh
```

The resulting `.snap` is written to `artifacts/packages/<version>/`. Snapcraft stages GTK 3, WebKitGTK 4.1, libsoup 3 and their package dependencies into the confined Snap. The project declares network and desktop/display interfaces plus `browser-support` for WebKit subprocesses. Depending on the host's Snap policy, that last interface may need an explicit local connection:

```bash
sudo snap connect rgaa-studio:browser-support
```

The same build can be run from GitHub Actions with **Build experimental Snap** (`workflow_dispatch`). The resulting `.snap` and SHA-256 file are uploaded as a 30-day workflow artifact; this workflow does not publish to the Snap Store.

## Test without touching APT data

Install the locally built artifact with `--dangerous` on a disposable Ubuntu installation:

```bash
sudo snap install --dangerous artifacts/packages/1.0.0-preview.8/rgaa-studio_1.0.0-preview.8_amd64.snap
```

The Snap stores its database, settings and WebKit profile under `$SNAP_USER_COMMON`, separate from the APT/DEB installation. Existing APT audit data is not migrated automatically; back it up and use synthetic test audits in the Snap prototype. Test fresh RGAA and WCAG audits, existing catalogue/audit upgrades, selected-page navigation, VMware rendering, restart/refresh persistence, removal and reinstall behavior, and the `browser-support` permission before considering wider testing.

Remove the test Snap and its test data with:

```bash
sudo snap remove --purge rgaa-studio
```

`grade: devel` intentionally marks this as a development prototype. Do not release it to a stable Snap Store channel until the WebView and persistence checks have passed and the Snap Store name, listing, publisher identity and licensing metadata have been reviewed.
