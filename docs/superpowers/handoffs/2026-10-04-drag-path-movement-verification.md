# Drag Path Movement Verification Record

This records the T01–T12 implementation snapshot before the Stage merge. For the current production View attachment, actual scene verification, green merged suite and local merge authorization, see the [Stage merge verification record](2026-10-04-stage-merge-verification.md). Historical scope restrictions and missing-scene statements below describe their recorded snapshot.

## Scope and baseline

- Initial authorized scope: T01; subsequent task-specific authorizations and evidence are recorded below. Current managed implementation/verification scope is T01-T12; production content/scene/entry-exit acceptance remains gated. No commit or push is authorized.
- Plan: [implementation plan](../plans/2026-10-04-drag-path-movement.md).
- Spec: [confirmed design](../specs/2026-10-04-drag-path-movement-design.md).
- HEAD: `7c9f724a3e20a3ff0c7c172175b34003d9a2c0ab`.
- Branch: `feature/Cheon/refactor/core-system`.
- Initial working tree: only the incoming plan and spec were untracked; runtime/test files were clean.
- Initial plan SHA-256: `1CC944DBB17151073E12BB15DFBF2A1ED6D29CAA5772828F02D5F7228E6FC59F`.
- Initial spec SHA-256: `65FBC4EE1D0E891BB75797673D172CC18F5305AE26C91DD40927B90C0CF03692`.
- Unity: `6000.0.73f1`, executable `D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe`.
- Evidence directory: `.superpowers/sdd/2026-10-04-drag-path-movement/` (git ignored).
- Initial dependency snapshot: Jobs and View Factory were unconnected at baseline. Their managed wiring is now verified in T10-T12; live bootstrap, authored environment, production presenters/scene and scene-exit integration remain gated.

Ruling: Execute T01 in the current feature checkout, whose runtime/test tree is clean. The user authorized this task in the shared workspace; no branch switch, commit, or additional worktree is needed for this additive slice.

Ruling: Stop after T01. The user's task scope overrides the execution skill's default of continuing through the entire plan.

## Baseline evidence

Command: `Invoke-MovementTests -RunName baseline-retry -Full`, using the plan's helper and existing Core, Framework, and CombatViewBoundary assemblies.

The first sandboxed invocation crashed before tests because Unity could not open its Windows user cache. The retry with cache access produced `baseline-retry.xml`, `baseline-retry.log`, and exit code `2`.

Baseline: **1,845 discovered, 1,840 passed, 5 failed, 0 skipped**. All five failures report `System.InvalidOperationException : [CombatStageDetail] Combat stage requires Economy.`:

- `ProjectL.Tests.Framework.ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `ProjectL.Tests.Framework.DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `ProjectL.Tests.Framework.DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `ProjectL.Tests.Framework.DataBaseAssetTests.EveryStage_HasNodeVisual`
- `ProjectL.Tests.Framework.DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

Ruling: Continue the authorized additive contract task with these recorded content failures. Do not fabricate Economy assets or report a fully green suite.

## T01 contract preflight

- T01 -> T02: initial context can use existing public actor values; ledger/modifier/environment fields are appended when their types exist.
- T01 -> T03: path/result contracts carry route facts but do not enable route execution yet; endpoint behavior remains until migration.
- T01 -> T07: request correlation and battle-instance production remain deferred; the initial context cannot authorize live execution.
- T01 -> T08/T11: automatic composition and prepared result publication must retain path/return payloads and all ordered events regardless of final intent.
- T01 -> T06: projected helper inputs retain main, soul counts, field, and changed character facts without restoring original values.

## T01 evidence

**T01 complete.** The additive contract slice compiles and retains the current live endpoint movement behavior. At this checkpoint, no later task had started.

Implemented immutable route, entered-prefix/stop, Return, and initial public actor-context contracts. Request, preview, transition, result, completion copy, attack-query copy, automatic composition, and prepared Buffer publication preserve the added metadata. Movement result data remains independent of final occupancy, and rejected results cannot carry executed movement/Return payloads.

The context currently retains identity, exact intent/path or Return target, and explicit selected-actor expected values. It stores no entire combat state, other-character list, hidden source payload, or presentation bookkeeping. T02 adds ledger/modifier/environment values; T07 adds the complete public comparison contract, turn identity, Capture/Matches, and the Buffer-owned battle-instance producer. It is not used as a live execution token in T01.

| Run | Evidence files (in the evidence directory) | Observed result |
|---|---|---|
| Initial contract RED | `t01-red.xml`, `.log`, `.exit-code.txt` | 2 discovered, 2 failed: required types absent. Tests compiled; this was a runtime assertion failure. |
| Composition RED | `t01-transport-red.xml`, `.log`, `.exit-code.txt` | 3 discovered, 2 passed, 1 failed: transition lacked PathMovement. |
| Publication fixture check | `t01-publication-red.xml`, `.log`, `.exit-code.txt` | 25 discovered, 24 passed; fixture AP was zero and failed before publication. This is not behavioral RED evidence. |
| Publication RED | `t01-publication-red-retry.xml`, `.log`, `.exit-code.txt` | 1 failed: after preparing resources through existing EndTurn calculation, published PathMovement was null. |
| Focused GREEN | `t01-green.xml`, `.log`, `.exit-code.txt` | 44/44 passed, zero skips, exit 0. Filter: MovementContractTests, CombatResultContractTests, CombatCommandPublicationTests. |
| Existing input-helper RED | `t01-copy-red.xml`, `.log`, `.exit-code.txt` | 1 failed: CombatCommandTestData.With dropped the path. |
| Final full suite | `t01-final.xml`, `.log`, `.exit-code.txt` | 1,855 discovered, 1,850 passed, 5 baseline failures, zero skips, exit 2. All 10 new tests and all 44 focused cases pass in this final run. |

Every run used the plan's Invoke-MovementTests helper with a fresh RunName; focused filters are named above or by the failed test. The final run used `Invoke-MovementTests -RunName t01-final -Full`. Failure names and messages match the baseline exactly. Compilation succeeded without errors. Recompiling the existing test helper reported CS8632 at CombatCommandTestData.cs:85 for its pre-existing nullable-reference annotation; that annotation was not introduced by T01. Full-suite green remains gated by the existing missing Economy content listed above.

Ruling: Use temporary reflection only for compiling the first tests before the public types/members exist. Once they exist, replace those tests with direct typed calls; no reflection scaffolding remains in MovementContractTests.

Ruling: Append `ReturnToCheckpoint = 9` in T01 so the initial context can identify a Return intent without depending on T03's missing enum. Actual Return calculation remains T03 work.

Ruling: Include the existing CombatStateBuffer result-construction line and CombatAttackQueries input copier in T01. Otherwise newly introduced payloads would be silently lost before T07 migration.

Ruling: Preserve the path/context and existing tail arguments in CombatCommandTestData.With. Its failing copy test demonstrated an actual transport loss; no gameplay calculation was changed.

Review: checked immutable collection exposure, result prefix/distance validation, forced final-stop preservation, same-anchor Return payload, rejected-result invariants, ordered composed events under Attack intent, completion-copy preservation, and the real prepare/install publication path. Existing Core.Contracts -> Domain/Application -> Systems dependency direction and assembly references are unchanged. Unity generated .meta files for all seven new C# files; their GUIDs are valid and distinct. No scenes or content definitions were authored. Unity's FMOD scanner changed only the existing catalog's lastScanTime; that generated timestamp was restored after testing. `git diff --check` passed.

No commit or push was performed. At the T01 handoff, the next implementation task was T02; production mine/modifier content, complete preview freshness, route calculation, View/Jobs, and scene-lifetime integration remained their declared later tasks/dependencies.

## T02 evidence

**T02 complete.** The immutable state/Buffer slice compiles. At that checkpoint, T03 had not started. The user authorized essential tests, necessary RED only, and source inspection for indirectly covered copy paths; this overrides the plan's broader default verification workflow.

Character facts and Character Buffer views now retain pending distance, the independent attack-refund basis, and a non-null position contribution. Expected-value comparison includes all three; position contribution equality includes source cell/IDs and signed values. Stored maximum-HP validation includes the independent signed position contribution without folding it into temporary Artifact/soul fields. Positive effective HP still uses the existing validated health snapshot and main-state consistency checks. T02 stores proposals; it does not resolve destinations, normalize HP, or implement refunds.

The typed destination source travels from BattleInitialization through BattlePreparationCalculator/CombatPreparation into CombatCommandState and the existing Combat Buffer. Deep expected-state comparison includes active/visibility/typed source values; placement validation rejects source cells outside the board. Preparation and publication remain the existing combined prepare/install operation. The real preparation/publication fixture proves source changes survive installation and stale active/visibility state prevents health/result publication. No environment store is installed independently.

MovementStepInput exposes only entered cell and current mover identity, ownership, health, stats, and location. It does not retain the supplied CharacterCombatFacts, its ledger, a command, or a projection. Concrete selected-target/profile/effect inputs can be added only when a T05 rule actually consumes them. MovementStepEffects contains immutable target/amount or target/direction/distance requests and at most one expected/final source pair with unchanged identity. It cannot return CharacterChanges, command state, events, barrier claims, or a stop reason. The coordinator must validate that pair against the selected latest source in T05. MovementEffectCause remains separate resolved evidence.

MovementPreviewContext retains the added actor values and only observer-visible environment copies. WithKnownTo copies concrete facts while VisibleTo removes hidden sources and other observers' knowledge lists. Filtering leaves authoritative source facts unchanged. Complete relevant-cell capture, matching, battle-instance production, and actual execution read-set validation remain T07; this additive context is still not a live execution precondition.

| Run | Evidence files in the evidence directory | Observed result |
|---|---|---|
| Initial compiler check | `t02-ledger-red.log`, `.exit-code.txt` | Missing Results namespace in the new test fixture; no test XML. Fixed; not behavioral RED evidence. |
| Essential RED | `t02-ledger-red-retry.xml`, `.log`, `.exit-code.txt` | 3 discovered, 3 failed, zero skips, editor exit 2. Each expected InvalidOperationException but publication did not throw when distance, refund basis, or modifier source changed independently. |
| Focused GREEN | `t02-green.xml`, `.log`, `.exit-code.txt` | 24/24 passed, zero skips, editor exit 0; includes all 11 T02 cases. Filter: MovementPublicationTests, MovementContractTests, CombatStateBufferTests. |

All Unity runs used 6000.0.73f1 through the approved AppData-access path. The sandbox CurlRequestCache.db limitation is environmental; no Unity upgrade, cache deletion, substitute harness, or production content repair was required. No full-suite rerun was performed in T02. The five missing-Economy baseline failures last observed in T01 remain an unverified content gate, not a claim that the full suite now passes. Existing CS0105/CS8632 warnings are outside this slice.

