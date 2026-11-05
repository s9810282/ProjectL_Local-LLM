# Stage Gimmick System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for direct execution, or superpowers:subagent-driven-development if the user selects delegated execution. Implement task-by-task only within the explicitly authorized Phase. Steps use checkbox (`- [ ]`) syntax for tracking. **Do not commit or push. Stop and report at every Phase boundary.**

**Goal:** Add configurable Rockfall and Haboob through a common progression contract, preserving existing Railway behavior and defining a future attachment boundary for Tornado, now user-owned and not yet implemented.

**Architecture:** Existing Combat/Character Buffers remain the runtime sources. Pure feature calculators produce immutable transitions through the existing controller-end, settlement, publication, and presentation-acknowledgement path. Explicit composition selects rules; it contains no feature geometry or effect formulas.

**Tech Stack:** Unity 6000.0.73f1, existing C# Core.Contracts/Core.Domain/Core.Application/Systems assemblies, existing ScriptableObject authoring, NUnit EditMode tests, existing Buffer transactions and CombatPlan/Jobs. No new package or runner.

**Spec:** [Stage gimmick system design](../specs/2026-10-31-stage-gimmick-system-design.md), decisions U1-U14 and closed decision records O1-O6.

**Status:** Phases 0-3 (common progression, preserved Railway, Rockfall and Haboob) are implemented and verified as recorded below. On 2026-11-01 the user confirmed they will also implement Tornado; its planned gameplay remains unimplemented. The user explicitly discarded the premature Phase 4 handoff. That document is deleted, P4.1 is withdrawn, and no handoff should be regenerated without a new request. View remains outside the user's Core ownership. Earlier completion/Phase 4 records below are historical and superseded by this decision. No Tornado implementation is authorized by the discard request.

## Global Constraints

- Unity version: `6000.0.73f1`. Use the established AppData-authorized execution route. Do not upgrade Unity or delete caches to work around the known sandbox restriction.
- Documents are English; added code comments/XML documentation are Korean. Use block namespaces matching folders, Allman braces, four spaces, LF, UTF-8, and `sealed` by default. New/moved C# files include `.meta`; preserve existing GUIDs.
- Do not commit/push, delete legacy Missing Script assets, or modify FieldCamera/Architecture. Coordinate shared files and Tornado ownership before editing them. Scope changes require a separate decision.
- Do not add Unity/Framework/Buffer implementation dependencies to pure Core. Existing Systems-to-Application references suffice; no assembly-dependency changes are planned.
- One completed player-side end or enemy-side end adds 1. Preserve the existing terminal enemy/Tornado early-return exception. No counters advance on queries, previews, unit actions, deployment, or undo.
- Order: legacy Tornado -> Railway -> Haboob -> Rockfall -> existing cleanup. Stop later rules after terminal outcome. No duplicate Tornado hook.
- Defaults: Rockfall cooldown `2`, warning `2`, count `3..5`, maximum-HP removal `20%`, Bind `1` affected-side opportunity; Haboob relocation interval `2`, warning lead `1`, default twelve-cell cross. SO controls tuning values.
- Rockfall positions are locked at warning. Direct HP removal floors once, has no minimum-1 correction, ignores mitigation, can kill, and uses existing settlement. Existing reserve-return cleanup takes precedence over Bind duration; add no night-specific persistence.
- Haboob blocks cross-boundary primary-cell designation, including empty cells, but allows indirect directional/All/splash hits. No full path or future position is revealed.
- States, RNG, warnings, and effects publish together through existing transactions. No second persistent Core world, per-feature runtime Buffer, global RNG, or display-owned gameplay timer.
- Keep new production gimmick configurations disabled until their View consumers are ready. Core-only test fixtures may enable them earlier; that is not playable-content acceptance.

## Review Focus

1. **A reused constructor resets a new state member:** pending warning/RNG must survive unrelated movement, attack, preview, and undo; stale publication must fail atomically. Covered by P1.2/P2.2.
2. **No valid cells or fewer than the configured minimum:** finish the cycle without rerolls, duplicate hits, or a stalled turn; occupied walkable cells remain candidates. Covered by P2.1.
3. **New Bind applied just before cleanup or recall:** preserve one future field opportunity, but retain normal Hand/EnemyWaiting cleansing and unchanged trait Bind behavior. Covered by P2.2.
4. **The aim is legal but indirect recipients cross the cloud:** direct range highlighting, preview, execution, and AI must agree without filtering legitimate splash/directional damage. Covered by P3.2.
5. **A typed event publishes but cannot be consumed:** evidence readers must accept it with meaningful evidence, reattachment must restore current state, and repeated playback/Job ticks must not rerun gameplay. Covered by P2.1/P3.1/P4.2.

## File Map and Interface Decisions

Paths below are repository-relative. To keep task references readable, these exact directory aliases are used throughout:

| Alias | Exact directory |
|---|---|
| `C` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator` |
| `S` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem` |
| `B` | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems` |
| `D` | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat` |
| `V` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat` |
| `T` | `Assets/Tests/EditMode` |
| `TI` | `Assets/Tests/EditMode/CoreIntegration` |

New filenames/interfaces are planned names, not claims about current APIs. Each path expands by replacing its first alias with the table's exact directory. Files sharing a row are one closely related contract family, not independent mandatory layers.

| Create / modify | Responsibility |
|---|---|
| Create `C/Application/Combat/StageGimmicks/IStageGimmickRule.cs` | Pure progression contract; no mutable retained battle state. |
| Create `C/Application/Combat/StageGimmicks/StageGimmickComposition.cs` | Explicit rule registration/order and preparation routing. |
| Create `C/Application/Combat/StageGimmicks/RailwayGimmickRule.cs` | Extract existing Railway calculation without changing semantics/events. |
| Create `C/Application/Combat/StageGimmicks/RockfallGimmickRule.cs` | Cooldown/warning/impact and direct-HP/Bind orchestration. |
| Create `C/Application/Combat/StageGimmicks/HaboobGimmickRule.cs` | Initial area, warning, relocation, footprint candidates. |
| Create `C/Application/Combat/StageGimmicks/HaboobTargetingRules.cs` | Stateless primary-cell membership restriction, reused by queries/execution. |
| Create `C/Contracts/Combat/StageGimmickState.cs` | Immutable collection/base plus typed Rockfall/Haboob states and boundary value. |
| Create `C/Contracts/Combat/StageGimmickEvents.cs` | Typed Rockfall/Haboob event payloads and their small event-step enums. |
| Create `C/Contracts/Commands/StageGimmicks/RockfallGimmickSpec.cs` and `HaboobGimmickSpec.cs` | Validated prepared numeric/mask definitions. |
| Create `D/RockfallGimmickAuthoring.cs`, `D/RockfallGimmickData.cs`, `D/HaboobGimmickAuthoring.cs`, `D/HaboobGimmickData.cs` | Serializable authoring records and separate pure immutable Data; no runtime state. |
| Modify `C/Application/Combat/CombatGimmickCalculator.cs`, `CombatTurnCalculator.cs`, `BattlePreparationCalculator.cs`, `CombatCommandComposition.cs` | Common hook and state preparation/composition. No feature formulas in the coordinator. |
| Modify `C/Contracts/Combat/CombatCommandState.cs`, `CombatChangeEvidenceReader.cs`, `CombatEvent.cs`; `C/Contracts/Common/StageGimmickKey.cs` | State preservation/equality and explicit event/kind support. Append values without renumbering existing enums. |
| Modify `D/CombatStageData.cs`, `CombatStageDetail.cs`; `S/BattleDefinitionAdapter.cs` | Typed authoring wiring and prepared definition conversion; retain serialized Railway fields. |
| Modify `B/Combat/CombatStateData.cs`, `CombatStateBuffer.cs` | Approved read exposure and integrity validation through the existing store. |
| Modify `C/Contracts/CombatEffects/EffectOwnerKind.cs`; existing origin/status readers only where required | Append stage-gimmick provenance; do not fake a trait/character or replace Bind. |
| Modify `C/Application/Combat/CombatAttackQueries.cs`; `C/Contracts/Results/AttackFailureReason.cs` | One direct-designation admission rule and consistent selectable-cell projection. |
| Conditional owner work: `V/Playback/Ordering/CombatEventTypeTable.cs`, `V/Playback/Handlers/CombatFactEventHandler.cs`, existing display composition | Register new events, restore state, and connect feature-specific presentation. Detailed owner tasks are P4.1/P4.2; Core phases do not edit these by default. |

### Stable interfaces used by the tasks

```csharp
// Proposed signatures; implementation comments must be Korean.
internal interface IStageGimmickRule
{
    StageGimmickKey Key { get; }
    CombatCommandTransition TryAdvance(CombatCommandInput input,
        StageGimmickSpec definition, StageGimmickId id,
        StageGimmickBoundary? boundary);
}

