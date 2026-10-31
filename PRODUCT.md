# Product

<!-- impeccable:product-schema 1 -->

## Platform

adaptive

## Stack

Avalonia is the selected primary desktop UI framework for Windows and Linux, by explicit user decision on 2026-10-30 after review of the client-approved Avalonia shell. The desktop app uses Avalonia 12 and the native `Avalonia.Controls.WebView` package as its current browser integration, using WebView2 on Windows and WebKitGTK/WPE WebKit on Linux; Linux runtime packaging and browser capabilities still require validation. WPF/WebView2 remains a temporary source/reference spike during migration, not a second product UI. The Electron UI spike remains rejected as a product interface. The current v1 distribution preference is a community-buildable/installable app; Microsoft Store publication is not a current requirement and remains a possible later channel.

## Users

Accessibility auditors working independently or in agencies, and accessibility teams in organizations. They audit rendered websites, make manual judgments, preserve evidence, and prepare reports.

## Product Purpose

Create a dedicated RGAA/WCAG website-audit workbench. At launch, auditors resume an active audit or create one; they configure its reference and scope, work page by page in an integrated browser, inspect elements and relevant rules when useful, record human decisions and evidence, then resume or report later.

## Positioning

A lot-centered audit workflow: one human result per criterion for the selected page sample, supported by page-specific applicability, observations, and evidence. The integrated browser provides the page context; automated detections assist but never replace the auditor’s lot-level decision.

## Operating Context

- Installable desktop targets are Windows and Linux; primary Linux install targets are Ubuntu 26.04 LTS and Fedora Workstation 44 x86-64. Ubuntu 26.10 is test-only.
- The product must be an installed desktop application with the audited site opened inside the app; it is not a browser-hosted website.
- Windows visual direction confirmed: frameless window, clean professional interface, modern menu and Lucide icon family. Preserve accessible resizing, minimize/maximize/close behavior and keyboard access; frameless must not remove operating-system window functions.
- The user confirms a specific shell-navigation pattern: hamburger/options menu; project-access icon; open project tabs that can be closed and reordered; plus/new-project action; update-available indicator; toggle for the audit side panel; and minimize, maximize/restore, and close controls. No application logo is wanted. The attached screenshot is a structural/visual reference, not permission to copy another product’s brand.
- When the dossier list is the current view, the project-access toolbar icon must show a persistent active state distinct from hover and keyboard focus. Dossiers are individual blocks with a visible hover state; clicking a row opens the dossier, and Enter/Space perform the same action. Do not add a redundant “Reprendre” label/button inside a row. Selected, hover and keyboard-focus states remain visually distinct.
- Optional favicon retrieval is disclosed separately by a visible opt-out at dossier creation. It is UI metadata, not audit evidence, and is independent of page navigation. The Avalonia WebView can open HTTPS pages selected into the active dossier; it blocks unselected and cross-origin destinations.
- Current Windows v1 distribution preference: documented community build/install. Microsoft Store is not a gate for now; it may be reconsidered as an additional channel later. Do not assume Linux has been removed.
- **Project/tab meaning confirmed:** one project is one audit dossier for one site and one reference. The project-access icon opens the dossier list; a top-level tab opens that dossier. Do not add a parent project container. Closing/reordering a tab changes navigation only and never deletes or changes the dossier.
- A current top-bar “update available” icon is requested; update source, security/signature policy, and exact user flow are not selected. It must not interrupt or misrepresent audit state.
- Interface and reports for v1.0.0 are in French.
- Each audit concerns one site and one fixed reference/version; a WCAG audit requires an explicit A/AA/AAA target.
- The expected flow is: resume/create → configure URL/reference/target → review and confirm candidate pages → inspect pages individually and record page-specific observations/applicability/evidence → review criteria across the complete lot and record one result per criterion → resume/report.
- The user expects candidate pages important/required for the site to be shown only when they exist, and expects the inspected element/state and screenshot to be retained as evidence. Exact page-discovery categories and mandatory/optional capture behavior remain open.
- Audits are local dossiers. Browser profiles are separate, persistent, explicitly assigned, and may be reused; this never merges audit findings or evidence.

## Capabilities and Constraints

- RGAA and WCAG are both supported; an audit uses one reference and version.
- Page suggestions are candidates, not a complete inventory. The auditor confirms what enters the audit lot; manual URL entry, imports, and adding the current page remain possible.
- Page discovery reads the HTTPS home page and same-origin sitemap for candidates; it is bounded and non-exhaustive. The auditor chooses which pages enter the dossier.
- Real selected pages can be opened in the integrated WebView and reviewed with axe-core or the contrast pipette. Pointer-based element inspection remains limited to the bundled demo page in the current preview.
- The auditor can point to an element and see available details and candidate relevant rules. Rule-level and page-/journey-level checks must also work without element selection.
- Element-to-rule relationships can be many-to-many. An element reference is contextual and may become stale when the site changes.
- Keep automated findings, manual observations, human conformity results, and review completion distinct. Non-applicable and non-verifiable results require justification.
- Evidence is attached to the right audit, criterion, and source page. Criteria receive one result for the audit lot; applicability, tests, findings, notes, reproduction steps, screenshots, and DOM/element context remain attached to their page; cookies, tokens, and browser-profile data must never enter evidence, reports, or logs.
- Expected report outputs are client PDF, editable ODT, and structured JSON. Jira is nonblocking.
- No complete visual identity, screenshot policy, page-category checklist, session-isolation policy, or release-ready browser security policy has been selected. Avalonia is the chosen primary desktop UI framework; final browser profile/session guarantees and Linux packaging still require verification.

## Brand Commitments

No approved product logo or complete brand system has been supplied; the user explicitly does not want an application logo in the navigation bar. The user-provided Windows app screenshot is now the confirmed structural/visual direction for frameless chrome, project tabs, a modern menu, and Lucide icons; do not copy the reference product’s brand. The current minimal Electron panel is an anti-reference, not an incumbent design system.

## Evidence on Hand

- Product and technical constraints are recorded in `docs/architecture/README.md` and its decision records.
- `spikes/windows/electron/` contains synthetic test data and a technical experiment only; it does not represent the intended UX or validate Electron as the final stack.
- A user-provided screenshot shows the requested Windows visual direction; no complete design system has been approved.

## Product Principles

1. Keep the auditor’s judgment authoritative; automation is assistance, never a conformance verdict.
2. Preserve traceability from audit and lot-level criterion result back to each page’s applicability, tests, findings, element/state context, and evidence.
3. Make page suggestions transparent and explicitly confirmed; never promise exhaustive discovery.
4. Support both element-level and page-/journey-level audit work, including keyboard and assistive-technology checks.
5. Keep audit data local and isolated from persistent browser profiles.

## Accessibility & Inclusion

The application itself must work with keyboard and screen readers. Mouse pointing and screenshots cannot be the sole route to a rule decision. Focus must move predictably between the target page and auditor workspace; findings, decisions, and review states must not be distinguished only by color.