Source audit: CharacterBuffer.BuildFacts, SoulEffectCalculator, CharacterSettlementCalculator, CombatCommandCalculator, CombatMovementCalculator, CombatPlacementCalculator, CombatTurnCalculator, and shared Core/Framework test builders preserve added character fields. Command/turn state reconstruction preserves Environment. ArtifactActionProjection passes the supplied final facts without reconstruction. BattleCharacterCalculator and initial fixture factories legitimately start neutral. ReadInput already forwards the installed battle.CommandState; it needs no new data source or constructor argument. Its forwarding was inspected rather than given a redundant integration test.

Remaining contracts: T03 supplies billing, Return, and checkpoint sealing; T06 supplies the separate attack refund behavior. Current legacy movement/turn copies merely preserve new ledger values and do not implement those policies. T04 must resolve actual destination contributions, integrate signed attack into the existing stats contract/reconstruction, preserve position contributions through soul/persistent-stat recalculation, and perform approved HP normalization. Nonzero signed attack application is not proven by T02's storage tests. T05 composes/resolves entry rules; no production mine parameters or overlapping-source combination policy have been invented. Production environment authoring remains pending. View/Flow/Jobs remain their planned owners and tasks.

Review: existing Buffer ownership, atomic preparation, defensive copies, source-scoped rule outputs, hidden-source filtering, and Core assembly dependency direction are retained. Unity generated all six new C# .meta files; all six GUIDs are valid and distinct. No assembly references, Framework infrastructure, scenes, or balance assets were changed. The generated FMOD catalog timestamp was restored. `git diff --check` passed. No commit or push was performed.

## T03 evidence

**T03 complete.** Effect-free route and explicit Return calculations are available through CombatCommandCalculator. T04 has not started. The established focused-test preference remains in force.

The route validator checks origin, board bounds, single-cell actor location, orthogonal edges, no revisits, current occupancy, full distance/AP budget, and ledger arithmetic before any execution result is produced. Preview reports the valid prefix of an invalid draft, but execution rejects that whole draft with no entered events/payload. Validation uses long arithmetic for quoted costs and rejects pending/refund-counter overflow before checked int bookkeeping.

MovementProjection is private to one command. It retains the original expected input separately from the current character facts and ordered entered-cell events, and constructs one immutable final transition. Unchanged environment, souls, and main facts remain in the original immutable input; no second combat owner or generic mutation framework was added. T04/T05 will add actual projected changes and interruption/barrier handling when those calculations exist.

Each entered step pays the current fixed quoted MoveCost and increments distance even at zero cost. Ordinary reverse movement also pays. Across the approved four-edge detour and two-edge continuation, the pending ledger reaches six steps/cost and AP 14. Return refunds those recorded values, clears pending distance/cost, preserves committed distance and movement-success state, and emits one Returned event without traversal/acquisition. A paid loop across separate drags can return from/to the same anchor and still refund. Empty, enemy, outside-board, and occupied-checkpoint returns reject. Refunds exceeding current resource capacity reject rather than overflow or silently lose paid resources.

SealPendingMovement commits actual pending distance without an AP refund, renews the checkpoint at the current stop, and preserves the attack-refund basis. New enemy route inputs seal immediately. A sealed four-step interval plus two later pending steps returns only the latter, preserving committed distance four and attack-refund basis four. Charges and checkpoint refunds maintain the independent basis now; the category's attack adapter and attack/end-turn resets remain T06.

Preview and execution call the same route projection and existing movement-success engine once per moved command. The trait test proves one effect event, preserved Focus/MoveAttackDiscount state after Return, and unchanged original trait facts. The detour Preview matches actual distance/cost and leaves original resources untouched. Facing uses the actual final entered edge, which is identical in Preview/execution. Range uses shared occupancy/budget checks and BFS to quote minimum reachable step cost; its legacy DistanceFromCheckpoint field remains geometric displacement, and the supplied path is never replaced or executed from that hint.

The Framework test calculates a three-step detour from installed main facts, publishes one path result/sequence with all three ordered movement events, then publishes one Return result/sequence. Character resources, occupancy, counters, and HP reconcile through the existing Buffer boundary. This is direct Core/Buffer evidence; it is not evidence that View inputs, Jobs, or playback have migrated.

| Run | Evidence files in the evidence directory | Observed result |
|---|---|---|
| Essential RED | `t03-red.xml`, `.log`, `.exit-code.txt` | 6 discovered, 6 failed, zero skips, editor exit 2. Five behavioral failures: detour AP 18 instead of 16; pending distance zero; reverse loop refunded automatically; intermediate blocker accepted; Return unsupported. The sixth was a zero-cost fixture violating authored UnitSpec validation. |
| Zero-cost RED | `t03-zero-cost-red.xml`, `.log`, `.exit-code.txt` | 1 failed, zero skips, editor exit 2: zero runtime cost moved, but Return was UnsupportedCommand. |
| Compiler check | `t03-green.log`, `.exit-code.txt` | Missing Snapshots namespace in MovementProjection; no test XML. Fixed; not behavioral RED evidence. |
| Focused check | `t03-focused.xml`, `.log`, `.exit-code.txt` | 31/32 passed, zero skips, editor exit 2. The enemy test reached Return construction, where the test helper incorrectly incremented Advance's mandatory zero command number. |
| Focused final | `t03-final.xml`, `.log`, `.exit-code.txt` | 32/32 passed, zero skips, editor exit 0; all 23 new T03 cases passed. |

Final command:

```powershell
Invoke-MovementTests -RunName t03-final -Filter 'ProjectL.Tests.Core.MovementRouteTests;ProjectL.Tests.Core.CheckpointReturnTests;ProjectL.Tests.Framework.MovementPublicationTests;ProjectL.Tests.Core.CombatCommonCommandTests.Move_ChargesCheckpointDifference_AndPreviewDoesNotMutate;ProjectL.Tests.Core.CombatCommonCommandTests.Move_RetargetCloserToCheckpoint_RefundsOnlyPendingCost'
```

Ruling: keep authored UnitSpec's positive cost restriction unchanged; only the runtime test snapshot has zero MoveCost. MovementTestData uses a positive profile definition for that case because movement billing reads the current runtime stats.

Ruling: preserve Advance command number zero in new route/Return test helpers, per the existing CombatCommandInput contract; player command numbers increment. No runtime numbering policy changed.

Ruling: maintain charge/Return bookkeeping for AttackRefundEligibleMoveCost in T03, as required to keep the confirmed separate ledger coherent; T06 owns the existing attack-category adapter and successful-attack/end-turn clearing. Checkpoint and attack refunds have not been combined.

Ruling: keep the projection minimal for effect-free T03 rather than adding unused mutable environment/soul/outcome APIs. Original state still reaches the transition; T05 must extend it with validated sequential effect merges and sticky actual-effect evidence.

All tests ran in real Unity 6000.0.73f1 with approved AppData access. No full-suite rerun, independent harness, cache deletion, or Unity upgrade was used. The existing CS8632 test-helper warning remains. Full-suite content failures last observed in T01 have not been rechecked.

Review: route results contain one immutable aggregate and entered-cell events; rejected routes cannot execute their valid prefix. CopyMovement preserves independent temporary/position contributions and identity. No Buffer, Framework scheduler, Job, scene, assembly reference, or production content provider was added. Unity generated all five new C# .meta files with valid distinct GUIDs. The FMOD-generated timestamp was restored; git diff --check passed. Prior T01/T02 changes remain uncommitted. No commit or push was performed.

Remaining gates: T04 actual destination modifier resolution/removal/normalization, T05 entry effects/interruption/death/outcome and full projected merges, T06 soul-arrival and legacy attack/turn settlement/category wiring, T07 public freshness/actual read dependencies and View/service migration, T08 final AI producer migration/API removal, and T09-T12 View/Flow/Job/content integration. The old endpoint Calculate/Preview branch is still explicit temporary compatibility and retains its old billing. Route Range requires a coherent route ledger; do not treat an old endpoint displacement ledger as a valid new route ledger. This slice does not claim the complete movement feature is playable.

## T04 execution record

Authorized scope: T04 only for this continuation; earlier T01-T03 working changes are preserved. No later task, full-suite rerun, commit, or push was performed.

Implemented the existing typed destination-source data path with PositionTileModifierCalculator.ResolveAt/Replace. Admitted routes remove the prior contribution before their first entered-cell event, then resolve once at the final stop. Preview uses observer-filtered environment facts; Return reconciles the checkpoint from current source facts without traversal, soul acquisition, or movement-success evaluation. Maximum-HP growth does not heal; removing/reducing a contribution caps current HP without restoring it on later Return. HealthNormalized is appended to the existing event contract and never dispatched as damage. Main current-HP caps propose the matching persistent current-HP change within the same CharacterChanges publication.

PositionAttackBonus is separate from base/Artifact/soul/temporary attack; existing stat copies and Character Buffer expected-value comparison/validation preserve it. SoulGrowthMath takes the separate signed position maximum-HP contribution; soul collection and persistent stat recalculation retain provenance and effective stats. ArtifactAttackPowerEvaluator includes the position contribution while preserving existing Artifact evaluation. ArtifactActionProjection.ProjectCollection already passes through the calculated final actor and needed no edit.

Tests compose typed destination facts, rather than only supplying a manually constructed modifier. The existing direct Buffer route/Return case now installs those facts through matching preparation and command-state inputs, exercises positive and negative contributions, checks actual actor/source publication, and proves capped Main HP stays capped after Return. No fixture framework or additional store was added.

| Run | Observed result |
|---|---|
| t04-red | Fixture property-name compiler error; no XML, not behavioral RED evidence. |
| t04-behavior-red | One discovered case failed because maximum HP remained 10 instead of 15; zero skips, editor exit 2. |
| t04-green | NUnit collection-assert overload compiler error; no XML. |
| t04-focused-green | 62/64 passed; two Buffer fixtures omitted the matching CombatPreparation environment and violated the existing preparation/state consistency contract. Fixed the fixture, not that contract. |
| t04-final | Preview property-name compiler error; no XML. |
| t04-final-verified | 67/67 passed, zero skips, editor exit 0. |

Final command (actual Unity 6000.0.73f1, approved AppData access):

```powershell
Invoke-MovementTests -RunName t04-final-verified -Filter 'ProjectL.Tests.Core.PositionTileModifierTests;ProjectL.Tests.Core.ArtifactSoulBufferCalculationTests;ProjectL.Tests.Core.MovementRouteTests;ProjectL.Tests.Core.CheckpointReturnTests;ProjectL.Tests.Framework.MovementPublicationTests'
```

