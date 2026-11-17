# Combat Health Evidence Consistency Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` for this single implementation phase. User restrictions override test-authoring, execution, and commit defaults in skills.

**Goal:** Accept lethal reflection followed by reconstruction using one interpretation of health events across the evidence reader.

**Architecture:** Keep the existing public events, damage application, and Buffer ownership. Extract the existing attack health-chain interpretation into a private, stateless projection in `CombatChangeEvidenceReader`. Its consumers read damage, recovery, normalization loss, and ordered before/after values rather than calculating their own net-health deltas.

**Tech Stack:** Existing Unity 6000.0.73f1 / C# Core contracts; no new dependencies.

**Spec:** User-approved common health interpretation from this chat; Core boundary handoff §5.88 records the implementation and extension contract.

## Design and scope

- The defect is semantic drift: reflection records actual health loss, while a reconstructed event contains the pre-hit health and restored maximum. The existing attack-chain reader already interprets this as pre-hit health -> zero -> restored health.
- Preserve this interpretation in one place. Ordinary damage/healing uses `HealthChanged` / `Died`; cap adjustment uses `HealthNormalized`; reconstruction contributes two ordered steps. These are event semantics, not trait-key or artifact-combination exceptions.
- Preserve attack-chain continuity and exact totals. Artifact and item checks retain their existing character-only recipient scope and required-amount comparison. Normalization loss must remain distinct from damage.
- Apply the common interpretation to attack chains, artifact damage/healing totals, item healing totals, and the generic trait-healing evidence check.
- Do not add public event fields, duplicate runtime state, relax missing-evidence rejection, change AI, or alter presentation events.

## Single phase

- [x] Extract ordered health-change projection and route the four reader consumers through it.
- [x] Inspect ordinary damage, healing, death, reconstruction, normalization, structure attacks, and non-health event exclusion for semantic consistency.
- [x] Run scoped `git diff --check` and independent read-only review; resolve actionable review findings.
- [x] Update Core handoff §5.88 and its title/date; report runtime verification as pending.

## Verification constraints

No test code may be added or changed in this task. Do not run Unity or MSBuild; do not commit or push. Static inspection and review are not a runtime pass.

The user should verify a low-health enemy with unused Reconstruction attacking a player while the reflection artifact is equipped, including reconstruction ranks with and without shields. Also verify ordinary lethal reflection, nonlethal reflection, zero damage/full absorption, reconstruction at full HP, healing after damage, cap normalization, normal character/structure attacks, and composed AI publication. Run existing evidence, artifact, reconstruction, combat, movement-undo, and CoreIntegration regressions.

## Future extension boundary

New traits that use existing health operations should emit the existing health events through the common execution boundary; their keys do not belong in the evidence reader. A genuinely new health operation must first define its event meaning and ordered health steps in the common interpreter, with integration verification. Merely naming a new event cannot make its meaning safely inferable.

For a future operation that cannot be represented unambiguously by current events, consider an execution-owned immutable health receipt containing actual loss/recovery/normalization and source identity. That is a public contract migration requiring a separate design, not part of this fix. Do not create a second mutable ledger or copy all combat state to achieve it.

## Execution record

- Planned against the current working tree, preserving the earlier Ground Compaction fix and unrelated asset changes.
- Implemented the common reader projection without changing public events or adding trait-key conditions. Independent read-only review found no critical or important issue. Scoped whitespace validation passed; no test files changed.
- Runtime verification remains pending.
