# RGAA Studio — WPF migration reference (Windows)

Temporary migration reference for the previous Windows-only WPF/WebView2 implementation. Avalonia under `src/RgaaStudio.Desktop/` is now the primary desktop application by user decision (ADR 0018). Keep this code as the reference for audit decisions, evidence, secure favicon behavior and workflows until those slices are migrated and verified; do not continue the product UI here. The embedded WebView2 still renders only its synthetic local fixture.

## Navigation shell

The single top bar provides a hamburger/options popup (projects, new dossier, fixture inspection, settings/about/quit), project-list access, open dossier tabs, new dossier, update-status icon, audit-panel toggle, and minimize/maximize/close in that order. The user’s complete design-state sheet is `docs/_sources/images/RGAA Studio · États de la barre et du menu.svg`; the interaction contract is summarized in ADR 0017. It specifies active/inactive window states, a persistent inspection badge while the window is inactive, menu-first Escape, menu command keyboard behavior, disabled-item behavior, active-tab underline, and maximize/restore icon state. The hamburger keeps a 20-DIP visual tile inside its 40-DIP hit target and uses the depicted solid-blue/white-icon appearance while the popup is open. Menu row hover and focus remain distinct; disabled commands have a visible disabled state. The bar keeps its 48-DIP height, 12-DIP horizontal safe insets, and reduced glyphs; the WindowChrome resize border remains unobstructed. Native ContextMenu closure returns focus to Options where specified. Toolbar actions retain explicit hover, pressed, and keyboard-focus treatments. The Projects control has a persistent selected surface and UI Automation active-view name/status only while the dossier list is shown; hover and keyboard focus remain separate states. Tabs identify dossier IDs and include reference/version in their labels; the active tab is distinguished by a white surface and blue 3-DIP underline. Clicking a tab resumes that dossier. Closing a tab changes only open-tab state; the persisted dossier remains in the project list. Tabs can be dragged to reorder; when focus is in the tab strip, `Ctrl+Shift+Left/Right` reorders the focused dossier tab and `Ctrl+W` closes it. `Ctrl+W` is only intercepted while focus is in the tab strip so it is not swallowed from the audited page. After closing a tab, focus returns to the remaining active tab or, when none remain, the project-list control. Dossier cards have separate resting, hover, selected, and keyboard-focus treatments. A single click anywhere on a card, Enter, or Space opens/resumes its dossier; “Reprendre” and a decorative chevron remain visible without nested controls. The empty state keeps its create action. The panel toggle exposes native checked state and a state-specific accessible name without changing the active dossier. Frameless `WindowChrome` retains OS resize borders and caption dragging.

Every toolbar button follows the trigger-state palette: 20×20-DIP tile, white rest with 1-DIP `#7B8190` outline, `#E9EFF9` hover, `#D5E1F5` pressed with `#214B9A` outline, and 2-DIP `#214B9A` keyboard focus. Hit targets remain 40 DIP (46×44 DIP for window controls). The burger is white/outlined when closed; when open, it is `#214B9A` with white glyph and underline. Focus on the open burger adds a 24×24-DIP outer ring with a 2-DIP white gap. The dropdown contains **text-only rows without icons**, matching the Figma export. Focus and pointer hover remain separate.

The update control reports that updates are unavailable: this prototype has no update service or available-update feed. All dossiers use the same local WebView2 profile; profiles are not isolated per dossier.

At dossier creation, a visible checked-by-default opt-out allows one separate HTTPS request for the same-origin `/favicon.ico`. The entered site receives that request and sees the client IP as usual; the DNS resolver also receives the hostname for resolution. It uses no browser cookies, credentials, proxy, redirects, Referer, or third-party service. Only public IPs are allowed and pinned at connection time; `.test`, localhost and non-public/reserved addresses are refused. A 4-second timeout, 64 KiB transfer cap, raster allow-list and 256×256/65,536-pixel decode cap apply. A validated frame is re-encoded to dossier-local `favicon.png`; failures use the Lucide globe. This UI metadata is not evidence and does not change audit progress or `UpdatedAt`. WebView2 still only renders the exact local fixture and blocks all other navigation.

