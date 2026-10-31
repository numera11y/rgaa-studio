# RGAA Studio desktop application

Avalonia desktop application for Windows and Linux. The client-approved Avalonia shell is now the primary app UI. `docs/_sources/images/RGAA Studio · États de la barre et du menu.svg` is the shell/component design-system reference and `docs/_sources/images/États des items.svg` is the dropdown-row state reference. The projects-list active cue is a filled outlined toolbar tile; it has no underline. `spikes/windows/wpf-native-spike/` remains a temporary reference while its remaining workflows are migrated.

## Build and run

```powershell
dotnet build .\RgaaStudio.Desktop.csproj
dotnet run --project .\RgaaStudio.Desktop.csproj
```

The app uses the native Avalonia WebView (`Avalonia.Controls.WebView`): WebView2 on Windows and WPE WebKit/WebKitGTK on Linux. Linux installations need the WebKitGTK 4.1 and libsoup 3 runtime libraries documented by Avalonia. It opens real HTTPS pages selected for the active audit; out-of-scope, unselected and cross-origin destinations are blocked. Candidate discovery scans the site's home page and public sitemap, then the auditor confirms which pages to include; it is not exhaustive. The bundled local sample remains available for pointer-based element inspection.

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
- Native WebView host for same-origin HTTPS pages selected in the audit; the bundled sample is retained for pointer-based element inspection.
- axe-core analysis and contrast pipette on real selected pages; automated results remain review suggestions.
- Append-only human decision events, required justifications for “Non applicable” / “Non vérifiable”, and an explicit review-completion event kept distinct from the conformity result.

Candidate discovery is not exhaustive; the auditor confirms the pages included in the audit. Authentication and navigation to unselected or off-origin pages are not supported. Pointer-based element inspection remains limited to the bundled sample. Linux installation/runtime validation and accessibility review with screen readers remain outstanding.
