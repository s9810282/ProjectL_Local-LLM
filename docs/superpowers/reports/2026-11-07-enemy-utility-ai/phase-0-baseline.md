# Enemy Utility AI Phase 0 baseline

2026-11-07 KST · `feature/Cheon/Core/NewCombatSytem` · source HEAD `9d8c9ce456db95e8e0aff766240af47353f18f46`.

**Review stop. Phase 0 source, test and current-scene performance evidence is recorded.** Five enemies is the normal workload. A six-enemy scene is a supplemental sample. The captures expose an over-budget preview and repeat-work costs. Phase 1 and production changes remain outside this execution.

## Source and scope

R0 found no tracked changes at entry. The two existing untracked inputs were the [implementation plan](../../plans/2026-11-07-enemy-utility-ai-hazard-forecast-implementation-plan.md), blob `fec272a92ce513631f2bd86762d93cb810b13860`, and the [design](../../specs/2026-11-07-enemy-utility-ai-hazard-forecast-design.md), blob `f5e31ee89b8c84bab3411e4975d161dd9284cd25`. This report uses the plan's §1.2 path aliases. The source trace agrees with design §2 and identifies where the proposed scheduling boundary in §7 would attach.

Ending HEAD matches the starting HEAD. This report is the only new documentation file. Unity rewrote the FMOD event catalog's local scan path and cleared dynamic data in two TMP font assets during the test runs. The live run also updated FMOD cache timestamps. Only these run-generated asset changes were restored after the Editors exited. Worktree and index diffs are empty for tracked files. Raw test and Profiler evidence is ignored locally and should travel with this report when handing off to another machine.

1. `F/Plans/CombatPlan.Next` uses progress facts to decide whether to schedule a `CombatNextJob`. That job re-reads progress via `CombatPlan.BuildStep`, then executes each child immediately and requires it to finish in one call. `CombatPlan.Decide` handles completion, abandonment and presentation before automatic progress. Its `Advance` step carries the prior result sequence.
2. `F/Jobs/CombatAdvanceJob.Execute` calls `ICombatAdvanceService.AdvanceCombat()` synchronously. Job success requires a newer result sequence, a latest result whose sequence matches the state, processing kind `Advance`, and an accepted result. The bool from the service alone is insufficient. The service may return false for a normal no-op or if terminal publication cannot be planned.
3. `S/CombatAdvanceService.AdvanceCombat` checks run/battle/phase, then calls `CombatInputAssembler.Assemble`, `CombatCommandCalculator.Advance`, and `CombatTransitionPublisher.Publish` in one call. The assembler reads installed definitions/profiles, battle state and membership, character and owned/Main facts, soul counts and field, artifact input, item inventory, gold and fame. The publisher attaches terminal progress/rewards where applicable and hands the transition to the existing Battle Buffer write sink. The transaction owns validation and application.
4. `A/CombatCommandCalculator.Advance` handles controller entry or delegates the enemy controller to `AI/EnemyAiTurnService.Calculate`. Pending boss roar precedes ordinary selection. Eligible enemies are ordered by EntityId. Each actor tries an attack using visible facts, then may pursue if still eligible to move. Hidden-fact attack rejection falls back to pursuit without trying another aim for that actor. An accepted move may be followed by an attack in the next Advance. The service returns one accepted ordinary action or ends the enemy phase after exhausting eligible actions. Actual reaction and undo reconciliation remain in the calculator.
5. `A/CombatCommandComposition.Project` materializes temporary facts from a transition. `Merge` creates a transition relative to the original Advance, including events and settlement, and guards the single ordinary action-use settlement. These methods have no Buffer publication responsibility.

## EditMode baseline

Unity `6000.0.73f1` ran in batch mode through the AppData-capable approved route. Both Editor-only assemblies use `UNITY_INCLUDE_TESTS`. `ProjectL.Tests.Core` has no engine references. `ProjectL.Tests.CoreIntegration` references Systems, Framework, Core and Unity UI. Raw evidence lives at `Logs/EnemyAi/phase-0-9d8c9ce45/` in the local checkout. Keep the NUnit XML, Unity log, command arguments and process exit files together.

| Check | Status | Evidence |
|---|---|---|
| RC · `ProjectL.Tests.Core` | Failed on the Phase 0 source | 5,008 total, 4,983 passed, 25 failed, 0 skipped, 0 inconclusive. Unity exit 2. `core.xml`, `core.log`. |
| RI · `ProjectL.Tests.CoreIntegration` | Failed on the Phase 0 source | 952 total, 905 passed, 47 failed, 0 skipped, 0 inconclusive. Unity exit 2. `integration.xml`, `integration.log`. |
| UF · interactive Unity scene and Flow | Partial | Editor loaded the unchanged source and both scenes reached deployment, enemy action playback and the next player turn. The full manual Flow checklist, including abandonment and terminal/transition cases, is NotRun. The live smoke checks are separate from the original batch tests. |