Fixture totals: PositionTileModifierTests 12/12; ArtifactSoulBufferCalculationTests 24/24; MovementRouteTests 14/14; CheckpointReturnTests 8/8; MovementPublicationTests 9/9. Fifteen cases/variants are new in this task. Existing forced-arrival soul accounting is covered by the focused soul fixture; the forced-movement algorithm is unchanged. The existing test-helper CS8632 warning remains. Previously recorded full-suite Economy authoring failures were not rerun.

Review/rulings: keep effective attack nonnegative and maximum HP positive, with checked arithmetic; signed contributions do not invent a clamp or stacking policy. Invalid values and overlapping active destination sources without an authored combination policy fail before publication. Unity generated the two new .meta files. The generated FMOD scan timestamp was restored; git diff --check passed, and no Unity process remains. Prior task edits remain intact.

Remaining integration: T05 connects ordered entry effects, sticky barriers, surviving interruption, terminal eligibility, and sequential projected merges to this same destination resolver. T06 connects final soul-arrival order and attack/turn settlement. T07 retains the registered-service Preview/freshness case; the direct Buffer source/publication test is already present and need not be duplicated. Production modifier content and combination policy remain gated, and View-owned input/Flow/Job/presentation work remains with its declared later tasks. The T04 separate-health-to-14/15 test isolates normalization; it does not claim a real gameplay heal permits Return or implement T05's healing barrier. No production content, scene, assembly reference, Framework infrastructure, or View scheduler was changed.

## T05 execution record

Authorized scope: T05 only for this continuation. Prior T01-T04 changes are preserved; no commit/push, later-task execution, scene/asset content edit, assembly reference, or Framework infrastructure change was performed.

Implemented ordered entry coordination in MovementEnvironmentCalculator with command-local MovementProjection changes. Hazard and Recovery evaluate current narrow mover/target/source facts separately, in stable source priority/ID order; existing ThornFields run through their direct typed adapter without duplicating a source. Damage uses existing Hurt, healing uses existing CharacterSettlementCalculator.SetHealth, forced displacement uses existing geometry/thorn traversal/forced arrival, and death/outcome/terminal-health handling reuses existing calculators and Finish. Final prepared publication keeps original expected facts and one aggregate route result with ordered events. Internal death bookkeeping prevents duplicate soul spawning at final Finish.

Barrier evidence is sticky after actual per-operation HP, source gameplay-state, method or external forced-location changes, never final net HP or arbitrary rule labels. Positive healing/damage to another entity, consumption with zero damage, and genuine source changes seal the whole pending interval; no-op healing, normalization, ordinary movement and movement-success bookkeeping do not. Successful forced movement stops the selected route, retains its paid prefix, and marks an already-resolved arrival locally. Dead/terminal actors receive no recovery, suffix or destination application; a surviving interruption resolves its actual final destination once. Current projected passability/resources are rechecked before charging the next selected edge.

Rule output remains typed requests plus one selected source change; it cannot process a combat command or replace a whole state. Phase-specific inputs add only copied target ID/owner/HP/occupancy and current same-cell sources. The existing MoveMethod field has a narrow expected/final request; this is not a generic stat mutation framework. A helper expected character must be the current immutable instance, and a source expected value must match its selected current typed value. Metadata/identity changes are rejected. Both the sequential Core case and the existing direct Buffer route variant verify that earlier target/source changes survive and publication expectations still refer to the original facts.

The production Factory explicitly composes an immutable thorn rule list; the processing service copies the supplied list once and uses it for execution/Preview/Advance. Calculator and AI/AI-policy preview dependencies are mandatory with no omitted/null default supported rule set. Mechanical migrations of existing direct calculator/service fixtures were required to keep this task compiling. Missing production mine/Object/modifier values and their mitigation/visibility/combination policy remain content gates; no balancing values were invented.

| Run | Observed result |
|---|---|
| t05-red | 2/2 failed, zero skips, editor exit 2: thorn entry left HP 10 rather than 8; the mine did not seal prior safe movement. |
| t05-first-green | Missing AI-policy preview rule dependency and Domain.Combat import caused compiler errors; no XML. Not behavioral RED evidence. |
| t05-core-check | Two remaining direct test caller dependencies caused compiler errors; no XML. Fixed mechanically. |
| t05-behavior-check | 37/37 passed, zero skips, editor exit 0. |
| t05-green | 94/94 passed, zero skips, editor exit 0. |
| t05-final | 95/95 passed, zero skips, editor exit 0, after adding projected-occupancy suffix rejection and using the existing health settlement helper. |

Final command (real Unity 6000.0.73f1, approved AppData access):

```powershell
Invoke-MovementTests -RunName t05-final -Filter 'ProjectL.Tests.Core.MovementEnvironmentTests;ProjectL.Tests.Core.ThornFieldDomainTests;ProjectL.Tests.Core.KnockbackTraitTests;ProjectL.Tests.Core.MovementRouteTests;ProjectL.Tests.Core.CheckpointReturnTests;ProjectL.Tests.Core.PositionTileModifierTests;ProjectL.Tests.Framework.MovementPublicationTests;ProjectL.Tests.Core.StageEnemyAiTurnTests;ProjectL.Tests.Framework.CombatPreviewPublicationTests;ProjectL.Tests.Framework.CombatCommandPublicationTests'
```

Focused coverage: MovementEnvironmentTests 15 cases; existing thorn/knockback regressions; route/Return/destination regressions; AI/Preview and existing prepared publication/terminal completion integration. New cases cover prior safe-drag sealing, damage then healing back to the initial HP, other-entity damage, zero-damage consumption, no-op healing, lethal hazard before recovery, living interruption, forced relocation, projected occupancy blocking an uncharged suffix, terminal stop, headquarters/main-death precedence, one death soul spawn, and hidden-only source behavior. The direct Buffer fixture adds one variant with source A damage/consumption and source B healing/consumption, one published result, original expectations and both final source states retained. No full-suite rerun or new test framework was used. Existing CS8632 warnings remain; previously recorded Economy-content baseline failures were not rechecked.

Remaining declared integration: T06 connects normal final soul collection and removes deferred voluntary attack/turn effects while preserving separate attack-refund/reset behavior. T07 connects public freshness and registered route/Return inputs; AI's endpoint branch remains temporary until its declared T08 producer/API migration. View owns input/schema coordination and Flow/Plan/Jobs/presentation as before. Source content and any unsupported authored effect policy remain explicit gates. The focused completion tests reuse the existing processing-service completion path; this record does not claim live View migration or authored mines are playable.

## T06 implementation evidence (2026-10-04)

Scope: T06 only, in the existing uncommitted feature checkout. Core now resolves normal and surviving interrupted soul arrival at the actual final stop, once, before destination-only modifiers. The same command-local arrival helper is shared with forced displacement; `SoulArrivalKind` explicitly distinguishes settled voluntary, resolved movement and forced callers. Main persistent HP is synchronized to projected HP before collection. Actual collection seals the pending checkpoint interval; safe empty arrivals retain their pending ledger. Return continues to reconcile only position sources, without collection or entry effects.

Attack and turn-end no longer reconstruct voluntary thorn traversal or acquire arrival souls. An admitted successful attack seals actual pending distance and uses the original independent AttackRefundEligibleMoveCost for the existing capped/once-per-turn category rule, then clears that interval. Rejected attacks preserve all resources and both ledgers. Turn end seals the pending ledger, clears the separate refund basis and retains field/soul countdowns, effects and night scheduling; turn start resets both movement ledgers and the refund basis. A living actor whose movement method became None can still end its turn. Existing Focus/MoveAttackDiscount Return behavior remains covered by the existing route fixture. Input-mode cancellation is View-owned and emits no Core movement settlement; T09 still owns its coordination.

The source contracts reused are the existing Collect/Finish, health and outcome helpers, SoulActionChanges/CharacterChanges, and existing prepared Buffer publication. No Framework infrastructure, scenes, assets, assembly references or new gameplay state owner were introduced. No runtime change was needed in CheckpointReturnCalculator: its current recorded-refund and position-only path already satisfies the T06 contract.

The temporary endpoint branch is still required by the declared T07 player/service and T08 AI caller migrations. Removing deferred effects from Attack/EndTurn would otherwise drop its live thorn/soul settlement. Its existing endpoint displacement is now settled during Move, using existing helpers, and represented in its pending-distance/refund fields; it retains the explicitly temporary geometric billing/retarget contract. It does not reconstruct a MovementPath or become a fallback for route commands. Its ordinary typed rule pipeline and public route Preview belong to the new route entry, with full live integration still pending T07/T08. T08 must remove this branch after all producers migrate.

| Run | Observed result |
|---|---|
| t06-red-arrival-refund | 4 cases: 3 failed / 1 passed, zero skips, editor exit 2. Normal final collection and living blocked arrival consumed no souls; Rider refunded 0 rather than the preserved basis 4. The existing forced-arrival path already collected once. |
| t06-green-initial | 160 cases: 149 passed / 11 failed, zero skips, editor exit 2. New arrival/refund behavior passed; failures exposed superseded deferred-settlement fixtures, missing new ledger values in fixtures, and an assertion that mistakenly treated the retained soul-countdown event as collection. |
| t06-green-migrated | 184/184 passed, zero skips, editor exit 0. Real Unity 6000.0.73f1 with approved AppData access. All project assemblies compiled; no full-suite rerun. |

Final command:

```powershell
Invoke-MovementTests -RunName t06-green-migrated -Filter 'ProjectL.Tests.Core.MovementEnvironmentTests;ProjectL.Tests.Core.CombatCommonCommandTests;ProjectL.Tests.Core.CombatCommandEffectTests;ProjectL.Tests.Core.ArtifactSoulBufferCalculationTests;ProjectL.Tests.Core.RiderManeuverRefundTests;ProjectL.Tests.Core.MovementRouteTests;ProjectL.Tests.Framework.MovementPublicationTests'
```

Fixture totals: MovementEnvironment 21; CombatCommonCommand 68; CombatCommandEffect 37; ArtifactSoulBufferCalculation 24; RiderManeuverRefund 10; MovementRoute 14; existing MovementPublication 10. Six added environment cases reuse the existing fixture/rule composition. They verify intermediate souls remain, actual final souls are collected before max-HP tile growth without growth healing, blocked/forced stops collect once, attack/turn end cannot replay a newly present entry hazard or acquire a newly present arrival soul, and barrier basis 4 -> safe basis 6 -> Return basis 4 -> successful attack refund 4/reset 0. The same scenario checks invalid attack preserves pending cost 2/basis 6 and Return never acquires a checkpoint soul. Existing main-arrival variants cover actual damage before collection and persistent HP synchronization (normal 99/101 HP, forced 101/101 HP; persistent base maximum remains 100).

