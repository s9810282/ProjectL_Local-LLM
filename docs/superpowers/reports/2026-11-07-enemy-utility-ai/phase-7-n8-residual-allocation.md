# Phase 7 N8 residual preparation allocation

2026-11-08. Unity 6000.0.73f1 EditMode. The matched comparison uses the same N8 seed-1 yielded Core service, 1 ms slice target, one warmup side, one measured side and preceding N5 fixture as the [6.18 MiB reference](phase-7-n8-allocation.md). Owner and call-stack captures are separate from the matched run. Every measured N8 update was paired with its native marker. Each retained an empty scope at 0 B and the 4,096 B array probe at 4,128 B. All N8 captures matched the synchronous Utility oracle for all six published results and final battle state.

## Attribution and changes

- Direct and follow-up attack preparation rebuilt the same diagnostic phase labels on consecutive Steps. Exclusive Unity markers around the two label assignments measured 652,968 B before and 96,748 B after. The two calculations now retain the most recent child phase label until the child phase changes. Two regression cases failed before the fix when equal text had different references. The final atom capture preserved all 1,364,675 existing fixture/sample/operation/version/label rows.
- The status compatibility path allocated a list and LINQ buffers even for empty status sets. The resolved stack total fell from 845,128 B to 218,880 B. `CharacterStatusCompatibility.Import` now creates its list only when it imports an entry. The equality check returns early for two empty status sets. Nonempty comparisons still compare owned statuses by key and order. Existing input validation, canonical status ownership and legacy projection remain in their normal call paths.
- The earlier unexplained 3,060 B per extra preparation update came from five source checks calling the test fixture's `DefinitionRead.Require`. It allocates NUnit constraints on the success path. The diagnostic changed only that fixture method to a direct battle-ID check. The `EnemySourceValidation` bucket then went from 173,196 B to zero observed bytes. The assembly and preparation buckets also lost allocations from calls to the same assertion. The original fixture and production source validity checks are retained. The matched before/after comparison deliberately uses the original fixture.

## Matched and supplementary measurements

| N8 seed 1, warmed side | Updates | Preparation | Largest preparation decision | Action calculation and publication |
|---|---:|---:|---:|---:|
| Previous matched capture | 44 | 6,480,196 B (6.18 MiB) | 1,639,800 B | 2,370,232 B |
| Final matched capture | 36 | 5,273,576 B (5.03 MiB) | 1,292,244 B | 1,770,808 B |
| Owner trace with a direct fixture ID check | 48 | 5,137,712 B (4.90 MiB) | 1,260,420 B | 1,770,808 B |

The matched preparation total fell by 18.6%. Accounting for the eight fewer updates at 3,060 B per update gives a descriptive work reduction of 1,182,140 B. The unadjusted matched total remains the recorded result. The direct-guard owner trace is supplementary. It changes the diagnostic fixture rather than the gameplay validation.

In the direct-guard trace, the largest exclusive owner buckets are candidate generation (909,254 B), follow-up assessment excluding narrower nested markers (679,888 B), movement assessment (593,910 B) and threat work (492,740 B). These are preparation phase buckets taken at Step entry, so a transition Step can have the preceding phase label. Narrower existing Core markers take precedence for nested work. Only 3,408 B fell directly under the update root. The remainder is assigned to phase scopes, with some leaf allocations still unresolved. The trace does not establish that every allocation in those phases is essential or removable. Necessary snapshots and validation remain in use.

**G-allocation remains unmet.** Both the matched and supplementary N8 sides exceed 2 MiB, and all six preparation decisions exceed 256 KiB in each. Item 3 live gameplay, F1–F8 and activation remain pending.

## N5 first attack

The historical native Editor trace attributed 29.9697 ms of the 34.6675 ms N5 version-1644 Step to direct `Mono.JIT` samples. The previous uninstrumented run measured 30.2972 ms. The final uninstrumented run on this source measured 28.3714 ms at the same work version and label, without a recorded collection. These runs establish a recurring first-use delay. They do not establish a JIT improvement. The current warmed matrix contains nine Steps above 1 ms with a maximum of 17.6069 ms. G-atom remains unmet. The N8 allocation work did not include startup or first-use JIT remediation. No target Player cold run or rendered gameplay capture was performed.

## Verification and handoff

The matched allocation, supplementary attribution and final atom captures passed. The Utility corpus preserves all 55 frozen inputs and 480 trace rows byte for byte. The two label-reuse cases passed after the fix. Status contracts (8/8), Utility decisions (14/14), frozen Preview parity (2/2), prepared service (27/27) and Utility publication (5/5) passed. Core selected 5,241 cases with 5,207 passed, 26 failed and eight skipped. CoreIntegration selected 1,016 with 967 passed, 47 failed and two skipped. The 72 Phase 0 failures and the previously observed partition-1 Preview timeout match by full name, label and message. All final runs match the same 121 changed C# source hashes. See `phase-7-n8-residual/regression-comparison.json` for the exact comparison.

The temporary owner markers, diagnostic fixture and capture harness were restored. The persistent changes are the two label writers, the status compatibility path and the two label regression cases.

Under `Logs/EnemyAi/`, read `phase-7-n8-residual/summary.json` for per-decision allocations, calibration and exact owner totals. The raw capture/export pairs use the suffixes `leaves-before`, `leaves-after`, `clean-guard`, `matched-after` and `stacks-after` after `phase-7-n8-residual-`. The prior stack sample is `phase-7-n8-after-stacks-export`. The final phase and timing comparison is `phase-7-n8-residual-atoms/phase-identity-and-timing.json`. Unity logs, XML and source manifests are retained with each run.