The eight named Core AI fixtures passed **175/175**. A different AI-relevant fixture, `TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True)`, executed and failed at line 620 with expected 1 Taunt event and actual 2. It counts all Taunt activations before asserting `TauntSelected`. The current source can emit Taunt presence changes in `TraitPresenceReconciler` as well as selection in `EnemyAiTurnService.RecordTaunt`. This is a source-supported explanation to verify against the exact event list. Keep this failure visible in parity reviews. It is a Phase 0 baseline failure and its ownership and intended contract must be checked before changing the assertion.

The first Core failure was `BossNightLifecycleTests.DeadSummon_NeverReturns_AndFreesItsSlot` with `WrongPhase`, expected true and actual false. Twenty-four failures exercise eight-turn night completion or its downstream behavior. `PlayerTurnRuleTests.NightLengthIsEight` directly confirms expected 8 and actual 99. `PlayerTurnRules.PlayerTurnsPerNight` is 99 at this source (line history `3a20dc9a39`), and `CombatTurnCalculator.EndEnemyPhase` checks that constant before night cleanup. The cluster includes a thrown `InvalidOperationException` in `PositionTileModifierTests.NightRecallAndPlacement_ReconcileOnlyCurrentPositionContribution` with “Sequence contains no matching element”. Exact failed case identities and messages are listed below. Keep the 99-turn source intact when comparing AI phases unless a separate, authorized change moves the baseline.

The first CoreIntegration failure was `ArmorBreakerPublicationTests.ActualNightRecall_ClearsArmorReduction_WhileOrdinaryTurnsPreserveIt`, expected Deployment and actual Combat. `CombatAdvanceServiceTests` passed 5/6. Its night-expiry case failed on the eighth response with expected one `NightEnded` event and actual zero. Its ordinary enemy publication, pending input, controller start and terminal checks passed. CoreIntegration reported 19 assertion failures and 28 errors. Thirteen errors were in `DeckSlotInfoPanelTests` SetUp and did not reach their test bodies. The first reported exception was `System.TypeLoadException` in `CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset",False)`. It reached the type lookup, and the behavior assertion could not run. This is a fresh observation of the older failure. Other failures include missing stage 1101 and production asset/growth expectations, missing Canvas, Deck view exceptions and night-transition assertions. Those outcomes stay in the baseline even when outside Utility AI scope. The appendix and raw XML preserve the identities. Assertions and errors in the raw XML remain distinct from environment-blocked runs and unexecuted checks.

NUnit XML SHA-256 for future evidence verification

- `core.xml` · `600EC06F0E981144112C170EF5ABB442EDB03BBFD24A8672455D2E9BCEF66695`
- `integration.xml` · `CC78CA92EE92D6655237DFF12BD5F98496819F5B8D4A90C77BFD85ADA2D67436`

### Test ownership and expectation inventory

Git history is authorship evidence. An owner review of future assertion changes is pending. Existing assertions stay in force through parity work.

| Test class | Git authorship and first addition | Assertion coverage to preserve or review |
|---|---|---|
| `StageEnemyAiTurnTests` | GwonCheonmyeong, `fdbd06bd0`. JMH moved the file in `a18d86d6a`. | Preserve hostile/ally eligibility, actual vs preview targeting, hidden rejection fallback and accepted route evidence. Phase 6 review candidates include zero-damage commitment, unit vs HQ ranking, HP loss vs badge break and choice-specific tie results. |
| `EnemyMoveThenAttackTests` | GwonCheonmyeong, `8ece0bbc1` | Preserve move once, subsequent attack admission, stable actor order, and skip of a moved enemy out of range. Keep these fixtures as parity vectors. |
| `EnemyMovementPathTests` | GwonCheonmyeong, `87348b86e` | Preserve allowance, movement use, safe legal prefix, terrain/mine visibility, deterministic tie order and hidden rejection. `ZeroDamageCurrentTarget_IsAttackedWithoutPursuit` encodes the old preference. |
| `EnemyPursuitEquivalenceTests` | GwonCheonmyeong, `bd90f0b6d` | Preserve reference reachability and path equality in the legacy-policy phases. |
| `TauntTraitTests` | GwonCheonmyeong, `f848f1f57` | Preserve required taunter targeting/pursuit, stable taunter ordering, source validity, fallback, legal paths and damage reduction. |
| `WallAttackAiTests` | GwonCheonmyeong, `66f6bb41a` | Preserve shared wall admission, occlusion and pierce evidence, and occupied-wall movement legality. Review policy-dependent route/attack choice separately. |
| `PiercingShotAiTests` | GwonCheonmyeong, `ee2b909f6` | Preserve delivery and pierce exhaustion legality. Exact damage/collateral ranking and aim ties depend on preference. |
| `ThornHeartReflectionPreviewAiTests` | GwonCheonmyeong, `ebf26246e` | Preserve preview isolation from actual-only reflection, real hit/death semantics and hidden information. Review exact aim expectations only where the new policy requires it. |
| `CoreIntegration/CombatAdvanceServiceTests` | GwonCheonmyeong, `c816c46df` | Preserve no-op behavior, one publication, result/actor/action-use evidence, pending input, night cleanup order and terminal suppression. Keep the attack-specific scenario as publication coverage. |