Superseded assertions and preserved coverage:

- LegalLethalPendingTraversal became LethalRoute: death is resolved in the paid Move command, with no Attack payload/cost/countdown; subsequent attack rejects without changes. AP is 4 after the actual one-cell charge, rather than the old artificially unpaid pending fixture's 5.
- EndTurn_LethalTraversalAtNightEnd became LethalRoute_RejectsEndTurnWithoutAdvancingNight: a dead mover cannot end its turn to advance the night. Immediate participant removal, unchanged night time and an unchanged rejection result remain checked.
- AutomaticEnemy_LethalPreviewSkipsAttack_AndSettlesDeathAtTurnEnd became AutomaticEnemy_LethalRouteSkipsAttackAndTurnEnd, using an explicit Advance route and proving immediate death with no attack/turn-end event. Full AI route-production/composition remains T08; this does not claim that future producer is implemented.
- RetargetedMovement_ConsumesOnlyFinalDiagonalPath became SuccessiveRoutes_ConsumeOnlyEnteredThorns_BeforeAttack. Two actual routes consume 7+5 damage immediately and preserve the unentered 9-damage field; the following attack does not repeat either effect. Total paid distance remains 3.
- TraversalOverflow became ExtremeThornDamage: per-cell lethal damage is capped and stops the prefix before the later field, so summing an unwalked suffix to overflow is no longer a valid route assertion. Original health/resources/fields remain immutable. PendingDistanceOverflow still proves checked seal overflow fails without changing source facts, with an explicit pending distance/refund basis supplied.
- Rider fixtures now execute a paid one-cell arrival before attacking: first final AP changes from the artificial 4 to actual 3; after a second paid move/attack the final AP changes from 1 to 0. Refund amount, cap/used flag, second-attack zero refund and next-turn flag reset remain asserted. The second ordinary move uses an explicit route.
- SurvivingArrivalSoul performs the arrival before attack and checks soul changes on Move, no collection on Attack, grown damage and unchanged count. Final AP is 2 after the actual one-cell cost, rather than artificial 3. Main arrival's normal variant now includes 2 damage followed by 1 soul healing, proving projected Main HP input is current.
- Existing successful attack-seal and main-cost fixtures obtain their pending movement from explicit routes before checking commitment. Existing invalid aim, insufficient AP, unsupported Artifact, blocked knockback, thorn countdown and original-fact atomicity coverage remains.

Existing CS8632 and Audio CS0105 warnings were observed; no new warning was introduced. Previously recorded five Economy-content baseline failures were not rechecked. Authoring mine/Object/modifier values remain gated; no production values were invented. T07 public freshness/registered route/Return ports, T08 full AI producer migration/removal, and View-owned input/Flow/Plan/Job presentation remain pending. No commit or push.

## T07 registered routes, public freshness, and real execution validation

**T07 complete.** The user authorized T07 only. Existing T01-T06 edits remain intact in the feature checkout; no T08 implementation, commit, or push was performed. Implementation documents are English and new code comments are Korean. Unity ran through the AppData-access-approved path. Existing scene, asset, assembly and Framework infrastructure ownership remains unchanged.

Core owns the public projection and explicit expected-fact comparisons. Existing Character/Combat Buffers retain authoritative runtime facts and validate all proposals before installation. The shared View request schema now transports immutable paths and captured contexts; View still owns gesture/revision/lifetime association and presentation. No scheduler, new Buffer, registry, generic mutation framework or retained projection was added.

Implemented contracts and evidence:

- View Move takes actor/path/context; its preview takes actor/path. Return is a separate request and preview. Endpoint-only View factories were removed. Preview transports the original public context separately from final projected actor/resources/modifier, quotes, ordered public effects and Return eligibility. All calculations use the same required rule composition.
- Capture/Matches compares exact intent/path/battle identity, named admission fields, actor/resources/ledger/effect state, canonical public occupancy and typed dependencies selected by the public projection. It excludes hidden sources before rule selection and simulation. Result/preview/handled/answered counters and presentation acknowledgements are not gameplay freshness. A newly visible relevant source invalidates; hidden-only source movement does not. Actual execution still damages the actor and consumes the real hidden source.
- Rule `TargetIds(tile, mover)` narrows copied target HP/occupancy to declared candidates from typed source/mover facts; requests outside that set reject. Each sequential evaluation still receives latest projected facts. Entry sources exclude destination-only facts. This supplies explicit candidate health dependencies without comparing unrelated living blockers' HP or adding a target engine. Concrete supported trait/category fields and artifact-arrival parameters are listed in T07's comparison table. Future authored adapters must declare and compare any additional consumed fields.
- Installed battle identity increments only after successful preparation, survives EndBattle and is copied through calculation/composition/attack-query/projection inputs. The service checks identity and latest request again before publishing a preview. An old context cannot authorize a new battle reusing actor IDs; checked identity overflow installs neither characters nor battle. Schema-only contexts without captured expected facts are rejected by the live service. Standalone legacy fixtures retain input ID 0; the registered path always supplies installed identity.
- Real preparation independently validates the original character/owned/soul-count/Main snapshot, service-captured placed Artifact instance-ID/artifact-ID membership, real command-state/environment, and real soul-field expectations. The essential RED proved that a directly changed, otherwise unchanged occupant was previously missed; the new validation rejects before movement/resources/field/result installation. Public stale rejection is an ordinary result with empty gameplay events, and duplicate processing cannot republish it.
- Registered composed Preview and execution agree on damage/recovery/source consumption, final HP/AP, destination modifier provenance, entered cells and ordered event kinds, while Preview leaves Buffers unchanged. Registered Return obtains its new checkpoint contribution from actual composed environment facts, publishes 7/15 HP and modifier source 7, refunds pending AP, and leaves the checkpoint entry hazard active. It captures no entry hazard, thorn or soul dependency. Existing T06 cases retain the actual no-soul-acquisition coverage.
- Bookkeeping-only changes, a later attack-range preview and unrelated living HQ HP changes preserve a route context. Direct actor HP change without a result-sequence increment rejects. Late preview publication against a replaced request rejects. T09 still owns asynchronous callback/scene attachment association.

### T07 test runs

The five-fixture publication filter is:

```powershell
Invoke-MovementTests -RunName t07-final-verified -Filter 'ProjectL.Tests.Framework.MovementPublicationTests;ProjectL.Tests.Framework.CombatViewBufferTests;ProjectL.Tests.Framework.CombatPreviewPublicationTests;ProjectL.Tests.Framework.CombatCommandPublicationTests;ProjectL.Tests.Framework.CombatCommonPublicationTests'
```

The changed producer and calculation regression filter is:

```powershell
Invoke-MovementTests -RunName t07-green-caller-regression -Filter 'ProjectL.Tests.Framework.ChapterContinuationServiceTests;ProjectL.Tests.Framework.CoreStageCombatCycleTests;ProjectL.Tests.Framework.TemporaryCoreChapterContinuationTests;ProjectL.Tests.Framework.TemporaryCoreRuntimeTests;ProjectL.Tests.Framework.TemporaryCoreShopRuntimeTests;ProjectL.Tests.Core.MovementRouteTests;ProjectL.Tests.Core.MovementEnvironmentTests;ProjectL.Tests.Core.PositionTileModifierTests;ProjectL.Tests.Core.CheckpointReturnTests;ProjectL.Tests.Core.StageEnemyAiTurnTests'
```

RecoveryStageCycleTests is a partial CoreStageCombatCycleTests fixture and is included in that filter. Only existing lifecycle fixture paths were migrated; the test helper submits explicitly supplied complete cells and cannot reconstruct an endpoint route.

| Run | Observed evidence |
|---|---|
| `t07-red-read-set` | Fixture compile error: CharacterData AP is under Stats. No test XML; not behavioral RED. |
| `t07-red-read-set-fixed-fixture` | 1/1 failed, zero skips, editor exit 2: unchanged occupant mutation incorrectly allowed publication. |
| `t07-compile-boundary` | 16/16 passed, zero skips, editor exit 0. |
| `t07-green-initial` | Fixture compile error: missing ThornMovementTileRule namespace import. No test XML; not behavioral RED. |
| `t07-green-import-fixed` | 63/63 passed, zero skips, editor exit 0. |
| `t07-green-caller-regression` | 152/152 passed, zero skips, editor exit 0; changed producer integration and Core calculation regressions. |
| `t07-final-publication` | 62/63 passed, zero skips, editor exit 2. Added discovery case passed; its subsequent hidden-only execution was skipped because direct fixture environment publication consumed the next View command number. This was a fixture-number conflict, not a public freshness failure. |
| `t07-final-verified` | 63/63 passed, zero skips, editor exit 0 after the environment fixture used Advance number 0. Includes extended visible discovery, full projected-character hidden equality and registered Return modifier publication. |

The two final filters cover 215 distinct cases; the caller filter's runtime source was unchanged before the final publication run. Evidence XML/log/exit-code files remain in the ignored evidence directory. No full suite was rerun; the previously recorded five Economy-content failures are not claimed fixed. The Unity-generated FMOD scan timestamp was restored to its prior value.

### T07 remaining producer and integration inventory

T08 owns `EnemyAiPolicy.TrySelectMovementDestination` (`EnemyAiPolicy.cs:80`) and `EnemyAiTurnService.Calculate`'s endpoint Move intent (`EnemyAiTurnService.cs:30-32`). `CombatMovementCalculator` still has the explicitly temporary no-Path Preview/Calculate/Evaluate branch with its displacement semantics. It does not create a MovementPath or act as a route fallback. Endpoint fixtures remain in CombatCommonCommandTests and several CombatCommandCalculationTests/CombatCommandEffectTests; AI behavior remains in StageEnemyAiTurnTests. T08 must migrate these together, remove the ordinary endpoint branch and rerun publication compilation/regressions. Low-level Range hints and separately specified forced movement retain their explicit meaning.

An `rg` inventory found no remaining endpoint-only CombatView Move factory/caller: all runtime schema and affected Framework lifecycle requests supply paths/contexts. No live player drag emitter existed to wire in this task. T09 still owns input sampling/draft editing and response lifetime correlation; T10-T11 own View Flow/Plan/Job scheduling and whole-result presentation acknowledgement. Production mine/Object/modifier values, visibility/perception providers and authored modifier-combination policy remain real content gates. The plan/spec updates clarify implemented contracts without changing confirmed gameplay.

## T08 safe enemy routes and endpoint removal

