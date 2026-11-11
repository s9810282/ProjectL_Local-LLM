# Phase 7 N8 preparation allocation

The [residual allocation follow-up](phase-7-n8-residual-allocation.md) records the later diagnosis of repeated diagnostic labels and status comparisons, plus the new 5.03 MiB matched result. This report retains the earlier trait-presence measurements.

2026-11-08. Unity 6000.0.73f1, Windows 11, EditMode. This item 2 follow-up starts from the [8.86 MiB N8 observation](phase-7-item2-performance.md#native-allocation-diagnostic). The matched capture uses the same yielded preparation service, seed 1, 1 ms slice target, one N8 warmup side and one measured side. It retains the N5 fixture beforehand, as in the original capture. The normal game path continues to use immediate `Advance`.

## Allocation attribution

Unity `GC.Alloc` reported zero bytes for the empty calibration scope and one 4,128 B allocation for the 4,096 B array. All 502 N8 measured updates in the before stack trace and all 273 in the after stack trace matched the capture by fixture, seed, decision and update. The first export retained 299 updates and missed the calibration. It was excluded. Re-exporting the original trace with a 2,000-frame history retained all updates and both calibration markers.

| Preparation allocation stack | Before | After |
|---|---:|---:|
| Trait presence reconciliation, inclusive resolved stacks | 3,086,984 B | 291,056 B |
| `AppliedStatusRules.ReplaceTaunted` | 1,274,592 B | 0 B observed |
| `CharacterTraitEffects` construction within reconciliation | 1,015,584 B | 0 B observed |
| `MovementProjection.InputWork.Step`, inclusive resolved stacks | 2,510,928 B | 502,680 B |
| `CharacterStatusCompatibility` | 845,128 B | 845,128 B |
| Input assembly or source stamp on resolved stacks | 147,552 B | 147,552 B |

The stack buckets overlap. Projection observation includes nested trait presence reconciliation. The two removed child paths were rebuilding the empty shield and taunt collections for recipients even when no presence source or relevant state changed. Core method symbols were absent or a test frame was the only recognized ProjectL frame for 4,827,334 B in the before trace and 4,126,594 B after. Those bytes remain unattributed to a production owner. Enabling allocation call stacks increased update counts, so the call-stack traces serve as attribution evidence. The matched measurements below use stack recording disabled.

## Change

`TraitPresenceReconciler` retains the current immutable trait effects when there are no existing shields or shield sources and the recalculated Cohesion defense bonus is unchanged. It skips taunt replacement only when there is no taunt source and the recipient has no status entries. Recipients with existing entries still follow the same cleanup path. When reconciliation produces no character changes, shield and taunt event construction is skipped. The existing rules, event order for changed recipients and state ownership are retained.

## Matched remeasurement

| N8 seed 1, warmed side | Updates | Preparation | Largest decision preparation | Action calculation and publication |
|---|---:|---:|---:|---:|
| Before | 48 | 9,288,364 B (8.86 MiB) | 2,429,720 B | 3,196,400 B |
| After | 84 | 6,602,596 B (6.30 MiB) | 1,642,860 B | 2,370,232 B |
| After, independent Editor repeat | 44 | 6,480,196 B (6.18 MiB) | 1,639,800 B | 2,370,232 B |

The before total exactly reproduces the previous N8 side. The two after runs reduce preparation allocation by 28.9% and 30.2%. Each run published the same six decisions and compared the full published values and final battle with the synchronous Utility oracle. The difference between the two after totals is exactly 3,060 B per additional update. The same increment fits the matched and stack-enabled totals for each source. Raw totals include the service update boundary and are used as measured, with no adjustment to the gate.

**G-allocation remains unmet.** The N8 side still exceeds 2 MiB and all six preparation decisions exceed 256 KiB in both after runs. The unresolved stack bytes and the remaining status compatibility and projection costs are separate from the removed paths. This follow-up does not recertify G-atom or G-frame. Item 3 live gameplay and F1–F8 remain pending.

## Regression and handoff

The temporary profiler test harness was restored to the item 2 source hash. Regression runs use that restored harness and the final Core source. The only C# change from the previous item 2 source is `TraitPresenceReconciler.cs`.

The final Utility corpus passed. All 55 inputs and 480 trace rows match the previous item 2 corpus byte for byte. Core selected 5,239 cases with 5,205 passed, 26 failed and eight skipped. CoreIntegration selected 1,016 with 967 passed, 47 failed and two skipped. The same 72 Phase 0 failures and the previously recorded 180-second partition-1 Preview timeout match by full name, label and message. Taunt, shield, effective stats, Rampage, frozen Preview parity, prepared service and Utility publication suites passed. The three final runs match all 120 C# hashes. Exact failure comparison is in `phase-7-n8-investigation/regression-comparison.json`.

Under `Logs/EnemyAi/`, the allocation summary and diagnostic source are in `phase-7-n8-investigation/`. The complete stack exports are `phase-7-n8-before-stacks-full` and `phase-7-n8-after-stacks-export`. Raw traces are in `phase-7-n8-before-stacks`, `phase-7-n8-after-stacks`, `phase-7-n8-before-matched`, `phase-7-n8-after-matched` and `phase-7-n8-after-repeat`. Each run retains source manifests, Unity XML and exit status. Read `summary.json` for calibration, per-decision bytes and resolved stack totals.