If Phase 6 changes the zero-damage preference, review the assertions demanding Attack over a legal Hold or reposition in the two named tests. Keep a separate legality check proving that zero damage does not make a command illegal. Exact replacements for rank or aim assertions depend on the reviewed fixture score facts and must be approved with any ownership restrictions.

## Performance evidence

### Capture setup and raw evidence

Unity `6000.0.73f1 (a166abc3bf0e)`, Windows 11, Ryzen 7 6800H, RTX 3070 Ti Laptop GPU, DX11, Editor Play Mode target, CPU Usage with Deep Profile off. The Profiler retained up to 1,988 frames. Recording started around the player's End Turn confirmation after the scene and deployment had settled. This warms scene entry only. A later action can still incur first-use JIT. EditorLoop, rendering and UI remain part of the exported frame CPU. No standalone Player trace was collected.

The existing Play From Boot override entered `Assets/Scenes/Combat.unity` on chapter 1, node 2, loadout 1, using the Database battle definition and the selected stage override. The progress node requested stage 1120, while the log confirms the actual battle data keys below. Both encounters used authored initial enemies plus a separate HQ structure, with stage timeline seed 1 and no starting item override. The player deployed only the Main on the left highlighted strip, near the center of the strip in the default camera, and ended turns without moving or attacking. The exact Main grid coordinate was not recorded. The Play From Boot settings and original 300-frame Profiler history were restored after capture.

All paths below are relative to `Logs/EnemyAi/phase-0-live-9d8c9ce45/`. Each capture has the named `.data` original and a same-stem directory with `frames.csv`, `markers.csv`, and `summary.md` copied from the existing exporter. `editor.log` confirms both selected stage keys and all three export events. Exported frames had no skipped entries within their retained ranges.

| Role | Authored encounter | Capture stem and exporter timestamp (KST) | Coverage |
|---|---|---|---|
| First-use diagnostic | Stage 1124, `Combat_LD_P24`, five enemies and tornado | `stage-1124-enemies-5` · `20261107-030553` | First enemy side, partial. Frames 0–1,050, five Advance frames and four EnemyTurn frames. Recording stopped before the side finished. No precise preview marker in this window. |
| Normal baseline | Stage 1124, same battle after its first enemy side | `stage-1124-enemies-5-turn-2` · `20261107-030741` | Second enemy side from End Turn through return to player input. Frames 0–1,656, nine Advance frames and seven EnemyTurn frames. First-use attack preview still incurred JIT. |
| Supplemental | Stage 1122, `Combat_LD_P22`, six enemies and railway | `stage-1122-enemies-6-turn-1` · `20261107-030950` | First enemy side, through return to player input. Frames 85–2,072, eight retained Advance frames and seven EnemyTurn frames. The ring buffer dropped the first 85 frames. Treat totals as retained-window totals. |

Stage 1122 has different terrain, placement and hazard inputs. This pair establishes current costs under two workloads. An enemy-count scaling comparison requires matched inputs. The previously affected November 3 setup was unavailable for a matched replay. Its [incident record](../../specs/2026-11-03-enemy-pursuit-pathfinding-performance-design.md) reports 231.98 ms for Advance and 226.53 ms for pursuit before `bd90f0b6d`. The current pursuit path retains target-directed aim filtering, cached geometry, per-cell reuse and the first-goal-distance cutoff.

### Captured cost

Marker rows are under `Core.Combat.Advance`. Each cell shows approximate window inclusive CPU total / exact exported maximum merged frame value / approximate inclusive GC allocation total. Window totals use the exporter averages multiplied by exported frame count. Millisecond totals have up to about 1 ms of rounding error and allocation totals are shown in MiB. Children are already included in their parents.