## Build and run

Prerequisites: .NET SDK 10.0 and the Microsoft Edge WebView2 Runtime (Evergreen). Package references are `Microsoft.Web.WebView2` **1.0.4314.35** and `SharpVectors.Wpf` **1.8.6** (BSD-3-Clause; upstream license and bundled Brotli/MinIoC notices are in `Assets/SHARPVECTORS-LICENSE.txt`).

```powershell
dotnet restore .\RgaaAuditor.Windows.csproj
dotnet build .\RgaaAuditor.Windows.csproj
dotnet build .\RgaaAuditor.Windows.csproj --configuration Debug
dotnet list .\RgaaAuditor.Windows.csproj package --vulnerable --include-transitive
.\verify-svg-icons.ps1
dotnet run --project .\favicon-checks\FaviconChecks.csproj
dotnet run --project .\RgaaAuditor.Windows.csproj
```

The Debug executable used by the repository's normal development workflow is `bin\Debug\net10.0-windows\RgaaAuditor.Windows.exe`; rebuilding Debug refreshes that path. It needs the .NET 10 runtime/SDK and adjacent `Assets` output.

### Windows x64 test executable

To create the self-contained .NET 10, single-file Windows x64 build (the Edge WebView2 Runtime is still required separately), run:

```powershell
dotnet publish .\RgaaAuditor.Windows.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output .\artifacts\design-system-states-20261030
```

Then launch `artifacts\design-system-states-20261030\RgaaAuditor.Windows.exe`. This dated output implements the supplied full toolbar/menu state sheet, including the inspection badge, menu-first Escape, open-state burger and minimize/maximize/restore glyphs, as well as the safe insets and smaller icons. Keep the adjacent `Assets` directory with the executable. This package includes the .NET runtime but not the Microsoft Edge WebView2 Evergreen Runtime; install that runtime separately. It is an unsigned, local test build of this fixture-only spike, not an installer or a production release. Windows SmartScreen may warn because it is unsigned. Build output is ignored by Git. If a Debug instance is open, leave it running and publish to this separate Release output; do not overwrite its locked Debug executable.

After creating at least two synthetic audits, check the local dossier structure with:

```powershell
.\verify-audit-store.ps1
```

The verifier checks manifest/directory identity, decision event IDs and ownership, proof ownership, and that each PNG exists within its dossier. It is a lightweight integrity check, not an application integration test and does not test UI resume behavior.

`favicon-checks` is a no-package, deterministic harness. It injects DNS answers and fake HTTP handlers; it must never access real sites. It checks normalized PNG fetch/cache, error/fallback conditions, safe-target rejections, opt-out, frozen local WPF image reload, and per-ID model updates through simulated tab switches/closes. It does not launch the window or verify pixels/UI Automation on screen.

The French fixture is `Assets/fixture.html`, copied to the output folder and loaded through its exact local file URL. It has a fictional email field, inert button and link. Do not enter real personal information. Inspect mode intercepts clicks in capture phase and prevents target activation; Escape or « Terminer l’inspection » ends mode. F6 reports the keyboard-focused fixture element. WebView2 is an untrusted rendering target: no host objects, raw privileged bridges, or external navigation are exposed. The message handler accepts a known schema, allow-listed tags, and bounded JSON/text.

## Local data and deletion

- WebView2 profile: `%LOCALAPPDATA%\RgaaStudioSpike\WebView2Profile`
- Synthetic audit dossiers (manifest, append-only decisions, PNG + JSON evidence, optional favicon UI metadata): `%LOCALAPPDATA%\RgaaStudioSpike\AuditDossier\<id>`