public readonly struct StageGimmickBoundary
{
    public Owner EndedSide { get; }
    public long CompletedControllerTurnCount { get; }
    public StageGimmickBoundary(Owner endedSide, long completedControllerTurnCount);
}

// Existing one-argument entry stays source-compatible; one registry executes both paths.
public static CombatCommandTransition TryAdvance(CombatCommandInput input);
public static CombatCommandTransition TryAdvance(CombatCommandInput input,
    StageGimmickBoundary boundary);

internal static CombatCommandState Initialize(CombatCommandState state,
    IReadOnlyList<StageGimmickSpec> definitions); // StageGimmickComposition

internal static bool CanDesignate(IReadOnlyList<CellCoord> area,
    CellCoord attackerCell, CellCoord primaryCell); // HaboobTargetingRules
```

`null` boundary is allowed only for the existing no-definition/Railway compatibility calls, whose behavior does not require an ended-side identity. New scheduled rules require the explicit validated boundary. The existing overload forwards into the same coordinator; it does not retain a second Railway algorithm. Production S1 always supplies the boundary. Current one-argument callers include Railway and attribution tests, so removing this signature is not a routine cleanup.

`StageGimmickStateSet` supplies immutable `Entries`, `Empty`, `Get(StageGimmickId)`, `Replace(StageGimmickState)`, `Without(StageGimmickId)`, and `SameValues(StageGimmickStateSet)`. `Get` requires an existing prepared state; absence is a configuration error, not lazy initialization. `Replace` cannot silently insert an unknown kind/ID. It is an immutable contract value, not a mutable store/service.

`RockfallGimmickState` contains `Id`, phase `Cooldown/Warning`, `NextBoundaryCount`, and `WarnedCells`. Warning may contain zero cells; therefore phase is meaningful and is not inferred from cell count. `HaboobGimmickState` contains `Id`, phase `Active/RelocationWarning`, `NextBoundaryCount`, and `Cells`. Do not store a second anchor/current-area source. Both implement value equality and immutable collection copying. Stable IDs follow existing prepared-definition identity; extracting Railway must not renumber it on Railway-only stages.

Specs expose: Rockfall `CooldownControllerTurns`, `WarningControllerTurns`, `MinimumRockCount`, `MaximumRockCount`, `MaximumHealthLossPercent`, `BindAffectedSideTurns`, `FootprintOffsets`; Haboob `RelocationIntervalControllerTurns`, `WarningLeadControllerTurns`, `FootprintOffsets`. Constructors validate the bounds in the spec. Normalized unique masks start at minimum x/y zero and are nonempty. Default masks remain 1x1 Rockfall and twelve-cell Haboob.

`CombatRockfallEvent` carries step `Warning/Impact`, typed expected/final state, and impact recipient IDs. `CombatHaboobEvent` carries step `Warning/Relocation` and typed expected/final state. Explicit steps disambiguate a relocation that retains the same area or immediately starts another warning. Append their event kinds; leave `CombatGimmickEvent`/Railway payload compatibility intact. Impact evidence must match the cell set and related character changes; zero-loss surviving victims can still receive Bind.

Terminal Rockfall impact clears its pending warning without scheduling another cooldown: remove that entry through `StageGimmickStateSet.Without(StageGimmickId)`, and allow the impact event's final Rockfall state to be null only for a terminal result. Integrity checks require the matching terminal outcome for this removal. Other gimmick facts need no special night/terminal timer; the finished combat gate prevents progression and normal EndBattle clears the battle. View clears remaining gimmick visuals on the existing terminal/exit lifecycle.

## Verification Commands and Evidence

Do not execute tests while merely editing this plan. During implementation, run Unity through an AppData-authorized process. The executable's install path was not found at the conventional Program Files path during planning; Phase 0 must resolve the actual installation instead of copying an invented path. If the project is open, use the Editor's Test Runner rather than launching a second Editor on the same project.

**V1 — targeted test cycle:** In Unity Test Runner, select EditMode, filter by the class named by the task, and Run Selected. For an available batch Editor, use the equivalent template below after setting `$gimmickUnityExe` to its verified absolute path and `$gimmickTestFilter` to that exact class. Run from the project root, with no concurrent Editor holding the project. Output artifacts go to a newly created directory under `$env:TEMP`.

```powershell
$gimmickResultDir = Join-Path $env:TEMP ('projectl-gimmicks-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $gimmickResultDir | Out-Null
& $gimmickUnityExe -batchmode -projectPath (Get-Location).Path -runTests -testPlatform EditMode -testFilter $gimmickTestFilter -testResults (Join-Path $gimmickResultDir 'results.xml') -logFile (Join-Path $gimmickResultDir 'editor.log')
```

Verify the process completion, the produced XML contains the intended tests, and its result is passed with no failures. A missing/empty XML, compile error, timeout, or pre-code cache failure is not a passing test. For red tests, verify the intended missing behavior, not an unrelated setup failure. Run newly added source through Unity import so `.meta`/assembly inclusion is checked.

**V2 — Phase regression:** Run full `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` assemblies through Test Runner; the batch equivalent replaces `-testFilter` with `-assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration'`. When authoring/assets change, include `DataBaseAssetTests`. Record pre-existing failures separately from new failures using the current Phase-0 baseline; do not assume historical counts or hide failures by changing production data.

**V3 — source/diff review:** `git diff --check`, `git status --short`, and inspect the complete Phase diff for ownership, accidental asset changes, state copies, and changed public contracts. No git add/commit/push. No claim of View/PlayMode acceptance from V1/V2.

## Phase Overview

| Phase | Tasks | Deliverable / stop gate |
|---|---|---|
| 0 | P0.1 | Current baseline, ownership map, closed behavior contract, valid test route. Merged into Phase 1 preflight by the execution request. |
| 1 | P1.1-P1.2 | Common rule composition, unchanged Railway, Buffer-owned new state carried safely. Stop and review. |
| 2 | P2.1-P2.3 | Rockfall logic, existing Bind reuse/direct HP removal, SO input and real publication. Stop and review. |
| 3 | P3.1-P3.3 | Haboob progression/direct designation, SO input and query/AI/execution agreement. Stop and review. |
| 4 | P4.1-P4.2 | Core-to-owner handoff, authorized display/content integration, combined acceptance. Stop; external incomplete items stay explicit. |

## Phase 0 — Freeze Execution Context

### P0.1: Verify the baseline and ownership boundary

**Files:** read repository `AGENTS.md`, `Assets/AGENTS.md`, the spec, the source map above, and the two existing test asmdefs. Record findings in this plan's execution record; no product-code changes.

**Interfaces:** consumes U1-U14/O1-O6; produces a current branch/worktree/source baseline, verified Unity test route, and a shared-file/owner list for later tasks.

- [x] Recheck `git status --short`, `git branch --show-current`, and `git rev-parse HEAD`; preserve other work and identify edits since the planning baseline.
- [x] Confirm ownership of `CombatTurnCalculator`, preparation/Contracts/Buffer files, the current Tornado hook, and View consumers. The external Tornado's implementation remains out of scope.
- [x] Resolve the actual Unity 6000.0.73f1 executable or active Editor. Use the approved AppData route; run V2 and retain XML/log evidence as the implementation baseline.
- [x] Confirm the terminal enemy/Tornado exception, existing reserve cleansing, and new-event rejection in `CombatChangeEvidenceReader` still match the spec.
- [x] Report baseline failures and ownership boundaries, then continue directly into Phase 1 as explicitly requested on 2026-10-31. The original separate Phase 0 review gate is superseded for this execution.

## Phase 1 — Common Progression and Canonical State

### P1.1: Extract Railway behind the common contract

**Files:** create the interface/composition/Railway files in the map; modify `CombatGimmickCalculator.cs`, `CombatTurnCalculator.cs`; tests `T/RailwayGimmickTests.cs`, new `T/StageGimmickProgressionTests.cs`. Add the boundary value in `C/Contracts/Combat/StageGimmickState.cs`.

**Interfaces:** produces the two `TryAdvance` overloads and `IStageGimmickRule` contract above. Production passes `new StageGimmickBoundary(endedSide, projectedCount)` at the existing end-gimmick hook; `EndedSide` is Player/Enemy and the count must match the immutable input. Registry registration is explicit, unique, and ordered.

- [x] Add tests `RegistryRejectsDuplicateAndMissingHandlers` and `ProductionEndPassesOneBoundaryWithoutIncrementingAgain`; retain all existing Railway public-entry tests. Assert the boundary count equals `FinalState.Turn.CompletedControllerTurnCount`, not count + 1.
- [x] Run the combined V1 state/progression cycle; confirm the new state behavior is red for the intended reason. Registry/boundary checks use the extraction regression coverage without artificial failing stubs (essential-verification execution adjustment).
- [x] Move S2's Railway logic into `RailwayGimmickRule`; keep existing invalid-rail checks, timing, empty attribution, event/death adjacency, and common settlement. Implement one coordinator using projection/merge, with no second runtime world.
- [x] Pass the boundary from S1 without moving legacy Tornado, status cleanup, regeneration, or recall. Preserve the early-terminal enemy path. No Haboob/Rockfall handlers are enabled until their tasks exist; configured unknown kinds fail preparation.
- [x] Run V1 for `StageGimmickProgressionTests` and `RailwayGimmickTests`. Assert identical Railway victim/death/order/reward outcomes and no later handler after terminal outcome.

### P1.2: Carry immutable gimmick facts through real transactions

**Files:** create the typed state/collection and both spec files; modify `CombatCommandState.cs`, `CombatCommandComposition.cs`, `BattlePreparationCalculator.cs`, `B/Combat/CombatStateData.cs`, `CombatStateBuffer.cs`, and `StageGimmickKey.cs`. Tests: new `TI/StageGimmickStatePublicationTests.cs`; extend `T/CombatCommandInputCopyTests.cs` where appropriate.

**Interfaces:** produces `CombatCommandState.Gimmicks` and its optional constructor/`With` input, the state/spec fields listed above, and `StageGimmickComposition.Initialize(state, definitions)`. Existing calls default to `StageGimmickStateSet.Empty`; all copy paths preserve it. Prepare one state per enabled supported kind, once; Railway needs no extra state.

- [x] Add `UnrelatedCommandPreservesWarningFacts` and `StaleGimmickStateRejectsWithoutPartialCharacterWrites`. Supply typed preparation fixtures directly to the real Buffer path while feature handlers are not yet enabled; this proves storage/publication, not production feature initialization. Assert warning cells/deadline, HP, RNG, and result sequence are unchanged on rejection.
- [x] Run V1 for `StageGimmickStatePublicationTests`; verify a behavioral failure rather than a fabricated fake-store success.
- [x] Extend state constructors/equality/composition/read exposure and integrity checks. Audit explicit and target-typed `CombatCommandState` constructors, especially S1's `State` helper. Preserve the existing query-revision/prepare-install mechanism.
- [x] Validate unique kind/ID, immutable input collections, state-kind agreement, legal phase/deadline/cells, and rejected transitions. Do not make Buffer pick candidates, recompute damage, or own phase rules.
- [x] Run V1, then V2/V3. Exercise movement/attack/preview/undo preservation and unchanged Railway-only preparation. **Stop after Phase 1** with the changed-file list, test evidence, and any public-contract changes.

## Phase 2 — Rockfall

### P2.1: Commit warning selections and repeat the cycle

**Files:** create `RockfallGimmickRule.cs`, the Rockfall event in `StageGimmickEvents.cs`; modify `StageGimmickComposition.cs`, `CombatChangeEvidenceReader.cs`, and `CombatEvent.cs`. Tests: new `T/RockfallGimmickTests.cs` and event cases in `T/CombatChangeEvidenceVariantTests.cs`/`CombatChangeEvidenceCoverageTests.cs`.

**Interfaces:** `RockfallGimmickRule.TryAdvance` implements the common contract. Preparation creates cooldown at `completedCount + CooldownControllerTurns`. `NextBoundaryCount` means next warning while Cooldown and impact while Warning. The warning event carries the committed selected cells; impact uses that state and never samples again.

- [x] Add `DefaultCycleWarnsAt2And6AndImpactsAt4And8`, `PreviewDoesNotConsumeSelectionOrRng`, `OccupiedWalkableCellsRemainCandidates`, `ShortageUsesAvailableDistinctCells`, and `ZeroCandidatesStillCompletesCycle`. Assert exact stage/deadline/cell identity and RNG stability at impact.
- [x] Run V1 for `RockfallGimmickTests`, confirming red behavior.
- [x] Implement stable candidate enumeration using existing wall/cliff semantics and blocking structures; sample the count uniformly in the inclusive SO range and anchors without replacement using existing environment RNG. Use the available count if fewer exist. Preserve the warned cell union even after terrain changes.
- [x] Register the rule and typed events. Add meaningful evidence handling before publishing new events; keep unknown-event failures strict. Test overlap deduplication for authored masks and matching event/state transitions, including an empty warned set.
- [x] Run V1 for feature/evidence tests. Verify no persistent rubble, no query mutation, and no independent counter/seed.

### P2.2: Apply direct HP removal and reuse Bind

**Files:** modify `RockfallGimmickRule.cs`, `EffectOwnerKind.cs`, and origin/read-model validation only where needed. Reuse `CombatCommandCalculator.Hurt/Finish`, `CombatCommandEvents`, `AppliedStatusRules`, `CombatActionAdmission`, and `TraitTurnLifecycle` without rewriting their policies. Tests: new `T/RockfallEffectTests.cs`, `TI/RockfallPublicationTests.cs`; existing `AppliedRestrictionStatusTests`, `CombatEffectOriginTests`, `StatusEffectPublicationTests`, `ReserveReturnPublicationTests`.

**Interfaces:** impact is the same transition as state/RNG progression; new provenance is `EffectOwnerKind.StageGimmick` with a stable gimmick ID, definition kind, and constant Rockfall Bind rule key. Surviving unique field victims receive existing `ActionRestrictionStatus(StatusEffectKey.Bind, origin, remainingEnds)`.

- [x] Add parameterized tests for both owners and both ended sides. For configured duration 1, assert the installed surviving field status has one affected-side end remaining; opposite ends preserve it and the next own end clears it. Assert attack remains available and existing forced movement rules are unchanged.
- [x] Add HP cases: max 100 removes 20 despite defense/shield/reduction; max 7 removes 1; max 4 removes 0; current HP 5/max 100 dies. Assert shield budget unchanged, one hit per victim, zero-loss survivors still receive Bind, and no fabricated attack-credit reward.
- [x] Run V1 to confirm failures, then implement `floor((long)MaxHealth * percent / 100)` with HP clamping through the existing helper. Apply all direct effects and use existing shared settlement once; stop later rules on terminal outcome.
- [x] Grant `configuredTurns + (victim.Owner == boundary.EndedSide ? 1 : 0)` before existing cleanup. Retain existing replacement and reserve/death cleanup. Add `ReturnCleansesBeforeAnOpportunityWithoutHiddenState`, including automatic recall through the actual path; do not introduce a night branch in Rockfall.
- [x] Run V1 for new and named existing effect/publication tests. Verify stale/repeated publication cannot duplicate HP removal, Bind, death, or rewards, and the status read model reports a non-trait source correctly.

### P2.3: Expose Rockfall tuning through stage SO data

**Files:** create `D/RockfallGimmickAuthoring.cs`, `RockfallGimmickData.cs`; modify `CombatStageData.cs`, `CombatStageDetail.cs`, `S/BattleDefinitionAdapter.cs`, and preparation composition. Tests: new `TI/StageGimmickAuthoringTests.cs`; existing `TI/DataBaseAssetTests.cs`. Do not enable production stage assets yet.

**Interfaces:** authoring `ToData()` returns a new immutable `RockfallGimmickData`; the adapter converts it to `RockfallGimmickSpec`. Use an authoring `enabled` field defaulting to false; disabled authoring returns null Data, rather than relying on Unity inline-serialized object nullability. Malformed enabled settings fail validation. All tuning fields use the names in the stable interface section.

- [x] Add `RockfallAuthoringChangesReachPreparedSpec` with non-default cooldown 3, warning 1, count 1..2, HP loss 35, Bind 2; assert actual warning/impact timing and effects use those settings. Add invalid range/mask/duration tests before state installation.
- [x] Run V1 red, implement typed authoring/conversion and initial state after existing environment preparation, then run V1 green. SO edits must not alter an already prepared battle.
- [x] Run V2/V3, including unchanged existing DB assets. **Stop after Phase 2**; report Rockfall Core/SO support separately from missing View/content acceptance.

## Phase 3 — Haboob

### P3.1: Prepare and relocate the canonical cloud area

**Files:** create `HaboobGimmickRule.cs`; add Haboob event/evidence support to the common event files; modify composition initialization/registration. Tests: new `T/HaboobGimmickTests.cs`, `TI/HaboobPublicationTests.cs`, existing evidence coverage tests.

**Interfaces:** preparation consumes the existing environment RNG after existing initial-environment preparation, returning a command state with one canonical Haboob cell set. Rule `TryAdvance` publishes warning/relocation and next deadline. With interval 2/lead 1, warn at count 1 and relocate at 2, then 3/4; do not sample a future position on warning.

- [x] Add `DefaultFootprintHasRows2_4_4_2`, `BlockedCellsDoNotExcludeHaboob`, `FullMaskMustFit`, `OnlyCandidateKeepsArea`, and `WarningDoesNotRevealOrSampleDestination`; assert a default 4x4 board has exactly one candidate and a board too small fails preparation atomically.
- [x] Run V1 red, implement full-mask candidates and stable RNG selection, then test new area/phase/deadline publication. Exclude current position when another exists, but allow wall/cliff/obstacle overlap without inserting Haboob into ordinary terrain-source validation.
- [x] For `lead == interval`, initialize in warning phase and keep the next warning active immediately after relocation. Publish a relocation event even when the same area is retained; no extra per-tick countdown or duplicate activation.
- [x] Run V1 for progression/publication/evidence tests. Assert unrelated character movement changes no Haboob state and no displayed area source is separately mutable.

### P3.2: Share direct-designation validation across queries and execution

**Files:** create `HaboobTargetingRules.cs`; modify `CombatAttackQueries.cs` and `AttackFailureReason.cs`; execution/AI callers change only if necessary to consume the existing shared query correctly. Tests: new `T/HaboobTargetingTests.cs`; existing `T/StageEnemyAiTurnTests.cs`.

**Interfaces:** `CanDesignate(area, attackerCell, primaryCell)` returns equal area membership. Add proposed failure reason `HaboobDesignationBlocked`. `Selection` checks only a primary-cell aim; `Range`/`UnitRange` expose legal directly selectable cells without deleting indirect coverage. AI keeps using `Range.AttackAims`/`DeliversTo`.

- [x] Add tests for outside->inside and inside->outside rejection, same-membership allowance, directly aimed empty cells, outside->outside across intervening cloud, valid-primary splash crossing the boundary, and legal Direction/All hits across it.
- [x] Run V1 red. Insert the helper at shared primary designation admission, retaining normal actor/eligibility/range/wall/delivery checks and existing multi-cell deferral. Do not add the restriction to generic target eligibility or final damage recipients.
- [x] Update range-display projections using the same helper. Test `DisplayedPrimaryCellsMatchExecutableAims` and `AiCanChooseLegalIndirectHitButNotForbiddenPrimary`; assert preview recipients/damage match actual execution for the same immutable facts.
- [x] Run V1 for Haboob targeting and affected attack/AI tests. Ensure moving or undoing a unit across the area changes subsequent queries without a copied unit-membership status.

### P3.3: Add Haboob authoring and verify composed behavior

**Files:** create `D/HaboobGimmickAuthoring.cs`, `HaboobGimmickData.cs`; modify stage SO/detail/adapter and extend `TI/StageGimmickAuthoringTests.cs`. Tests: new `TI/StageGimmickCompositionTests.cs`.

**Interfaces:** `ToData()` returns fresh immutable data when authoring `enabled` is true, otherwise null; default disabled. The adapter supplies `HaboobGimmickSpec`. Preparation rejects duplicate kinds, unknown handlers, invalid normalized masks, or `lead <= 0 || lead > interval`. Defaults remain the approved mask/2/1.

- [x] Add `NonDefaultHaboobSettingsDriveRealPreparationAndRelocation` with interval 4/lead 2 and a valid authored mask; verify warning at 2, relocation at 4, and no mutation from later SO edits. Run V1 red, implement the mapping, then run green.
- [x] Add actual combined-flow tests confirming legacy Tornado -> Railway -> Haboob -> Rockfall, projected victims, terminal short-circuit, and deployment count stability. Keep one RNG stream; do not assert independent random sequences when enabled features differ.
- [x] Run V2/V3 and authoring tests; verify existing Railway/Tornado fixtures still pass without migration. **Stop after Phase 3** with the precise exported contracts and all missing external acceptance items.

## Phase 4 — Owner Handoff and End-to-End Acceptance

### P4.1: Deliver concrete contracts to DB/View/Tornado owners

**Withdrawn on 2026-11-01 by explicit user instruction.** The previously drafted handoff was discarded and deleted. Do not recreate it during this task. The original checklist below is retained only as historical planning context. Add necessary shared-ownership requests to the existing `docs/superpowers/handoffs/2026-09-24-view-core-data-boundary-handoff.md` rather than silently editing restricted files.

**Interfaces:** consumes implemented state read members, event payloads, failure reason, SO fields, and completion semantics; produces an owner-facing contract/checklist. It must not invent an API or claim an external message was sent.

- [ ] Record actual initialization, state/result read paths, warning/impact clearing, Haboob range refresh, Bind source/readout, cancellation, and detach/reattach behavior. Include one real result example per new event and actual non-default SO examples.
- [ ] Record the Tornado attachment procedure: replace its legacy periodic hook when the new adapter registers; preserve source/RNG ownership; reconcile its clock; retain its owner's gameplay choices. No Tornado rewrite is performed here.
- [ ] Record production stage/visual asset keys actually selected by their owners. Do not guess where to activate the new content or copy the planning deck's example coordinates into production maps.
- [ ] Review links/members against the final diff and label each item implemented/tested/manual-pending/owner-pending. Deliver the document locally; sending it externally requires explicit user instruction.

### P4.2: Integrate authorized consumers and verify playable behavior

**Files:** conditional View-owner work in `V/Playback/Ordering/CombatEventTypeTable.cs`, `V/Playback/Handlers/CombatFactEventHandler.cs`, and existing display composition; feature presenters proposed as `V/Display/Map/FieldRockfallPresenter.cs` and `FieldHaboobPresenter.cs`. Tests: owner-authorized `TI/StageGimmickPresentationTests.cs`; selected stage/View SO assets only after owner/content assignment.

**Interfaces:** View consumes committed facts/events; it does not choose cells, calculate damage/duration, or increment turns. Existing presentation completion/cancellation releases CombatPlan's gate. New delegates/adapters must use the project's current presentation contracts; no independent UI service or per-feature Job runner.

- [ ] Before enabling content, add presentation-consumption checks: current warning/area restored on attach; impact clears warning; new Haboob area updates only at the event; repeated presentation does not call Core; cancel/detach releases resources/completion without a hidden pending operation.
- [ ] Connect event routes and feature presenters through the existing owner-controlled composition. Keep Railway's contact/death grouping and timing unchanged. Use View DB for art/tunnel anchors; no gameplay SO reads in View.
- [ ] Run V1/V2 and `DataBaseAssetTests` after asset changes. In PlayMode, verify both-side turns, selectable cells/indirect attacks, 2+2 Rockfall warning retention, normal reserve cleansing, terminal result presentation, cancellation, and combined due events.
- [x] If owner authorization, art/assets, or consumers are unavailable, finish the handoff and report Phase 4 **pending/partial**. Do not replace missing visual acceptance with a Core-only result or silently implement other-owner code.
- [x] Run V3, report the final actual verification matrix and remaining limitations, then **stop**. User performs any commit/push.

## Execution Record

Append evidence here when a Phase is explicitly executed; do not precheck tasks or insert predicted counts.

| Phase | Authorization / source revision | Changed scope | Tests and artifacts | Remaining work / next review |
|---|---|---|---|---|
| 0 | Authorized as Phase 1 preflight; baseline `2555a516b` | Source/ownership and approved Unity route verified | Core 4425/4425; CoreIntegration 577/578 | Complete; baseline failure recorded below |
| 1 | Explicit 2026-10-31 execution request; same baseline | Common progression, Railway extraction, immutable state/specs, existing Buffer integrity | New tests 10/10; final regression and corrected contract guard below; focused review found no required fixes | Complete; stop here. Phase 2 requires authorization |
| 2 | Explicit 2026-10-31 execution request; existing uncommitted Phase 1 retained | Rockfall rule/events/evidence, direct HP and Bind, disabled SO configuration | Core 4451/4451; CoreIntegration 594/595; all 33 added cases passed; details below | Complete; stop here. Phases 3-4 require authorization |
| 3 | Explicit 2026-10-31 execution request; existing Phases 1-2 retained | Haboob progression/events, shared targeting, disabled SO authoring and combined publication | Core 4467/4467; CoreIntegration 606/607; all 28 added cases passed | Complete; stop here. Phase 4 owner/content/visual acceptance remains pending |
| 4 | Prior execution superseded by explicit 2026-11-01 discard request | Premature handoff deleted; P4.1 withdrawn | Documentation/reference cleanup only | User owns future Tornado implementation; not implemented. Do not regenerate handoff without a new request |

### Phase 0 + 1 execution evidence — 2026-10-31

**Scope and authorization.** Phase 0 was executed as Phase 1 preflight, with no separate stop. The existing feature checkout was retained. Initial worktree changes were only the untracked design and plan supplied for this task. No commits/pushes, production content activation, View/FieldCamera/Architecture changes, or external messages were made. The approved Unity executable was `D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe`; no Editor held the project when each batch run started. Runs used the AppData-authorized process route. The generated FMOD catalog scan-folder change was identified and restored to the clean baseline.

**Preflight contracts.** `CombatTurnCalculator` retains the legacy Tornado hook before the common gimmick hook. Its terminal enemy/Tornado return still precedes the enemy-end count/event; player completion keeps its existing behavior. Reserve-return cleansing, cleanup, regeneration, and recall were not moved. `CombatChangeEvidenceReader` still rejects unknown event variants; this phase introduces no new events. Core/Contracts/Combat Buffer modifications are authorized by the named Phase 1 tasks. External Tornado gameplay and View consumers remain owner work, with no claim that coordination messages were sent.

**Implemented contracts.**

- `IStageGimmickRule` is an internal, stateless progression contract; `StageGimmickComposition` stores a copied, unique, explicitly ordered rule list. Each rule receives the preceding accepted projection, and terminal outcomes stop later rules. Definition-list position remains the stable battle-local ID; Railway-only IDs are unchanged.
- Production calls the new `TryAdvance(input, StageGimmickBoundary)` overload with the ended side and already-incremented completed count. The original one-argument Railway entry remains supported. Boundary/count mismatches are rejected, and the coordinator cannot change turn facts.
- `CombatCommandState.Gimmicks` defaults to `StageGimmickStateSet.Empty`; `With`, value comparison, turn construction, movement projection, and preparation carry it. `CombatStateData.Gimmicks` is a read-through property, not a second state source. Existing `CombatCommandComposition` already passes the final immutable state through projection/merge and required no edit.
- `StageGimmickStateSet` supplies `Entries`, `Empty`, `Get`, `Replace`, `Without`, and `SameValues`. Typed Rockfall/Haboob states defensively copy and normalize cell-set order. Identity/kind uniqueness, phase/deadline/cell validity, prepared-definition correspondence, and board bounds are validated. The existing Buffer transaction compares the expected state before writes and rejects any unsupported runtime gimmick-state mutation in this phase.
- `StageGimmickKey` appends `Haboob = 20` and `Rockfall = 30`; existing values are unchanged. Typed specs expose the planned validated numeric settings and immutable normalized masks. Only Railway is registered for production. `Initialize` is wired after existing preparation and returns the unchanged state for Railway, which needs no extra entry. Rockfall/Haboob production preparation remains rejected until their handlers are implemented. Typed direct preparation fixtures prove storage/publication, not feature initialization.

**Changed files** (aliases are defined in the file map; all new C# files and the new folder have `.meta` files):

| Area | Files |
|---|---|
| Common progression | Modified `C/Application/Combat/CombatGimmickCalculator.cs`, `CombatTurnCalculator.cs`, `BattlePreparationCalculator.cs`; added `C/Application/Combat/StageGimmicks/IStageGimmickRule.cs`, `StageGimmickComposition.cs`, `RailwayGimmickRule.cs` |
| State copies and contracts | Modified `C/Application/Combat/MovementProjection.cs`, `C/Contracts/Combat/CombatCommandState.cs`, `BattlePreparation.cs`, `C/Contracts/Common/StageGimmickKey.cs`, `C/Contracts/Commands/StageGimmicks/StageGimmickSpec.cs`; added `C/Contracts/Combat/StageGimmickState.cs`, `C/Contracts/Commands/StageGimmicks/RockfallGimmickSpec.cs`, `HaboobGimmickSpec.cs` |
| Existing Buffer | Modified `B/Combat/CombatStateBuffer.cs`, `CombatStateData.cs` |
| Tests | Added `T/StageGimmickProgressionTests.cs`, `TI/StageGimmickStatePublicationTests.cs`; updated the existing `T/ContractShapeGuardTests.cs` property list after verifying every copy/equality path |
| Documentation | Updated this plan and the existing [Core boundary handoff, section 5.69](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#569-stage-gimmick-common-state-and-railway-extraction-2026-10-31-explicitly-authorized-phase-0--1-not-sent) |

**Verification actually run.** Each directory below contains `results.xml` and `editor.log`; Unity import compiled the new sources and their assembly inclusion. All listed runs completed and produced the intended XML. No database assets were changed, so a separate DB-only rerun was unnecessary; the assembly regression includes its existing tests.

| Evidence | Result | Artifact directory |
|---|---|---|
| Preflight V2 | Core 4425/4425; CoreIntegration 577/578 | `C:/Temp/projectl-gimmicks-phase01-baseline` |
| New state RED | 3/7 passed, 4 expected failures: turn-copy state loss and preparation not carrying the new member | `C:/Temp/projectl-gimmicks-phase01-red` |
| Focused progression/Railway/count/publication | 42/43 passed; sole failure was the test fixture requesting `CompleteEnemyTurn` at controller start. Corrected the fixture to existing `StartControllerTurn`; no product workaround | `C:/Temp/projectl-gimmicks-phase01-targeted` |
| Publication after fixture correction | 3/3 passed: prepare/install, preview, movement, undo, attack, stale expected facts, and forged final facts through real Buffer transactions | `C:/Temp/projectl-gimmicks-phase01-publication` |
| Final V2 before contract-list update | Core 4431/4432; CoreIntegration 580/581. All 10 new tests passed. Failures: one expected public-property inventory update plus the unchanged baseline Cinemachine failure | `C:/Temp/projectl-gimmicks-phase01-final` |
| Contract-list correction | Exact failing guard rerun: 1/1 passed after adding `Gimmicks` to the expected property list | `C:/Temp/projectl-gimmicks-phase01-contract` |

The final full-run XML is retained as produced, not relabelled as green. Only the existing guard's expected property list changed afterward, so its exact test was rerun instead of repeating both entire assemblies. The remaining known failure is `CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset", False)`, which throws `TypeLoadException` for that missing Cinemachine type both before and after this work. It is outside Phase 1 and was not suppressed or fixed. Historical StageKey 2101 failures were not assumed; this baseline had only the camera failure.

A single read-only review of the Phase diff found no required fixes or optional findings. `git diff --check` passed. The existing shape guard caught the additional public member; the behavior tests independently proved its preservation and atomic rejection before the expected-property list was updated. No getter-only test matrix, repeated full regression after that list-only correction, PlayMode, or visual acceptance was added.

**Change locality and architecture.** Previously Railway calculation and dispatch occupied one calculator and rejected every other definition. The common facade now handles ordering/projection while Railway owns its unchanged activation/settlement logic. Adding a later rule requires its own calculation plus explicit registration and necessary contract/authoring/publication wiring, without editing unrelated feature internals. Shared state schema changes still require constructor/equality/Buffer/undo understanding and the shared regression; this is a real dependency, not something hidden by splitting files. No Framework registration, Job lifecycle, persistent state ownership, or assembly dependency changed. Rule statelessness and complete immutable transitions preserve substitutability; the one-operation interface avoids unrelated callbacks. Future Rockfall/Haboob locality is a design expectation until those phases are implemented.

**Stop gate.** Phase 1 is complete with the baseline camera failure explicitly retained. Phase 2 gameplay/evidence/SO work, Phase 3 Haboob, and Phase 4 View/content/PlayMode remain pending. The next phase must replace the temporary runtime-state-preservation gate with typed event/state transition validation before publishing new gimmick events. No owner-facing claim of playable Rockfall/Haboob support is made.

## Planning Self-Review

- U1-U14/O1-O6 map to P1 boundary/state, P2 Rockfall, P3 Haboob, and P4 consumption/ownership tasks; no active gameplay decision remains open for Rockfall/Haboob.
- Every Review Focus entry names its owning task; new event evidence and the one-argument Railway compatibility entry are explicitly covered.
- Interface names/fields above are shared across tasks. Runtime storage, rendering state, prepared definitions, and RNG are not independent writable copies.
- Existing regression obligations remain. Live visuals, deployment/recall interactions, and real transaction behavior require their stated checks; document/link checks are not substitutes.
- The original planning request produced only this plan and the spec. The subsequent execution request authorized Phase 0 + 1; its actual implementation and test evidence are recorded above. Stage activation, commit, push, and external handoff delivery remain unperformed.

### Phase 2 execution evidence — 2026-10-31

**Authorization and scope.** The user explicitly requested Phase 2. P2.1-P2.3 are complete in the existing `feature/Cheon/Core/NewCombatSytem` checkout at HEAD `2555a516b1be29f1464907e57592e91c908c93a4`. Uncommitted Phase 1 work and the already-dirty FMOD catalog were preserved. No commit/push, production gimmick activation, View/FieldCamera/Architecture change, Haboob implementation, or external message was performed. The previous Phase 1 stop-gate paragraph above is historical and is superseded only for Phase 2 by this record.

**Implemented contracts.**

- `RockfallGimmickRule` is registered after Railway. Preparation creates Cooldown with deadline `completedCount + CooldownControllerTurns`. Warning uniformly chooses the inclusive count and distinct fitting anchors using the existing environment RNG, unions overlapping masks, and commits immutable warned cells. Walls, cliffs and living blocking structures are excluded; occupied walkable cells remain candidates. Shortage uses available candidates, and zero candidates still complete warning/impact. Oversized normalized masks produce no candidate without overflowing coordinates.
- Impact uses the frozen cells and current living field occupants, once per entity, even after movement or terrain changes. It removes `floor((long)MaxHealth * percent / 100)` through existing `Hurt`, without defense/shield/reduction or fabricated attack attribution. Existing `Finish` handles settlement once. A terminal impact removes its pending state; a nonterminal impact schedules the next cooldown. Impact consumes no selection RNG and creates no rubble.
- Survivors receive existing Bind through `AppliedStatusRules.Apply`, with `configuredTurns + (victim.Owner == endedSide ? 1 : 0)` before existing cleanup. `EffectOwnerKind.StageGimmick = 5` carries battle-local ID, Rockfall definition kind and rule key `rockfall-bind`. Same-value refresh still emits an existing typed status fact, including when HP loss is zero. Attacks remain allowed; reserve/automatic night-return cleansing keeps its existing precedence. No common Bind, forced-movement or night policy was rewritten.
- `CombatRockfallEvent` publishes `Step`, `Expected`, `Final` and immutable `RecipientIds`; `CombatEventKind.RockfallAdvanced = 42` appends without renumbering existing kinds. Evidence records warned/impacted cells and actual recipients, requires supporting HP/Bind facts and a terminal outcome for null final state. Existing Buffer publication checks stored expected facts, event/state correspondence, a real completion boundary, complete recipients and final board bounds. The existing action-state validator continues to own exact counter increments. Stale/forged proposals remain atomic; numbered-command repeats are ignored by the existing receipt gate, while repeated automatic advances reject stale character facts.
- `CombatStageData.rockfall` is typed `RockfallGimmickAuthoring`, disabled by default. `ToData()` returns null when disabled and a fresh validated immutable `RockfallGimmickData` when enabled. `CombatStageDetail.Rockfall` reaches `RockfallGimmickSpec` through the existing `BattleDefinitionAdapter`, preserving Railway serialization/IDs. The existing Inspector already draws this added serialized property. Production stage assets remain unchanged and disabled. Non-default cooldown 3, warning 1, count 1..2, HP loss 35 and Bind 2 were exercised through SO conversion, the actual DB adapter, preparation and real turn publication; later authoring/mask edits did not alter the prepared battle.

**Verification actually run.** Unity `D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe` used the AppData-authorized hidden batch route with no concurrent project Editor. Each evidence directory contains its actual `results.xml` and `editor.log`; compilation failures were not counted as RED or passing tests.

| Evidence | Result | Artifact directory |
|---|---|---|
| Progression RED | 0/5; expected missing Rockfall handler | `C:/Temp/projectl-gimmicks-phase2-red` |
| Effect RED | 0/8; expected missing handler after fixing a test-only missing using | `C:/Temp/projectl-gimmicks-phase2-effect-red` |
| Authoring RED and common/Core feature checks | 62/63; only typed SO authoring absent | `C:/Temp/projectl-gimmicks-phase2-authoring-red` |
| Initial feature/publication cycle | 94/95; one test wrongly expected numbered command replay to throw; corrected to verify existing idempotence and unchanged stores | `C:/Temp/projectl-gimmicks-phase2-integration` |
| Edge RED | 14/16; reproduced zero-loss identical Bind evidence gap and oversized-mask overflow, then fixed both | `C:/Temp/projectl-gimmicks-phase2-edge-red` |
| Focused required regression | 133/136; test-only source-allocation high-water marks and automatic-advance replay expectation corrected before final V2 | `C:/Temp/projectl-gimmicks-phase2-targeted` |
| Final V2 | Core **4451/4451**, CoreIntegration **594/595**; all **33 added cases passed**, including new event evidence; `DataBaseAssetTests` **9/9** | `C:/Temp/projectl-gimmicks-phase2-regression` |

The sole final failure is the retained Phase 0/1 baseline: `CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset", False)` raises `TypeLoadException` for the missing Cinemachine type. It was not hidden or repaired outside this scope. Final V2 includes existing Railway/Tornado, restriction/origin/status publication, reserve-return, movement/undo and DB-asset tests. No redundant whole-suite rerun was made after the final XML; subsequent source editing only expanded enum braces to the required Allman style, with no token/behavior change.

**Review and locality.** A separate read-only reviewer found no actionable findings. The actual modification boundary is Rockfall's feature rule plus explicit registration, typed contracts/evidence, existing Buffer integrity and SO-to-prepared-spec mapping. Tuning/formulas do not require Railway/Tornado internals; changing common publication still requires understanding the existing turn, character, state-copy and transaction contracts and therefore retains the shared regression. No alternate state store, Framework service, Job or runner was added. Scope exclusions from review (View/Inspector visual acceptance, production activation, Haboob and PlayMode) remain pending owner/later-Phase acceptance; Unity compilation and EditMode results were independently verified here. All new C# files have imported `.meta` files; `git diff --check` passed.

**Stop gate.** Phase 2 is complete with the one retained baseline failure. Phase 3 Haboob and Phase 4 View/content/manual acceptance remain pending. Core/SO support is not playable-content or presentation acceptance. User performs any commit/push.
### Phase 3 execution evidence — 2026-10-31

**Authorization and scope.** The user explicitly requested Phase 3 after the Phase 2 report. P3.1-P3.3 implementation, V2 and independent final review are complete. Work remains in the existing uncommitted feature checkout at HEAD `2555a516b1be29f1464907e57592e91c908c93a4`, preserving Phases 1-2 and the already-dirty FMOD catalog. No commit/push, production-content activation, View/FieldCamera/Architecture work, external message or Phase 4 implementation was performed.

**Implemented progression and reads.** `HaboobGimmickRule` initializes after the existing initial environment preparation and consumes the existing environment RNG only when choosing among multiple candidates. Every full-mask-fitting board anchor is eligible, including walls, cliffs, living structures and character cells. Invalid fit fails preparation before installation. Default mask is the twelve-cell cross (rows 2/4/4/2). Relocation excludes the current anchor when another exists; the unique-candidate case retains the same area and still publishes a relocation. No terrain source, copied unit membership, second anchor, display timer or per-feature store was added.

`HaboobGimmickState.Cells` is the canonical immutable current area. In Active phase `NextBoundaryCount` is the next warning; in RelocationWarning it is the next relocation. Defaults warn at 1, relocate at 2, then warn at 3/relocate at 4. Warning preserves area/RNG and exposes no destination. For lead equal to interval, preparation and each relocation leave the warning active with the next relocation deadline. The common registry now runs Railway -> Haboob -> Rockfall after the untouched legacy Tornado hook; prepared-definition list indices remain battle-local IDs.

**Public result and targeting contracts.** `CombatEventKind.HaboobAdvanced = 43` carries `CombatHaboobEvent.Step` (Warning/Relocation), `Expected` and `Final`. Both states are non-null. Warning must preserve cells; relocation preserves the full translated footprint, even if its position stays the same. Evidence describes the old/new cell union. Existing `ValidateGimmickChanges` checks current expected state, one event per kind/ID, the actual completed-side boundary, board bounds and final state correspondence in the existing atomic Buffer publication. No selection or movement formula was duplicated in the store.

`AttackFailureReason.HaboobDesignationBlocked = 51` identifies a cross-membership primary aim. `CombatAttackQueries.Selection` uses `HaboobTargetingRules.CanDesignate` before primary-target eligibility, so directly aimed empty cells are covered. `RangeCells` uses that same helper for Single choice, projecting legal directly selectable cells into `AttackRangeResult.RangeCells`/`BasicAttackCells` and `UnitAttackRangeResult.RangeCells`; `AttackAims`, preview, execution and AI continue through the common selection/delivery path. Direction/All have no primary and retain indirect coverage. A legal primary can still deliver splash across the boundary. Walls, actor admission, hostile eligibility, delivery and existing multi-cell deferral remain in their existing policies. Real movement and undo change subsequent queries immediately while preserving the cloud facts/RNG, with no membership status to synchronize.

**Authoring and composition.** `CombatStageData.haboob` is typed disabled-by-default `HaboobGimmickAuthoring`. `ToData()` returns null when disabled or fresh validated immutable `HaboobGimmickData` when enabled; `CombatStageDetail.Haboob` maps through the existing `BattleDefinitionAdapter` to `HaboobGimmickSpec`. The authoring default mask reuses the spec's approved default footprint. Actual SO -> Data -> DB adapter -> preparation -> Buffer turn flow verified non-default interval 4/lead 2 and a two-cell mask, warning at 2/relocation at 4, unaffected by later SO/mask edits. Production stages remain disabled and unchanged. Railway/Tornado fixtures require no content migration.

**Verification actually run.** All Unity runs used `D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe`, hidden AppData-authorized batch execution, without a concurrent Editor holding the project. Evidence directories below contain actual `results.xml` and `editor.log` where behavioral results are listed.

| Evidence | Result | Artifact directory |
|---|---|---|
| Initial progression/designation RED | 4/14 passed; 10 expected missing-handler/missing-restriction failures | `C:/Temp/projectl-gimmicks-phase3-red` |
| Core/progression/evidence + authoring RED | 64/65 passed; only typed Haboob authoring absent | `C:/Temp/projectl-gimmicks-phase3-core` |
| SO + real publication | 72/72 passed | `C:/Temp/projectl-gimmicks-phase3-integration` |
| Combined/AI regression | 118/121; test-only command-number and Tornado Terrain-classification fixtures corrected | `C:/Temp/projectl-gimmicks-phase3-targeted` |
| Combined/AI after fixture correction | 120/121; only the movement fixture omitted intermediate Step cells | `C:/Temp/projectl-gimmicks-phase3-targeted-green` |
| Actual crossing/undo and blocked-cell placement | 2/2 after supplying a real four-step path | `C:/Temp/projectl-gimmicks-phase3-movement` |
| Final V2 | Core **4467/4467**; CoreIntegration **606/607**; all **28 added cases passed**; DB assets **9/9** | `C:/Temp/projectl-gimmicks-phase3-regression` |

The `phase3-authoring-red` attempt overlapped import of incomplete newly added sources and stopped at compilation; it produced no behavioral evidence and is not counted as RED or passing. The later `phase3-core` run supplies the actual authoring RED. The full-fit test was tightened to require the intended geometry error instead of accepting any missing-handler exception. No production workaround was introduced for fixture errors.

The final V2's single failure remains `CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset", False)` with the same missing-type `TypeLoadException` recorded in Phases 0-2. It was not hidden or changed. All new progression, targeting, evidence, authoring, preparation-failure atomicity, stale/forged publication, real combined turn order/terminal short-circuit, deployment-count stability and crossing/undo checks passed. Cannon direct-display cells were compared with offered executable aims; preview and execution accepted every offered aim. Existing AI, Railway/Tornado, attack, status and undo regression remains in the full assemblies. All new sources have imported `.meta` files and LF; no separate redundant DB-only or full-suite rerun is needed after this final run.

**Change locality and ownership.** Before this phase, Haboob preparation was rejected and attacks ignored stored cloud facts. The feature now owns its placement/scheduling calculations and membership helper; composition owns explicit registration/preparation routing, the shared query owns attack admission/projections, Contracts/Buffer own immutable facts and publication integrity, and authoring owns conversion. Changing Haboob timing/mask requires its SO/spec/rule contract rather than Railway/Rockfall internals. Changing direct designation necessarily requires understanding primary vs indirect selection and the shared range/preview/AI delivery contract; those tests were retained. Shared state/counter/publication changes still require cross-feature integration verification. No Framework registration, assembly dependency, service lifetime or Job contract changed.

**Remaining acceptance.** The current owner-facing read path is existing `CombatStateData.Gimmicks`; the actual members/events/failure reason above are implemented and EditMode-tested. View event routing, warning/relocation visuals, target-range refresh presentation, completion/cancellation, detach/reattach visual acceptance, selected production assets and PlayMode/manual acceptance remain Phase 4/owner work. The new current-state query is not evidence that View consumes these events. Phase 3 stops before all of that work.

**Independent final review and stop gate.** The read-only reviewer inspected the actual code and final V2 XML and found no actionable findings, required fixes, deferred minors or unresolved behavioral rulings. Modification boundaries and the retained shared regression were confirmed. Review excluded the explicitly deferred Phase 4 View/content/PlayMode acceptance; that exclusion does not claim those consumers work. Final git diff --check passed. Phase 3 is complete with the one retained Cinemachine baseline failure. Stop here; Phase 4 requires separate authorization and owner/content readiness. No commits or pushes were made.

### Historical Phase 4 execution evidence — 2026-10-31 (handoff discarded on 2026-11-01)

**Authorization and actual deliverable.** The user explicitly requested Phase 4. A local Rockfall/Haboob handoff was drafted at this time; the user subsequently discarded it and the file was deleted on 2026-11-01. P4.2 remains pending under its explicit missing-assets/consumers condition. No production StageKey, Rockfall/Haboob visual prefab/effect/configuration keys or off-board tunnel anchor were assigned in the session. An optional question was presented while source investigation and handoff work continued; no assignments were available when this record was written. No external message, commit or push was made.

**Current consumer evidence.** The existing `CombatDisplayValueReader.Read` already supplies `CombatDisplayValues.State`, so `CombatStateData.Gimmicks` requires no new polling service. `CombatSceneDisplay.Install/RefreshCore` does not render those facts. `CombatEventTypeTable` has no Rockfall/Haboob rows: `RouteOf` returns Skip, and `OrderedCombatPresentation` logs a warning then completes those entries without a fact callback. `CombatFactEventHandler` and the existing factory/field ports contain no new feature consumer. Scoped View source/DB inspection found no Rockfall/Haboob visual configuration. An immediate Facts registration alone would therefore hide a missing gameplay warning/area. No placeholder/no-op route, guessed assets or production activation was added.

**P4.1 contents checked.** The handoff records actual prepared initialization and `CombatStateData.Gimmicks`/result/status reads; typed event kinds42/43, immutable Expected/Final and recipient contracts; exact fixture-backed warning/impact/relocation examples; non-default SO examples; Bind StageGimmick source and affected-side duration; Haboob shared direct-designation/indirect-hit behavior and result-triggered preview refresh; existing event/barrier/final acknowledgment, cancel/detach/release and reattachment contracts. The Tornado procedure identifies its legacy `ControllerTurnSequence` clock versus the new completed-count boundary, authoritative source/RNG, replacement of both periodic hooks without duplicating entry behavior, explicit registration order and the retained enemy terminal exception. Production key/art/tunnel assignments are explicitly Unassigned. Implemented/tested facts are separated from owner-pending and manual-pending acceptance; no future presenter API is presented as existing.

**Verification actually performed.** All 34 local document links resolved to existing files; owning source members were read, relevant View consumers were traced through reader -> factory -> type table/handler -> display -> finalization/release, and `git diff --check` passed. The prior Phase 3 XML at `C:/Temp/projectl-gimmicks-phase3-regression/results.xml` was re-read: Core4467/4467, CoreIntegration606/607, with only the retained missing `CinemachineCameraOffset` TypeLoadException. These are previous Unity results, not a Phase 4 rerun. Phase 4 changes documents only, so no redundant Unity/DB-only run, new presentation test, PlayMode or visual acceptance was performed. Product/test/asset/scene files, existing Phase 1-3 edits and the previously dirty FMOD catalog were preserved.

**Change locality.** This phase changes only the concrete handoff, this plan and the existing boundary-handoff reference. It identifies the feature presenter/View DB plus existing display/event/factory lifecycle wiring required for visual changes, and the public facts/completion contracts that worker must understand. It does not require that worker to reimplement Core geometry, effects, membership or RNG. Shared registration/assembly changes remain legitimate and explicit. Verification remains presentation/resource/target-refresh + real combined flow when consumers are implemented; existing Core success is not substituted for those missing checks.

**Pending P4.2 acceptance.** Supply actual production stage/visual assignments; implement and wire feature consumers through existing owners; add `StageGimmickPresentationTests` covering attach/current-state restoration, impact warning clear, event-timed area replacement, no repeated gameplay, cancellation and detach/reattach resource release; run affected EditMode/Core integration and asset checks after implementation; perform PlayMode/manual acceptance for both-side turns, 2+2 warning lifetime, target/indirect-hit behavior, reserve cleansing, terminal and combined event presentation. No FieldCamera/Architecture or external Tornado gameplay rewrite is included. Phase 4 remains **partial**, not feature-complete.

**Independent review and stop gate.** A separate read-only reviewer checked the actual source contracts and the previous Phase 3 XML. One result-read interface name was corrected to `ICombatResultReadSink` through `IBufferQuery.CombatResultReadSink`, after verifying the owning declarations; the attach call chain was spelled out. All 34 handoff links resolved and final diff whitespace validation passed. There are no unresolved required document fixes. P4.1 is complete locally; P4.2 remains pending and Phase 4 is explicitly partial. No production/test/asset/scene changes or fresh Unity results are claimed. Stop here with the content/consumer/manual acceptance checklist in the handoff.

### Ownership and completion clarification — 2026-11-01

The user answered that stage/visual assignments are not yet decided and instructed completion of the handoff with View/production acceptance waiting. They then explicitly clarified their role: Core programmer; View is not their responsibility. This clarification governs the task scope over the older repository note describing assistance to a View worker.

**The requesting user's scope is complete:** Phase 0-3 Core gimmick implementation and the P4.1 local owner handoff. The retained Cinemachine test failure is already documented; no new Core failure or implementation gap is implied by missing View assets. P4.2 remains tracked solely as external View/content-owner integration and visual acceptance. Its unchecked tasks are not a completion gate for this user's Core task and do not require the user to select art or implement View. Earlier Phase 4 partial statements refer to full playable integration readiness, not to incomplete Core delivery. No gameplay, View, test or asset changes were made for this clarification; no additional Unity run, external message, commit or push was performed.
### Tornado completion-scope clarification — 2026-11-01

The user confirmed that they have not implemented the planned Tornado feature. The completed implementation claim is limited to the common progression framework, behavior-preserving Railway extraction, Rockfall and Haboob, with their concrete local View handoff. It does not claim all future stage gimmicks or planned Tornado gameplay are complete.

P4.1 originally requested a Tornado attachment procedure, so the handoff included future integration notes. That section is now explicitly titled as unimplemented future work, and the document title names Rockfall/Haboob. The repository's existing legacy TornadoCalculator/periodic-entry hooks were inspected and preserved; their presence and regression results are not evidence that the user's planned Tornado gameplay was implemented. No new Tornado rule/adapter/gameplay was created, and this clarification does not authorize implementing it.
### Handoff discarded; Tornado ownership confirmed — 2026-11-01

The user explicitly stated that they will implement Tornado and instructed discarding the handoff for now. The premature `2026-10-31-stage-gimmick-core-view-handoff.md` file has been deleted and its active references withdrawn. P4.1 is withdrawn; previous local-handoff completion statements are historical, not current deliverables. Do not recreate or externally send a handoff without a new request.

Common progression, preserved Railway, Rockfall and Haboob remain implemented. Planned Tornado gameplay remains unimplemented and is now explicitly user-owned Core work; it is not assigned to another worker. This instruction does not start its implementation or create a new design/plan. View remains outside the user's ownership. No gameplay/test/asset changes, Unity execution, commit or push were performed for this documentation cleanup.