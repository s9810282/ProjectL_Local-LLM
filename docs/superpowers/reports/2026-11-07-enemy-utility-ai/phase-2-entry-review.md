# Enemy Utility AI Phase 2 entry review

**Status:** Phase 2 authorized; entry review completed. The user subsequently approved choice 1, including the preceding Preview optimization task. The [prerequisite result](phase-2-preview-optimization.md) now records the verified metadata optimization and remaining failed atom gate. Tasks 2.1–2.3 have not started. This is not a Phase 2 completion report.

## Verified starting point

- Branch: `feature/Cheon/Core/NewCombatSytem`; start/end HEAD for this review: `9d8c9ce456db95e8e0aff766240af47353f18f46`.
- The working tree contains Phase 1 instrumentation in five tracked files, untracked diagnostics and fixture/measurement tests with metadata, and the existing design/plan/reports. These changes were preserved. No commit or push was performed.
- All 13 source-file SHA-256 entries in `fixture-manifest.json` match the current files. The raw `inputs.tsv` hash also matches. This verifies the recorded source fingerprint set, not every file in the repository.
- The retained Core XML reports 5,043 cases: 5,017 passed, 25 failed, one skipped. CoreIntegration reports 957: 909 passed, 47 failed, one skipped. These are Phase 1 results, not new test runs. The Phase 1 report records no new failure identities relative to Phase 0.
- Recomputed from `Logs/EnemyAi/phase-1-9d8c9ce45-final/atoms.csv`, filtering `section=EnemyAimPreview` and `stage=warm`, with `runtime.txt` frequency 10,000,000 ticks/s: 1,400 calls, all above 1 ms, maximum 55.000 ms; N5 maximum 26.034 ms. Allocation remains unavailable in this runtime probe.

## Entry condition requiring a decision

Task 1.2's exit requires an approved atom strategy. Task 2.3 retains the existing precise attack scan and treats one full preview as one atom. The existing `EnemyAiPolicy.TrySelectAttackAim` calls `CombatCommandCalculator.Preview` synchronously. That delegates to the existing attack calculator and projection; yielding after this call cannot interrupt it.

The Phase 1 measurements are complete enough to establish this conflict. Repeating those measurements or wrapping the call in a resumable outer loop would not resolve the measured oversized atom. General Phase 2 authorization does not select a new assessment strategy or waive the recorded gate.

## Concrete scope choices presented for review

1. **Recommended: authorize a preceding owner-local preview optimization task.** First separate admission/selection, geometry construction, projection setup, delivery and settlement costs within the existing preview path. Evaluate reuse of immutable geometry and elimination of repeated setup in `CombatAttackQueries`, `CombatAttackCalculator`, and `CommandAttackProjection`, with the AI consumer supplying only a snapshot-local context. Keep mutable attack projections independent and retain the existing damage/effect/reaction owners. Apply only evidence-supported reductions and rerun the frozen corpus before accepting an atom strategy. If this remains insufficient and requires resumable effect execution or reduced assessment coverage, present that design separately rather than silently implementing it.
2. **Explicitly amend the Phase 2 entry condition to permit parity-only migration with the oversized preview retained.** Execute Tasks 2.1–2.3 without optional caps or Utility activation, keep G-atom failed, and prohibit claims of bounded frame time or Phase 3 readiness until a later reviewed remedy succeeds. This is a gate deferral requiring a decision, not an optimization.

Expected locality for choice 1: attack-query/calculator/projection setup plus the AI consumer and focused measurements. Required understanding includes snapshot identity, public visibility, attack admission and ordered effect settlement; it must not duplicate trait internals, Buffer ownership or Framework registration. Verification includes preview/actual semantics, taunt/collateral/reactions, frozen action traces, RC/RI failure-identity comparison and matched performance samples. Reduced modification or verification scope has not yet been demonstrated.

Choice 2 retains the original Phase 2 modification scope and its full parity/regression requirements. Its cost is a known potentially long Step; it cannot establish runtime frame protection.

## Work performed and remaining

- Completed: read Phase 0/1 reports, Phase 2 contracts, relevant design gates and current preview source; verified raw totals and source/input fingerprints; reconciled stale plan-level status entries.
- Product/test changes: none in this entry review. Tasks 2.1–2.3 remain unimplemented. Existing task checkboxes were not marked complete from historical summaries.
- New RC/RI/PF runs: NotRun. UF: NotRun; compilation/playback unverified (컴파일 미확인). No Unity process was launched.
- Changed documentation: the implementation plan's execution status/Phase ledger and this entry review. Existing Phase 0/1 evidence remains intact.
- Subsequent decision: the user selected "Preview 최적화 선행 작업 포함". Carry out choice 1, retain its evidence, and then continue Phase 2 if the atom strategy is resolved. Phase 3 remains outside this request.