Close the app and delete those two directories to remove this spike's profile and dossier data. Each audit has a durable ID and `manifest.json`; decisions are immutable timestamped JSON events and completion is a separate event. Evidence files are stored under the active audit dossier, with audit ID and page context in each JSON record. Optional `favicon.png` is local dossier UI metadata only, never an audit proof and never included by the evidence-capture workflow. The WebView2 profile is a single shared local profile stored separately from dossiers; there is no real authentication and no per-audit profile isolation. The « Capturer la fixture locale » action uses `CapturePreviewAsync`, previews the PNG, and writes a JSON record. This is evidence of the synthetic fixture only, never a real-site capture. No cookies, tokens, or site credentials are saved by this workflow.

## Verification and limitations

`dotnet build .\RgaaAuditor.Windows.csproj`, the NuGet vulnerability check, `verify-svg-icons.ps1`, `verify-audit-store.ps1`, and the no-network `favicon-checks` harness are the current checks. The harness tests fake responses and injected DNS only; it does not establish live-network/DNS/TLS behavior or exercise the WPF UI itself. The stronger resting-state toolbar treatment now compiles; inspect both the empty and populated tab states and check hover/focus manually before treating its visual/accessibility validation as complete. A one-off UI Automation/keyboard smoke previously checked the shell, tab management, panel toggle, and fixture inspection. The dossier-card active/hover/selected/focus visuals and click/Enter/Space activation require manual interactive verification if UI Automation is unavailable. These are smoke checks, not complete keyboard/accessibility validation. No Narrator/NVDA validation is claimed. Human review decisions are not automated. `Conforme` and `Non conforme` are recorded as human-selected values; `Non applicable` and `Non vérifiable` require a nonblank reason. A saved decision is not completion of the audit.

This is a local Windows WPF/WebView2 candidate spike, not the final shell decision, not a Store-ready package, not signed, and not tested against real sites. It does not alter the required Linux v1 scope. No Microsoft Store certification, accessibility conformance, production security review, or real-site compatibility is claimed. The WebView2 runtime must be installed separately on the machine.

## Suggested manual keyboard and screen-reader checklist

- Tab and Shift+Tab through menu, window buttons, panels, decision and evidence controls; verify visible focus.
- Use Enter/Space on controls; verify window resize/minimize/maximize/restore/close affordances.
- Enter inspect mode, use Tab to focus a fixture control, press F6 to inspect it, and Escape to exit inspect mode. Verify clicks while inspecting do not activate target controls.
- Review names/order with Windows Narrator and NVDA; verify selection status, required justification error, evidence status, and decisions are announced.
- Test high contrast, 200% text scaling, and window resizing; record issues before any production claim.

The keyboard and screen-reader checklist is not a claim that those accessibility checks have been performed. UI Automation smoke coverage does not substitute for keyboard-only, Narrator, or NVDA testing.

## Icons

The toolbar and page-list icons use the supplied Lucide SVGs in `docs/_sources/images/` directly as WPF resources through SharpVectors; the build links them under `Assets/Icons/` without editing the supplied files. The inspection-menu icon uses the existing Lucide `Assets/inspect.svg`. `plus.svg`, the globe fallback, and the card’s `chevron-right.svg` were added unchanged from Lucide’s library; favicon images overlay the globe as decorative UI. Lucide notice: `Assets/LUCIDE-LICENSE.txt` (ISC). SVGs use a 24×24 viewBox; sizes are explicit WPF DIPs: topbar menu/projects/panel 18, plus/update/chevron 16, window controls 14–16, tab favicon 16, dossier-card favicon 20, tab close 12, and page-list icons 16. Each uses `Stretch="Uniform"` and sits within a larger control hit target. `verify-svg-icons.ps1` checks source viewBoxes, renderer/resource mapping, rendered sizes, and notices. Local Lucide assets are not fetched remotely; the site's favicon is retrieved separately as documented above. Native shell typography uses system Segoe UI.
