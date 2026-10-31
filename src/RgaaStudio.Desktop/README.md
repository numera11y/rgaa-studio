# RGAA Studio desktop application

Avalonia desktop application for Windows and Linux. The client-approved Avalonia shell is now the primary app UI. `docs/_sources/images/RGAA Studio · États de la barre et du menu.svg` is the shell/component design-system reference and `docs/_sources/images/États des items.svg` is the dropdown-row state reference. The projects-list active cue is a filled outlined toolbar tile; it has no underline. `spikes/windows/wpf-native-spike/` remains a temporary reference while its remaining workflows are migrated.

## Build and run

```powershell
dotnet build .\RgaaStudio.Desktop.csproj
dotnet run --project .\RgaaStudio.Desktop.csproj
```

The app uses the native Avalonia WebView (`Avalonia.Controls.WebView`): WebView2 on Windows and WPE WebKit/WebKitGTK on Linux. Linux installations need the WebKitGTK 4.1 and libsoup 3 runtime libraries documented by Avalonia. The browser currently loads only the bundled synthetic local fixture; real-site navigation is not yet enabled.

Audit manifests remain compatible with the WPF spike's local dossier format at `%LOCALAPPDATA%\RgaaStudioSpike\AuditDossier` on Windows (and the matching .NET local application-data location on Linux). They contain no browser cookies or credentials.

For an isolated test run, set `RGAA_STUDIO_AUDIT_ROOT` to a temporary directory before launching the app; the default local dossier directory is otherwise retained.

## Current slice

- Approved frameless Avalonia toolbar and text-only Options popup.
- Persistent audit dossier creation, listing and resumption.
- A centered, wide projects view with site host, reference/version, and last update in a clear hierarchy; clicking anywhere on a row opens it, so there is no duplicate “Reprendre” affordance.
- Browser-style open audit tabs remain available while returning to the projects list; closing a tab only changes navigation and never deletes the dossier.
- The tab strip takes the space between project actions and right-side window controls, shrinks tab widths to a readable minimum, then exposes previous/next navigation when the strip overflows. The close control is shown for the active or hovered tab.
- The resting active tab uses a white surface with a 3-DIP blue underline; the full outline is a hover/focus treatment, not the resting frame.
- Progressive reference setup: select RGAA/WCAG first, then its available version; show A/AA/AAA only after selecting a WCAG version.
- Native WebView host with a bundled fictional fixture and fixture-only element inspection.
- Append-only human decision events, required justifications for “Non applicable” / “Non vérifiable”, and an explicit review-completion event kept distinct from the conformity result.

Screenshot evidence, secure favicon retrieval, tab reordering, production navigation policy, and installer packaging remain to be migrated or implemented in later vertical slices. The interface makes no claim that the prototype fixture is a real site audit.