**T08 complete.** The user authorized T08 after identifying this boundary as the best intermediate review point. T09 and a separate intermediate review have not started. No commit or push was performed; existing T01-T07 edits remain intact.

`EnemyMovementPathfinder.TryFind` is a pure command-local BFS. It uses `MovementRouteValidator.CanEnter`, searches full reachable geometry for the existing pursuit target's legal attack positions, and then cuts the chosen route by current remaining range/AP. It returns no successful one-cell/no-step path. Ties use shortest route, goal Y/X and fixed neighbors +X,+Y,-X,-Y. Current attack affordability does not hide future geometric goals; the zero-cost fixture can move with zero AP. Existing attack selection geometry includes splash, with zero-percent additional roles excluded as goals. It does not execute tile entry or attack settlement during search.

The existing environment payload now carries immutable `MovementTileFact.IsMine`, default false. The policy excludes only active classified sources known to that actor, without inferring mines from rule-key strings or damage. Classification participates in source equality, survives typed copies, and cannot be altered by an entry rule. This is authored source metadata in the existing Combat Buffer, not a new perception store, content registry or effect engine. The test mine's parameters are fixture values only; production mine authoring and a live visibility provider remain gated.

`EnemyAiTurnService` retains target ranking and attack-first scoring, passes the actual path into the common calculator, and suppresses attacks after an interrupted route or terminal outcome. It closes only a living actor's still-active turn. Existing composition preserves original expectations and the complete movement/effect/attack/turn-end stream in one proposal. Existing enemy sealing and Return rejection are retained.

The common calculator's no-Path Preview/Calculate/Evaluate branch and its endpoint effect helper were removed. A Move endpoint without Path now rejects InvalidPath; no runtime or fixture helper constructs a route from an endpoint. The fixture Move helper receives complete cells. `EnemyAiPolicy.TrySelectMovementDestination` was removed; all ordinary runtime producers now supply explicit paths. Existing Range/MovementDestination transport hints, unrelated legacy adapters and separately specified forced displacement geometry retain their documented meaning.

### T08 evidence

| Run | Observed result |
|---|---|
| `t08-red` | Fixture compile errors: installed NUnit's `Does.Not.Contain` overload expects a string; use `Has.No.Member` for these collection checks. No XML; not behavioral RED. |
| `t08-red-compiled` | 4/4 failed, zero skips, exit 2. The essential detour assertion failed because the old AI produced no PathMovement. Three interruption variants dereferenced that absent path payload and are not claimed as separate verified interruption REDs. |
| `t08-green-pathfinder` | 15/15 passed, zero skips, exit 0 (initial ten path cases and five existing selection cases). |
| `t08-green-migrated` | 196/197 passed, zero skips, exit 2. The composed-turn HP assertion still assumed the old direct attack position. |
| `t08-final-regression` | 345/346 passed, zero skips, exit 2. The composed-turn distance assertion incorrectly assumed two edges; the shortest legal splash goal requires one. |
| `t08-zero-splash-red` | 1/1 failed, zero skips, exit 2: a zero-percent splash target at the current location incorrectly prevented approaching a damaging position. |
| `t08-final-verified` | **347/347 passed, zero skips, editor exit 0** after zero-percent goal filtering and corrected route-policy expectations. |

Final exact command, using the existing helper with approved AppData access:

```powershell
Invoke-MovementTests -RunName t08-final-verified -Filter 'ProjectL.Tests.Core.EnemyMovementPathTests;ProjectL.Tests.Core.StageEnemyAiTurnTests;ProjectL.Tests.Core.CombatCommonCommandTests;ProjectL.Tests.Core.CombatCommandCalculationTests;ProjectL.Tests.Core.CombatCommandEffectTests;ProjectL.Tests.Core.MovementRouteTests;ProjectL.Tests.Core.MovementEnvironmentTests;ProjectL.Tests.Core.CheckpointReturnTests;ProjectL.Tests.Core.PositionTileModifierTests;ProjectL.Tests.Framework.MovementPublicationTests;ProjectL.Tests.Framework.CombatViewBufferTests;ProjectL.Tests.Framework.CombatPreviewPublicationTests;ProjectL.Tests.Framework.CombatCommandPublicationTests;ProjectL.Tests.Framework.CombatCommonPublicationTests;ProjectL.Tests.Framework.ChapterContinuationServiceTests;ProjectL.Tests.Framework.CoreStageCombatCycleTests;ProjectL.Tests.Framework.TemporaryCoreChapterContinuationTests;ProjectL.Tests.Framework.TemporaryCoreRuntimeTests;ProjectL.Tests.Framework.TemporaryCoreShopRuntimeTests'
```

Final coverage includes 12 new enemy route cases, one additional existing malformed-route variant, all affected Core fixture families, the five T07 publication families and changed lifecycle/service callers. RecoveryStageCycleTests is included through its partial CoreStageCombatCycleTests fixture. No full suite was rerun; the recorded five Economy-content baseline failures are not claimed fixed. Existing CS8632 warnings remain. XML/log/exit-code evidence is retained in the ignored directory; Unity-generated source metas are present, and its FMOD scan timestamp was restored.

Migration rulings and preserved checks:

- The original endpoint charge test now checks entered route cost and pure Preview. Invalid origin-only/out-of-bounds/over-budget/occupied routes still reject atomically. An endpoint without a Path is a negative regression case only.
- Moving toward the checkpoint pays new entered steps: after a two-edge move and one reverse edge, AP is 2 and pending cost is 3. Returning to the origin via two new edges or moving to the former equal-radius endpoint each leaves AP 1 and pending cost 4. Existing separate Return tests retain actual ledger refund coverage; attack-category refunds remain independent.
- Full-route quote/cost cases retain their five accepted/rejected resource expectations. Structure footprint/death-release, repeated attack resource, Main movement and Heavy/Focus/discount fixtures now supply their exact authored route cells. Heavy's corner fixture ends with a Right edge and retains R90 facing; original actor facts remain unchanged.
- The composed enemy-turn fixture's nearest legal attack position is a one-edge splash goal. The existing 70% additional hit deals 14, leaving Main HP 86 rather than the old direct-hit HP 80. Target/aim ranking and damage rules were not changed; ordered movement-before-attack-before-turn-end assertions remain.
- Known-mine detour, budget beyond goal distance, zero-cost movement, sealed approach, unknown/inactive mines, deterministic goal/route ties, and death/forced relocation/movement block are verified. Unknown sources activate through actual common route execution, while interruption suppresses following attacks. The added zero-percent case proves the AI still approaches a directly damaging position.

Remaining scope: production environment authoring/perception and modifier combination policy; View draft sampling/editing/lifetime association (T09); existing View Flow/Plan/Job wiring (T10); complete result presentation and single acknowledgement (T11). No new Buffer, Framework infrastructure, assembly reference, scene or authored asset was introduced. T01-T08 now form the intended Core/Buffer intermediate review checkpoint.

## Intermediate review corrections before T09

The user authorized correcting the three reproduced review findings and reviewing the fixes before T09. No new Buffer owner, Framework infrastructure, registry, assembly reference, authored content or scene change was introduced.

- Non-mover forced relocation now uses the existing ledger helper to renew its checkpoint without charging displacement or clearing attack-refund eligibility. A following Knockback attack no longer mistakes the old checkpoint for unsettled voluntary movement.
- Attack knockback removes the departure contribution before existing forced traversal, emits normalization separately, then reconciles the living nonterminal destination after eligible soul collection. Recall/return-to-hand removes the contribution; placement and enemy redeployment resolve the actual destination. Main placement includes persistent current-HP synchronization in the same proposal.
- Attack hit/preview payloads carry HealthNormalizationLoss separately from damage. DamageDealt and AI damage scoring exclude the cap; HP/death facts retain the resolved attack/traversal result. The existing hit snapshot still precedes arrival collection/modifiers. Public attack previews filter the newly evaluated destination sources, while execution retains real-source validation and unsupported-content gates.
- AI goals use bounded ray collection and the shared impact/falloff target budget independently of AP. Post-fix review additionally found candidate cells retaining origin modifiers. Candidates now remove the departure contribution and resolve only enemy-visible destination contributions; the origin query retains current facts. These queries do not execute entry hazards, acquire souls, apply damage or spend resources.

A separate read-only post-fix review found no remaining actionable concern after the candidate correction. Command-local projection, original expectations, one prepared Buffer publication and View-owned scheduling remain intact.

### Verification evidence

Unity used the approved AppData-access path and existing Invoke-MovementTests helper. XML/log/exit-code files remain under the ignored `.superpowers/sdd/2026-10-04-drag-path-movement/` directory.

| Run | Observed result |
|---|---|
| `review-fixes-red` | 5/5 failed, zero skips, exit 2: checkpoint mismatch, two knockback modifier variants, pierce approach and night recall. |
| `review-fixes-green-initial` | 173/175 passed, zero skips, exit 2. A new fixture replaced its configured deployment depth via a helper; an old targeting-preview test still expected deferred movement death during Attack. |
| `review-fixes-hidden-red` | 1/1 failed, zero skips, exit 2: public attack preview resolved hidden overlapping destination sources and threw the content gate. |
| `review-fixes-green-core` | 178/178 passed, zero skips, exit 0. |
| `review-fixes-final` | 375/375 passed, zero skips, exit 0, before the candidate-position review finding. |
| `review-fixes-candidate-red` | 2/4 failed, zero skips, exit 2: leaving a -10 modifier and approaching a known +9 modifier were missed; original and hidden-destination cases passed. |
| `review-fixes-final-verified` | **378/378 passed, zero skips, editor exit 0**, including the candidate correction. |

The migrated `TargetingPreviewParityTests.AttackPreview_DoesNotReplaySettledMovementHazards` starts from a real resolved route, adds a thorn at the occupied cell, and verifies Attack Preview does not replay traversal or mutate HP/AP/fields. Its former AttackerDiedOnMovementSettlement expectation belonged to the removed deferred-settlement behavior; confirmed gameplay was not changed to satisfy it.

The final filter is exactly the T08 final filter above plus `ProjectL.Tests.Core.TargetingPreviewParityTests`, with RunName `review-fixes-final-verified`. It covers the changed Core fixtures, existing movement/Return/modifier regressions, five publication fixture families and service-cycle callers. This was focused verification as requested, not a full-suite rerun; the five recorded Economy-content baseline failures are not claimed fixed. Unity's generated FMOD scan timestamp was restored. T09 remains unstarted. No commit or push was performed.


## T09 drag editing and preview correlation

**T09 complete.** Implemented the approved View-local input task in the existing feature checkout, preserving the uncommitted T01-T08 and review corrections. No commit or push. T10/T11 have not started.

