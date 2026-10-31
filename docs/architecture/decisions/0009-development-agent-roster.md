# ADR 0009: Development-agent roster and implementation readiness

- **Status:** Recommended (not user-approved)
- **Date:** 2026-10-29
- **Decision owners:** Project council; implementation framework and browser engine remain unselected

## Context

The repository has advisory agents (`project-lead`, `product-strategist`, `auditor-ux`, `browser-architecture`, `opensource-steward`, and `decision-facilitator`), but no dedicated implementation or review agents. There is no product code yet. The v1.0.0 scope includes a Windows/Linux desktop application with an integrated browser, persistent authenticated profiles, isolated audit records and evidence, and PDF/ODT/JSON outputs. The browser engine and application framework are not selected. Some substantial requirements remain open, including profile concurrency, the first automated-check catalogue, and retention/legal obligations.

Building immediately with implementation agents would risk turning unresolved platform and browser assumptions into costly architecture. Conversely, waiting to establish any implementation capability until every product question is closed would delay useful, bounded technical discovery.

## Decision

**Recommend creating a small, permission-bounded development roster now, but do not start full v1.0.0 implementation or describe the product as ready to code.** Use agents first for reversible discovery, spikes, and testable prototypes. Keep framework and browser selection open until comparative evidence is reviewed.

Recommended roster:

1. **Desktop/browser implementation agent** — prototype candidate runtime/browser integrations and report platform constraints; no authority to select the stack or commit the project to one.
2. **Test/release engineer agent** — define a minimal CI and packaging proof for Windows, Ubuntu 26.04 LTS, and Fedora 44 x86-64; Ubuntu 26.10 is test-only. Verify buildability and document gaps; do not claim supported installation until demonstrated.
3. **Application implementation agent** — added after the first architecture gate, to implement an agreed vertical slice against a reviewed domain contract.
4. **Independent code-review agent** — added before the first production-bound vertical slice; review security, accessibility, isolation, test coverage, and maintainability. It must not approve its own changes.

Initially grant repository-scoped read/write access only on isolated branches/worktrees, require human review before merge, and prohibit releases, signing, publishing, secrets access, external service commitments, and changes to architecture baselines without council approval. Agents must label assumptions and distinguish automated findings from auditor conclusions. Do not delegate legal retention conclusions or make unsupported claims about WCAG/RGAA coverage.

## First tasks and sequence

1. **Architecture discovery (parallel):** compare a short list of desktop/browser candidates against the mandatory OS targets, embedded browsing and inspection needs, persistent profile lifecycle, multi-audit isolation, accessibility of the host UI, update/security model, open-source buildability, and package feasibility. Produce a decision matrix with cited primary sources, unknowns, and a recommended bounded spike—not a binding stack decision.
2. **Risk spikes:** prove integrated-page-to-audit context access and profile reuse/concurrency or the accepted serialized fallback. Demonstrate that audit evidence/history remain isolated while browser profile state may be shared. No real credentials in fixtures or logs.
3. **Release proof:** create a minimal reproducible build/package experiment for the required Windows/Linux targets and identify signing, native dependencies, and maintenance burden. This is evidence gathering, not a release pipeline commitment.
4. **Product contract:** before the vertical slice, approve a minimal canonical audit model and acceptance criteria covering one reference per audit, explicit WCAG target selection, manual versus automated result distinction, evidence types, and incomplete-report behavior.
5. **Vertical slice:** only after the architecture gate, implement one end-to-end local audit flow with a single page, a small explicitly scoped check set, human review/result recording, local evidence, and one report path. Expand formats and catalogue only against validated contracts and tests.

Jira investigation may proceed as a separate nonblocking study; it must not gate the audit workflow. Do not implement the RGAA suggestion aggregation until its algorithm and applicability rules are validated against the selected catalogue/version.

## Alternatives considered

- **Wait until all architecture and product questions are settled before creating development agents.** Rejected: it postpones low-cost, reversible evidence gathering and packaging/browser feasibility work. The remaining open questions justify constrained spikes, not inactivity.
- **Recruit a broad implementation team and begin full product construction immediately.** Rejected: no framework or browser engine is selected, no product code exists, and key concurrency, security, and catalogue assumptions are unvalidated. This creates avoidable rework and risks overstating automated audit coverage.
- **Create only a general-purpose coding agent.** Rejected: it concentrates implementation and verification, and does not establish independent release/testing or review accountability.

## Consequences, dependencies, and validation

- This is a staffing/agent-governance recommendation, not a framework/browser decision and not authorization to ship.
- The first gate is a review of the comparison and spike results. Select a stack only if it can meet integrated-browser needs, required-platform packaging, profile/data isolation, and reproducible open-source builds with acceptable maintenance cost.
- Validate all required targets in CI or documented reproducible environments; tests must cover audit-data isolation, profile reassignment/transfer, secret exclusion, keyboard access to the host UI, report incompleteness, and the boundary between automated suggestions and auditor decisions.
- Risks include agent-generated code being mistaken for validated product capability, unsuitable dependencies, and platform packaging burden. Branch isolation, human review, explicit evidence, and non-release permissions mitigate these risks.
- Legal retention duration remains unresolved; no automatic deletion policy or legal-compliance claim is implied by this ADR.
- Revisit this recommendation after the architecture gate or if feasibility evidence shows that the browser/platform constraints cannot be met by an open-source, maintainable desktop stack.

## Consultation and evidence note

The requested specialist-agent consultation (`product-strategist`, `auditor-ux`, `browser-architecture`, `opensource-steward`) was not performed in this environment: no Task/delegation tool was available. No specialist positions, confidence levels, or objections are represented here. This ADR is therefore a council recommendation based on the repository baseline and stated project facts; it is not an accepted user decision. The comparisons and technical feasibility claims remain to be validated by the first tasks above.