| Marker | 1124 · five enemies, turn 2 | 1122 · six enemies, turn 1 |
|---|---:|---:|
| `Core.Combat.Advance` | 268.4 ms / 92.146 ms / 29.05 MiB | 518.9 ms / 170.001 ms / 41.06 MiB |
| `Core.Combat.AssembleInput` | 3.3 ms / 0.464 ms / 0.22 MiB | 2.0 ms / 0.307 ms / 0.19 MiB |
| `Core.Combat.Calculate` | 255.2 ms / 90.869 ms / 27.28 MiB | 508.9 ms / 168.890 ms / 39.48 MiB |
| `Core.Combat.Publish` | 11.6 ms / 1.950 ms / 1.56 MiB | 8.0 ms / 1.431 ms / 1.38 MiB |
| `Core.Calc.EnemyTurn` | 250.2 ms / 90.656 ms / 26.83 MiB | 505.0 ms / 168.741 ms / 39.17 MiB |
| `Core.Calc.AttackRange` | 41.4 ms / 9.987 ms / 5.32 MiB | 97.4 ms / 35.707 ms / 8.02 MiB |
| `Core.Calc.EnemyAttackAim` | 106.0 ms / 67.109 ms / 8.00 MiB | 220.7 ms / 140.248 ms / 14.48 MiB |
| `Core.Calc.EnemyAimPreview` | 92.8 ms / 65.424 ms / 5.78 MiB | 196.8 ms / 134.574 ms / 11.62 MiB |
| `Core.Calc.EnemyPathfind` | 41.4 ms / 16.643 ms / 6.72 MiB | 79.5 ms / 30.538 ms / 10.43 MiB |
| `Core.Calc.EnemyCanAttackFrom` | 23.2 ms / 10.442 ms / 4.10 MiB | 43.7 ms / 16.391 ms / 6.42 MiB |

For the primary capture, main-thread frame CPU median / p95 / max was 11.601 / 24.335 / 108.371 ms. The max frame's PlayerLoop took 105.500 ms. Peak frame GC was 6.16 MiB at frame 995. For the supplemental capture, main-thread frame CPU median / p95 / max was 10.273 / 14.731 / 181.029 ms. The max frame's PlayerLoop took 178.086 ms. Peak frame GC was 10.03 MiB at frame 904. These frame percentiles include idle and presentation frames in the recorded windows. Per-decision percentiles are unknown.

### Bottleneck attribution

- **First use is a separate spike.** At stage 1124 frame 97, one `EnemyAimPreview` took **65.424 ms inclusive, 11.906 ms self, 516.13 KiB GC**. Its `Mono.JIT` child accounted for **52.919 ms** inclusive. At stage 1122 frame 750, four previews took **134.574 ms inclusive, 25.715 ms self, 1.87 MiB GC** and the merged `Mono.JIT` child accounted for **107.056 ms**. The 65.424 ms row is a single observed invocation. The 134.574 ms row is a group.
- **Repeated precise previews and full range work are expensive after first use.** At stage 1124 frame 995, nine previews took **16.347 ms inclusive, 13.613 ms self, 3.15 MiB GC**, and four full range queries took **9.427 ms inclusive, 7.935 ms self, 1.38 MiB GC**. The tree shows allocation work under both and no material JIT under those rows. At stage 1122 frame 904, ten previews took **28.465 ms and 4.52 MiB** and five range queries took **15.036 ms and 2.04 MiB**. At least one of the nine previews in frame 995 took about 1.8 ms or more by the aggregate lower bound. The exact warmed maximum remains unknown.
- **Pursuit is the other expensive branch.** Stage 1124 frame 720 spent **16.643 ms inclusive, 5.239 ms self, 2.29 MiB GC** in one `TryFind`. Its 104 position checks cost **10.442 ms combined**, including 1.47 MiB of allocation. That same Advance was 49.566 ms, of which calculation was 47.718 ms, assembly 0.409 ms and publication 1.434 ms. The repeated taunt-search case remains a source candidate. This trace does not attribute the observed pursuit work to duplicate taunt searches.
- **Use the cold partial trace separately.** Stage 1124 turn 1 frame 561 shows 143.245 ms in Advance and 139.695 ms in EnemyTurn. The partial window recorded no precise preview. The cold peak and window totals are useful first-use evidence. Side totals use the complete turn 2 window.

`CoreProfiling` is a Unity-free hook bound in `CoreProfilerMarkers`. `ProfilerFrameScan` reads the main thread in merged hierarchy mode. Inclusive times contain descendants and self times exclude them. The summary tree has a per-frame call count, so a row with one call supports an individual observation. The exporter combines multiple same-path invocations and truncates displayed trees. Full warmed maxima for position checks and previews, isolated selected-action and merge costs, and separate preparation/execution allocation remain unknown. The current code has no separate preparation stage or selected-action/merge marker. Single-invocation maxima must not be inferred from aggregate averages.