- `MovementPointerSampler` enumerates grid boundary crossings, including every intermediate cell of a fast sample and X-first exact corner ties. Cell coordinates use floor semantics, with centers at `(x+0.5,y+0.5)`; the scene converts its own screen/world coordinates. Inner-cell inset is an explicit constructor configuration with no invented production default.
- `MovementDraft` handles append, immediate predecessor pop, older-cell/nonadjacent rejection, suspension, prefix retention and immutable snapshots. Revision describes route edits; pointer validity and suspension remain separate gesture facts.
- `CombatMovementInteraction` owns the mutable draft privately. Public-to-View reads are immutable snapshots/state. It submits only through the existing CombatView Buffer, reads battle identity and existing command/presentation gates, and never calculates movement cost, calls processing services, or acknowledges a result. All Core gameplay and authoritative Character/Combat state ownership is unchanged.
- Correlation checks current request number, draft revision, actor, exact path, battle instance and attachment lifetime. Successful route responses also require T07's real public context and matching path metadata. Core still checks relevant public gameplay freshness on execution and real hidden read dependencies during prepared publication; this View layer adds no hidden comparison or broad combat version.
- An invalid Core preview retains exactly its `ValidStepCount` prefix, keeps drop invalid and resets continuity. Endpoint/predecessor re-entry allows editing and fresh validation. A frozen invalid response cancels the entire draft. UI/outside samples suspend; re-entry never bridges the gap. Fast samples stop at the first locally rejected edit and still request Core validation of preceding edits in that same sample.
- Release freezes the exact route and submits it once after matching validation. Frozen gestures refuse another Begin; repeated release/response cannot submit twice. Focus/capture loss, Cancel, Detach, battle end and replacement discard unsubmitted state. Cancellation after submission preserves the actual queued command and does not acknowledge or roll back gameplay.

### Verification evidence

Unity 6000.0.73f1 ran through the approved AppData-access path, using the existing ignored Invoke-MovementTests helper. Fresh XML/log/exit-code files are under `.superpowers/sdd/2026-10-04-drag-path-movement/`.

| Run | Observed result |
|---|---|
| `t09-red` | 5/5 failed, zero skips, exit 2: backtracking, three corner variants and preview-wait submission. Compiling placeholder interfaces established behavioral RED, not missing-type errors. |
| `t09-green` | 38/38 passed, zero skips, exit 0: initial MovementDraft and StageInput cases. |
| `t09-edge-red` | 1/2 failed, exit 2: frozen Begin guard. The partial-sample fixture lay at the float-sensitive inset boundary and did not exercise local rejection; moved its pointer strictly inside the cell. |
| `t09-edge-red-verified` | 2/2 failed, zero skips, exit 2: frozen gesture replacement and retained candidate edits missing from the latest preview. Both corrected. |
| `t09-final` | **61/61 passed, zero skips, editor exit 0**: 25 MovementDraft cases, 18 StageInput, 15 UnityStageInput and 3 CombatPreviewPublication cases. |

The movement fixture reuses existing real Buffers and CombatProcessingService to obtain previews and publish a route; no new harness or fabricated preview authorization is used. Coverage includes rejected X-first corners, immutable snapshots, origin/invalid/outside/cancel-area drops, frozen valid/invalid routes, superseded responses, duplicate submission, UI re-entry, jitter, focus/capture loss, detach/reattach, same-actor/path battle replacement, prefix revalidation and presentation gating. Existing T07 freshness/hidden-information tests remain the evidence for direct HP changes and hidden hazard validation; this task does not duplicate that calculation suite. No full-suite run or manual production-scene/zoom testing was claimed. Unity's generated FMOD scan timestamp was restored.

### Remaining integration boundary

Existing StageSelectInput/IStageInputReceiver/StageInputReceiverSlot already supply press, release and forced cancellation; their contracts remain unchanged. T11's scene owner must select eligible actors from Buffer data, convert screen coordinates, forward the final pointer sample and lifecycle signals, call interaction Attach/Detach (including after release), cancel on mode/context changes, and render immutable candidate/validated preview data. A null grid sample or cancel-area flag breaks sampling continuity. Production inset configuration and Unity usability tuning remain pending. T10 wires the existing Jobs; T11 owns playback, Return UI and one acknowledgement after the complete result. No scene, content asset, assembly reference or Framework infrastructure was added/modified by T09; mine/Object/modifier authoring and perception remain separately gated.

## T10 existing Combat Job service wiring

**T10 complete.** The three existing Jobs now call their matching Core processing service through the already injected InGame/Core service context: Command -> ProcessNextCommand, Preview -> ProcessLatestPreview, Advance -> AdvanceCombat. Each sets IsCompleted only after normal return; later Execute calls are no-ops. A false/no-work service return completes the Job, while exceptions propagate unchanged and leave it incomplete. No new Job type, scheduler, Buffer, effect policy or acknowledgement was added. Preparation wiring and the production Flow/Plan/Runner remained unchanged.

Existing CombatPlanTests now check matching calls, repeated Execute, exact exception propagation, no-work completion and Runner retention after a Core exception. The old unconnected-exception expectations were removed. Existing test service/registry helpers are reused by CombatFlowTests, with actual Buffers injected for real-service cases; no new fixture/harness class was introduced.

CombatFlowTests add real Runner -> registry -> CombatPlan -> Job -> CombatProcessingService -> Buffer coverage. A preview replaced after registration is read at execution. A validated explicit route publishes once; another queued route with its old context stays blocked until prior presentation acknowledgement, then publishes a StaleContext rejection without billing/movement changes. Preview calculation remains allowed during movement presentation. The automatic case admits EnterCombat from Deployment, waits for that result's presentation, then runs a real accepted Advance that starts the Main unit turn. Jobs never advance the presented sequence. The fake no-work service case proves that an idle completed Runner does not bypass Flow's actual handled/answered/result-evidence checks; missing evidence stops further registration once. Existing canceled/replaced-preview and preparation regressions remain intact.

### Verification evidence

Unity 6000.0.73f1 used the approved AppData-access execution path and the existing Invoke-MovementTests helper. Fresh XML/log/exit-code files remain under `.superpowers/sdd/2026-10-04-drag-path-movement/`.

| Run | Observed result |
|---|---|
| `t10-red` | 5/5 failed, zero skips, editor exit 2: all three matching-call variants and both real Runner/Flow cases stopped at existing unconnected Job exceptions. Compilation succeeded. |
| `t10-green` | 69/70 passed, exit 2: automatic fixture tried EnterCombat from the reused preview fixture's existing Combat phase (WrongPhase). |
| `t10-final` | 69/70 passed, exit 2: explicit fixture preparation index 0 violated CombatTurnFacts' minimum of 1. |
| `t10-final-verified` | **70/70 passed, zero skips, editor exit 0**: CombatPlanTests 22, CombatFlowTests 32, MovementPublicationTests 16. |

Final filter: `ProjectL.Tests.Framework.CombatPlanTests;ProjectL.Tests.Framework.CombatFlowTests;ProjectL.Tests.Framework.MovementPublicationTests`. The fixture corrections preserve existing admission and constructor contracts; they are not gameplay RED evidence or production fixes. The final automatic fixture uses explicit Deployment and existing typed turn defaults. This is focused verification, not a full-suite or production-scene run. Unity's generated FMOD catalog scan timestamp was restored, and whitespace checks passed.

### Remaining integration boundary

T11 still owns actual scene/input/lifetime attachment, preview rendering, movement-specific playback and the single complete-result presentation owner that acknowledges after all events and final reconciliation. CombatLifeCycle's existing unconnected scene transition and queued-Job exit coordination remain documented prerequisites; T10 neither clears the shared Runner on scene exit nor substitutes service booleans for presentation acknowledgement. Authored mine/Object/modifier content, perception and input inset usability tuning remain pending. No Core calculation, authoritative Buffer implementation, Framework infrastructure, assembly reference, scene or authored asset changed; no commit or push was performed. T11/T12 have not started.

## T11 movement presentation and whole-result coordination

**T11 adapter implementation verified; production presentation and entry/exit integration remain gated.** The agreed CombatViewSystem/CombatSceneRoot and migrated production presenters were absent. The plan-authorized minimal adapters were implemented in the existing feature checkout. T12 is unstarted. No commit or push.

CombatViewSystem is the single complete-result presentation owner. It captures the actual result and battle/attachment identity, preflights non-movement support, and dispatches the ordered event stream regardless of top-level intent. CombatMovementPlayback handles one Moved event or immediate field-to-field Returned snap only. Night recall to hand/reserve is delegated to ICombatResultPresenter. Movement completion does not acknowledge a result. After every handler completes, the owner reconciles read-only authoritative Buffers, removes handled commands, and calls AcknowledgePresented once. Duplicate or late non-movement callbacks cannot complete another event, detached attachment or replacement battle. Missing handlers block reconciliation/acknowledgement rather than silently skipping events. No catch-all combat presentation policy or second scheduler was introduced into movement playback.

The movement port receives immutable draft paths, nullable Core quotes (null means pending), separate pointer validity, actual step events and Core Return availability. CancelMovement must invalidate previous animation callbacks; IsMovementComplete refers only to the current BeginStep. ICombatPointerMapping supplies scene-configured inset and screen-to-grid/cancel-area mapping. Root caches providers and must be configured before attachment. ICombatResultPresenter owns actual non-movement visual policies and final reconciliation; fixture completion timing is not a production visual implementation.

The existing View Factory registers the module, and the existing View group exposes Combat through ICombatViewService. PostBind reads existing Buffer/root/global services with typed keys; existing group Tick/Shutdown propagate lifecycle. Attach binds the StageInput receiver and Deployment/Combat layer. Pointer input forwards the final sample before release; focus/capture loss, mode changes, battle replacement and detach cancel unsubmitted drafts. Player input is admitted only in PlayerInput with no pending command or presentation, so automatic turns cannot leave deferred EndTurn commands. Return requests a fresh Core preview and submits the existing Return intent once. View does no movement billing, damage, occupancy, checkpoint or soul calculation. Attack-target/placement interaction policies are not implemented by this movement adapter.

Detach invalidates callback lifetime before cleanup, unbinds the receiver, cancels animation/draft/owned Return preview, detaches the result presenter and clears the layer. It does not acknowledge unfinished results or clear queued commands/the shared Runner. The production transition owner must stop Flow registration, finish already-registered Jobs, settle the complete result and then detach/clear inputs. Its F2/transition boundary remains absent, so that integration step remains unchecked in the plan.

### Verification evidence

Unity 6000.0.73f1 used the approved AppData-access path and existing Invoke-MovementTests helper. XML/log/exit-code files are under `.superpowers/sdd/2026-10-04-drag-path-movement/` (ignored).

