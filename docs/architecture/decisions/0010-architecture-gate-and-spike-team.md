# ADR 0010: Architecture gate and bounded spike team

- **Status:** Recommended (not user-approved)
- **Date:** 2026-10-29
- **Decision owners:** Project council; framework and browser engine remain unselected

## Context

There is no product code, the framework/browser engine are unchosen, and product specifications remain incomplete. The council received these specialist positions (as relayed in the decision request; no confidence scores or supporting technical artifacts were supplied):

- **product-strategist:** a very narrow vertical prototype; provisionally focus on an independent/agency auditor, explicitly not a confirmed target decision; minimal local audit model, integrated browser on Windows/Linux, one human decision, save/resume and progress report. Defer multi-page, full catalogue, full exports and Jira. Suggested roles include desktop/browser, persistence, UX and QA.
- **auditor-ux:** do not have implementation agents build product in parallel before a spike; a technical lead and UX executor should run it, testing keyboard/screen-reader use, target/tool boundary and profile control. Bring specialists in for bounded contributions.
- **browser-architecture:** instrumented comparison of desktop shell plus embedded Chromium (Electron is a candidate), Playwright as a visible integrated-workflow comparator, and a limited Qt WebEngine study; no binding choice. Compare profile/data behavior, packaging and focus.
- **opensource-steward:** clear desktop/navigation, packaging, QA and human open-source review responsibilities; investigate licensing/runtime during spikes; require reproducible builds and community buildability.

The positions agree on a gated, evidence-producing prototype and oppose premature stack commitment. Their tension is whether to staff implementation agents immediately or keep the spike under a small lead/UX pair. The evidence does not establish a need for a broad implementation roster or parallel product construction.

## Decision

**Recommend authorizing one small, time-boxed spike cohort now, not a general implementation team and not parallel product feature development.** A technical lead owns integration and the decision matrix; one UX executor owns the testable workflow and accessibility observations. Specialists may execute separate, non-overlapping spike work packages in parallel, with explicit interfaces and a shared evidence template. A human reviews results and decides at the architecture gate. No engine/framework is selected by this ADR.

The spike must demonstrate a narrow local workflow: open a target in an integrated-browser candidate, preserve an explicit target/tool focus boundary, record one human-reviewed outcome with its manual/automated provenance clear, save and resume local audit state, and produce a visibly incomplete progress report. Use synthetic data and no real credentials. The independent/agency persona is a provisional test lens, not a confirmed exclusive audience. Do not build a full criterion catalogue, multi-page workflow, complete PDF/ODT/JSON exports, or Jira in this spike.

### Work packages and ownership

1. **Lead — integration and architecture:** define common scenarios, instrumented measures, candidate matrix, and final report; coordinate interfaces and eliminate duplicate work.
2. **Desktop/browser specialist — runtime comparison:** compare Electron/embedded Chromium and one alternative shell/runtime (including the bounded Qt WebEngine study where feasible); use Playwright only as a visible-workflow comparator, not as an assumed production runtime. Measure navigation/context access, focus transitions, profile creation/reuse/reset, and target OS feasibility.
3. **UX executor — workflow/accessibility:** prototype the narrow flow against the same scenarios; test keyboard-only operation and at least one screen reader on the host UI; document focus loss, target/tool boundary comprehension, and profile selection/control. Do not independently implement a second competing product prototype.
4. **Packaging/open-source specialist:** independently verify a reproducible build/package path for Windows and the confirmed Linux targets, dependency/runtime licensing, native prerequisites, and community build instructions. No signing, publishing, or release commitment.
5. **QA/reviewer:** define and run repeatable smoke tests, inspect evidence and test coverage, and review the spike artifacts independently. QA may run concurrently but must not duplicate candidate implementation.

The lead assigns distinct workspaces and deliverables before parallel execution. Candidate prototypes share the same scenario and acceptance checklist; there is one UX workflow prototype, one packaging investigation, and one consolidated comparison. Human review is required; agents do not merge architecture-baseline changes or publish artifacts.

## Architecture gate: exit criteria

The gate is passed only when the council has a reviewed evidence pack that:

1. compares at least two viable integrated-browser approaches on the same scripted scenarios, with Playwright clearly identified as comparator where used;
2. demonstrates URL/navigation context and usable target/tool focus transitions, and records keyboard plus screen-reader findings and blocking defects;
3. demonstrates profile create, reuse and explicit reset/control, explains shared-session risks, and either proves safe independent interaction or documents the already accepted single-controller/explicit-transfer fallback; audit records remain isolated;
4. proves save/resume of a synthetic local audit and preserves a human decision distinct from automated observations, with a progress report that cannot be mistaken for a completed audit;
5. records reproducible packaging/build evidence for Windows, Ubuntu 26.04 LTS and Fedora 44 x86-64, with Ubuntu 26.10 test-only; lists failures and does not claim unsupported installation;
6. includes a dependency/runtime licence inventory, security/update and maintenance risks, build instructions, test results, unresolved assumptions, and effort/complexity comparison;
7. receives human UX/accessibility and open-source/release review, with no unresolved critical data-leakage, focus-trap, or misleading-audit-status defect.

At the gate, choose a candidate only if it meets the mandatory workflow, profile/data safety, packaging and reproducibility criteria. Otherwise run one explicitly scoped follow-up spike for the blocking gap or revise the MVP constraint; do not silently select the least-bad candidate. Gate failure does not authorize broad implementation.

## Alternatives considered

- **Start a broad set of implementation agents on product features immediately:** rejected. With no code or selected runtime, parallel feature work would duplicate effort and harden untested assumptions; UX/accessibility and persistence contracts are not settled.
- **Only a lead and UX executor, with no concurrent specialist tasks:** rejected as unnecessarily serial. Packaging/licensing and a separately bounded runtime comparison can produce independent evidence concurrently, provided ownership and artifacts are disjoint.
- **Choose Electron, Playwright, or Qt WebEngine now:** rejected. These are candidates/comparators, not validated choices; the supplied positions include no instrumented results or platform packaging proof.
- **Wait for complete specifications before any spike:** rejected. A narrow spike can test the riskiest browser, focus, profile and packaging assumptions without claiming product readiness.

## Consequences, dependencies, risks, validation

- This is a council recommendation, not user approval, a framework selection, or authorization for full product implementation.
- Only bounded, reversible spike code is authorized under isolated branches/workspaces and human review. No credentials, release/signing, publishing, or external service commitments.
- The confirmed Windows/Linux distribution constraints apply; the technical claims about candidate runtimes and packages remain unverified until demonstrated. Licence compatibility is an investigation, not a legal conclusion.
- Product strategy's auditor focus is provisional; the spike must not rewrite the confirmed audience constraint or exclude organizational accessibility teams.
- A narrow prototype risks underrepresenting complete audit and reporting needs. It must label automated observations as assistance, not compliance conclusions; acceptance of the prototype is not evidence that the full audit workflow is solved.
- Reassess staffing and authorize a vertical-slice implementation only after gate evidence and a reviewed minimal product contract. Full catalogue, multi-page, complete exports and Jira remain deferred for this spike, not permanently rejected.
- No material legal or financial commitment is made. No user preference is needed to proceed with this reversible evidence-gathering plan.

## Evidence and consultation note

The four specialist positions above were supplied in the user's request as actually received. Their confidence levels, detailed evidence, and specific objections were not included; this ADR does not invent them. The disagreement is resolved by separating the lead/UX-owned product prototype from independent, bounded technical/packaging spikes. All candidate feasibility statements remain hypotheses pending the exit criteria.