| Gate | Phase 0 disposition |
|---|---|
| G-baseline | Current-source scene evidence captured and frozen for the five-enemy reference and six-enemy supplement. Review pending. Exact replay of the historical incident is NotRun. |
| G-atom | Observed baseline over target. One first-use preview was 65.424 ms and included 52.919 ms of JIT. A later nine-preview group implies at least one invocation above 1 ms. Exact warmed maxima and the revised design's acceptance remain open. |
| G-frame | Unknown. There is no preparation slice in current production. Compare future 1 ms target, 1.5 ms p95, 2 ms p99 and any frame above 4 ms with dedicated evidence. |
| G-latency | Unknown. Neither the 150 ms normal p95 nor the 300 ms stress p95 has a current decision distribution. |
| G-allocation | Observed baseline over the proposed allocation scale. The five-enemy side's Advance scope totals about 29.05 MiB, and one current Advance has 6.16 MiB. These are inclusive current-path measurements. Separate future preparation/actual execution bytes and the standardized eight-enemy allocation gate remain unknown. |
| G-correctness | Open. Record current failing cases, compare future runs by identity and complete the required manual checks. |
| G-isolation | NotRun. Stale-publication and slicing equivalence are later behavior checks. |

Keep the original captures and exports together. The user clarified that five field units is the current standard and larger fights can occur. Carry that distinction into workload review. The implementation plan's later fixture matrix was outside this work.

## Improvements to evaluate before activating Utility AI

The captures prioritize precise previews, full range queries and pursuit. The proposed boundaries below preserve feature ownership. Any optimization benefit still needs measurement.

| Priority and source evidence | Smallest proposed change | Modification, understanding and verification scope |
|---|---|---|
| 1. `EnemyAiTurnService.Calculate` drains actor selection, full aim enumeration, pursuit and a real action inside one Advance. A time check outside any of those calls cannot interrupt it. | Measure indivisible operations first. Preserve ordering and fallback in a resumable Core decision session with bounded units. Keep frame policy at the Systems/Flow boundary and keep actual execution on the existing path. | Before, changing the decision loop requires understanding execution, rejection and phase settlement in the same method. After, preference changes should need the decision/assessment contracts and their tests. Scheduling still requires Core parity, service freshness/publication and interactive Flow/abandonment/terminal checks. |
| 2. `EnemyAiTurnService.Calculate` calls full `CombatAttackQueries.Range` for every scanned actor. It materializes basic/extended range, target lists, blocked contacts and finishing-shot display facts, while the policy consumes success, actor and aims. `EnemyAiPolicy.TrySelectAttackAim` then calculates a precise `Preview` per eligible aim and aggregates damage from transition events. | Measure Range, candidate counts, precise preview and scoring separately. Provide a narrow resumable enumeration over shared admission and geometry. Use shared precise assessment for a bounded shortlist only when Utility policy is active. Preserve the public range payload and old aim order for parity. | The AI should depend on the shared attack query contract and coverage facts. Attack owners keep eligibility, wall, pierce, splash, reaction and effect rules. Verify old range/aim parity, affected attack and trait rules, preview vs actual divergence and max cost per preview. A single preview can exceed a slice on its own. |
| 3. `EnemyMovementPathfinder.TryFind` can exhaust the reachable region when the target is unreachable. A popped cell can still drain `AttackReach.From` and `AimsThatCanHit`, including list creation, ordering and multiple delivery checks. `EnemyAiPolicy.ReachableTaunter` discards the path. The selected taunter may be searched in attack admission, pursuit selection and path execution within one decision. | Measure reachable/unreachable searches and repeated taunt work first. Preserve current prefiltering and goal-layer cutoff. Retain search and aim cursors and reuse an already computed route within the same immutable decision if the duplicate work is material. | The pursuit owner needs movement admission, visible terrain, stable traversal and attack delivery reach. AI consumes the result and keeps taunt order. Verify the 108 reference cases, hidden terrain/rejection, movement prefix, tie order and forced taunt. Recheck walls, splash and pierce when shared query semantics change. |
| 4. `CombatInputAssembler.Assemble` rebuilds character/owned/soul/artifact facts per Advance. `CombatCommandComposition.Project/Merge` materializes final facts and changes. Publication is a distinct validated transaction with extra terminal work. | Measure assembly, accepted calculation/composition and publish independently. In preparation, keep one immutable input plus a validity stamp tied to all assembler inputs and do one checked refresh before execution. Change composition or publication only if its isolated cost requires a separately reviewed adjustment. | Systems needs source identity/revision and existing service registration. Flow needs readiness, cancellation and publication postconditions. Verify invalidation beyond combat QueryRevision, one accepted result, action-use settlement, pending input and terminal/reward ordering. Preference tuning should not require reading Buffer writes or reward internals. |

This leaves one owner for attack/movement legality, one owner for authoritative state and one existing transaction for real publication. Scoring can consume feature-owned facts without copying rules or duplicating mutable combat state. A synchronous compatibility caller and a prepared production caller must reach the same deterministic selection policy. The new preparation contract must preserve FW-001/002 service registration, FW-004 state boundaries, FW-016 execution ordering and the existing one-shot child Job contract.