| Run | Observed result |
|---|---|
| `t11-red` | 0/3 passed, exit 2: movement start and rejected-result reconciliation were behavioral RED; composed fixture preparation/command phase mismatch was setup failure. |
| `t11-composed-red` | 0/1 passed, exit 2: participant/Timeline setup mismatch, not behavioral RED. |
| `t11-composed-red-verified` | 0/1 passed, exit 2: corrected real enemy result reached the missing View non-movement callback (expected 1, actual 0). Behavioral RED. |
| `t11-green` | Compilation failed, no XML: ServiceStoreReader requires a typed SystemGroupKey argument. Corrected to the existing contract; not behavioral RED. |
| `t11-green-compiled` | 64/64 passed, zero skips, exit 0: View 7, Draft 25, Flow 32. |
| `t11-final` | 100/102 passed, exit 2: terminal-main death lacked the existing progression service; Global factory rejected empty controller definitions. Both were fixture prerequisites, not production behavior fixes. |
| `t11-final-verified` | 102/102 passed, zero skips, exit 0. Actual nonterminal enemy death and nonempty Deployment/Combat controller definitions exercised the intended paths. |
| `t11-recall-red` | 1/2 passed, exit 2: normal composed case passed; night-ending recall incorrectly invoked movement Return (expected 0 snaps, actual 1). |
| `t11-final-reviewed` | 103/103 passed, zero skips, exit 0 after non-field recall dispatch correction. |
| `t11-input-phase-red` | 0/2 passed, zero skips, exit 2: automatic-phase EndTurn input left a queued command in both existing composed variants. |
| `t11-final-input-verified` | **103/103 passed, zero skips, editor exit 0** after PlayerInput admission guard: View 13, Draft 25, Flow 32, StageInput 18, UnityStageInput 15. |

Final command: `Invoke-MovementTests -RunName t11-final-input-verified -Filter 'ProjectL.Tests.Framework.CombatMovementViewTests;ProjectL.Tests.Framework.MovementDraftTests;ProjectL.Tests.Framework.CombatFlowTests;ProjectL.Tests.Framework.StageInputTests;ProjectL.Tests.Framework.UnityStageInputTests'`.

The new View fixture reuses the existing real Buffer/CombatProcessingService fixture and existing registry/service helpers. A delegate around the real input write sink counts acknowledgement calls; it does not replace authoritative state. Supplied mapping, input-layer and presentation probes substitute for production coordinates, InputAction assets and visual timing. Factory/group coverage uses the real Global/View factories, typed service context, StageInput routing, group lifecycle, actual Preview/command publication and real Buffers. The composed enemy case runs the real Flow/Runner/Plan/Job/service path; it delays Attack after movement and proves Flow stays gated until the whole stream finishes, including night-ending recall. Death coverage uses an actual enemy entry hazard, retains Died.Expected cell with Final.Anchor null, then reconciles after the death handler. Buffer gameplay facts remain unchanged by presentation.

Additional coverage includes rejected/empty result completion, delayed movement blocking the next mutation, Core-produced Return preview/one intent/no traversal, unsupported handlers on repeated ticks, duplicate callbacks, replacement-battle callbacks, focus/capture loss of a frozen draft, and battle end before a pointer callback. This is focused managed verification, not a full suite or manual production-scene/zoom/frame-rate run. T01's five Economy-content baseline failures are not claimed fixed.

### Remaining real integration dependencies

- Production entry/bootstrap must supply and attach a configured CombatSceneRoot through the exported View group service. Authored mapping/inset, Field/Unit/Hand/Ghost/HUD/result presenters, Render bindings, visual configuration/assets and the actual scene remain absent. Port tests prove dispatch/ownership, not rendered usability.
- The existing F2/scene transition owner must coordinate pending Flow/Job/result completion before normal detach/input clearing. No substitute transition manager or global Runner clear was added.
- Mine/Object/destination-modifier authoring and perception remain the separately recorded content gates. No production balance values, new Terrain Buffer or alternate rule composition were introduced.
- T12's registered full cycle, full-suite baseline comparison and manual Unity interaction checks await authorization and the applicable production prerequisites.

T11 changes are confined to View adapters, View Factory/group integration, the planned EditMode fixture and English tracking documents. Core calculations, authoritative Buffer implementations, Framework infrastructure, assembly references, scenes and authored assets were not modified by T11. New Assets metadata comes from Unity import; the generated FMOD scan timestamp is restored separately. No commit or push.

## T12 registered cycle and final handoff

**Managed implementation verification and handoff complete; production playable acceptance remains gated.** The new `CoreStageCombatCycleTests.RegisteredDragCycle_PreviewMoveBarrierReturnAttackEndTurn` exercises actual registered Core and View group services, existing StageInput routing, Flow/registry/Plan/Runner/Jobs, and real Buffer publication in one battle/service lifetime. It does not call processing services directly for the asserted Preview/Move/Return/Attack/EndTurn/Advance operations.

The cycle verifies pure Preview, an exact four-step detour with two-cell displacement, passage without soul collection, failed attack and mode switching without sealing, full safe Return refund, prior safe movement followed by a thorn route and final soul arrival sealing at the actual stop, later safe movement/Return to that renewed checkpoint, successful attack sealing, rejected sealed Return, turn-end sealing without duplicate hazard/soul resolution, then enemy TurnStarted and composed enemy movement/attack/turn completion. Each accepted drag publishes exactly one result; each result completes the actual View owner before further mutation. Enemy activation and action are two existing Advance operations, not one invented combined scheduling step.

Fixture substitutions are explicit: existing CycleDatabase/CycleSource supply definitions/economy; the source's movement-only variant supplies an 8x6 board and an initial Demon in a legal enemy deployment region. The initial fixture installs a funded active player turn, one surviving enemy, a typed thorn and two soul fields through actual Buffers. This bypasses input-driven battle preparation/placement, already covered by the existing cycle tests. Source definitions and registered gameplay services remain real. The existing T11 mapping/presentation/layer probes provide grid-center samples, configured inset and visual completion timing; they are now shared internally by the existing tests, without a new test harness. Production InputAction/Render/assets are not substituted with fabricated content claims. Cleanup stops test Flow and disposes its isolated Runner; this is not evidence that the production F2 transition drains the shared Runner.

### Full-suite regression discovered and corrected

The first full run found 170 additional failures: registering CombatViewSystem made its PostBindService require Global input and stage-slot services even for valid Core-only InGame compositions with an inactive View. This was a real T11 integration regression. The smallest correction keeps PostBind dependent on its existing mandatory Buffer ports and moves scene input dependencies to explicit `ICombatViewService.Attach(CombatSceneRoot scene, IStageInputReceiverSlot slot, IInputLayerService layers)`. The scene entry owner supplies existing input services; missing slot/layers still fail at Attach, with null-argument assertions added to the existing Factory integration test. No optional service search, swallowed binding exception, alternate supported rules, Framework interface change or new context registry was added. Constructor-based fixtures likewise supply input ports at Attach. Existing Core-only fixture failures provide behavioral RED; all affected regressions pass after the correction.

This supersedes T11's original Attach/PostBind description above. Current APIs are also documented in [the movement guide, Section 8](../guides/unit-movement-attack-core-handoff.md#8-current-drag-path-movement-integration), with a current-contract pointer in [the StageSession guide](../guides/stage-session-view-handoff.md). Legacy endpoint/checkpoint-difference guidance is explicitly superseded; dated historical specifications remain unchanged.

### Verification evidence

All runs used Unity 6000.0.73f1 with the approved AppData-access execution path and the existing helper. Each RunName is unique. Evidence root: `.superpowers/sdd/2026-10-04-drag-path-movement/` (ignored).

| Run | Result / interpretation |
|---|---|
| `t12-cycle-initial`, `t12-cycle-compiled` | Compiler failures, no test XML: new fixture used guessed APIs instead of actual Character/Combat contracts; corrected to existing ports/types. Not behavioral RED. |
| `t12-cycle-contracts` | 0/1 passed, exit 2: fixture tried the active-main service before preparing a battle. Corrected initial persistent state through Character Buffer. |
| `t12-cycle-preparation` | 0/1 passed, exit 2: fixture enemy placement outside configured deployment region. Corrected source region; no production admission relaxation. |
| `t12-cycle` | 67/68 passed, zero skips, exit 2: new test expected enemy movement from the existing separate TurnStarted Advance. Corrected test to present activation, then run the next action Advance. |
| `t12-cycle-verified` | **68/68 passed, zero skips, exit 0**: full CoreStageCombatCycleTests (including recovery partial), MovementPublicationTests and CombatMovementViewTests. |
| `final` | 1,831/2,006 passed, 175 failed, zero skips, exit 2: 170 Global-service binding regressions plus the 5 original Economy-content failures. |
| `t12-bind-fix` | **196/196 passed, zero skips, exit 0**: registered cycle families plus ChapterContinuationServiceTests and ShopStageServiceTests after explicit Attach correction. |
| `final-verified` | **2,001/2,006 passed, 5 failed, zero skips, editor exit 2**. Exactly the baseline failure identities and messages; no additional failures. |

Commands:

```powershell
. '.superpowers/sdd/2026-10-04-drag-path-movement/Invoke-MovementTests.ps1'
Invoke-MovementTests -RunName t12-cycle-verified -Filter 'ProjectL.Tests.Framework.CoreStageCombatCycleTests;ProjectL.Tests.Framework.MovementPublicationTests;ProjectL.Tests.Framework.CombatMovementViewTests'
Invoke-MovementTests -RunName t12-bind-fix -Filter 'ProjectL.Tests.Framework.CoreStageCombatCycleTests;ProjectL.Tests.Framework.MovementPublicationTests;ProjectL.Tests.Framework.CombatMovementViewTests;ProjectL.Tests.Framework.ChapterContinuationServiceTests;ProjectL.Tests.Framework.ShopStageServiceTests'
Invoke-MovementTests -RunName final-verified -Full
```

Each run has its own `.xml`, `.log` and `.exit-code.txt`. The helper shell itself can return normally after test failure; the counts and Unity `.exit-code.txt` are the evidence. Baseline comparison against `baseline-retry.xml`: 1,845 original cases -> 2,006 final cases, **184 added identities and 23 retired identities** (net +161), no skips. Exact lists are retained in `t12-baseline-comparison.json`. All 184 added identities pass. The 23 replaced identities consist of 19 old Core movement/AI/targeting cases and 4 unconnected-Job placeholder cases. Replacements cover actual-route billing instead of geometric difference/refunds, immediate lethal/overflow/traversal settlement instead of deferred attack/turn replay, safe-path AI instead of endpoint Y-then-X movement, and actual service-call/exception/Runner contracts instead of NotConnected expectations. Existing occupancy, rejection, atomic publication, attack/trait, forced movement and service-cycle tests remain in the full run; they were not removed to suppress failures.

