# New Stage Gimmicks Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. The user has requested planning only. Choose the execution method when implementation is authorized; stop and report after every Phase.

**Goal:** Add the approved stage gimmicks (five confirmed; Volcanic Smog on hold) through the existing Core/data/Buffer pipeline, with deterministic timing, shared damage and occupancy rules, Run-persistent stat edits, and automatic Contract exchanges with randomly drawn rewards.

**Architecture:** Extend registered gimmick rules and existing immutable command transitions. Stage SOs own authoring; Buffer remains the accepted runtime-state owner; shared damage, status, arrival, roster, and publication contracts remain the only authorities for their respective facts. Add common capabilities with their first consuming mechanic, not as a speculative framework rewrite.

**Tech Stack:** Unity 6000.0.73f1, C#, existing Core.Contracts/Core.Domain/Core.Application/Systems assemblies, NUnit EditMode tests. No new dependencies.

**Spec:** [Approved design](../specs/2026-11-17-new-stage-gimmicks-core-design.md), approved for planning on 2026-11-17. Baseline inspected: `0515527b1`.

**Equivalent Exchange revision (2026-11-17).** The [Contract rule in Task 9](#task-9-resolve-equivalent-exchange-at-player-turn-end) supersedes the linked design's player-choice rules, their turn gate, and their View activation prerequisite. At turn end, resolve Altar, settle automatic Contract exchanges, prepare warnings, and continue through normal next-side progression.

**Lava Eruption revision (2026-11-17).** [Task 6](#task-6-warn-and-launch-characters-from-lava-eruption) records the revised slide 21 mechanic and the user's confirmed launch, HP, Attack, and Chain rules. It governs Eruption where the linked design describes the earlier mechanic.

**Volcanic Smog hold (2026-11-17).** The planners have not confirmed Smog, and it may be dropped. [Task 5](#task-5-on-hold-smog-state-stacks-common-damage-hooks-and-publication) moves to the last phase (Phase 8) and starts only when the user confirms its rule. Phases 4-7 do not depend on Smog: add no Smog key, status, damage hook, authoring field, or placeholder before Phase 8, and let Task 10 integrate and hand off the other five gimmicks. If Smog is cancelled, delete Phase 8 and the Smog rows below; nothing in Phases 4-7 changes. Task numbers and their anchors are unchanged.

## Global constraints

- Core, pure Data, stage SO authoring, Buffer state/read/result contracts, and a View handoff only. No View implementation, scenes, production asset activation, unrelated terrain/trait/item changes, or ordinary-pillar redesign.
- The user commits and pushes. No commit/push steps are authorized, including at Phase completion.
- Stage-local SO settings and placements; no standalone gimmick definition SO assets. Disabled/absent new sections preserve legacy stage behavior.
- One timing unit is one faction end. Keep existing Tornado/Railway/Haboob/Rockfall behavior and relative order. Do not change global turn semantics.
- Automatic night-return no longer exists. Do not recreate it or test a hypothetical post-night redeployment loop. Current manual Return is deployment-only; movement undo is a separate existing capability.
- Test-owned inputs only. Add no View tests; do not run combat View MSBuild. Core test success does not establish Unity presentation acceptance.
- New/changed C# files need matching `.meta` files; preserve existing GUIDs. Block namespaces, Allman braces, four spaces, LF, UTF-8, and Korean code comments/XML documentation. Documents remain English.
- Preserve Framework service registration/composition and Buffer transaction ownership. No new rule-specific Framework System/Job or alternate state store merely to host a helper.
- If a Phase requires a gameplay, ownership, or ordering deviation, stop and ask before implementing it. Preserve unrelated working-tree edits and do not clean up unrelated FMOD/assets.
- Existing tests owned by others must not have their behavioral expectations changed without asking. First fix the implementation; only make authorized compatibility adjustments. Do not hide infrastructure/compilation failures as passing tests.
- Apply the [locality review and operational boundaries in design sections 9.1-9.2](../specs/2026-11-17-new-stage-gimmicks-core-design.md#91-locality-review-against-the-supplied-guideline). This refinement changes implementation responsibilities, not approved gameplay or runtime state ownership.
- Keep feature-specific initialization/allocation rules in their feature owner. Normal definition registration, adapter mapping, phase coordination, and integrity validation remain shared responsibilities; no reflection-based auto-registration, extra event bus, or per-feature Bootstrap/Factory layer is needed.

## Review focus

Each risk has an owning test task below; these are mandatory acceptance cases, not a second implementation scope.

1. A failed candidate search, invalid warned footprint, or relocation with no free cells terminates deterministically. Preserve fixed warnings, unique victims, and legal occupancy. Tasks 2, 4, 6, and 10.
2. A warned Chain heading that becomes blocked keeps its direction and stops at the blocker. Task 4 checks that case. Task 6 verifies passage through Eruption cells and contact using the positions after Eruption settles.
3. Shield-only damage, HP normalization, or Reconstruction reaches the Smog execution boundary. Verify eligibility and one set of health/death evidence. Task 5 only (on hold; Task 10 does not cover it).
4. Run-persistent Sub stat changes coexist with level growth, Hunter, souls, copy methods, and new battle entry. Preserve contributions once and clear them at the correct ownership lifetime. Tasks 7-10.
5. An ineligible Contract circle leaves its cost, usage, and RNG untouched. A stale or repeated settlement cannot grant a second reward or advance the turn again. Tasks 9-10.

## File map and naming

Prefixes expand to exact repository directories; all `Create` entries also require `.meta` files.

| Prefix | Directory |
|---|---|
| `C/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` |
| `S/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/` |
| `B/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/` |
| `D/` | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat/` |
| `T/` | `Assets/Tests/EditMode/` |

For each mechanic name `M` in `VolcanicSmog` (only if Phase 8 resumes), `Meteor`, `LavaEruption`, `LavaChain`, `DevilsAltar`, and `EquivalentExchange`, its owning task creates:

- `C/Contracts/Commands/StageGimmicks/MGimmickSpec.cs`: validated immutable input, following existing specs.
- `C/Contracts/Combat/MGimmickState.cs` and `MGimmickEvents.cs`: source facts and evidence; use `CombatMEvent` for the event type.
- `C/Application/Combat/StageGimmicks/MGimmickRule.cs`: mechanic-specific initial-state construction and boundary calculations implementing `IStageGimmickRule`.
- `D/MGimmickAuthoring.cs` and `MGimmickData.cs`: serialized inputs and fresh immutable pure Data.
- Modify `D/CombatStageData.cs`, `D/CombatStageDetail.cs`, `S/BattleDefinitionAdapter.cs`, and the existing gimmick key/composition/state/evidence registration for that mechanic only.

Expand `M` literally, for example `C/Contracts/Combat/LavaChainGimmickState.cs`. Do not create all mechanic families in advance. Mechanics use explicit typed state; no mutable dictionary of string effect names or generic rule interpreter.

Authoring is the only new production tuning/default source per stage. Pass selected values explicitly through pure Data/spec constructors; do not introduce competing fallback values or gameplay formulas in adapters. Updating one stage's amount does not require a rule/registry change. Adding a field or type still legitimately changes its schema, adapter, validation, and read contracts. Pure test fixtures own their scenario values independently of production assets.

## Verification procedure used by every task

**Before execution:** recheck the branch, diff, current design, Editor version, test fixture ownership, and whether Unity already has this project open. Do not launch a conflicting second Editor or kill the user's Editor. Use an available approved running-Editor test route if necessary, otherwise report the lock.

The installed Editor was verified at `D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe`. Command options were checked against this project's local Test Framework documentation, `Library/PackageCache/com.unity.test-framework@082c24152fd4/Documentation~/reference-command-line.md`; rediscover that package path if its hash changes.

For an available batch run, use this template through the **approved AppData-access execution path**, not the restricted sandbox launch that fails at `CurlRequestCache.db`. Do not upgrade Unity or delete caches for that known launch problem.

```powershell
$gimmickEditor = 'D:\Program Files\Unity\Hub\Editor\6000.0.73f1\Editor\Unity.exe'
$gimmickProject = 'D:\My_ProjectWork\Github_desktop\Project_L'
$gimmickResults = Join-Path $env:TEMP 'ProjectL-NewStageGimmicks'
New-Item -ItemType Directory -Path $gimmickResults -Force | Out-Null
$gimmickAssembly = 'ProjectL.Tests.Core'
$gimmickFilter = 'ProjectL.Tests.Core.NewGimmickBoundaryTests'
$gimmickRunTag = 'phase1-boundary-red'
& $gimmickEditor -batchmode -nographics -projectPath $gimmickProject `
    -runTests -testPlatform EditMode -assemblyNames $gimmickAssembly `
    -testFilter $gimmickFilter `
    -testResults (Join-Path $gimmickResults ($gimmickRunTag + '.xml')) `
    -logFile (Join-Path $gimmickResults ($gimmickRunTag + '.log')) | Out-Host
```

Each task names the fixture filter to substitute; CoreIntegration fixtures use assembly `ProjectL.Tests.CoreIntegration` and namespace `ProjectL.Tests.CoreIntegration`. Separate multiple fully qualified filters with `;`. Use distinct red/green filenames; a stale XML file is not evidence. Require fresh results with a nonzero selected test count and zero failures for green; inspect logs for compilation/runtime faults. Do not use `-quit` to terminate tests early.

After the new tests pass, run the owning regression fixtures listed per task. At Phase completion, run all `ProjectL.Tests.Core` tests by omitting `-testFilter`, plus the Phase's relevant CoreIntegration fixtures. Do not label this as the entire CoreIntegration/View suite passing. Report any blocked check honestly and stop at the Phase gate.

## Phase gates and dependencies

The priority implementation ends at the Phase 3 review. Phases 4-7 cover the remaining confirmed gimmicks and the release of their Core contracts to View. Phase 8 (Volcanic Smog) is on hold. Use the phase tables below for implementation sequencing in place of the outline in design section 11.

### Implement first

| Phase | Tasks | Reviewable result | Depends on |
|---|---|---|---|
| 1 | 1 | Shared turn-end stages and legacy gimmick regression | Baseline |
| 2 | 2 | Meteor warnings, impacts, configurable Fire duration, and flames | 1 |
| 3 | 3-4, one implementation unit | Shared occupancy, Lava Chain, and Meteor/Chain integration | 1-2 |

### Deferred phases

| Phase | Tasks | Reviewable result | Depends on |
|---|---|---|---|
| 4 | 6 | Lava Eruption warnings, bounded launches, direct HP loss, and temporary Attack increase | 1-3 |
| 5 | 7-8, in order | Persistent stat edits and Devil's Altar | 3 |
| 6 | 9 | Automatic Contract settlement, random reward, and atomic persistence | 5 |
| 7 | 10 | Cross-feature regressions and Core-to-View handoff for the five confirmed gimmicks | 1-6 |

### On hold

| Phase | Tasks | Reviewable result | Depends on |
|---|---|---|---|
| 8 | 5 | Volcanic Smog, its damage hooks, and its interactions with the finished gimmicks | 1-7 and the user's confirmation of the Smog rule |

A Phase 7 report does not authorize Phase 8. Start it only after the user confirms the Smog rule, then answer the open questions in Task 5 before writing tests.

**Every Phase ends with a stop/report.** Summarize changed contracts, actual modification/understanding/regression scope, fresh test counts/artifacts, and remaining limitations. The report is not permission to start the next Phase. No implementation checkbox below is completed by this planning task.

At each gate, name the concrete feature change and distinguish necessary shared-contract edits from accidental coupling. Identify which unrelated rule bodies a later editor no longer needs to read and which contracts still must be understood. Focused tests follow the actual dependency path; the full Core and selected CoreIntegration Phase gates above remain mandatory. Before implementation, improvements are expected scope reductions only, not measured results.

## Phase 1 — Shared turn-end processing

### Task 1: Localize initialization and add explicit boundary stages without changing legacy behavior

**Files:** Create `C/Contracts/Combat/StageGimmickBoundaryPhase.cs`; modify `C/Contracts/Combat/StageGimmickState.cs`, `C/Application/Combat/StageGimmicks/IStageGimmickRule.cs`, `StageGimmickComposition.cs`, the four existing rule declarations, `C/Application/Combat/CombatGimmickCalculator.cs`, and `CombatTurnCalculator.cs`. Test: create `T/NewGimmickBoundaryTests.cs`.

**Interfaces:** Add enum values `Expiry`, `Scheduled`, `EndOccupancy`, `AfterCleanup`, `Warning`. Extend `StageGimmickBoundary(Owner endedSide, long completedControllerTurnCount, StageGimmickBoundaryPhase phase = StageGimmickBoundaryPhase.Scheduled)` with a read-only `Phase`. Add `bool RunsAt(StageGimmickBoundaryPhase phase)` and `StageGimmickState Initialize(CombatCommandState initialState, StageGimmickSpec definition, StageGimmickId id, ref uint randomState)` to the existing `IStageGimmickRule`; keep its existing `TryAdvance` and forecast signatures. Initialization may advance only the caller-local RNG and returns an immutable state, or null for the existing stateless Railway case. Existing rules participate only in `Scheduled` boundary execution.

- [x] Write `EndBoundaryIncrementsOnceAcrossStages`, `LegacyRulesRunOnlyInScheduled`, and `TerminalStageSkipsRemainingStages`. Pin `Assert.That(final.Turn.CompletedControllerTurnCount, Is.EqualTo(beforeCount + 1));` and legacy event order equal to `Tornado, Railway, Haboob, Rockfall` for an all-due nonterminal fixture.
- [x] Add `LegacyInitializationKeepsStateIdsAndRandomSequence` using fixed test-owned definitions/seeds and independently specified baseline facts. Verify initial phases, cells, next boundaries, the original definition-index IDs, no Railway state entry, and final RNG state. Cover invalid/duplicate definitions producing no installed partial state.
- [x] Run `NewGimmickBoundaryTests` red; distinguish expected missing phase behavior from an Editor/bootstrap failure.
- [x] Move the current Rockfall constructor expression and Haboob/Tornado preparation calls from `StageGimmickComposition.Initialize` to their registered rule's Initialize method, retaining the existing helpers. The coordinator continues to iterate definitions in their original order, assign `new StageGimmickId(i + 1)`, dispatch by key, validate/collect returned states, and publish the final local RNG once. Do not add per-kind formulas, alternate stores, or feature factories to the coordinator.
- [x] Route the existing end functions through Expiry -> Scheduled -> EndOccupancy -> existing cleanup -> AfterCleanup -> Warning, projecting each accepted transition. Composition filters `RunsAt`; initialize definitions once and preserve the two-argument boundary constructor's old meaning. Keep approved runtime coordination here; a rule must not call a neighboring rule to force ordering. Add new mechanic registrations only in their owning tasks.
- [x] Run green plus existing `StageGimmickProgressionTests` and `CoreIntegration.StageGimmickCompositionTests`. Verify a repeated/stale boundary cannot increment the clock again, and no existing initial/default stage requires new content.
- [x] **Phase 1 gate:** report and stop after verifying the shared boundary stages and existing gimmick order.

**Execution record (2026-11-17, Phase 1 only).** The turn calculator sequences the five named phases around existing cleanup and merges the continuing projections into one transition. Initialization dispatches by rule key in authoring order; each rule owns its initial facts. Changing legacy initial placement or cadence now requires the relevant rule/helper and its spec, not the other three rule bodies. Changes to phase order still require reviewing the coordinator and the existing transaction contract; no Buffer or Framework registration changes were needed.

- Unity 6000.0.73f1 EditMode: red fixture 6 passed / 4 expected missing-phase failures; focused green 22 passed; full `ProjectL.Tests.Core` 6,066 passed, 0 failed, 11 opt-in diagnostics skipped; `StageGimmickCompositionTests` and `StageGimmickStatePublicationTests` 6 passed, 0 failed.
- XML/log artifacts: `%TEMP%/ProjectL-GimmickPhase1-20261117-134336/phase1-red.{xml,log}` and `%TEMP%/ProjectL-GimmickPhase1-green-20261117-134648/phase1-{focused,core,integration}.{xml,log}`.
- Review: no blocking findings. A forged Tornado event's new boundary phase is outside the fields checked by the existing evidence validator. Normal rule dispatch emits only `Scheduled`; no evidence validator change was made in Phase 1.
- No View or PlayMode validation. No scene or production asset changes, commits, or pushes. Stopped before Phase 2.

## Phase 2 — Meteor and flames

### Task 2: Reuse Fire with variable duration and add complete Meteor behavior

**Files:** Create the `Meteor` family; modify `C/Domain/CombatEffects/Rules/PeriodicStatusRules.cs`, `C/Contracts/CombatEffects/FireStatus.cs` only if needed for equality/read contracts, `C/Application/Combat/PeriodicStatusApplication.cs`, existing arrival/undo receipt handling, and `C/Application/Combat/StageGimmicks/StageHazardForecast.cs`. Tests: `T/NewGimmickFireRulesTests.cs`, `T/MeteorGimmickTests.cs`, `T/CoreIntegration/MeteorPublicationTests.cs`.

**Interfaces:** `ApplyFire(StatusEffectSet current, EffectOriginKey origin, int remainingAffectedSideStarts = 2)` preserves the original call form. Use the design's maximum-remaining-duration and origin-selection rule. `MeteorGimmickState` owns warned footprints and source-identified flame areas with absolute expiry counts. Rule stages: Expiry, Scheduled, EndOccupancy, Warning.

- [x] Write red cases for ordinary Fire 2 versus incoming 3, non-shortening refresh, full 3x3 warning, nonoverlapping 2-4 sites, site shortage, and invalidated warned sites. Pin `Assert.That(refreshed.RemainingAffectedSideStarts, Is.EqualTo(3));`, a warning at 2/impact at 4, and damage 60 before protection for max HP 100.
- [x] Run the fixtures red; implement shared Fire duration without another Burn type or duplicated tick damage. Enumerate eligible footprints with deterministic order and use the existing environment RNG. No unannounced retargeting; planning shortage below minimum skips the whole cycle, while later invalidation cancels only the affected warned footprint.
- [x] Resolve one hit per character per meteor through numeric EnvironmentDamage and existing settlement; survivors receive Fire even when shields absorbed the numeric damage. Leave matching flame footprints, default expiry `activationCount + 4`; apply Fire on legal entry and own-side end once even when flame sources overlap.
- [x] Wire arrival, supported movement undo receipts, typed result/read/forecast and Buffer evidence validation. Add `MeteorPublicationTests` for conversion, warning/impact/expiry, changed RNG rejected in a read-only preview, and malformed transition rejection. Run green with `PeriodicStatusRulesTests`, `PeriodicStatusTurnTests`, `MovementUndoRulesTests`, and `MovementUndoPublicationTests`.
- [x] **Phase 2 gate:** report and stop; ordinary Fire and old meteor-free stages must retain their behavior.

**Execution record (2026-11-17, Phase 2 only).** Stage authoring is the tuning source. `MeteorGimmickRule` owns placement and boundary effects, with the existing numeric environment damage, Fire ticks, arrival, and movement undo paths handling their own settlement. The rule publishes committed footprints, per-site impact or cancellation, and independent flame expiry. Existing state and result readers expose `MeteorGimmickState`, `CombatMeteorEvent`, and `CombatMeteorImpactEvent`. Forecasts report committed footprints and possible damage because terrain and damage protection can change before impact.

- The planner checks alternate legal placements if its random choices cannot meet the minimum. Overlapping flames refresh one Fire status using the latest impact, then the lowest site ordinal. A positive warning lead must be shorter than the cycle so the first warning occurs at a faction end.
- Changing a stage's percentage or timing uses its Meteor authoring section. Placement or fire-source selection changes stay with the Meteor rule and its evidence and forecast cases. Changes to shared Fire refresh or arrival still require the periodic status and movement undo regressions. No Framework service or second status store was added.
- Unity 6000.0.73f1 EditMode ran two expected missing-feature failures before implementation. Final focused run passed 55 tests. Full `ProjectL.Tests.Core` passed 6,079 with zero failures and 11 opt-in diagnostics skipped. The five selected CoreIntegration fixtures passed 58 with zero failures.
- XML and logs are in `%TEMP%/ProjectL-GimmickPhase2-20261117-135345/phase2-red.{xml,log}` and `%TEMP%/ProjectL-GimmickPhase2-20261117-141850/phase2-{focused,core,integration}.{xml,log}`.
- The full Core run initially caught a public shape change in movement undo. Flame guards are now internal to the existing undo facts, and the original shape check and frozen route samples pass. The tested preview leaves the RNG and warning intact. Buffer tests reject a changed warning cursor, missing warning evidence, and false cancellation.
- Verification covered Core and the selected CoreIntegration fixtures. Stopped at the Phase 2 review.

## Phase 3 — Shared occupancy and Lava Chain

Tasks 3 and 4 form one implementation unit. Define the Chain state from Task 4 before Task 3 projects its resting head into the placement and landing queries.

### Task 3: Share placement and landing checks for the Chain head

**Files:** Create `C/Domain/Movement/GimmickOccupancyRules.cs`; modify owning queries in `CellEnvironmentQuery.cs`, `CombatPlacementCalculator.cs`, `CombatMovementCalculator.cs`, `AttackForcedMovement.cs`, and random standing-cell selection consumers that actually bypass those queries. Test: `T/GimmickOccupancyTests.cs` and `T/CoreIntegration/GimmickOccupancyPublicationTests.cs`.

**Approved location adjustment (2026-11-17).** The user approved placing the pure occupancy query in `Domain/Movement`: the existing shared traversal/standing-cell owner is in Domain, which cannot reference Application. Application placement and forced landing consume the same Domain query. This preserves the owning state and gameplay. The user also requested attention to optimization: bound heading selection to eight candidates, reuse query-local blockers/reservations, and avoid a second combat simulation during publication validation.

**Interfaces:** `IReadOnlyCollection<CellCoord> BlockingCells(StageGimmickStateSet gimmicks)` and `bool Overlaps(StageGimmickStateSet gimmicks, IReadOnlyList<CellCoord> occupiedCells, IReadOnlyCollection<CellCoord> transientReservations)` on `GimmickOccupancyRules`. Derive occupied cells from the `LavaChainGimmickState.HeadCell` defined in Task 4. Pass the active Chain path reservations in the query. Later pillar rules extend the same projection.

- [x] Write red cases for full candidate footprints overlapping the resting head or a transient Chain reservation. Cover deployment, normal movement, forced landing, and random relocation. Pin `Assert.That(Overlaps(state, candidateCells, reservations), Is.True);` and assert identical attack reach/line-of-sight with and without the head.
- [x] Run red. Integrate the derived query in the existing movement and placement admission checks, including full-footprint checks for forced landing on gimmick cells. Preserve the existing special movement rules for intermediate cells.
- [x] Keep the resting head position in the Chain state and transient reservations in the displacement calculation. Read each through the shared query.
- [x] Run pure query tests with `CellEnvironmentQueryTests`, `EnvironmentPlacementRuleTests`, `EnvironmentTerrainRuleTests`, and targeted existing knockback tests. Complete `GimmickOccupancyPublicationTests` using the registered Chain rule and its adapter. Verify that a head relocation invalidates stale admission and forecast inputs.

### Task 4: Warned heading, extending head path, perpendicular push, and retraction

**Files:** Create the `LavaChain` family and `C/Application/Combat/StageGimmicks/LavaChainGeometry.cs`; connect Task 3 occupancy, existing arrival/displacement integration, and new typed path/contact evidence. Tests: `T/LavaChainGeometryTests.cs`, `T/LavaChainGimmickTests.cs`, `T/CoreIntegration/LavaChainPublicationTests.cs`.

**Interfaces:** `CellCoord LeftOf(CellCoord travelStep)` returns `(-dy, dx)`; `int StepsToBoundary(int width, int height, CellCoord origin, CellCoord step)` counts in-bounds grid steps; `IReadOnlyList<CellCoord> LateralPriority(int width, int height, CellCoord victimCell, CellCoord travelStep)` returns outer then inner unit steps, tie left. `LavaChainGimmickState` owns `HeadCell`, the committed warning heading, and the schedule. Transient visited cells are query-local. Rule stages are `Scheduled` and `Warning`.

- [x] Write red tests for all eight directions, uniform legal heading selection, warning 2/activation 3, no legal direction, new blocker after warning, lateral fallback, no-space/immune defeat, Reconstruction survival, and no retraction hit. For a 9x9 board at `(4,4)` heading `(1,1)`, pin `Assert.That(priority, Is.EqualTo(new[] { new CellCoord(-1, 1), new CellCoord(1, -1) }));` because distances tie.
- [x] Run red. Initialize the head and schedule before character placement validation. Prepare one heading one end before activation using current valid directions. Use that direction and the latest blockers on activation with no additional direction draw. Register Chain after Meteor in `Scheduled`.
- [x] Advance the head in ordered grid steps, reserve current/next head and traversed chain cells, and resolve unique contacts per step. Try the outer then inner one-cell landing using shared forced-entry/terrain effects. Instant defeat uses ordinary Reconstruction behavior; any surviving undisplaced target stops the head before its occupied cells. Retraction clears only transient reservations and cannot hit again.
- [x] Add publication tests for ordered head path, contact/actual lateral landing, final occupancy, stale context, and altered warning/RNG rejection. Run green with Task 3 occupancy fixtures and existing forced-movement/arrival regressions.
- [x] Add a same-boundary case to `LavaChainPublicationTests` where Meteor impacts before Chain and Chain pushes a character from outside the impact into the new flames. Assert one end-count increment, no Meteor hit on that character, and Fire applied on forced entry.
- [x] **Phase 3 gate:** run the full Core assembly and the priority Phase integration fixtures. Confirm the ordered head path, lateral pushes, and retraction in the published results and forecasts. Report the Meteor and Chain read/event contracts and Unity acceptance scenarios, then stop at the priority implementation review.

**Execution record (2026-11-17, Phase 3 only).** Tasks 3-4 are implemented. The Chain head is initialized before placement validation. The existing faction-end pipeline commits the warning heading and later resolves extension, lateral contact effects, and retraction. The shared Domain occupancy query derives the resting head from its state; only the active displacement projection owns transient reservations. Deployment, movement, forced movement, random standing candidates, and platform transport consume the appropriate admission checks. Legacy anchor-only forced terrain rules and attack occlusion remain unchanged.

- Authoring is opt-in and converts fresh immutable Data into `LavaChainGimmickSpec`. Defaults are cycle 3, warning lead 1, push distance 1, with an explicitly authored initial head cell. Initialization rejects overlap with an initially placed character or structure.
- The focused red runs demonstrated missing head admission, missing Chain behavior, forged warning/path acceptance, stale forecast blockers, per-activation contact suppression, platform/tile movement bypasses, and successful-contact evidence with no actual push. The final fixes preserve multi-cell footprints and allow a later genuine head-step contact, stop platforms/tile pushes at the head, and require actual forced-movement evidence. Four existing corpse-knockback regressions caused by reading a removed anchor were fixed in production; their test expectations were unchanged.
- One independent final review identified three important issues: later genuine contacts, platform admission, and missing push evidence. Each received a regression test and an implementation fix. No second review loop was requested. The evidence coverage fixture gained rows for the two new event types; existing assertions were preserved.
- Optimization: heading selection has at most eight candidates; boundary distance uses arithmetic; extension reuses a cell-to-occupant index and refreshes it only after contact effects; reservations use a `HashSet` allocated only for Chain resolution. Resting-head checks do not allocate a cell collection. Preview capture stores only blocking cells; forecast freshness also observes current hard blockers. Publication reads event evidence without replaying damage or arrival effects. No frame-time or allocation benchmark was performed.
- SSOT/SOLID/FW/locality review: state and accepted results remain Buffer-owned, pure occupancy is in Domain, feature formulas stay in the Chain rule/geometry, and composition/adapter mapping remain explicit. No Framework service, Job, alternate scheduler, or duplicate persistent occupancy store was added. The maintainability observations below remain separate from verified gameplay correctness.

**Fresh verification.** Unity `6000.0.73f1` at `C:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe` ran EditMode in batch mode. Artifacts are under `C:/Users/hyeon2-400/AppData/Local/Temp/ProjectL-GimmickPhase3-20261117/`.

| Gate | Result | Artifacts |
|---|---|---|
| Full `ProjectL.Tests.Core`, no test filter | 6,121 passed, 0 failed, 11 existing opt-in diagnostics skipped (6,132 total) | `core-final.xml`, `core-final.log` |
| Selected `ProjectL.Tests.CoreIntegration` | 142 passed, 0 failed, 0 skipped | `integration-final.xml`, `integration-final.log` |
| Review regressions | Both platform admission and omitted-push cases failed before the fixes; multi-cell recontact failed before its fix and passed afterward | `review-red2.xml`, `recontact-red.xml`, `recontact-green.xml` |
| Static checks | `git diff --check` clean; matching `.meta` for every new C# file; no Unity reference in new pure Core/Data files | Working-tree inspection |

The 15 integration fixtures were `LavaChainPublicationTests`, `GimmickOccupancyPublicationTests`, `MeteorPublicationTests`, `StageGimmickCompositionTests`, `StageGimmickStatePublicationTests`, `StageGimmickAuthoringTests`, `TornadoGimmickPublicationTests`, `HaboobPublicationTests`, `RockfallPublicationTests`, `MovementUndoPublicationTests`, `MovementPublicationTests`, `MovementEffectCombinationPublicationTests`, `PillarPlatformPublicationTests`, `PullPublicationTests`, and `ControllerTurnCountPublicationTests`. This is not a claim that all CoreIntegration or View tests ran. No View implementation, scene wiring, or production asset activation was performed. The Unity-generated FMOD scan-path edit was restored; pre-existing Phase 1-2 edits were retained. No commit/push.

**Priority read/event contract for View.** Use `ICombatStateBufferReadSink.Current.Gimmicks` for current facts and the existing result/history readers for accepted `CombatResult.Events`. The normal result/playback acknowledgement contract is unchanged; no new timer or choice gate exists.

| Feature | Current read facts | Published result facts |
|---|---|---|
| Meteor | `MeteorGimmickState.Warnings` contains every `MeteorFootprint.Cells` cell; `Flames` contains footprint plus `ExpiresAtBoundaryCount`; `NextBoundaryCount` is an absolute faction-end count | `CombatMeteorEvent` describes warning, skipped cycle, expiry, or resolution. `CombatMeteorImpactEvent` gives the committed footprint, cancellation, and recipient IDs. Damage and Fire use their ordinary character/status events. |
| Chain | `LavaChainGimmickState.HeadCell`, `IsWarning`, nullable committed `Heading`, and `NextBoundaryCount`. Warning with null heading means a committed no-movement cycle. Logical +Y is north. | `CombatLavaChainEvent.HeadPath` is ordered and includes the original head and successfully entered head cells; retraction leaves only `Final.HeadCell`. A `CombatLavaChainContactEvent` precedes its effects and identifies the attempted `PathIndex`, contact cell/recipient, `From`, planned `LandingCell`, actual post-entry `FinalCell`, failure reason, and survival. A blocked attempted contact cell can lie beyond the published successful path. |

**Manual Unity acceptance after View wiring (not performed here):**

1. Render every warned Meteor cell, then impact/cancel the same footprint and display flames until their own expiry. Do not derive the warning from a center-only marker.
2. With default Chain settings, show the warning after faction end 2 and activate at end 3; player and enemy ends each increment the count once. Show the committed eight-direction heading, ordered extension, perpendicular pushes, and visual retraction without a second hit.
3. Confirm an intervening wall stops the committed heading without rerolling, an undisplaced Reconstruction survivor stops the head before overlap, and no-heading warnings retain the head while advancing the schedule.
4. Confirm the old head becomes available after retraction while the final head rejects deployment, standing, forced landing, and platform transport. Attacks keep their original reach/occlusion.
5. In a same-boundary Meteor/Chain fixture, display Meteor first, then a Chain push from outside the impact into its fresh flames with Fire on entry. This ordering and Fire application passed the integration test; animation remains unverified.

**Requested switch/guard maintainability inspection (2026-11-17).** Inspected the new gimmick rules, states/specs/events and both evidence validators, plus the connected occupancy/movement code, `CombatChangeEvidenceReader`, `StageGimmickStateSet`, `CombatHealthEvidenceValidator`, and `CombatStateBuffer`. This was a focused source review, not a whole-repository performance audit. Follow-up recommendations, not additional implemented scope:

1. Highest priority: `MeteorGimmickEvidenceValidator` combines current-character projection, event dispatch, phase rules, and per-impact evidence in one method. `ReadBoardBefore` separately replays an event prefix, while Chain maintains a similar `EvidenceWorld`. A small shared, call-local event-state projection would give common event interpretation one owner; keep Meteor/Chain rules separate. Do not replace each event with a handler class solely to remove `switch`.
2. `LavaChainGimmickEvidenceValidator.Validate` has several local functions sharing path, pending contact, victim queue, and push-progress state. A private per-validation object with named boundary/warning/contact/retraction methods would make those invariants easier to review, following the existing `CombatHealthEvidenceValidator.Validation` pattern. Its missing-state branch also redundantly scans for events already found by `hasEvents`; failed-contact checks repeatedly scan health executions and can use a local evidence index.
3. Several event/state constructors combine unrelated shape, phase, and RNG conditions into one exception. Split them into named invariant checks and useful failure messages where that improves diagnosis. Preserve constructor range checks and publication integrity checks; they reject invalid authored or proposed facts and are not ordinary gameplay failure paths.
4. `CombatChangeEvidenceReader.ValidateGimmickChanges` mixes common expected/final state chaining with legacy Haboob/Rockfall policy, while `CombatStateBuffer.ValidateEnvironmentChange` combines source identity, allocation, and environment-specific evidence checks. These are separate, larger follow-ups with existing tests and ownership boundaries. Registration dispatch and small closed-enum switches are appropriate as they stand. Merely exchanging `switch` for `if` or delegate dictionaries provides no demonstrated performance gain.

Stopped at the Phase 3 review. Phases 4-8 remain unimplemented.

**Authorized follow-up: Meteor/Chain validator refactoring (2026-11-17).** The user approved reducing duplication and separating responsibilities while preserving behavior and required validation. Scope is the two Application validators and one call-local shared event-state projection. Public contracts, gameplay, Buffer transaction ownership, and other validators remain unchanged. Common projection owns only observed character/structure/terrain facts; each feature retains its clock, RNG evidence policy, and acceptance rules. No handler registry, event bus, service, or persistent state is introduced.

- [x] R1: capture the existing focused test baseline and pre-refactor sources; preserve current unrelated edits.
- [x] R2: extract common spatial event projection and move Chain validation state into a private per-call object with boundary, warning, contact, path, and completion methods. Preserve event observation order and failure predicates; index instant-defeat evidence once.
- [x] R3: use that projection in Meteor, remove prefix replay, and separate warning/expiry/impact/completion checks. Preserve the board snapshot captured at the first impact, recipient capture timing, mitigation checks, Fire validation, and existing RNG checks.
- [x] R4: run unchanged focused tests, targeted projection-order characterization, full Core and selected CoreIntegration regression tests; request one read-only review, inspect the scoped diff, and report. No commit/push or Phase 4 work.

Pre-flight: R2 and R3 consume the same spatial projection. Its `Apply` runs after feature inspection of each event, so validation reads facts immediately before that event. Feature-specific state/RNG handling stays outside this common projection. The earlier review recommendation is the approved design; this follow-up only makes its execution steps explicit.

**Refactoring execution record (2026-11-17).** R1-R4 completed. `GimmickEvidenceWorld` now owns the shared character/structure/terrain event observation. Both validators use a private per-call `Validation` object and named checks instead of one long method with captured local state. Meteor no longer replays event prefixes; its first-impact board remains frozen and warning selection reads current preceding facts. Chain's failure evidence is indexed lazily once. Environment/binding snapshots are reused until their corresponding observed facts change, and ordinary character-only commands do not copy the board dictionaries. All previous rejection predicates and exception messages were retained; no gameplay, public schema, or other validator refactoring was included.

- Baseline: 36 existing focused tests passed. Four new event-order characterization cases also passed against the original validators before edits; they accept preceding movement/wall evidence and reject the same evidence placed after the consumer. The initial characterization run had a test-input array type compilation error, corrected before obtaining this baseline.
- Intermediate Chain extraction: 23 passed. Final focused run: 40 passed. Existing tests were not modified for this refactor.
- Full `ProjectL.Tests.Core`: **6,125 passed, 0 failed, 11 existing opt-in diagnostics skipped** (6,136 total). The same 15 CoreIntegration fixtures listed in the Phase 3 record: **142 passed, 0 failed**.
- Artifacts: `C:/Users/hyeon2-400/AppData/Local/Temp/ProjectL-GimmickValidatorRefactor-20261117/` contains `baseline.xml`, `ordering-baseline.xml`, `chain-refactor.xml`, `focused-after.xml`, `core-final.xml`, `integration-final.xml` and matching logs, plus both `*.before.cs` source snapshots. Unity 6000.0.73f1 EditMode was used; no performance benchmark or View/PlayMode claim is made.
- One independent read-only review found no Critical, Important, or Minor issues and no declined scope items. It compared all rejection predicates, observation order, Meteor capture/mitigation/Fire checks, Chain contact/RNG checks, and projection cache invalidation against the saved originals. Static checks passed: clean `git diff --check`, LF, matching new `.meta`, and no Unity types in the new pure helper.
- SSOT/SOLID/FW/locality: the shared object is a derived per-validation observation, not a runtime authority. A Meteor acceptance-rule change now stays within its named Meteor method; a common spatial-event change has one implementation and intentionally requires both validators' regression checks. Chain clock/RNG policy remains Chain-local. No service registration, assembly dependency, Buffer write path, or persistent cache changed. Pre-existing Phase 1-3 and unrelated working-tree changes were preserved; the already-dirty FMOD catalog was not reset. No commit/push. Stopped before Phase 4.

**Code review follow-up (2026-11-17).** A Claude review of Phases 1–3 ([review](../specs/2026-11-17-shortest-move-meteor-chain-code-review.md)) applied behavior-preserving seam fixes; Unity compile and tests have not been run since.
- R1: gimmick occupancy now comes from the virtual `StageGimmickState.BlockingCells`. Fixed occupants override this property and `GimmickOccupancyRules` consumes it.
- R2: there is now one `GimmickOccupancyRules.Blocks(gimmicks, cell, reservations)` predicate.
- R3: the chain's wall check uses `EnvironmentTerrainRules`, and `HardBlockers` now takes `bindings`.
- R4: the initial head overlap is checked only by preparation's `ValidateInitialPlacement`.
- R5: on-entry effects go through `IStageGimmickEntryRule` and `StageGimmickComposition.ApplyEntry`. Eruption will use the same shared arrival path when it launches characters (Task 6, not implemented yet).

Follow-up implemented the same day (review §7, user "둘다 권장대로"; Unity compile and tests not run):
- Q1: Meteor sites exclude gimmick blocking cells.
- D1: on-entry undo receipts come from the same `IStageGimmickEntryRule` (`CollectEntryReceipts`).
- D3: one `EnvironmentRandomTrace`. New RNG gimmick events implement `IEnvironmentRandomEvidence`; events that change gimmick state implement `IStageGimmickStateChange`. This also removes a latent chain-validator false rejection after a Hunter mark redraw.
- Q3: shortest-path tests.

**Phase 4 entry gate.** Complete these in order before writing Eruption tests:

1. Establish a fresh baseline for the code review follow-up above. Compile in Unity, then run all of `ProjectL.Tests.Core` and the 15 CoreIntegration fixtures listed in the Phase 3 record. Record the counts and artifact paths here. Fix a failure in its owning follow-up change; do not carry it into Eruption.
2. Separate the follow-up from unrelated working-tree work. The tree also holds the Thorn Bomb trait change (new `ThornBomb*` files and the removed `ThornField*` view table). Report which files belong to each change so the user can commit them separately. Eruption starts from a tree whose gimmick baseline is known.
3. Write and get approval for the D2 design (per-rule forecast impact). Eruption's landing uncertainty is its first new consumer. The review document's phase numbers predate the Smog hold: its "Phase 5 Eruption" is now Phase 4, and its Smog case moves to Phase 8.

The Eruption Attack questions are decided; Task 6 records the decisions and the SO tuning values.

## Phase 4 — Lava Eruption

### Task 6: Warn and launch characters from Lava Eruption

**Confirmed gameplay (2026-11-17).**

- Eruption activates every three faction ends and warns two faction ends before activation. The first warning is at count 1 and the first activation is at count 3. The warning and the launch both cover the five-cell cross, including the center.
- Use the environment RNG to place one to three non-overlapping crosses on standable non-platform cells. Reduce the count to the valid sites available and skip the cycle if none fit. Revalidate each warned cross on activation and cancel invalid crosses at their announced sites.
- Choose a legal, unoccupied landing uniformly inside the 7x7 square centered on the cross that launched the character. Exclude that source cross's five cells. Cells of other eruption crosses are valid landing candidates when they satisfy the ordinary landing rules. Reserve each accepted landing footprint.
- Resolve each original target once per activation. Capture the targets across all valid crosses before moving any character. A character that lands on another cross does not become a new target in the same activation.
- Settle normal arrival on a successful launch, then subtract 10% of the target's effective maximum HP from current HP, using the existing integer percentage rounding. With no legal landing, keep the target in place and apply the same HP loss and Attack effect. HP loss bypasses shields and numeric damage modifiers and uses the existing Rockfall-style lethal/Reconstruction settlement.
- Double a surviving target's effective Attack from the start through the end of its own next faction turn. The effect applies to that turn only.
- Treat eruption cells as warning and launch locations. Eruption publishes no blocking cells or standing structures, so Chain can cross the eruption cells.

**Attack decisions (2026-11-17, user approved the recommendations).**

1. Re-application refreshes. A target keeps at most one Eruption Attack status. A later activation replaces an unexpired window with its own window and never multiplies, so Attack never exceeds the configured multiplier. A short SO cycle makes this case reachable.
2. The multiplier applies before the intimidation decrease. `CombatEffectiveStatsCalculator.AttackPower` adds the supplies bonus to the effective Attack; apply the multiplier percentage and the existing intimidation decrease to that sum in one checked integer expression, so the result floors once (design section 6, Percent arithmetic).

**SO tuning.** The numbers in the confirmed rules are defaults. The stage's Eruption authoring section owns every value below; `LavaEruptionGimmickData` and `LavaEruptionGimmickSpec` carry them to the rule. The rule, adapter, and status code hold no fallback copy. The five-cell cross shape stays fixed, as design section 4.4 keeps topology separate from tunable amounts.

| Value | Default | Validation |
|---|---|---|
| Cycle in faction ends | 3 | At least 1 |
| Warning lead in faction ends | 2 | At least 1 and less than the cycle, so the first warning falls on a faction end (same rule as Meteor) |
| Minimum crosses per activation | 1 | At least 1 |
| Maximum crosses per activation | 3 | At least the minimum |
| Landing square radius in cells | 3 (7x7) | At least 1 |
| HP loss in percent of effective maximum HP | 10 | 1 to 100 |
| Attack multiplier in percent | 200 | More than 100 |
| Attack window length in own faction turns | 1 | At least 1 |

Warning and activation counts come from the cycle and lead: activation at every multiple of the cycle and warning `lead` faction ends earlier. The defaults give warning 1 and activation 3. With fewer valid sites than the minimum, skip the cycle; with fewer than the drawn count but at least the minimum, use the valid sites. Invalid authoring fails stage validation before battle start.

**Files:** Create the `LavaEruption` family from the file map. Extend the existing forced relocation/arrival, health evidence, applied status, faction-turn lifetime, effective Attack, and read/forecast paths. Tests: `T/LavaEruptionGimmickTests.cs`, `T/LavaEruptionBenefitTests.cs`, and `T/CoreIntegration/LavaEruptionPublicationTests.cs`.

**Interfaces:** `LavaEruptionGimmickState` owns warned centers and the schedule. The rule runs in `Scheduled` and `Warning`. Publish `CombatLavaEruptionEvent` with warning or cancellation cells, original recipients and their source crosses, resulting positions, and HP/Attack outcomes, including targets that stayed in place. Use `IStageGimmickStateChange` for state changes and `IEnvironmentRandomEvidence` for any draw. The temporary Attack modifier belongs to the recipient's combat statuses and is evaluated by `CombatEffectiveStatsCalculator.AttackPower`. Its lifetime needs a start bound (the recipient's next own faction start) as well as an expiry, because a target hit at its own faction end waits through the opposing faction's turn first. Existing `StatusEffectLifetimeKind` values define only an expiry. The status stores the multiplier percent and window length from the spec that granted it, so a later authoring change cannot alter an applied status.

- [ ] Write red cases for warning 1/activation 3, center and cardinal targets, one to three non-overlapping valid crosses, valid site shortage, platform rejection, and cancellation at the warned site.
- [ ] Write red landing cases for the 7x7 bound, full-footprint occupancy, exclusion of the source cross, and acceptance of a free tile on another cross. Pin one resolution for a target that lands on another cross. With no legal destination, assert original position, the same HP loss, and the same Attack window.
- [ ] Write red settlement cases for arrival before HP loss. Pin 10 HP lost with maximum HP 100 regardless of shield or numeric damage modifiers. Cover lethal loss/Reconstruction, and pin 2x effective Attack at the next own faction start and expiry at that faction end, including fixtures for both faction owners. For a target hit at its own faction end, pin normal Attack during the opposing faction's intervening turn, including a reaction attack in that turn. Pin both Attack decisions: a second hit refreshes the window and keeps 2x, and the multiplier with an intimidation decrease floors once.
- [ ] Write red tuning cases with test-owned values that differ from every default, for example cycle 4, lead 1, crosses 2-2, radius 2, HP loss 20%, multiplier 150%, and a two-turn window. Pin that each value reaches the outcome with no hidden default, and that every invalid row in the SO tuning table fails validation before battle start.
- [ ] Run the fixtures red. Capture the original distinct targets and source crosses before displacement. Check the latest projected occupancy for each destination and reuse the existing admission and arrival paths. Apply the same health and surviving-target status settlement to launched and stationary targets. Publish the accepted warning or activation state, RNG trace, and actual movement, health, and status evidence together.
- [ ] Register Eruption between Meteor and Chain in `Scheduled`. Verify that Chain crosses the eruption cells and resolves contact from the resulting character positions. Include a launch into active Meteor flames to exercise the existing arrival path and validate the event order and forecast freshness.
- [ ] Verify rejection of invalid warning, landing, health, and RNG evidence with no partial Buffer publication. Run the Eruption, shared placement/forced-arrival, Meteor, Chain, and affected health/status regression fixtures.
- [ ] **Phase 4 gate:** report and stop after verifying Eruption movement, the stationary case, HP loss and Attack lifetime, and the same-boundary interaction with Meteor and Chain.

## Phase 5 — Persistent stats and Devil's Altar

### Task 7: Extend owned Sub adjustments through the existing baseline and transaction owners

**Files:** Create `C/Contracts/Roster/PersistentStatAdjustment.cs`, `C/Contracts/Roster/GimmickBaselineStats.cs`, and `C/Application/Combat/StageGimmicks/PersistentGimmickStatsCalculator.cs`; modify `OwnedCharacterRecord.cs`, its copy/change paths, `CharacterBaselineStatsCalculator.cs`, `CharacterSettlementCalculator.cs`, `B/Character/CharacterBuffer.cs`, and `B/Core/BattleTransactions.cs` only where prepared-change validation requires it. Tests: `T/PersistentGimmickStatsTests.cs`, `T/CoreIntegration/PersistentGimmickStatsPublicationTests.cs`.

**Interfaces:** `PersistentStatAdjustment` is an immutable signed triple with `MaxHealth`, `Attack`, `Defense`, default zero; add it to owned Subs only, rejecting nonzero Main adjustment to avoid duplicating Main's owner. `GimmickBaselineStats` exposes validated `MaxHealth`, `Attack`, `Defense`. Shared calculator methods are only `GimmickBaselineStats Read(CombatCommandInput input, CharacterCombatFacts character)` and `CharacterChanges CreateChanges(CombatCommandInput input, CharacterCombatFacts character, GimmickBaselineStats finalBaseline)`. The caller determines the final baseline and owns the allocation or reward draw, cost, and consumption. Publication stays with the combat transition.

- [ ] Write red tests for baseline exclusion of soul/trait/equipment/position effects, Main versus Sub owners, copy/equality preservation, later growth, new battle, and new Run. Pin unchanged current HP after a maximum increase, `Assert.That(currentHp, Is.EqualTo(Math.Min(oldCurrentHp, finalEffectiveMaxHp)));`, and a Sub's next baseline equal to the newly grown original baseline plus the retained signed adjustment.
- [ ] Run red; preserve Main's existing persistent fields and settlement path. For Subs, add adjustment only in the shared baseline calculation used by both roster queries and battle entry. Audit every constructor/With/copy caller for preservation rather than creating another snapshot store.
- [ ] Validate/populate `CharacterChanges.Owned` or `.Main` together with the combat projection; clamp against final effective max HP and record normalization, not damage. New acquisitions/Run start reset the contribution; level gain and existing Hunter contributions do not.
- [ ] Add integration rejection proving invalid persistent data installs neither projection nor owned record. Run green with `CharacterBaselineStatsTests`, `OwnedCharacterStatsCalculatorTests`, `CharacterRosterContractTests`, and relevant existing Main growth/settlement fixtures.

### Task 8: Altar placement, one-use redistribution, and lasting application

**Files:** Create the `DevilsAltar` family and `C/Application/Combat/StageGimmicks/DevilsAltarRedistributionRules.cs`; extend Task 3 blocker projection. Tests: `T/DevilsAltarGimmickTests.cs`, `T/CoreIntegration/DevilsAltarPublicationTests.cs`.

**Interfaces:** `DevilsAltarGimmickState` owns placement IDs, centers, and consumption; the diagonal blockers are derived from those placements even after use. Rule stage: AfterCleanup, and only when `boundary.EndedSide` is `Owner.Player` (design section 5.3). AfterCleanup runs at every faction end, so at an enemy end the rule returns no transition and leaves the RNG unchanged, even when an owned character still stands on the center. `DevilsAltarRedistributionRules.Redistribute(GimmickBaselineStats before, int minHealth, int minAttack, int minDefense, ref uint randomState)` returns `GimmickBaselineStats`. This Altar-only policy consumes no combat/Buffer state and changes only the caller-local RNG. It is not a method of Task 7's shared calculator, and Contract must not depend on it.

- [ ] Write red tests for the 3x3 cross/pillar layout, owned-only center eligibility, exact pool preservation, minima, uniform valid triples, one-use behavior, pillars remaining after consumption, and an enemy-end boundary with an owned character on the center that changes nothing and keeps the RNG state. Pin `Assert.That(after.MaxHealth + after.Attack + after.Defense, Is.EqualTo(beforeTotal));` and minimum health 1/attack 0/defense 0.
- [ ] Run red; implement uniform allocation in `DevilsAltarRedistributionRules` using existing deterministic RNG and checked arithmetic. For a small pool, enumerate possible allocation outcomes to verify the mapping, not a flaky frequency assertion. Reject invalid configured minima/unsupported arithmetic before changing state. The Altar rule calls shared Read -> its own Redistribute -> shared CreateChanges; the shared calculator never calls back into Altar.
- [ ] Apply Task 7 changes and consumed state in one accepted transition after normal end cleanup. An enemy or a character moved off the center cannot consume it. Do not include temporary bonuses in the pool or heal merely because max HP increased.
- [ ] Verify publication, persistence into the next battle, later level growth, and stale/duplicate use rejection. Run the new fixtures and Task 7 regressions.
- [ ] **Phase 5 gate:** report and stop after Altar and shared persistent-stat verification.

## Phase 6 — Equivalent Exchange

### Task 9: Resolve Equivalent Exchange at player turn end

At the player faction's turn end, each unused one-cell circle automatically exchanges stats for a living owned player Main or Sub still on it after normal end cleanup and Altar settlement. The character pays the configured cost from its persistent baseline and receives one of the two configured rewards by a uniform random draw. Treat both rewards as equally likely (50% each). Each circle is usable once per stage.

| Circle | Cost | Random reward |
|---|---|---|
| Health Contract | Maximum HP -5 | Attack +3 or Defense +3 |
| Attack Contract | Attack -3 | Maximum HP +5 or Defense +3 |
| Defense Contract | Defense -3 | Maximum HP +5 or Attack +3 |

**Files:** Create the `EquivalentExchange` family and `C/Contracts/Commands/StageGimmicks/GimmickStatKind.cs`. Extend existing gimmick registration, read/result projection, and transition validation for the new settlement. Tests: `T/EquivalentExchangeGimmickTests.cs`, `T/CoreIntegration/EquivalentExchangePublicationTests.cs`.

**Interfaces:** `GimmickStatKind` exposes `MaxHealth`, `Attack`, and `Defense`. Each configured circle has one cost and two validated rewards. `EquivalentExchangeGimmickState` owns the placements and consumption. `EquivalentExchangeGimmickRule.TryAdvance` runs at `AfterCleanup` only when `boundary.EndedSide` is `Owner.Player`, and processes eligible circles by placement ID, then owned character ID. At an enemy end it returns no transition and draws nothing. `CombatEquivalentExchangeEvent` records the placement, recipient, actual cost, and drawn reward. Project the consumed flag and actual settlement facts for View through existing state and result publication.

- [ ] Write red cases for all three defaults, configurable amounts, post-cleanup eligibility, and insufficient baseline stats. Pin Defense 3 -> 0 with exactly one of Maximum HP +5 or Attack +3. With Defense 2, including a fixture with a temporary Defense bonus, assert zero stat changes, an unconsumed circle, and the original RNG state. Also pin that a dead, departed, enemy, or already-used case spends no RNG, and that an enemy faction end with an owned character still on the circle changes nothing.
- [ ] Write fixed-seed cases for both outcomes, replay from the same input, and multiple eligible circles. Pin one draw per settled circle in placement/owned ID order. Run the fixtures red.
- [ ] Register the rule after Altar at `AfterCleanup`. Check affordability using Task 7's baseline before drawing. The cost must leave Maximum HP at least 1 and Attack and Defense at least 0. Draw with `EnvironmentRandom.Choose(ref random, 2)` from the projected environment RNG, using the authored reward order.
- [ ] Publish the checked cost, drawn reward, Task 7 persistent/combat stat changes, HP normalization, consumed placement, actual exchange result, and updated RNG as one transition. Preserve current HP up to the new effective maximum. The result lasts for the Run and subsequent level growth still applies.
- [ ] Verify no state or RNG mutation on preview or rejected/stale publication and no second charge or draw from a consumed circle. When multiple Main/Sub circles settle on the same boundary, assert that each exchange changes only its occupant. Pin the normal next-faction transition and one end-count increment after all exchanges. Run both Phase 6 fixtures with the Task 7 publication and existing turn progression regressions.
- [ ] **Phase 6 gate:** report and stop after confirming automatic settlement and publication through the existing turn and result flow.

## Phase 7 — Integration, regression, and handoff

### Task 10: Close shared-contract coverage and publish the Core handoff

**Files:** Create `T/NewStageGimmickIntegrationRulesTests.cs`, `T/CoreIntegration/NewStageGimmickIntegrationTests.cs`, and `docs/superpowers/handoffs/2026-11-17-new-stage-gimmicks-core-view-handoff.md`; modify only the owning new code if integration reveals a defect. Do not refactor unrelated systems during this task.

**Interfaces:** Deliver the state/read/event contracts as implemented. The handoff must name exact public types and fields, warning clocks, presentation acknowledgement behavior, and the published Contract cost, reward, and consumption.

- [ ] Write cross-feature tests for the approved order without Smog's step, exact terminal cutoff, source expiry, and repeated preview purity. Verify Meteor -> Eruption launch and settlement -> Chain on the resulting board, including Chain passage through Eruption cells. Verify Altar -> automatic Contract exchanges -> warnings before normal next-faction progression and one end-count increment for the boundary.
- [ ] Run the new fixtures red; trace each discrepancy to its owning rule/transaction and fix there. Verify placement/forced/random landing all exclude a head, attacks remain unchanged, invalid warned sites never retarget, and old stages load with disabled/absent new sections.
- [ ] Run the full Core assembly and all new CoreIntegration fixtures plus the relevant existing authoring, state publication, command/advance, persistent-stat, and movement-undo fixtures named above. Record test counts, fresh XML/log paths, and any blockers; no claim of View/PlayMode or unselected CoreIntegration coverage.
- [ ] Write the handoff with the five confirmed configuration sections, state/events, Eruption warnings and resulting positions with their HP/Attack outcomes, automatic Contract result display through the existing playback flow, scene/UI owner responsibilities, source-expiry behavior, and manual Unity acceptance scenarios. State that Smog is on hold and, if confirmed, arrives as an addendum to this handoff. No external sending. If the existing mandatory View/Core boundary handoff must be edited later, update its title/date as required by AGENTS; this task does not silently edit it.
- [ ] **Phase 7 gate:** self-review SSOT/SOLID/FW and actual edit/understanding/regression scope against design 9.1-9.2. Walk three changes: a stage's Eruption cross count, Altar allocation, and a stage's Meteor percentage. Check that feature formulas remain local, shared contracts retain their validation, and normal registration/schema edits are not hidden by auto-discovery or global access. Keep required regression gates even when a local edit needs little unrelated knowledge. Inspect the final diff for production asset/FMOD/unrelated changes, report the result, and stop. No commit/push.

## Phase 8 — Volcanic Smog (on hold)

The planners have not confirmed Smog. Do not start this phase until the user confirms the rule; if Smog is cancelled, delete this phase. The rule below is the 2026-11-17 design (section 5.1) and may change with the confirmation.

### Task 5 (on hold): Smog state, stacks, common damage hooks, and publication

**Before starting.** Recheck design sections 4.2, 5.1, and 6 against the confirmed rule. Answer these questions, which arise because Phases 4-7 now land first:

1. Does Eruption's direct HP loss count as actual HP loss for Smog execution? Amplification already excludes it.
2. Does Altar or Contract maximum-HP normalization stay outside execution? Design section 4.2 says normalization does not qualify; confirm this for persistent stat edits.
3. Does a Chain push or Eruption landing into the Smog band change anything before the ending faction's own stack settlement?

**Files:** Create the `VolcanicSmog` family from the file map and `C/Contracts/CombatEffects/VolcanicSmogStatus.cs`; modify `StageGimmickKey.cs`, `StatusEffectKey.cs`, `AppliedStatusRules.cs`, `CombatDamageApplication.cs`, `StatusEffectReadModel.cs`, and gimmick evidence/read projections. Tests: `T/VolcanicSmogRuleTests.cs`, `T/VolcanicSmogDamageTests.cs`, `T/CoreIntegration/VolcanicSmogPublicationTests.cs`.

**Interfaces:** `VolcanicSmogGimmickState` owns the active band, pending relocation, and next warning/activation boundaries. `VolcanicSmogStatus` owns `Stacks` and stage lifetime. `VolcanicSmogGimmickRule.RunsAt` selects Scheduled, EndOccupancy, Warning. Add pure rule entry points `int Amplify(int damage, VolcanicSmogStatus status, int multiplierPercent)` and `bool ShouldExecute(int actualHpLoss, int currentHp, int maxHp, bool insideSmog, int thresholdPercent)` on `VolcanicSmogDamageRules` in `C/Application/Combat/StageGimmicks/VolcanicSmogDamageRules.cs`. Supply forecast impact through the per-rule mechanism from D2 instead of adding Smog fields to the shared forecast loop.

- [ ] Write red cases for initial three-row band, relocation at 5 before own-side stacks, retained/capped stacks outside, and every numeric damage kind. Pin `Assert.That(Amplify(15, thresholdStatus, 200), Is.EqualTo(30));`, execution false at HP 10/100, true at 9/100 only after positive HP loss, and false after shield-only loss or normalization.
- [ ] Run the two Core fixtures red; implement typed state/authoring and defaults from spec 4.4. Construct the initial band and schedule in `VolcanicSmogGimmickRule.Initialize`, using existing RNG and sorted eligible bands; do not loop when the only band is the current one. Composition needs registration, not a Smog-specific constructor branch. Add warning publication and read-only forecast handling in this task.
- [ ] Insert amplification once after existing exposure and before item protection; insert execution after real HP loss and before post-hit healing. Keep Smog's arithmetic/eligibility in its rule helper; the damage owner retains protection, HP, Reconstruction, evidence, death, and reward processing. Ordinary lethal/Reconstruction handling must not execute again. Add copy/equality/status-read/cleanup support for the new field without recreating night return. Do not create a second damage pipeline to isolate Smog.
- [ ] Add test-owned SO conversion, accepted event publication, and forged/missing-state-evidence rejection in `VolcanicSmogPublicationTests`; run green with `EnvironmentDamageTests`, `StatusEffectContractTests`, and `CoreIntegration.StageGimmickStatePublicationTests`.
- [ ] Register Smog relocation ahead of Meteor in `Scheduled`. Add a same-boundary case where the character reaches stack 3 after impact and the earlier Meteor hit keeps its original damage. Add the cross-feature cases Task 10 left out: Smog with every numeric damage family, the full approved order including Smog's step, and the answers to the questions above. Run the Meteor, Eruption, Chain, Altar/Contract, and Task 10 integration fixtures alongside the Smog fixtures.
- [ ] Add Smog's configuration section, state, events, and acceptance scenarios to the Task 10 handoff as an addendum, and update its title and date.
- [ ] **Phase 8 gate:** report and stop after verifying Smog, its damage hook, and its interactions with the finished gimmicks.

## Spec coverage and planning verification

| Design area | Owning task(s) |
|---|---|
| Scope, SO inputs, disabled compatibility | Every mechanic task and 10 |
| Approved clocks, rule-owned initialization, and complete end order | 1, priority composition in 4, composition without Smog in 10, Smog's step in 5 (on hold) |
| Smog stacks, amplification, execution, exact HP evidence | 5 (on hold) |
| Shared Fire and Meteor/flames | 2 |
| Shared occupancy and attack paths | 3-4 with the Altar pillar projection in 8 |
| Eruption 7x7 launch, stationary settlement, direct HP loss, temporary Attack increase, and Chain pass-through | 6 and cross-feature coverage in 10 |
| Chain heading warning, lateral priority, transient occupancy, survival | 4 |
| Persistent stat ownership, growth, normalization | 7 |
| Altar-owned distribution and one-use footprint; shared baseline/write contract only | 7-8 |
| Automatic Contract settlement, random reward, stale rejection, and normal turn progression | 9-10 |
| Defaults, edge cases, publication evidence, forecasts, compatibility | Owning mechanic task and cross-feature coverage in 10 |
| Supported manual return and movement undo within existing contracts | 2-7 and 10 |
| View handoff | Meteor and Chain in the Phase 3 report, the five confirmed gimmicks in Task 10, Smog as an addendum in Task 5 (on hold) |

- Planning and the subsequent user-requested locality review change only this plan and its design. The review refined initializer ownership, Altar-only policy placement, and operational boundary/tuning contracts without changing approved gameplay or runtime state owners.
- The 2026-11-17 Contract correction replaces the player-choice requirement with automatic random settlement. Task 9 supplies the current rule, and Task 10 covers its order and result handoff.
- The 2026-11-17 priority revision schedules shared turn-end processing, Meteor, and occupancy with Lava Chain first. Tasks 5-10 cover the deferred gimmicks and full integration.
- The 2026-11-17 Eruption revision records the user-confirmed five-cell cross, source-cross landing exclusion, stationary settlement, direct HP loss, and next-own-turn Attack effect in Task 6.
- The 2026-11-17 Smog hold moves Task 5 to Phase 8 because the planners have not confirmed Smog. Eruption, Altar, Contract, and integration become Phases 4-7. Task numbers and anchors are unchanged.
- Current design choices are not a runtime verification result. No tests, builds, Unity launch, or product implementation occurred while writing this plan.
- Before any execution, review the Phase to be authorized. Each Phase has its own stop/report even when one execution approach is used for the whole effort.