## Continuation ledger

- [x] Task 0.1 R0, source entry points, publication and Job postconditions, and test-ownership inventory.
- [x] Task 0.1 RC executed and failures retained as baseline evidence.
- [x] Task 0.1 RI executed and failures retained as baseline evidence.
- [ ] Future changes to policy-sensitive assertions await phase-specific review. No test changes were made.
- [x] Task 0.2 current-scene capture, original files and CSV export, cost attribution and gate comparison. Freeze the complete 1124 second side as the normal reference, with the cold partial and 1122 supplement kept separately.
- [ ] Matched historical-incident replay and exact warmed invocation maxima. The matched setup was unavailable and the exporter grouped repeat samples.
- [ ] Phase 0 evidence review. Any further measurement work requires the next phase authorization. Behavior and scheduling changes remain behind G-baseline review and the atom assessment.

### Exact failed case identities

Case names below omit the assembly namespace. Full NUnit messages, stack traces and all passing identities remain in the XML.

**core.xml**

| Case | Outcome and message |
|---|---|
| `BossNightLifecycleTests.DeadSummon_NeverReturns_AndFreesItsSlot` | Assertion · WrongPhase / Expected: True / But was:  False |
| `BossNightLifecycleTests.NightDayNight_PreservesIdentityAndStats_ReturnsAroundBossWithOpening` | Assertion · WrongPhase / Expected: True / But was:  False |
| `BossNightLifecycleTests.NightDayNight_ReserveReturnResetsRoarCounter` | Assertion · Expected: 0 / But was:  4 |
| `BossNightLifecycleTests.NoRoomOnReturn_StaysWaiting_AndCombatEntryContinues` | Assertion · WrongPhase / Expected: True / But was:  False |
| `BossNightLifecycleTests.PendingRoarAcrossDay_RoarsAfterReturnAtNextNight` | Assertion · Expected: <empty> / But was:  < 2 > |
| `BossNightLifecycleTests.SummonThenNightEnds_RecallsSameEntityWithRecord` | Assertion · Expected: EnemyWaiting / But was:  Field |
| `CombatCommonCommandTests.NightRecallCapacityFailure_LeavesFactsUnchanged` | Assertion · Expected: <System.InvalidOperationException> / But was:  null |
| `CombatItemLifetimeTests.OutgoingEnvironmentalDamage_ActivatesWhiskeyBeforeRoundExpiry` | Assertion · Expected: Deployment / But was:  Combat |
| `CombatItemLifetimeTests.RoundEnd_ClearsPlayerAndEnemyItems_AndRedeploymentDoesNotRegrant` | Assertion · Expected: True / But was:  False |
| `EightTurnNightTests.EighthResponseExpiresItemsAndSuppliesOnMain_BeforeDay` | Assertion · Expected: True / But was:  False |
| `EightTurnNightTests.EightPlayerEnds_WaitForEachEnemyResponse_ThenEnterDay(0)` | Assertion · Expected: Deployment / But was:  Combat |
| `EightTurnNightTests.EightPlayerEnds_WaitForEachEnemyResponse_ThenEnterDay(8)` | Assertion · Expected: Deployment / But was:  Combat |
| `HeldEnvironmentEffectTests.NightEnd_ExpiresSuppliesBeforeRecallIncludingMain(8,True)` | Assertion · Expected: 0 / But was:  7 |
| `HuntersMarkStateTests.TurnPreservesMark_NightRecallClearsOnlyReturnedRecipients(8,True)` | Assertion · Expected: Hand / But was:  Field |
| `IntimidationStatePreservationTests.EndTurnPreservesDebuff_NightRecallClearsOnlyReturnedRecipients(8,True)` | Assertion · Expected: Hand / But was:  Field |
| `MainDeckInitialPlacementTests.FirstNightEnd_KeepsManuallyPlacedMain_AndRecallsSameSub` | Assertion · Expected: Deployment / But was:  Combat |
| `PlayerTurnRuleTests.NightLengthIsEight` | Assertion · Expected: 8 / But was:  99 |
| `PositionTileModifierTests.NightRecallAndPlacement_ReconcileOnlyCurrentPositionContribution` | Error · System.InvalidOperationException : Sequence contains no matching element |
| `RailwayGimmickTests.LastEnemyEndChecksRailwayBeforeRecall_AndTerminalStopsNightCleanup(False)` | Assertion · Expected: 1 / But was:  0 |
| `RegenerationCadenceTests.LastEnemyEnd_HealsBeforeRecallAndKeepsPersistentMainProgress` | Assertion · Expected: EnemyWaiting / But was:  Field |
| `RegenerationTurnTests.EighthEnemyCompletion_DoesNotHealPersistentMain` | Assertion · Expected: Deployment / But was:  Combat |
| `TargetingAngleLifecycleTests.RecallRedeploy_PreservesFactionForward` | Assertion · Expected: Hand / But was:  Field |
| `TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True)` | Assertion · Expected: 1 / But was:  2 |
| `WeakGroundTurnTests.ControllerCompletion_DoesNotCollapseOrCountGround(Enemy,8)` | Assertion · Expected: Deployment / But was:  Combat |
| `WeaknessExposureLifecycleTests.EnemyEndAndNightRecall_UseAffectedClockThenClearBothReserves(8,True)` | Assertion · 1 / Expected: <empty> / But was:  < <ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects.WeaknessExposureStatus> > |