All five retained failures still report `System.InvalidOperationException : [CombatStageDetail] Combat stage requires Economy.`:

- `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `DataBaseAssetTests.EveryStage_HasNodeVisual`
- `DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

### V01-V36 evidence map

All named managed cases below ran in `final-verified` and passed. Names omit their namespace; Core/Framework fixture names identify the boundary. This matrix records managed behavior, not live rendered usability or missing authored content.

| ID | Direct evidence | Limit |
|---|---|---|
| V01 | MovementDraftTests cancellation/focus cases; MovementPublicationTests.Preview_IsPureAndMatchesKnownExecution_FromComposedEnvironment; registered cycle preview/soul passage | Authored mine/Object visuals pending |
| V02 | MovementRouteTests.Detour_ChargesFourSteps; registered cycle exact four-step detour | Managed route, not manual drag rendering |
| V03 | CheckpointReturnTests.MultipleDrags_ReturnRefundsSixSteps | Actual ledger calculation |
| V04 | CheckpointReturnTests.PaidLoop_SameAnchorReturnRefundsLedger; CombatCommonCommandTests.Move_BackTowardCheckpoint_AccumulatesCostWithoutRefund | No implicit refund |
| V05 | MovementEnvironmentTests.MineSealsPriorSafeDragsAtFinalStop; registered cycle prior safe interval + hazard/soul final stop | Fixture-backed source |
| V06 | Registered cycle later safe move/Return to renewed checkpoint | Real Jobs and publication |
| V07 | MovementEnvironmentTests.OtherEntityDamage_BlocksReturn; DamageThenHeal_RemainsBarrier_AndPreservesEarlierChanges | Bounded typed effects |
| V08 | MovementEnvironmentTests.ZeroDamageConsumedMine_BlocksReturn; NoOpHeal_DoesNotBlockReturn | No production mine balance implied |
| V09 | PositionTileModifierTests.PassThrough_DoesNotApplyModifier; Return_RecomputesModifierWithoutRestoringHp; MovementPublicationTests.BookkeepingAndUnrelatedLiveBlockerHealth_DoNotInvalidate_AndReturnDoesNotAcquire | Environment-to-Buffer connection verified |
| V10 | PositionTileModifierTests.ApplyMaxHp_DoesNotHeal | Maximum-only behavior |
| V11 | PositionTileModifierTests.RemoveMaxHp_CapsWithoutDamage; CombatCommandEffectTests.Knockback_ReconcilesPositionModifiers_WithoutCountingHpCapAsDamage | HP normalization distinct from damage |
| V12 | MovementEnvironmentTests.LethalHazard_PreventsHealingAndSuffix | No resurrection/arrival |
| V13 | MovementEnvironmentTests.ThornEntry_ConsumesAndDamagesInTheMoveCommand; MineSealsPriorSafeDragsAtFinalStop | Living route continuation |
| V14 | MovementEnvironmentTests.ForcedRelocation_DoesNotResumeOriginalRoute; TerminalEffect_PreventsLaterSteps; ProjectedOccupancy_BlocksNextStepBeforeCharging; lethal/surviving interruption cases | Actual stop and charged prefix |
| V15 | MovementEnvironmentTests.InterruptedArrival_CollectsOnce; SurvivingInterruption_HasOneFinalArrival | Combined buff-departure/mine/interruption/soul case remains a minor coverage gap |
| V16 | MovementEnvironmentTests.Soul_CollectedOnlyAtActualDestination_BeforePositionModifier; registered cycle passage and arrival | No passage collection |
| V17 | MovementEnvironmentTests.AttackAndTurnEnd_DoNotRepeatMoveEffects_AndSealActualDistance; registered cycle | No deferred replay |
| V18 | CombatCommonCommandTests.MovementSettlement_AttackAndTurnCommitOnlyAcceptedSegment; registered cycle successful/failed attack and mode switch | Preserves pending movement on failure/cancel |
| V19 | CheckpointReturnTests.InvalidCheckpoint_RejectsWithoutRefund; Return_WithoutPendingMovement_IsRejected; registered cycle sealed Return rejection | Mutation-free rejection |
| V20 | MovementDraftTests.Backtrack_PopsOnlyImmediatePredecessor | Immutable snapshots after editing |
| V21 | MovementDraftTests.FastDiagonal_RecordsBothOrthogonalCells; BlockedXCorner_DoesNotTryYFirst; LongSample_DoesNotOmitIntermediateCells | Manual zoom/FPS still pending |
| V22 | MovementDraftTests.InvalidDrop_DiscardsValidPrefix; FrozenInvalidPreview_DiscardsWholeDraft | No prefix fallback |
| V23 | MovementDraftTests.UIReentry_RequiresEndpointOrPredecessor_AndNeverBridgesGap; FocusOrCaptureLoss_DiscardsFrozenDraft; LateResponseAfterDetachAndReattach_DoesNotOwnNewGesture; CombatMovementViewTests lifecycle cases | Normal production scene-exit drain remains gated |
| V24 | MovementDraftTests.SupersededResponse_DoesNotValidateNewRevision; MovementPublicationTests.DirectHealthChange_InvalidatesContext_WithoutResultSequenceChange | Identity + relevant visible facts, not sequence only |
| V25 | MovementPublicationTests.UnknownMine_DoesNotLeakThroughPreview_OrCauseHiddenOnlyStaleRejection | Live perception/authoring pending |
| V26 | MovementDraftTests.ReleaseBeforePreview_SubmitsExactRouteOnce; CombatPlanTests matching-call/repeated execution; MovementPublicationTests duplicate service call | Once-only charges/publication |
| V27 | MovementContractTests immutable route cases; MovementPublicationTests.NewLedgerFacts_RoundTripAndRejectStaleExpectedValues; draft snapshot cases | Source and expected values retained |
| V28 | CombatCommandPublicationTests.InvalidFieldOrTimeline_PublishesNeitherHealthNorResult; SoulCollection_ChangedFieldRejectsHealthCountAndPersistentMainTogether; MovementPublicationTests.UnchangedOccupantMutation_RejectsPreparedRouteWithoutPartialPublication; EnvironmentPreparation_PublicationAndStaleValidationAreAtomic | Real prepare/install rejection |
| V29 | MovementPublicationTests.Preview_IsPureAndMatchesKnownExecution_FromComposedEnvironment | Same composed supported rules |
| V30 | MovementRouteTests.MovementTraits_RunOnce_AndReturnDoesNotRewindThem | One success per moved drag |
| V31 | MovementEnvironmentTests.Rider_BarrierSafeIntervalReturnAndAttack_UseSeparateRefundLedger; RiderManeuverRefundTests; CombatCommandEffectTests Rider cases | Attack compatibility separate from checkpoint refund |
| V32 | EnemyMovementPathTests.SafeDetour_CanInitiallyIncreaseManhattanDistance; SafeGoalBeyondBudget_UsesPrefix | Deterministic affordable prefix |
| V33 | EnemyMovementPathTests.NoSafeApproach_DoesNotMove; UnknownOrInactiveMine_IsNotUsedByPlanner | Known mine avoidance, not omniscience |
| V34 | EnemyMovementPathTests.InterruptedMove_SuppressesFollowingAttack_AndClosesOnlyLivingTurns | Death/terminal/interruption suppress attack |
| V35 | CombatMovementViewTests delayed movement, rejected result, Return, composed automatic-turn variants; CombatFlowTests; registered cycle | Actual ordered acknowledgement; production presenters absent |
| V36 | CombatCommandEffectTests knockback/forced thorn/forced-arrival cases; MovementEnvironmentTests.ForcedOtherEntity_RenewsCheckpointBeforeFollowingKnockback | Existing geometry preserved, modifier/checkpoint compatibility repaired |

### Final review and actual Unity integration status

A fresh reviewer performed a read-only whole-feature review of Core/Buffer projection/publication, movement/Return/refund, public/hidden freshness, enemy policy, input lifetime, presentation and Job wiring. It found no Critical or Important production issue in that reviewed source snapshot. The later full-suite input-binding regression was independently found and fixed by the coordinator as described above, with 196-case and full-suite evidence. One reviewer minor is deliberately deferred: an exact combined buff-origin HP cap -> nonlethal mine -> surviving interruption -> destination soul ordering regression. Existing separate cases pass; the combined case has not been claimed covered. The reviewer performed no independent Unity execution.

The reviewer declined live production content/perception, presenters/Render, bootstrap/scene, F2 exit coordination and manual usability. These remain real missing dependencies, not gameplay questions or silently accepted behavior. Source recheck confirms the Core Factory composes ThornMovementTileRule only and live DB/source adapters do not populate mine/Object/destination-modifier source facts. CombatLifeCycle still documents its missing transition boundary and stops registration/clears input without draining Jobs. The new View ports do not resolve these authoring/transition gaps.

Manual checks at low/high zoom and frame rates, edge jitter, actual UI overlap/Return rendering, live hazard/death/HP-cap timing, and scene exit during pending work were **not run** because no migrated production scene/presenter/bootstrap/transition path exists. Pure geometry and ordered presentation probes cannot substitute for these checks. No new scene, assets, balance values or transition manager were invented. Report: **Core behavior verified with fixtures; authored environment integration pending. Scheduler and View ports verified; production rendered interaction and entry/exit integration pending.**

No commit/push or destructive cleanup was performed. The ignored evidence/ledger is retained because this checkout remains uncommitted. T12 changed existing integration tests, the necessary T11 Attach dependency correction and current English handoff/guides; Framework infrastructure and assembly references remain untouched. Production acceptance can close only after the content/scene owners supply their remaining integration and the listed manual checks pass.

### Final workspace audit

`git -c core.whitespace=cr-at-eol -c core.safecrlf=false diff --check` passes; an explicit untracked C# trailing-whitespace scan reports zero findings. All 34 untracked C# files have adjacent Unity-imported metadata; 35 new metadata files (including the Combat folder) contain no new GUID conflicts against the Assets metadata inventory. The generated FMOD scan timestamp was restored to its incoming value, leaving no FMOD catalog diff. No Framework infrastructure or assembly-reference diff exists. View contains one whole-result AcknowledgePresented call and no ProcessNextCommand/AdvanceCombat call; existing Jobs retain service routing. Unity has exited and no test editor process remains. Baseline comparison failure identities/messages are equal; evidence is retained in the ignored directory.