**integration.xml**

| Case | Outcome and message |
|---|---|
| `ArmorBreakerPublicationTests.ActualNightRecall_ClearsArmorReduction_WhileOrdinaryTurnsPreserveIt` | Assertion · Expected: Deployment / But was:  Combat |
| `CombatAdvanceServiceTests.TwoNights_ExpireSuppliesAfterEnemyResponseAndPreserveBadge` | Assertion · Expected: 1 / But was:  0 |
| `CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset",False)` | Error · System.TypeLoadException : Could not load type 'Cinemachine.CinemachineCameraOffset' from assembly 'Cinemachine, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. |
| `CombatStructureInspectionTests.UnitInfoPrefab_HidesMissingStructureFieldsAndRestoresCharacterFields` | Error · System.InvalidOperationException : [CombatUnitInfoView] UIId.CombatUnitInfo, 'UnitInfo(Clone)': 특성 설명 위치를 계산할 상위 Canvas가 필요합니다. |
| `ControllerTurnCountPublicationTests.RealProgressCountsEachSideOnce_PreservesAcrossDayAndNewNight` | Assertion · Expected: Deployment / But was:  Combat |
| `DataBaseAssetTests.CategoryDefaultBattle_ProductionAssignmentsAndGrowthPools` | Assertion · Expected: 1 / But was:  0 |
| `DataBaseAssetTests.CategoryDefaultBattle_ProductionMainAndStagePrepareSpecializedTraits(Rider,Leap)` | Error · System.Collections.Generic.KeyNotFoundException : Stage data not found. StageKey: 1101. |
| `DataBaseAssetTests.CategoryDefaultBattle_ProductionMainAndStagePrepareSpecializedTraits(Heavy,SteadfastWill)` | Error · System.Collections.Generic.KeyNotFoundException : Stage data not found. StageKey: 1101. |
| `DataBaseAssetTests.GrowthProduction_AkimboKeepsPrePresentationTestNativeTrait` | Assertion · Expected is <ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.TraitKey[1]>, actual is <System.Linq.EmptyPartition`1[ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.TraitKey]> / Values differ at index [0] |
| `DataBaseAssetTests.GrowthProduction_CannonKeepsPreviouslyLegalTraitWithoutReopeningIt(Taunt)` | Error · System.InvalidOperationException : 성장 이력이 검증된 단계·후보와 다릅니다: Unit=Unit_Ballista, Category=Cannon, Step=10, Trait=Taunt. |
| `DataBaseAssetTests.GrowthProduction_CannonKeepsPreviouslyLegalTraitWithoutReopeningIt(Acceleration)` | Error · System.InvalidOperationException : 성장 이력이 검증된 단계·후보와 다릅니다: Unit=Unit_Ballista, Category=Cannon, Step=10, Trait=Acceleration. |
| `DataBaseAssetTests.RunGoldFacilities_ActiveRestReferencesAndOrdinaryShopMatchPurchases` | Assertion · Expected is <System.Int32[2]>, actual is <System.Collections.ObjectModel.ReadOnlyCollection`1[System.Int32]> with 3 elements / Values differ at index [0] / Expected: 2 / But was:  1 |
| `DeckMainChangePolicyTests.EmptySlot_LockAppendsNoticeAndUnlockRestoresOriginalLabel` | Error · System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckMainChangePolicyTests.LockWhileOpen_ClearsChangeModeAndMovesFocusToInfo` | Error · System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckMainChangePolicyTests.MainDragAndDrop_ObeySameSetting(False)` | Error · System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckMainChangePolicyTests.MainDragAndDrop_ObeySameSetting(True)` | Error · System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckMainChangePolicyTests.MainOverride_DoesNotOverrideOverallEditRestriction` | Error · System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckMainChangePolicyTests.Open_AppliesSettingToMainButtonsAndClickPlacement(False)` | Error · System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckMainChangePolicyTests.Open_AppliesSettingToMainButtonsAndClickPlacement(True)` | Error · System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckMainChangePolicyTests.Open_RefreshesSerializedSettingAndClearsPreviousChangeMode` | Error · System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckSlotInfoPanelTests.Abilities_ManyToFewClearsHiddenDataAndReusesClones` | SetUp error, body not run · SetUp : System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckSlotInfoPanelTests.Abilities_ShowAllContentsInExactlyOneArea(0)` | SetUp error, body not run · SetUp : System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckSlotInfoPanelTests.Abilities_ShowAllContentsInExactlyOneArea(1)` | SetUp error, body not run · SetUp : System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckSlotInfoPanelTests.Abilities_ShowAllContentsInExactlyOneArea(2)` | SetUp error, body not run · SetUp : System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckSlotInfoPanelTests.Abilities_ShowAllContentsInExactlyOneArea(3)` | SetUp error, body not run · SetUp : System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckSlotInfoPanelTests.Abilities_ShowAllContentsInExactlyOneArea(4)` | SetUp error, body not run · SetUp : System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckSlotInfoPanelTests.Abilities_ShowAllContentsInExactlyOneArea(5)` | SetUp error, body not run · SetUp : System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckSlotInfoPanelTests.BodyHasNoCloseButton_AndMissingTraitsDoNotPartiallyRender` | SetUp error, body not run · SetUp : System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckSlotInfoPanelTests.Close_IgnoresRightClickDisabledAndHiddenInput` | SetUp error, body not run · SetUp : System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckSlotInfoPanelTests.MissingRequiredReference_ThrowsBeforeBinding` | SetUp error, body not run · SetUp : System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckSlotInfoPanelTests.SharedSelection_CloseClickAndSubmitUseUISystem(False)` | SetUp error, body not run · SetUp : System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckSlotInfoPanelTests.SharedSelection_CloseClickAndSubmitUseUISystem(True)` | SetUp error, body not run · SetUp : System.NullReferenceException : Object reference not set to an instance of an object |
| `DeckSlotInfoPanelTests.Stats_UseDedicatedFieldsAndPendingExperience` | SetUp error, body not run · SetUp : System.NullReferenceException : Object reference not set to an instance of an object |
| `HunterPersistenceTests.EnterCombat_LateHunterSelectionAndRngPublishTogether(False)` | Assertion · Expected: Deployment / But was:  Combat |
| `HunterPersistenceTests.EnterCombat_LateHunterSelectionAndRngPublishTogether(True)` | Assertion · Expected: Deployment / But was:  Combat |
| `HunterPersistenceTests.NightAndRecall_PreserveCapAndRunBonus_ButClearInactiveTarget` | Assertion · Expected: Deployment / But was:  Combat |
| `MainSpecializationBattleTests.NightRecallAndStageTeardownKeepOwnedClass(Rider)` | Assertion · Expected: Deployment / But was:  Combat |
| `MainSpecializationBattleTests.NightRecallAndStageTeardownKeepOwnedClass(Heavy)` | Assertion · Expected: Deployment / But was:  Combat |
| `MainSpecializationBattleTests.NightRecallAndStageTeardownKeepOwnedClass(Cannon)` | Assertion · Expected: Deployment / But was:  Combat |
| `RegenerationPublicationTests.NightRecall_PublishesZeroCadenceForReturnedSub_AndRetainsFieldMainProgress` | Assertion · Expected: Hand / But was:  Field |
| `RockfallPublicationTests.ReturnCleansesBeforeAnOpportunityWithoutHiddenState` | Error · System.InvalidOperationException : Sequence contains no elements |
| `RunGoldFacilityTests.ProductionRestVisit_EntersSelectsAndCompletes(2,58,60,5)` | Assertion · Expected is <System.Int32[2]>, actual is <System.Linq.Enumerable+SelectIListIterator`2[ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.RecoveryOptionSpec,System.Int32]> / Values differ at index [0] / Expected: 2 / But was:  1 |
| `RunGoldFacilityTests.ProductionRestVisit_EntersSelectsAndCompletes(4,40,70,10)` | Assertion · Expected is <System.Int32[2]>, actual is <System.Linq.Enumerable+SelectIListIterator`2[ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.RecoveryOptionSpec,System.Int32]> / Values differ at index [0] / Expected: 2 / But was:  1 |
| `TornadoRuntimeIntegrationTests.SameBattleReturnToDeploymentKeepsPreparedStateAndPlacementInactive` | Assertion · Expected: Deployment / But was:  Combat |
| `TraitActivationPublicationTests.EnterCombat_LateHunterResultRereadDoesNotDrawAgain` | Assertion · Expected: Deployment / But was:  Combat |
| `TraitGrowthStatSettlementTests.RankedDefault_RemainsInstanceLocalAcrossRecallAndClearsOnNewRun` | Assertion · Expected: Hand / But was:  Field |
| `TraitMovementAttackPublicationTests.Acceleration_SurvivesOrdinaryTurns_ButActualNightRecallClearsIt` | Assertion · Expected: Deployment / But was:  Combat |
