# Drag Path Movement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task after the user selects an execution method. Steps use checkbox (`- [ ]`) syntax for tracking. This document does not start implementation or authorize automatic commit/push.

**Goal:** Execute the exact route selected by the player, with path-based resource consumption, immediate traversal effects, conditional checkpoint return, and enemy avoidance of known mines.

**Architecture:** Core calculates immutable route/effect transitions from current Buffer facts. Existing Character and Combat Buffers remain authoritative, and their existing prepare/install boundary publishes each command once. The existing View-owned LifeCycle/Flow/Plan/Job path schedules processing and waits for ordered presentation acknowledgement; there is no second Core execution loop.

**Tech Stack:** Unity 6000.0.73f1, C#, NUnit EditMode, existing `ProjectL.Core.Contracts`, `ProjectL.Core.Domain`, `ProjectL.Core.Application`, `ProjectL.Systems`, and Framework services.

**Spec:** [Drag Path Movement and Conditional Checkpoint Return](../specs/2026-10-04-drag-path-movement-design.md). Read the entire spec, including confirmed rules G01-G20 and acceptance cases V01-V36, before execution.

**Planning baseline:** `7c9f724a`, inspected 2026-10-04. The design document is an incoming untracked file. No runtime or test files were changed while writing this plan.

## Global Constraints

- "Language: design and implementation documents are written in English; new code comments are written in Korean."
- "Dragging is preview only. It changes no authoritative position, HP, ActionCost, movement range, Object, soul, trait, or battle state."
- "Actual movement uses orthogonally adjacent cells. Every intermediate cell participates in validation and applicable effects."
- "ActionCost and movement-range consumption accumulate according to the actual selected route, including detours and successive executed drags."
- "A return-blocking event seals the pending movement through the actual final stop of that command. A surviving character starts its next refundable interval at that stop."
- "A successful attack and turn end seal pending movement. Entering or canceling attack selection does not."
- "A temporary maximum-HP tile increases maximum HP only. On removal, current HP is capped to the restored maximum. This cap is not damage and does not itself block return."
- "Player and enemy ordinary movement share traversal, obstruction, and cell-effect rules. Enemies have no checkpoint return/refund operation."
- "One route is one gameplay command." No per-cell Buffer publication or animation-owned gameplay.
- Keep Framework dependency direction, Factory registration, narrow sinks, existing Buffer ownership, and View-owned scheduling. Do not introduce a second state store or generic transaction/event framework.
- Preserve incoming work and Unity asset GUIDs. Create `.meta` files through the established Unity import workflow for new Assets files. Generated IDE project files are not source-of-truth assembly definitions.
- Execution uses an isolated checkout if repository workflow requires it; creating a worktree is an execution concern, not a prerequisite for editing this plan.
- Production mine balance, blast shapes, discovery rules, and unrelated View/bootstrap authoring are external content/integration dependencies. Fixture-backed success must be labeled accordingly.
- A checkpoint refund restores recorded pending movement cost and range, not a prior HP/trait snapshot or the whole turn budget. Ordinary movement toward the checkpoint remains paid.

**Contract references:** The existing design already fixes ledger/return semantics in [Sections 7.1-7.4](../specs/2026-10-04-drag-path-movement-design.md#7-movement-ledger-and-checkpoint-return), sequential effects and arrival in [Sections 8.1-8.4](../specs/2026-10-04-drag-path-movement-design.md#8-tile-effects-interruption-and-final-arrival), public preview versus authoritative publication in [Sections 9.2-9.3](../specs/2026-10-04-drag-path-movement-design.md#9-contracts-preview-and-authoritative-publication), and full-result presentation in [Section 11](../specs/2026-10-04-drag-path-movement-design.md#11-view-playback-and-lifecycle-integration). The contracts below make those requirements implementable; they do not reopen G01-G20.

**Framework basis:** Follow [Framework Development Guidelines](../framework/Framework%20Development%20Guidelines.md), Sections 2/6 (responsibility), 9 (Factory composition), 20/21 (domain versus presentation), 22/23 (authored data versus runtime state), and 27 (no speculative abstraction), together with the architecture/programmer-guide links in design Section 1. Retain the recorded source investigation; recheck a specific integration contract only when it changes.

## Review Focus

1. Same anchor with a nonzero pending ledger after several drags: Return must refund the ledger even though displacement is zero (T03).
2. Direct health/effect publication without a new combat result sequence: stale drop validation must notice relevant visible facts changed (T07).
3. A buff-origin departure, nonlethal mine, and surviving interrupted arrival: modifier removal, normalization, barrier, soul arrival, and billing must occur exactly once in order (T04-T06).
4. A release while the newest preview is pending, then scene detach and a late callback: no old route may submit in the next battle (T09, T11).
5. Enemy safe detour initially moves farther from its target or crosses an undiscovered mine: planning uses its knowledge, execution uses actual effects, and no invalid post-death attack follows (T08).

## File map and dependencies

Paths below are repository-relative. The following exact prefixes are abbreviations only:

| Prefix | Directory |
|---|---|
| `C` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator` |
| `B` | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems` |
| `S` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem` |
| `V` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem` |
| `L` | `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat` |
| `F` | `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame` |
| `TC` | `Assets/Tests/EditMode` |
| `TF` | `Assets/Tests/EditMode/Framework` |

New files are grouped by responsibility, not by a new root subsystem:

| Responsibility | New files | Existing integration owners |
|---|---|---|
| Immutable route and return contracts | `C/Contracts/Combat/MovementPath.cs`, `MovementPreviewContext.cs`; `C/Contracts/Results/MovementPathResult.cs`, `CheckpointReturnResult.cs`, `CheckpointReturnPreview.cs` | CombatCommandInput, CombatCommandTransition, CombatResult, CombatPreviewResult, MovePreview |
| Ledger and position contribution | `C/Contracts/Combat/PositionTileModifier.cs` | CharacterCombatFacts, CharacterData, CharacterCombatFactsComparer, CharacterBuffer |
| Traversal and return calculation | `C/Domain/Movement/MovementRouteValidator.cs`; `C/Application/Combat/CheckpointReturnCalculator.cs`, `MovementProjection.cs`, `PositionTileModifierCalculator.cs` | CombatMovementCalculator, CombatCommandCalculator, CombatTurnCalculator |
| Environment extension boundary | `C/Contracts/Combat/MovementEnvironmentFacts.cs`, `MovementStepInput.cs`, `MovementStepEffects.cs`, `IMovementTileRule.cs`; `C/Application/Combat/MovementEnvironmentCalculator.cs` | CombatCommandState, input composition, thorns, soul calculation, preparation adapters |
| Enemy path selection | `C/Application/Combat/EnemyAi/EnemyMovementPathfinder.cs` | EnemyAiPolicy, EnemyAiTurnService |
| Drag interaction | `V/StageInput/MovementPointerSampler.cs`, `MovementDraft.cs` | StageSelectInput, IStageInputReceiver, combat View owner |
| View integration and presentation | `V/Combat/CombatMovementInteraction.cs`, `CombatMovementPlayback.cs`, `ICombatMovementPresenter.cs` | Existing agreed CombatViewSystem/CombatSceneRoot if delivered; otherwise the minimal integration adapter described in T11 |
| Tests | `TC/MovementTestData.cs`, `MovementContractTests.cs`, `MovementRouteTests.cs`, `CheckpointReturnTests.cs`, `MovementEnvironmentTests.cs`, `PositionTileModifierTests.cs`, `EnemyMovementPathTests.cs`; `TF/MovementPublicationTests.cs`, `MovementDraftTests.cs`, `CombatMovementViewTests.cs` | Existing Core/Framework suites listed in each task |

Dependency order is `T01 -> T02 -> T03 -> T04 -> T05 -> T06 -> T07 -> T08 -> T09 -> T10 -> T11 -> T12`. Keep execution sequential until contracts and publication are stable. These are one cohesive feature's integration slices, not independently deployed subsystems.

T07 migrates player/View/service inputs but retains the existing endpoint entry point only for the explicitly outstanding AI callers. T08 migrates that final producer and then removes the endpoint API/branch and all ordinary-move endpoint fixtures in the same task. T06 migrates its own fixtures before changing their settlement expectations. This temporary compatibility boundary must compile; it is not an endpoint-to-route fallback, and T08 cannot be declared complete while it remains.

| Work ownership | Tasks and boundary |
|---|---|
| Core calculations and existing Buffer publication | T01-T08: immutable gameplay facts, calculations, equality, and prepare/install integration. Systems Factory composes dependencies; Core owns no scheduler or View lifecycle. |
| View-owned input/schema coordination | T01/T07 shared request, preview, result schemas require View/Core agreement; View owns CombatViewBuffer input production and response correlation. T09 implements gesture state. |
| View / Flow / Plan / Job integration | T10-T11 belong to the existing View scheduling/presentation owners, including Factory registration and scene exit. Their presence in this feature plan does not assign them to the Core implementer. |
| Cross-owner acceptance | T12 records each owner's evidence and remaining content/scene gates. |

Each task ends with a reviewed diff and recorded verification. If the user authorizes commits, commit only the task's files with the suggested message; otherwise leave verified changes in the working tree. Do not repeatedly ask for commit permission.

## Verification commands and evidence

The installed editor path was checked while planning; tests were not run. Before execution, verify the path again and avoid starting a second Unity instance against an already-open project. Use that editor's Test Runner or an isolated project checkout when necessary.

Use this PowerShell session helper for the commands below; it is a test invocation convenience, not production code. Run from the implementation checkout root. Piping the native editor invocation keeps the shell waiting for its completion.

```powershell
function Invoke-MovementTests {
    param([string]$RunName, [string]$Filter = '', [switch]$Full)
    $movementEditor = 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe'
    $movementEvidence = Join-Path (Get-Location) '.superpowers/sdd/2026-10-04-drag-path-movement'
    New-Item -ItemType Directory -Force -Path $movementEvidence | Out-Null
    $movementXml = Join-Path $movementEvidence ($RunName + '.xml')
    if (Test-Path -LiteralPath $movementXml) { throw 'Use a fresh RunName for every invocation.' }
    $movementArgs = @('-batchmode', '-nographics', '-projectPath', (Get-Location).Path,
        '-runTests', '-testPlatform', 'EditMode',
        '-assemblyNames', 'ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary',
        '-testResults', $movementXml, '-logFile', (Join-Path $movementEvidence ($RunName + '.log')))
    if (!$Full) {
        if ([string]::IsNullOrWhiteSpace($Filter)) { throw 'A focused run requires a filter.' }
        $movementArgs += @('-testFilter', $Filter)
    }
    & $movementEditor @movementArgs | Out-Host
    $movementExit = $LASTEXITCODE
    $movementExit | Set-Content (Join-Path $movementEvidence ($RunName + '.exit-code.txt'))
    if (!(Test-Path -LiteralPath $movementXml)) { throw 'No fresh test XML; inspect compilation/editor log.' }
    [xml]$movementResult = Get-Content -LiteralPath $movementXml -Raw
    $movementResult.'test-run' | Select-Object result, total, passed, failed, skipped
    if ([int]$movementResult.'test-run'.total -eq 0) { throw 'Zero discovered tests.' }
}
```

For every red run, inspect the named failure: a missing file, compile failure, undiscovered fixture, or unrelated baseline failure does not prove the behavioral test failed correctly. For every green run, require the requested cases to appear in XML, zero new failures/skips, and the appropriate process exit status. Use new run names for repeats. New Core test fixtures use namespace `ProjectL.Tests.Core`; Framework fixtures use `ProjectL.Tests.Framework`.

Historical evidence from 2026-10-03 is 1307/1312 passing with five Economy authoring failures. Refresh this baseline; do not assume those counts or failures still apply, and do not report a fully green suite while known failures remain. No balance assets should be fabricated to hide an unrelated authoring failure.

## T01: Introduce immutable route/result contracts and establish the baseline

**Files:** Create route/result files and `TC/MovementContractTests.cs`, `TC/MovementTestData.cs` from the file map. Modify `C/Contracts/Combat/CombatCommandInput.cs`, `CombatCommandTransition.cs`, `CombatResult.cs`, `CombatPreviewResult.cs`, `C/Contracts/Results/MovePreview.cs`, and `C/Application/Combat/CombatCommandComposition.cs`. Create `docs/superpowers/handoffs/2026-10-04-drag-path-movement-verification.md` as the execution evidence record.

**Interfaces:**

- `MovementPath(IReadOnlyList<CellCoord> cells)`; `IReadOnlyList<CellCoord> Cells`, `CellCoord Origin`, `CellCoord Endpoint`, `int StepCount`. Defensive copy; require at least an origin. Core admission handles illegal edges and revisits as normal rejection reasons.
- `MovementPathResult`: immutable `MovementPath RequestedPath`, `IReadOnlyList<CellCoord> EnteredCells`, `CellCoord FinalStop`, `int ActionCostSpent`, `int DistanceSpent`, `MovementStopReason StopReason`, `bool ReturnBarrierOccurred`. `EnteredCells` excludes the origin. Add stop reasons `Completed`, `Dead`, `MovementBlocked`, `ForcedRelocation`, `BattleEnded`, `NextStepInvalid`, `InsufficientResources` in this file.
- `CheckpointReturnResult`: `CellCoord From`, `CellCoord To`, `int ActionCostRefunded`, `int DistanceRefunded`.
- `CheckpointReturnPreview`: `bool CanReturn`, `MovementFailureReason FailureReason`, nullable `CheckpointReturnResult Return`.
- Add optional `MovementPath Path` and `MovementPreviewContext MovementContext` to `CombatCommandInput`; add `PathMovement` and `ReturnMovement` to transition/result without changing attack/placement meanings. Add `PathMovement` and `int ValidStepCount` to `MovePreview` for public projection/admission feedback, including an invalid draft's valid prefix, and a distinct return variant to `CombatPreviewResult`. `ValidStepCount` never authorizes executing a shortened command.
- `MovementPreviewContext` defensively retains `BattleInstanceId`, `Observer`, `ActorId`, intent kind, the exact requested path (or Return checkpoint), and only the named public expected values in T07's capture table. T01 introduces identity and expected fields using existing types; T02 appends the new ledger/modifier/environment fields when their types exist. Do not reference a future task's missing type at a compiling boundary. Do not retain/expose a whole `CombatCommandState`, an unfiltered character list, or omniscient environment facts. The constructor's admission input is only a selector for the named scalar fields; it is not stored or compared as a state object. T07 supplies capture/matching and the battle identity producer; T09 owns request/revision correlation separately.
- `MovementTestData.SafeRoute(int ap, int range, int moveCost, params CellCoord[] cells) -> CombatCommandInput`, `Actor(CombatCommandTransition) -> CharacterCombatFacts`, and `After(CombatCommandTransition) -> CombatCommandInput` are test-only helpers built on `CombatCommandTestData`. `After` carries all projected character/field/main/count/state facts; it never restores stale original facts.

- [x] Record HEAD, branch, working-tree status, incoming file hashes, editor version, current Job stubs, and missing View/content providers in the evidence record. Run `Invoke-MovementTests -RunName baseline -Full` and preserve exact failure identities/messages.
- [x] Add `Path_CopiesCells_AndCountsEdges` and `Result_CopiesEnteredPrefix_AndPreservesDeadStop`. Required assertions:

```csharp
var cells = new[] { new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(2, 2) };
var path = new MovementPath(cells);
cells[1] = new CellCoord(7, 7);
Assert.That(path.Cells[1], Is.EqualTo(new CellCoord(2, 1)));
Assert.That(path.StepCount, Is.EqualTo(2));
Assert.That(path.Origin, Is.EqualTo(new CellCoord(1, 1)));
```

- [x] Run `Invoke-MovementTests -RunName t01-red -Filter ProjectL.Tests.Core.MovementContractTests`; establish the missing contract/defensive-copy failure, then implement the declared DTOs and helper methods.
- [x] Preserve every new payload through `WithCompletion`, result publication constructors, and `CombatCommandComposition.Intent/Project/Merge`. For composed automatic turns, retain the entire ordered event stream, including movement followed by attacks, deaths, and turn completion even when the top-level intent is Attack or EndUnitTurn. T11's overall View owner consumes this stream; path/return payloads do not create a second playback stream. Append enum values rather than renumbering serialized existing values.
- [x] Run `Invoke-MovementTests -RunName t01-green -Filter 'ProjectL.Tests.Core.MovementContractTests;ProjectL.Tests.Core.CombatResultContractTests'`. Inspect defensive-copy and rejected-result invariants. Review the diff; suggested commit: `feat: add immutable movement path contracts`.

**Deliverable:** Additive compiling contracts, with no change to live movement behavior yet. Retain the old endpoint branch only until the final AI migration and API removal in T08; do not generate a route from that endpoint as a permanent fallback.

## T02: Extend character/environment facts through existing Buffers

**Files:** Create `C/Contracts/Combat/PositionTileModifier.cs` and the environment contracts in the file map. Modify `C/Contracts/Combat/CharacterCombatFacts.cs`, `CombatCommandState.cs`, `MovementPreviewContext.cs`, `BattleInitialization.cs`, `BattlePreparation.cs` (its `CombatPreparation` payload), `C/Application/Combat/BattlePreparationCalculator.cs`, `B/Character/CharacterData.cs`, `CharacterCombatFactsComparer.cs`, `CharacterBuffer.cs`, `B/Combat/CombatStateData.cs`, `CombatStateBuffer.cs`, and `S/CombatSystem/CombatProcessingService.cs` input composition. Add tests to `TF/MovementPublicationTests.cs` and `TC/MovementContractTests.cs`.

**Interfaces:**

- Append `int pendingMoveDistance = 0`, `int attackRefundEligibleMoveCost = 0`, and `PositionTileModifier positionModifier = null` to character construction. Expose `PendingMoveDistance`, `AttackRefundEligibleMoveCost`, and a non-null neutral `PositionModifier`.
- `PositionTileModifier` stores source cell, source IDs, signed attack/max-HP contributions, and value equality; `None` has no sources/contribution. Keep it separate from artifact/soul temporary fields. Reject content whose effective maximum HP is nonpositive.
- `MovementEnvironmentFacts(IReadOnlyList<MovementTileFact> tiles)` is an immutable list in `CombatCommandState`, not a new physical Buffer. Missing environment input maps to an empty list. In the same contract file, define abstract immutable `MovementTileFact` with `long Id`, `CellCoord Cell`, `string RuleKey`, `int Priority`, `bool Active`, defensively copied `IReadOnlyList<Owner> KnownTo`, and `bool SameValues(MovementTileFact other)`. Concrete rule facts carry their own typed parameters and implement value equality; no mutable parameter dictionary or arbitrary object payload is allowed.
  `WithKnownTo(IReadOnlyList<Owner>)` copies the same concrete typed source with only its visibility replaced. `MovementEnvironmentFacts.VisibleTo(observer)` removes unknown sources and retains only that observer in each visible copy; other observers' knowledge lists must not enter the public context. Runtime source equality still compares the full authoritative visibility.
- Destination source facts are a concrete typed variant in the existing `MovementEnvironmentFacts.cs`: cell, stable source ID, active/observer visibility, signed attack and maximum-HP contributions. Only active sources applicable to that cell contribute. Resolve source combinations only according to a supplied typed content definition; overlapping sources without an authored combination rule remain unsupported content, not an invented stacking policy. Single-source fixtures need no stacking subsystem. No production values are implied. The preparation/content adapter copies these facts into `CombatCommandState`; the existing Combat Buffer preserves them into command/preview inputs. `CharacterCombatFacts.PositionModifier` records the contribution already applied to that actor, never the tile's source definition. No Terrain/Modifier Buffer is added.
- `IMovementTileRule`, `MovementStepInput`, and `MovementStepEffects` are pure, bounded tile-evaluation contracts; they are not combat-command processors. The rule exposes `string Key`; `Evaluate(MovementStepInput input, MovementTileFact tile) -> MovementStepEffects` selects the one tile's applicable effects. Input carries the entered cell, current mover snapshot, and only the current target/profile/effect facts needed by that concrete rule. It has no `CombatCommandInput`, command number, full combat state, completion, pending ledger authority, or mutable projection reference. The coordinator selects targets from the latest projection before each evaluation; no precomputed batch of stale inputs.
- `MovementStepEffects` contains only typed requests/contributions for that selected effect and a source-scoped expected/final tile change when consumption or a gimmick mutation is requested. Use public typed source specifications where their semantics fit. Within `MovementStepEffects`, bound health requests to copied target/requested-amount values and forced movement to target/direction/distance values, with source identity taken from the selected tile; no arbitrary callbacks or property paths. A hazard's raw damage is not a fabricated attack/category. The Application adapter maps these values to existing `EffectHealContribution`, `EffectKnockbackContribution`, and effect-state calculations where applicable; those Domain-internal types must not leak into public Contracts. A new Object/gimmick content kind must supply its concrete typed source facts rather than a generic mutation payload. No generic mutation list, independent rule engine, `CharacterChanges`/whole-state replacement, `SoulFieldChange`, free-form event list, barrier labels, or stop reason is returned by a tile rule.
- Existing calculation helpers resolve those contributions in T05. Their resolved `CharacterStateChange`/`CharacterChanges`, structure changes, effect-state changes, and `SoulActionChanges`/`SoulFieldChange` remain the merge contracts at the coordinator boundary. `FinalState` belongs only to the final combat transition, not to a tile-rule result.
- `MovementEffectCause` (`Damage`, `Healing`, `Death`, `ObjectConsumed`, `ObjectAcquired`, `GimmickChanged`, `PositionModifier`, `HealthNormalized`, `MovementTrait`) is resolved evidence produced by the coordinator from actual typed calculator results and validated source changes. It is not a rule-supplied barrier switch. Source-only changes require expected-value validation; unchanged/inactive source consumption is a no-op, not an invented barrier.

- [x] Add `NewLedgerFacts_RoundTripAndRejectStaleExpectedValues`, `EnvironmentFacts_CopyAndCompareByValue`, and `PositionModifier_DoesNotOverwriteArtifactBonuses`. Compare independently changed pending distance, refund basis, modifier source, and consumed/visibility state.
- [x] Run the essential RED only: `Invoke-MovementTests -RunName t02-ledger-red-retry -Filter 'ProjectL.Tests.Framework.MovementPublicationTests.NewLedgerFacts_RoundTripAndRejectStaleExpectedValues'`. All three stale-field cases failed because publication did not throw. The initial compile failure is not behavioral RED evidence.
- [x] Implement validation and conversions in the existing stores. Audit all `new CharacterCombatFacts` / `new CombatCommandState` call sites with `rg`; update copies in calculation, preparation, cleanup, Artifact projection, and test builders. Defaults are permitted for initial state only, not for copying existing state.
- [x] Carry the immutable environment payload through `BattleInitialization -> BattlePreparationCalculator -> BattlePreparation.Combat (CombatPreparation) -> CombatStateBuffer/CombatCommandState -> CombatProcessingService.ReadInput`. Add/copy that payload in the existing contracts, defaulting to empty only at initial construction. The existing definition source supplies authored facts when supported; a fixture initialization source supplies explicit test values until then. `ReadInput` reads installed runtime source state, not a fresh DB copy that would revive consumed Objects. Extend the existing prepare/install path to include environment facts and new character fields; do not install them independently. Do not add gameplay calculations to Buffer setters.
- [x] Run `Invoke-MovementTests -RunName t02-green -Filter 'ProjectL.Tests.Framework.MovementPublicationTests;ProjectL.Tests.Core.MovementContractTests;ProjectL.Tests.Framework.CombatStateBufferTests'`. Require failed expected-state validation to leave all stores and result counters unchanged. Suggested commit: `feat: persist movement ledger in existing buffers`.

Execution scope: the user approved focused core tests and source inspection for indirectly covered forwarding paths. T02 passed 24/24 cases in real Unity EditMode through the approved AppData-access path; no full-suite rerun or separate copier RED runs were required. See the verification handoff for exact evidence and later calculation/content gates. No commit was made.

**Deliverable:** New state has one authoritative owner and complete equality/copy/publication coverage. Production environment content is not implied by accepting an empty or fixture list.

## T03: Implement path billing, traversal validation, and explicit Return

**Files:** Create `C/Domain/Movement/MovementRouteValidator.cs`, `C/Application/Combat/MovementProjection.cs`, `CheckpointReturnCalculator.cs`, `TC/MovementRouteTests.cs`, `CheckpointReturnTests.cs`. Modify `CombatMovementCalculator.cs`, `CombatCommandCalculator.cs`, and `C/Contracts/Results/MovementFailureReason.cs`.

**Interfaces:**

- `MovementRouteValidator.Validate(CombatCommandInput input, MovementPath path, out int validStepCount) -> MovementFailureReason`: origin matches actor, in bounds, orthogonal edges, no repeated cells, no other occupied cells, and full-route affordability/range. Return the valid prefix length for draft feedback; command admission still rejects the whole invalid path.
- `MovementProjection` is an internal command-local accumulator, constructed from `CombatCommandInput`; it holds projected characters/environment/souls/main, ordered events, entered cells, spent resources, stop reason, and sticky barrier. It never escapes as authoritative mutable state and is discarded after this command. It merges existing calculator outputs; attack policy, soul/death/outcome policy, rule composition, presentation, and publication stay with their existing owners. Preserve original expected facts for final publication separately from successive projected values.
- Keep the Preview/Range/Calculate result types; route inputs use the new evaluator. T05 adds the same required composed rule dependency to Preview and Calculate and forwards it through all dispatch/composition paths; no optional rule-set fallback. Range uses shared reachability and reports minimum reachable step cost only as a hint.
- `CheckpointReturnCalculator.Preview(CombatCommandInput) -> CheckpointReturnPreview` and internal `Calculate(CombatCommandInput) -> CombatCommandTransition`. Add `ReturnToCheckpoint` to command kinds.
- Pure ledger helpers in `CombatMovementCalculator`: internal `SealPendingMovement(CharacterCombatFacts) -> CharacterCombatFacts` and `RecordEnteredStep(CharacterCombatFacts, int stepCost) -> CharacterCombatFacts`.

- [x] Add `Detour_ChargesFourSteps`, `MultipleDrags_ReturnRefundsSixSteps`, `PaidLoop_SameAnchorReturnRefundsLedger`, `BlockedIntermediateCell_RejectsAll`, `EnemyReturn_IsRejected`, and `ZeroMoveCost_ReturnStillRestoresRange`. Use AP/range 20, MoveCost 1, an orthogonal four-edge detour with displacement two, then a two-edge safe continuation. Pin values:

```csharp
Assert.That(first.PathMovement.DistanceSpent, Is.EqualTo(4));
Assert.That(first.PathMovement.ActionCostSpent, Is.EqualTo(4));
Assert.That(afterSecond.PendingMoveDistance, Is.EqualTo(6));
Assert.That(afterSecond.Stats.CurrentActionCost, Is.EqualTo(14));
Assert.That(returned.ReturnMovement.DistanceRefunded, Is.EqualTo(6));
Assert.That(returnedActor.Stats.RemainingMoveRange, Is.EqualTo(20));
```

- [x] Run `Invoke-MovementTests -RunName t03-red -Filter 'ProjectL.Tests.Core.MovementRouteTests;ProjectL.Tests.Core.CheckpointReturnTests'`; confirm endpoint billing and implicit refunds fail the new assertions. Five failures were behavioral; the zero-cost fixture initially violated authored UnitSpec validation. `t03-zero-cost-red` then confirmed its real Return rejection with zero runtime cost.
- [x] Implement route admission and per-edge cost bookkeeping. Ordinary costs are nonnegative and never refunded by direction. Return refunds recorded pending resources and clears pending distance/cost while preserving `CommittedMoveDistance`. Seal commits pending distance and clears pending distance/cost without a checkpoint refund, renewing the checkpoint at the final stop (design Sections 7.1-7.2). Ordinary movement accumulates paid distance/cost in either direction. Keep `AttackRefundEligibleMoveCost` separate as specified in T06; environmental sealing does not clear it. Refund actual stored cost, including zero-cost steps, and reject invalid/occupied checkpoints without mutations.
- [x] Emit entered-cell movement events and one aggregate path result. Keep traits once per moved command and preserve them on Return. Return uses its own result/event, with no traversal or acquisition callbacks.
- [x] Run the focused final verification, `t03-final`, covering MovementRouteTests, CheckpointReturnTests, MovementPublicationTests, and the two retained endpoint charge/refund compatibility cases. All 32 cases passed, including checked-arithmetic/malformed route checks and real Buffer route/Return publication. See the handoff for the exact filter. Suggested commit: `feat: execute explicit routes and checkpoint returns`.

Execution scope: T03 completes the effect-free Core route/Return slice. Its minimal projection keeps unchanged environment/soul/main facts in the original immutable input; T04/T05 add only the projected changes those calculations need. T03 records paid attack-refund basis and subtracts checkpoint refunds so Return can preserve a coherent separate ledger; T06 still wires attack consumption/reset and replaces legacy attack/turn sealing. Endpoint Calculate/Preview compatibility remains until T08; it never synthesizes a route. Range now quotes reachable minimum cost from a valid route ledger, and cannot authorize a substitute path. No full-suite rerun or commit was performed.

**Deliverable:** Effect-free route/return behavior is testable through the Core dispatcher; normal live View migration still waits for T07.

## T04: Reconcile destination modifiers and maximum HP

**Files:** Create `C/Application/Combat/PositionTileModifierCalculator.cs`, `TC/PositionTileModifierTests.cs`. Modify `CombatMovementCalculator`, `CheckpointReturnCalculator`, `MovementProjection`, `CharacterSettlementCalculator`, `CombatCommandEffects`, `CombatEvent`, `UnitStatsSnapshot`, `SoulEffectCalculator`, `ArtifactAttackPowerEvaluator`, the existing `SoulGrowthMath`, and Character Buffer validation/comparison. Extend the existing soul/publication tests and affected stat-copy helpers. Inspect `ArtifactActionProjection.ProjectCollection`; it already preserves the supplied final character and needs no edit.

**Interfaces:** `PositionTileModifierCalculator.ResolveAt(CellCoord finalStop, MovementEnvironmentFacts environment) -> PositionTileModifier` selects active destination-source facts at the actual stop and resolves their authored contributions/source IDs (or `None`), with checked arithmetic. Unsupported source combinations remain gated as in T02. `Replace(CharacterCombatFacts actor, PositionTileModifier next) -> CharacterCombatFacts` subtracts the actor's prior tile contribution and adds that resolved contribution. The producer of `next` is always `ResolveAt`, not the View or an ad hoc caller value. Add a typed `HealthNormalized` event/cause that existing damage-trigger dispatch cannot mistake for damage.

Data path: authored typed definition / fixture preparation input -> existing Combat Buffer environment facts -> command-local current environment -> `ResolveAt(actualStop, environment)` -> `Replace(latestActor, resolved)` -> final `CharacterChanges` -> existing Character Buffer prepared publication. No separate modifier service, rule registry, or store is needed.

The existing stat contract carries a separate signed `PositionAttackBonus`; `EffectiveAttackPower` equals base + Artifact/soul/temporary contribution + position contribution. Preserve it through existing stat copies and validate it against `CharacterCombatFacts.PositionModifier.AttackBonus` at publication. Effective attack remains nonnegative, effective maximum HP remains positive, and checked arithmetic rejects invalid/overflowing content before publication. No new clamping or source-stacking policy is introduced. Main HP normalization also proposes the corresponding persistent current-HP change; persistent base stats remain unchanged. `HealthNormalized` is an appended event kind, never a damage-trigger dispatch or a Return barrier; T05 maps it to the existing typed normalization cause.

- [x] Add `ApplyMaxHp_DoesNotHeal`, `RemoveMaxHp_CapsWithoutDamage`, `PassThrough_DoesNotApplyModifier`, and `Return_RecomputesModifierWithoutRestoringHp`. Include a pre-existing nonzero soul/artifact bonus.

```csharp
Assert.That(applied.Health.CurrentHealth, Is.EqualTo(8));
Assert.That(applied.Health.MaxHealth, Is.EqualTo(15));
Assert.That(removed.Health.CurrentHealth, Is.EqualTo(10));
Assert.That(removed.Health.MaxHealth, Is.EqualTo(10));
Assert.That(normalizationCauses, Does.Not.Contain(MovementEffectCause.Damage));
```

- [x] Run the essential route-based RED; implement `ResolveAt`, `Replace`, and typed normalization. `t04-behavior-red` discovered one case and failed because the typed destination source did not change maximum HP (1 failed, editor exit 2). Earlier `t04-red` had a fixture property-name compile error and is not behavioral evidence.
- [x] Remove the prior position contribution only after route admission succeeds and before entering the first cell. Apply `ResolveAt` once at the actual normal stop; Preview uses observer-filtered environment facts. Return calls only `ResolveAt(checkpoint, currentEnvironment)` and `Replace`, with no entry-rule dispatch, Object consumption, or soul acquisition. Intermediate route cells never apply destination modifiers. T05/T06 wire this existing resolver after eligible final soul/other arrival effects, at surviving interruptions, with terminal/dead eligibility guards (design Section 8.2, step 8); those effects do not yet exist in the T04 slice.
- [x] Test repeated replacement idempotence, negative position modifiers, and removal after a separate health calculation to 14/15. Prove the cap alone does not cause damage or a new barrier, and preserve caps through later Return. T05's existing healing/barrier cases still establish that an actual heal seals pending movement; the T04 stat test does not authorize Return after such a gameplay heal.
- [x] Verify soul growth and artifact recalculation retain the independent tile contribution in effective maximum HP/attack without folding it into `TemporaryMaxHealthBonus`, `TemporaryAttackBonus`, or permanent soul counts. A subsequent Return removes only the tile contribution.
- [x] Run focused final verification `t04-final-verified`: PositionTileModifierTests, ArtifactSoulBufferCalculationTests, MovementRouteTests, CheckpointReturnTests, and MovementPublicationTests passed 67/67, zero skips, editor exit 0. This includes 12 new modifier cases, two new soul/position cases, and one additional Buffer-publication variant. No full-suite rerun or commit.

- [x] Extend `Return_RecomputesModifierWithoutRestoringHp` and `PassThrough_DoesNotApplyModifier` with actual typed environment facts. Extend the existing `RouteAndReturn_PublishLedgerAndOrderedEventsOncePerCommand` through installed preparation facts and existing Buffer publication: positive modifier and negative modifier/HP-cap variants verify provenance, independent effective stats, Main current-HP synchronization, unchanged tile source data, and Return without HP restoration. T07 still verifies the registered-service Preview/interruption path with its existing planned case; do not duplicate this direct publication coverage.

**Deliverable:** Location effects are isolated from existing bonuses and use the confirmed maximum-HP behavior.

Execution scope: T04 completes the Core/Buffer destination calculation and publication slice only. Production modifier definitions/combination policy remain gated; no production values, new Buffer, registry, assembly references, or View/Flow/Job infrastructure were added. Hazard barriers/interruption and final soul-arrival ordering remain T05/T06; public context/service migration and presentation remain their declared later tasks. Evidence: [verification handoff](../handoffs/2026-10-04-drag-path-movement-verification.md).

## T05: Resolve ordered entry effects and interruption on projected state

**Files:** Create `C/Application/Combat/MovementEnvironmentCalculator.cs` and `TC/MovementEnvironmentTests.cs`. Keep fixture rule adapters in the existing planned test files/helpers; use a small local adapter in `TF/MovementPublicationTests.cs` for its registered-service case, without a cross-test-assembly dependency or another fixture framework. Modify `C/Application/Combat/CombatMovementCalculator.cs`, `MovementProjection.cs`, `CombatCommandCalculator.cs`, `EnemyAi/EnemyAiTurnService.cs`, `C/Contracts/Combat/CombatEvent.cs`, `S/CombatSystem/CombatProcessingService.cs`, and `F/Core/InGameCoreSystemFactory.cs` for explicit rule composition; update affected caller fixtures in this task.

**Interfaces:** Internal `MovementEnvironmentCalculator.ResolveEntry(MovementProjection projection, CellCoord enteredCell, IReadOnlyList<IMovementTileRule> rules) -> void` coordinates the existing pure calculations within this one command. Only the coordinator receives the projection; each rule receives T02's narrow current input. Bind explicit rule keys in existing Core calculation composition, with the production thorn adapter and fixture adapters for missing content. This is a bounded movement adapter, not a new effect engine, Framework service, registry, or generic mutation interpreter.

T05 contract refinement: `MovementStepInput.Phase` distinguishes Hazard from Recovery; evaluate each phase against the latest projected facts. Hazard output cannot contain healing, and Recovery output cannot contain damage or forced movement. The input adds copied `MovementHealthTarget` values (ID, owner, HP and occupancy only) and current `SourcesAtCell`, including inactive sources, so a subsequent rule can observe prior target/source changes without receiving a combat input/state or movement ledger. A source change is restricted to the selected concrete source's existing identity/cell/key and priority/visibility; metadata edits cannot masquerade as gameplay mutations. The narrow `MovementMethodChange` carries target/expected/final values of the existing MoveMethod field, never a whole stats replacement. Changed HP, source gameplay state, movement method or successful forced displacement supplies actual barrier evidence; no-op/blocked requests, ordinary step occupancy, destination normalization and movement-success bookkeeping do not.

The `ThornMovementTileRule` adapter reads the existing typed `ThornFieldCellSnapshot` directly through `EvaluateThorn`; it does not create a duplicate environment fact or invent thorn instance IDs. Resolve that existing thorn category before the configured typed-source hazard phase, then evaluate Recovery. Source rules use priority then stable source ID. Each phase reselects current active sources; a consumed source does not run again. Thorn damage retains its raw environmental semantics and does not acquire attack-category/defense rules. Other authored hazard mitigation policies remain content-specific; fixture health requests use explicit amounts, including zero.

| Responsibility | Owner and permitted result |
|---|---|
| Select sources/targets and evaluate one tile | Coordinator selects from latest projected facts in deterministic category/priority/ID order; rule returns the narrow typed contributions and source change defined in T02. |
| Resolve damage, healing, forced relocation | Coordinator invokes existing helpers/effect-engine paths for the selected contributions, respecting the source's current semantics (thorns do not acquire attack/category rules). Extract a narrowly reusable helper only where existing calculation is private; do not copy the engine. |
| Resolve death, soul spawn/arrival, outcome/completion | Existing calculators invoked by route/arrival orchestration at design Section 8.2's boundaries. A rule cannot collect final-arrival souls, finish a turn, choose an outcome, or publish. |
| Merge resolved changes | `MovementProjection` validates each helper's Expected against the latest projection and updates only named character/source/structure/field entries. Preserve all untouched facts and earlier changes; do not copy a helper's original whole state back over the projection. |
| Pay steps, interrupt, seal, publish | Movement calculator owns billing and derives interruption from resolved survival/mobility/location/outcome. It seals once through the final stop. Processing service submits one final transition to the existing prepare/install boundary. |

The merge keeps the command's original expected facts for publication, the latest per-entity/source final facts, and all resolved events in order. Two changes to the same entity chain `original -> firstFinal -> secondFinal`; the final proposal is `original -> secondFinal`, not a second change based on the original input. Source updates are keyed, expected/final changes; one consumed source never replaces the whole environment list. Reject an invalid helper expectation before publication. Do not introduce a general patch/transaction framework.

The internal character merge requires the latest immutable fact instance as its expected value; source merges compare the selected latest typed value. Death boundaries reuse existing soul-spawn/outcome calculation and remember only deaths resolved in this command, preventing final Finish from spawning them twice. Final Finish reuses existing terminal-health, turn/timeline cleanup and outcome calculation; the existing processing-service Publish adds completion/progression to that same transition. Forced displacement reuses existing one-cell geometry, passability and thorn traversal, preserves the ordinary paid ledger, and invokes existing forced arrival only once, marked locally for the mover. Normal path soul collection still joins this boundary in T06.

Compose one immutable, non-null supported rule list at the existing Factory/processing-service boundary and require it through the internal `CombatCommandCalculator.Calculate/Advance`, `CombatMovementCalculator.Preview/Calculate`, and `EnemyAiTurnService.Calculate` paths. Update every affected call site and fixture in T05 so this task compiles. No optional/null argument may silently select built-ins instead of the composed rules. Preview filters source facts by knowledge, not the rule set; normal/interrupted arrival and Return all use T04's same destination resolver. Rules retain no mutable battle state and are not embedded in inputs. Keep Domain-internal contributions inside Application's existing friend access. Public rule ports carry only Contracts types, so Systems Factory and Framework fixture rules can compose the same list without new friend declarations or assembly references.

An active unsupported source in the evaluated input fails explicitly; an unknown hidden source cannot leak through public preview support checks. Missing production definitions remain gated. A thorn adapter reads/consumes existing `ThornFields`; never duplicate a thorn as a second environment entry.

- [x] Add `MineSealsPriorSafeDragsAtFinalStop`, `DamageThenHeal_RemainsBarrier`, `OtherEntityDamage_BlocksReturn`, `ZeroDamageConsumedMine_BlocksReturn`, `NoOpHeal_DoesNotBlockReturn`, and `LethalHazard_PreventsHealingAndSuffix`. Give the lethal fixture actor HP 2, damage 2 on the second entered cell, a heal on that cell, and a four-step route.

```csharp
Assert.That(result.Accepted, Is.True);
Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(2));
Assert.That(result.PathMovement.ActionCostSpent, Is.EqualTo(2));
Assert.That(result.PathMovement.StopReason, Is.EqualTo(MovementStopReason.Dead));
Assert.That(result.PathMovement.ReturnBarrierOccurred, Is.True);
Assert.That(remainingSuffixEffects, Is.Empty);
```

- [x] Run `Invoke-MovementTests -RunName t05-red -Filter ProjectL.Tests.Core.MovementEnvironmentTests`; implement entry resolution with explicit priority then stable instance-ID ordering and hazards/death before healing. Derive sticky barrier evidence after each resolved operation: positive actual damage/healing to any entity, actual death, actual soul/Object consumption/acquisition, or a validated gameplay-changing gimmick delta. Check per-operation outcomes, not final net HP or a rule's claimed cause. A prevented hit plus consumed mine still seals; zero effective healing with no source mutation does not. Normalization and destination/trait bookkeeping alone remain excluded.
- [x] Stop before charging any suffix cell after death, movement blocking, forced relocation, changed passability, insufficient projected resources, or battle termination. Preserve the final stop in the result even when final facts exclude a dead actor. Seal all prior pending movement through the final stop only after arrival settlement finishes.
- [x] Reuse existing death, soul-spawn, outcome, and completion calculations. For main death and enemy-headquarters destruction, preserve existing terminal precedence and publish the completion in the same transition. Never heal a dead mover or execute post-terminal effects.
- [x] Add `SurvivingInterruption_HasOneFinalArrival`, `ForcedRelocation_DoesNotResumeOriginalRoute`, and `TerminalEffect_PreventsLaterSteps`; extend the existing `DamageThenHeal_RemainsBarrier` sequential-effects case so the first rule damages another entity and consumes source A, while the next heals from the latest HP and changes source B. Assert the second rule observes both first changes, final state retains A and B plus unrelated characters, and final publication expectations still refer to original Buffer facts. Include a stale helper expected value and prove it cannot overwrite the first change. These are assertions in the existing fixture, not another test framework. Run `Invoke-MovementTests -RunName t05-green -Filter 'ProjectL.Tests.Core.MovementEnvironmentTests;ProjectL.Tests.Core.ThornFieldDomainTests;ProjectL.Tests.Core.KnockbackTraitTests'`. Suggested commit: `feat: resolve movement hazards in route order`.

**Deliverable:** Typed entry effects and partial accepted results work without per-cell publication. Missing production mine definitions remain a named content dependency, never a silently ignored rule or a claim of playable mine completion.

Execution record: T05 is complete for this Core/Buffer calculation/composition slice. Essential `t05-red` failed both entry-thorn damage and mine sealing assertions (2 failed, zero skips, editor exit 2). Final `t05-final` passed 95/95, zero skips, editor exit 0, covering the planned environment/thorn/knockback cases plus route, Return, destination, AI, Preview and existing publication integration. Fifteen environment cases and one additional direct Buffer variant are new. The sequential damage/heal case and existing Buffer route case both prove current facts are observed and prior character/source changes survive. A projected occupancy change also proves an unexecuted suffix cell is not charged. Mechanical caller migration includes EnemyAiPolicy's attack-preview dependency and direct service-construction fixtures; all compiling callers now pass an explicit rule list. The production Factory composes an immutable thorn list and the service copies it once; it never picks a supported rule set from an omitted/null argument.

Remaining ownership/integration: no View-owned schema/input/Flow/Job/playback migration is included. AI endpoint production remains the declared temporary T08 compatibility branch, with no newly synthesized route fallback. T06 still owns normal final soul collection, removal of deferred voluntary settlement, and attack-category refund/reset integration; T07 still owns public freshness and registered route/Return ports. Mine/Object/modifier authored content, mitigation/combination policy and visibility providers remain gated. No production values, new Buffer/registry, Framework infrastructure, assembly references, scenes, commit or push were added. See the [verification handoff](../handoffs/2026-10-04-drag-path-movement-verification.md).

## T06: Move soul arrival to movement and preserve attack/turn semantics

**Files:** Modify `C/Contracts/Combat/SoulCollectorInput.cs`, `C/Application/Artifact/SoulEffectCalculator.cs`, `C/Application/Combat/CombatCommandCalculator.cs`, `CombatTurnCalculator.cs`, `CombatMovementCalculator.cs`, `CheckpointReturnCalculator.cs`, and `C/Domain/CombatEffects/Contexts/AttackActionCostRefundEffectContext.cs`. Tests: `TC/MovementEnvironmentTests.cs`, `CombatCommonCommandTests.cs`, `CombatCommandEffectTests.cs`, `ArtifactSoulBufferCalculationTests.cs`, `RiderManeuverRefundTests.cs`.

**Interfaces:** Add `SoulArrivalKind` (`SettledVoluntary`, `ResolvedPath`, `Forced`) and carry it in `SoulCollectorInput`. Keep `Collect(SoulCollectorInput, SoulFieldSnapshot, bool movementSucceeded) -> SoulActionChanges`. A `ResolvedPath` invocation is valid only from the finalized living arrival projection; do not bypass validation by setting `ForcedArrival = true`. Preserve legacy forced callers through explicit mapping.

- [x] Add `Soul_CollectedOnlyAtActualDestination`, `InterruptedArrival_CollectsOnce`, `AttackAndTurnEnd_DoNotRepeatMoveEffects`, and `AttackFailure_PreservesPendingLedger`. Retain main-character persistent HP synchronization expected by `SoulCollectorInput` before invoking collection. Failure preservation is covered inside the composed barrier/safe-interval/Return/attack scenario rather than a duplicate fixture.
- [x] Run essential RED for normal arrival, surviving interruption and the separate refund basis (`t06-red-arrival-refund`: 3 failed / 1 forced-arrival case passed); remove deferred voluntary thorn/soul settlement from attack and turn-end only after its movement equivalent is in place. Preserve field countdowns and forced-arrival behavior.
- [x] Implement the already approved, separate attack-refund compatibility adapter (design Section 7.4): record actual paid movement in `AttackRefundEligibleMoveCost`; subtract checkpoint refunds; preserve it through environment barriers; capture it before successful attack settlement and then clear it. Preserve existing category cap and once-per-turn flags. Clear on turn end/start. Record scenario evidence for this approved behavior; do not reopen it or equate checkpoint refunds with attack-category refunds.

```csharp
Assert.That(afterBarrier.PendingMoveActionCost, Is.Zero);
Assert.That(afterBarrier.AttackRefundEligibleMoveCost, Is.EqualTo(4));
Assert.That(afterSafeTwoSteps.AttackRefundEligibleMoveCost, Is.EqualTo(6));
Assert.That(afterReturn.AttackRefundEligibleMoveCost, Is.EqualTo(4));
```

- [x] Test successful attack sealing versus failed attack/mode cancellation, return never undoing Focus/MoveAttackDiscount, and a new safe interval after a barrier. Keep traits once per moved drag, with actual entered distance/cost; do not apply them per cell. Reuse the existing route/Return trait case. View-only cancellation has no Core command and remains owned by T09; rejection has no seal or interval reset. A movement-blocked living actor may still end its turn.
- [x] Migrate the affected old fixture calls to explicit routes before changing their geometric-refund expectations; record each superseded assertion and retain its still-valid failure/atomicity coverage. `t06-green-migrated`: 184/184 passed, zero skips, editor exit 0, using the five suggested fixtures plus existing MovementRouteTests and MovementPublicationTests. See the verification handoff for the exact command and migration evidence. No commit or push.

**Compiling boundary:** T07/T08 still own ordinary caller/API migration. The explicitly temporary endpoint branch keeps its displacement-based billing and retarget semantics; it now resolves its existing thorn traversal and finalized soul arrival during Move, records its existing pending displacement and refund basis, and seals environmental/enemy arrivals. It does not synthesize a MovementPath, provide the new route rule pipeline, or claim endpoint displacement is route length. Its removal remains a T08 check. Only finalized command-local arrivals use `ResolvedPath`; forced callers map explicitly to `Forced`. No new Buffer or presentation owner is introduced.

**Deliverable:** Movement, attack, souls, traits, and turn settlement share one consistent ledger without duplicate effects.

## T07: Publish route intents, pure previews, and stale-context rejections

**Files:** Modify `B/CombatView/CombatViewCommand.cs`, `CombatViewCommandKind.cs`, `CombatViewPreview.cs`, `CombatViewPreviewKind.cs`, `S/CombatSystem/CombatProcessingService.cs`, `B/Core/BufferSystemGroup.cs`, `B/Combat/CombatStateBuffer.cs`, `C/Application/Combat/CombatAttackQueries.cs`, and the T01 context contract. Tests: `TF/MovementPublicationTests.cs`, `CombatViewBufferTests.cs`, `CombatPreviewPublicationTests.cs`, `CombatCommandPublicationTests.cs`, `CombatCommonPublicationTests.cs`.

**Interfaces:**

- `CombatViewCommandData.Move(EntityId unit, MovementPath path, MovementPreviewContext context)` and `ReturnToCheckpoint(EntityId unit, MovementPreviewContext context)`.
- `CombatViewPreviewData.Move(EntityId unit, MovementPath path)` and `ReturnToCheckpoint(EntityId unit)`; existing `MoveRange(EntityId)` remains.
- `MovementContextCalculator.Capture(CombatCommandInput, Owner observer, IReadOnlyList<IMovementTileRule> rules) -> MovementPreviewContext` and `Matches(MovementPreviewContext, CombatCommandInput, Owner observer, IReadOnlyList<IMovementTileRule> rules) -> bool` live in new `C/Application/Combat/MovementContextCalculator.cs`. The rule argument is mandatory and is the same immutable composition used for Preview and execution. Capture filters unknown sources before the public projection, then applies the table below to its queried dependencies; Matches recaptures the same intent/path for the same observer and compares those named values. No call to broad `CombatCommandState.SameValues` or whole-result equality is permitted for preview freshness.
- `MovePreview.ProjectedActor`, `PublicEffects`, `QuotedActionCost`, `QuotedDistance`, and `CanReturn` expose the final public projection specified in design Section 9.1. Final settlement and Return eligibility use the existing pure calculations without publishing. `CombatPreviewResult.MovementContext` transports the captured original expected facts separately from this projected result. Schema-only T01 contexts without `HasExpectedFacts` cannot authorize live service execution.
- Add monotonic `BattleInstanceId` to `CombatStateBuffer`/read sink and copy it through inputs and preview contexts; increment on successful battle installation, not on every preview. Calculate the next ID during preparation so overflow cannot partially install a battle. Use value comparison for visible mutations within a battle, not result sequence alone.

**Three separate boundaries (design Sections 9.2-9.3):**

1. **Response correlation, owned by View:** request number + draft revision + actor + battle instance + current scene attachment/interaction lifetime must match the outstanding request. The frozen path must match exactly. These are T09's local associations, not gameplay facts or a general revision system.
2. **Public expected facts:** `Capture/Matches` uses the following value comparison. It never copies the real hidden read set into a public context.
3. **Execution read set:** after public context admission, calculate from current real Buffer facts, including undiscovered hazards. Validate every real dependency read by that calculation during existing preparation, including unchanged occupancy/source/target facts and collection membership used by queries. This synchronous expected-state validation is independent of the earlier preview; a hidden change since preview alone is not a stale-context rejection.

| Public context field group | Exact capture/comparison rule |
|---|---|
| Identity and intent | Equal `BattleInstanceId`, observer, actor ID, Move/Return kind, ordered path cells for Move, checkpoint target for Return. No endpoint-equivalence comparison. |
| Battle/turn admission | Equal board width/height, combat phase, active actor, `IsNight`, `Turn.StagePhase`, `Turn.CurrentTime`, and `Turn.CompletedUnitTurnCount`. These distinguish a later turn by the same actor. Do not copy Timeline ordering, other participants' schedule metadata, enemy-origin history, or preparation bookkeeping into this context. |
| Mover | Compare identity/ownership/role/area, anchor/rotation/occupied cells, current and max HP, checkpoint, committed/pending distance, pending AP, attack-refund basis, and applied tile contribution/source IDs. Compare `MoveMethod`, `MoveRange`, `MoveCost`, `RemainingMoveRange`, `CurrentActionCost`, `MaxActionCost`, `PhysicalDefense`, attack/soul/artifact bonus components and the two temporary bonus fields used in arrival stat reconstruction. Compare concrete movement-success/incoming-effect trait/category state fields read by the supported calculations; ignore unrelated attack targeting/range and timeline Priority/Delay fields. HP equality includes direct health publications even without a new result sequence. |
| Public occupancy | Canonical set of visible living field occupants and blocking structures: identity, owner, occupied cells, alive/blocking status. Compare membership and cells, independent of enumeration order. HP amounts/stats of an occupant are added only when it is an eligible target of a public rule; a blocker elsewhere changing HP while still alive does not invalidate solely for HP. |
| Public effect dependencies | For each supported public rule, capture its source cell/ID, active/consumed state, priority, and concrete typed effect parameters; include current/max HP, applicable defense/effect state, and location of its publicly selected targets. Include visible thorn values used by that projection, visible soul IDs/cells/values at eligible arrival, collector soul-count/base-growth/main-HP facts used by arrival, and T02 destination-source values for the queried stop. Determine dependencies by the same public projection, including its forced-arrival destination if any. Record membership/absence for queried public source cells/target sets so a newly visible relevant source or target invalidates. Return queries only checkpoint legality and modifier sources, not hazard/soul acquisition. |
| Definition/effect inputs | Supported source parameters and profile/owned/artifact fields actually read by those public movement, incoming-effect, arrival, and movement-success calculations compare by value. Stable battle definitions may be referenced under the same battle instance only if their immutability is enforced; otherwise capture the consumed values. No general definition/Run version is introduced. |

The public dependency comparisons are explicit, typed comparisons in the existing context helper and rule adapters; they are not a generic read-tracking subsystem. Canonical entity/source sets compare by stable ID (cells by coordinates), exact scalar equality, and concrete payload equality. Ordered route/event inputs preserve their semantic order. New supported content must declare its public calculation inputs and add their equality checks in the same adapter change.

Implemented dependency details: `IMovementTileRule.TargetIds(tile, mover)` declares candidates using the selected typed source and mover identity. `MovementStepInput.Targets` contains only those candidates' latest projected HP/occupancy; effect requests outside that set reject. This declaration is shared by public capture and actual evaluation, without a target registry or second effect engine. Non-modifier same-cell sources (including inactive ones) and modifier sources at actual arrival cells are separate dependencies. Return captures only checkpoint modifier sources, excluding entry sources, thorns, souls, collectors, definitions and artifact-arrival inputs.

Supported mover state additionally compares Heavy `DefenseFacing/MovedThisTurn`, Focus `ActiveStacks/GainScheduled/MovedThisTurn`, and MoveAttackDiscount `FirstMoveConsumed/DiscountArmed`. Target reconstruction compares identity/ownership/area/HP/location, position/temporary contributions, physical defense and attack/soul/artifact components. Collectors compare owned ID/key/role, soul count, and Main current HP/persistent attack/max HP when applicable. Immutable `UnitSpec` references come from the installed battle definition cache and compare by reference within the same battle instance; mutable authoring assets are never substituted into the context. Compiled SoulAltar origins/attack/max-HP/heal/duration and HealingChalice origins/heal values compare as typed values. Other artifact attack policies are excluded.

Actual publication separately validates the complete original character/owned/soul-count/Main snapshot, placed Artifact instance-ID/artifact-ID membership captured by the service, the real command-state/environment snapshot, and real soul-field expectations before installing anything. `BattleInstanceId == 0` and absent Artifact membership are retained only for existing standalone calculation/publication fixtures; the registered service always supplies installed battle identity and membership. No new Run/version owner is introduced.

Filter unknown sources *before* source selection, dependency capture, and simulation. Omit hidden IDs, counts, visibility lists for other observers, private payloads, and hidden-only revisions/hashes. `KnownTo` itself is not serialized: capture only the resulting observable subset. Identical public facts must produce equal contexts and `Matches == true` when only a hidden mine moves/changes/appears. A newly discovered source is a public change. Never use omniscient `CombatCommandState.SameValues` as this freshness gate.

Exclude result sequence, handled/answered numbers, preview number/revision, prior preview contents, presentation acknowledgement, animation state, and scene-local tokens from gameplay value equality. Battle-instance equality is retained for the actual delayed-preview/new-battle path; scene attachment lifetime remains a View callback guard. Actual prepare/install checks may compare a complete immutable snapshot **read by execution**, but must not substitute that comparison for public preview freshness.

- [x] Add the planned cases in the existing MovementPublicationTests: composed known-effect Preview/execution, hidden-only mutation with real activation, newly discovered visible-source stale rejection, direct HP change without a sequence increment, reused actor/new battle, atomic battle-ID overflow, duplicate processing, late response, and unrelated blocker HP/bookkeeping acceptance. Registered Return recomputes its modifier from composed checkpoint source facts and leaves a checkpoint hazard active. Existing T06 soul/no-acquisition cases supply the complementary acquisition coverage.
- [x] Essential RED: `t07-red-read-set-fixed-fixture` failed 1/1 because an unchanged occupant's direct HP mutation did not reject preparation. Implement the route/Return ports, public context and latest-request protection. Initial compiler errors are not behavioral RED evidence.
- [x] Recalculate from current authoritative facts before applying and validate the actual execution read set defined above, including unchanged blockers, sources and targets. Preserve original expected facts when merging the projected route. Preview uses the same composed rules with known facts before projection. Core-issued context is a public precondition, not trusted client billing or a snapshot of hidden execution dependencies.
- [x] Migrate player/View/service requests, attack-query/composition copies, and their fixtures to route/context inputs. Inventory the remaining endpoint-only AI producers and their tests in the evidence record; retain their existing API/branch until T08 migrates them. Do not add an endpoint-to-route fallback or declare all callers migrated here. Legacy result adapters outside this runtime retain documented semantics; do not relabel displacement as route length. `t07-green-caller-regression`: 152/152 passed, zero skips, editor exit 0, including all changed service-cycle fixtures and Core route/effect/Return/AI regressions.
- [x] Assert multi-store validation failure installs neither resources, field state, character changes, nor result counters; normal rejection publishes its result without gameplay events. Run `Invoke-MovementTests -RunName t07-final-verified -Filter 'ProjectL.Tests.Framework.MovementPublicationTests;ProjectL.Tests.Framework.CombatViewBufferTests;ProjectL.Tests.Framework.CombatPreviewPublicationTests;ProjectL.Tests.Framework.CombatCommandPublicationTests;ProjectL.Tests.Framework.CombatCommonPublicationTests'`: 63/63 passed, zero skips, editor exit 0. Suggested commit: `feat: publish validated movement routes through combat buffers` (not executed).

Execution scope: T07 is complete in the existing uncommitted feature checkout. The two final focused filters cover 215 distinct cases; no full-suite rerun or redundant RED cycles were required. Core owns projection/freshness calculations, existing Buffers own identity/state/atomic installation, and the shared View input schema transports paths and contexts. No View interaction/Flow/Job/presentation implementation was included. T08 still owns AI route production and removal of the temporary endpoint branch; T09-T11 own draft/lifetime coordination and scheduling/playback. Authored mine/Object/modifier values and perception providers remain gated. No scenes, assets, assembly references, Framework infrastructure, commit or push were changed. See the [verification handoff](../handoffs/2026-10-04-drag-path-movement-verification.md).

**Deliverable:** Registered service and real Buffer route/preview/return integration works, including stale input, hidden information, composed destination-source publication, and duplicate protection. All T07 edits compile with the documented temporary AI endpoint entry point; its removal is a T08 acceptance check.

## T08: Make enemies select safe explicit routes

**Files:** Create `C/Application/Combat/EnemyAi/EnemyMovementPathfinder.cs`, `TC/EnemyMovementPathTests.cs`. Modify `EnemyAiPolicy.cs`, `EnemyAiTurnService.cs`, the remaining endpoint branch in `CombatMovementCalculator.cs`, `C/Contracts/Combat/MovementEnvironmentFacts.cs`, source-metadata validation in `MovementProjection.cs`, and inventoried endpoint cases in `TC/CombatCommonCommandTests.cs`, `StageEnemyAiTurnTests.cs`, `CombatCommandCalculationTests.cs`, `CombatCommandEffectTests.cs` and `MovementRouteTests.cs`. Reuse existing composition and View route ports; no new composition owner is needed.

**Interfaces:** `EnemyMovementPathfinder.TryFind(CombatCommandInput input, EntityId targetId, out MovementPath path) -> bool`. Consume shared orthogonal passability and the enemy-visible known-mine subset of `MovementEnvironmentFacts`. Return an affordable prefix toward a legal attack position; never return a one-cell path as a successful move.

Data-path clarification: `MovementTileFact.IsMine` is immutable authored classification in the existing environment payload, defaulting false for existing non-mine sources. The policy excludes only sources with `IsMine && Active && KnownTo.Contains(actor.Owner)`. Consumption/visibility copies preserve classification; rule source changes cannot alter it. Do not infer mines from rule-key strings, damage amounts, or all hazardous tiles. Concrete mine payloads/rules and a live perception provider remain gated; this task uses a typed fixture source and does not invent production values or add another Buffer.

Attack goals reuse existing attack-range/selection geometry, including applicable source/target roles and splash. Goal testing ignores `CanAffordAttack`; actual attack admission remains unchanged. A zero-percent additional splash contribution cannot establish a goal, preserving the existing AI's refusal to choose damage-free attacks. Goal queries do not execute tile entry or attack settlement. Uniform BFS uses full reachable geometry before cutting by the mover's current remaining range/AP, including zero-cost movement.

- [x] Add `SafeDetour_CanInitiallyIncreaseManhattanDistance`, `SafeGoalBeyondBudget_UsesPrefix`, `NoSafeApproach_DoesNotMove`, `UnknownOrInactiveMine_IsNotUsedByPlanner`, and `InterruptedMove_SuppressesFollowingAttack_AndClosesOnlyLivingTurns` (death/forced relocation/movement block). Add equal-goal/neighbor ordering and zero-percent splash approach checks in that same fixture.

```csharp
Assert.That(path.StepCount, Is.LessThanOrEqualTo(2));
Assert.That(path.Cells, Does.Not.Contain(knownMineCell));
Assert.That(firstStepDistanceToTarget, Is.GreaterThan(originDistanceToTarget));
Assert.That(eventsAfterMoverDeath.Any(e => e.Kind == CombatEventKind.Attacked), Is.False);
```

- [x] Essential RED: `t08-red-compiled` confirmed the old automatic-turn path had no route result (4/4 failed); the initial NUnit compile error is not behavioral RED. `t08-zero-splash-red` independently failed 1/1 because a zero-percent splash role incorrectly hid an otherwise damaging approach. Implement deterministic BFS with ties by route length, goal Y/X, then fixed neighbors `+X,+Y,-X,-Y`. Goal geometry does not gate on current AP.
- [x] Keep target ranking and attack-first behavior; replace only destination selection. Search beyond current movement budget to find detours, then cut by actual movement range/AP. If no safe approach exists, retain any current legal attack and otherwise end the turn. Do not fall back to a known mine.
- [x] Pass the actual path into the common calculator and preserve all composed events. Suppress further attack after a non-completed route, death, or battle termination; close only a living actor's still-active turn. Enemies seal executed distance and cannot Return. Existing automatic-turn composition still produces one proposal with movement, attack and turn-end events in order.
- [x] Remove the ordinary no-Path Preview/Calculate/Evaluate branch and legacy movement-effect helper after migrating the AI producer. Migrate endpoint fixtures to supplied explicit cells; change geometric refund assertions to actual entered-step accumulation. Extend the existing malformed-route cases so a valid endpoint without Path rejects as InvalidPath. `rg` confirms the old destination selector and ordinary endpoint execution branch are absent. Keep low-level forced displacement helpers and Range/legacy transport hints with their existing meanings.
- [x] Run `t08-final-verified` with the three AI fixtures, migrated Core fixtures, route/effect/Return/modifier cases, all five T07 publication fixtures, and changed service-cycle callers: 347/347 passed, zero skips, editor exit 0. See the verification handoff for the exact filter and migration evidence. Suggested commit: `feat: route enemies around known mines` (not executed).

Execution scope: T08 completes the Core/Buffer route-producer migration boundary in the existing uncommitted feature checkout. The common movement calculator now rejects a missing path; it cannot reconstruct a route from an endpoint. Existing target ranking, attack selection, forced displacement and one prepared automatic-turn publication are reused. The nearest legal attack goal can be an existing splash position: the composed-turn fixture now walks one edge and applies its existing 70% additional damage (14), instead of the previous two-edge direct hit (20). This reflects the confirmed shortest-attack-position policy without changing attack balance. No View gesture/Flow/Job/playback task, authored mine values, Framework infrastructure, scene, asset, assembly reference, commit or push was included. T09 remains unstarted; this is the intended intermediate Core/Buffer review checkpoint.

**Deliverable:** Minimal safe path policy, with fixture-backed perception inputs where live content is absent. No wider targeting or discovery redesign.

## T09: Implement deterministic drag sampling and editing

**Pre-T09 review gate:** Completed the user-requested T01-T08 intermediate review corrections without starting T09. Non-mover forced relocation now renews its checkpoint; attack knockback, recall, placement and enemy redeployment reconcile position contributions using existing calculators. HP normalization remains distinct from damage in results, previews and AI scoring. Pierce goals use the same bounded target budget as execution and project only enemy-visible candidate destination modifiers. Final focused Core/Buffer/service verification passed 378/378 with zero skips and editor exit 0; separate post-fix review found no remaining actionable issue in these corrections. See the [verification handoff](../handoffs/2026-10-04-drag-path-movement-verification.md#intermediate-review-corrections-before-t09) for RED evidence and the corrected legacy expectation. Production content/perception and T09-T11 integration remain pending.

**Files:** Create `V/StageInput/MovementPointerSampler.cs`, `MovementDraft.cs`, `V/Combat/CombatMovementInteraction.cs`, `TF/MovementDraftTests.cs`. Reuse `StageSelectInput.cs`, `IStageInputReceiver.cs`, and `StageInputReceiverSlot.cs`; modify their contracts only if an actual missing lifecycle signal requires it.

**Interfaces:**

- `MovementPointerSampler.AppendCrossedCells(Vector2 fromGrid, Vector2 toGrid, List<CellCoord> output) -> void`: grid-space geometry only, X-first at exact corner ties.
- `MovementDraft.Begin(EntityId actor, CellCoord origin)`, `Accept(CellCoord cell) -> DraftEditResult`, `Suspend()`, `Cancel()`, `Snapshot() -> MovementPath`; `DraftEditResult` values `Unchanged`, `Added`, `Removed`, `Rejected`. Expose draft revision and pointer-valid state.
- `CombatMovementInteraction` connects draft edits to `ICombatViewBufferWriteSink`. `OnPreview(CombatPreviewResult result)`, `Release(CellCoord? cell, bool overCancelArea)`, `Cancel()`. Retain exact frozen draft while awaiting its newest valid preview. One interaction owns actor, draft, current request number, and submission state. Associate each request with battle instance, current attachment/interaction lifetime, exact path, and draft revision. `OnPreview` must match that association before using T07's public context; late callbacks cannot reopen a canceled/frozen interaction. Revision/request equality does not replace public-fact matching at execution.

Implemented View coordination contract: construction also receives the existing input read sink and Combat state read sink, plus an explicit configured `innerCellInset` (0 <= inset < 0.5; no production default). Grid cell `(x,y)` occupies `[x,x+1) × [y,y+1)`. `Attach/Detach`, `FocusLost/CaptureLost`, `Begin(actor, origin, pointerGrid)`, and `PointerMoved(Vector2? grid, bool overCancelArea)` are called by the scene owner in T11. A null grid sample denotes outside-board input; the owner converts screen coordinates and samples the final pointer before release. Mutable draft state is private; `Snapshot`, `DraftRevision`, `ValidatedRevision`, `PointerValid`, `IsActive`, `Preview`, `IsAwaitingPreview`, and `IsSubmitted` provide display/coordination reads. The interaction reads existing pending-command/presentation gates; it never acknowledges results or schedules processing. An awaiting frozen gesture refuses another Begin. Cancel after submission clears only local state, preserving the submitted command and its lock until handling/presentation completes. Scene detach must call `Detach` even after release, when the receiver slot no longer owns a pressed selection.

- [x] Add `Backtrack_PopsOnlyImmediatePredecessor`, `FastDiagonal_RecordsBothOrthogonalCells`, `BlockedXCorner_DoesNotTryYFirst`, `InvalidDrop_DiscardsValidPrefix`, `ReleaseBeforePreview_SubmitsExactRouteOnce`, and `OldPreviewAfterCancel_DoesNotSubmit`.

```csharp
Assert.That(crossed, Is.EqualTo(new[] { new CellCoord(1, 0), new CellCoord(1, 1) }));
Assert.That(draft.Snapshot().Cells, Is.EqualTo(new[] { a, b, c }));
Assert.That(viewCommandsAfterDuplicateRelease.Count, Is.EqualTo(1));
Assert.That(viewCommandsAfterCancel.Count, Is.Zero);
```

- [x] Run essential RED for backtracking, corner sampling (three variants), and pending-preview submission: 5/5 failed as expected, with compilation successful. Implement segment traversal, inner-cell hysteresis, editing, suspension/resumption, and preview correlation. Core remains the authority for passability/cost; candidate edits cannot become valid via local guessed billing.
- [x] Reset segment continuity across UI/outside regions; require endpoint/predecessor re-entry. Track candidate and Core-validated draft revisions separately. A matched invalid preview uses `ValidStepCount` to retain only the validated prefix for editing and keeps the current drop invalid; it never submits that prefix automatically. Handle multiple cells from one fast sample in order and stop at the first invalid addition. Preview retained candidate edits even when a later cell in the same sample is locally rejected. A later out-of-order reply cannot restore a rejected continuation.
- [x] Add focus/capture loss, origin release, skipped old response, jitter, and late response after scene detach cases. `t09-green`: 38/38 passed. Essential follow-up RED proved frozen-gesture replacement and missing partial-sample preview; both corrected. `t09-final`: 61/61 passed, zero skips, editor exit 0 (25 new movement-input cases plus existing StageInput, UnityStageInput and CombatPreviewPublication regressions). No full-suite run, commit or push.

**Deliverable:** Gesture state is testable without a production scene and cannot mutate gameplay or submit a stale/duplicate route.

Execution scope: T09 complete. Existing stage input contracts required no modification. New classes remain View-local and use existing Buffer ports; no Core/Buffer calculation, Framework infrastructure, assembly reference, scene or authored content was changed. T10 still owns Job calls and T11 owns actual scene/input mapping, lifecycle signal forwarding, preview display/playback and whole-result acknowledgement. Input inset tuning remains a Unity usability/configuration dependency. See the [verification handoff](../handoffs/2026-10-04-drag-path-movement-verification.md#t09-drag-editing-and-preview-correlation).

## T10: Connect existing Jobs without moving Flow ownership

**Files:** Modify `L/Jobs/CombatCommandJob.cs`, `CombatPreviewJob.cs`, `CombatAdvanceJob.cs`; tests `TF/CombatPlanTests.cs`, `CombatFlowTests.cs`. Read `L/CombatFlow.cs`, `L/Plans/CombatPlan.cs`, `L/CombatLifeCycle.cs` and `S/CombatSystem/CombatProcessingService.cs`.

**Interfaces:** Existing calls only: `_service.CoreGroup.Combat.ProcessNextCommand()`, `.ProcessLatestPreview()`, `.AdvanceCombat()`. `Execute()` invokes its one service once and sets `IsCompleted = true` after normal return; repeated execution is a no-op. No new JobType is needed for Move or Return.

- [x] Replace `Execute_ThrowsNotConnected_AndStaysIncomplete` with `Execute_CallsMatchingServiceOnce`, `Execute_ExceptionRemainsIncomplete`, and `Execute_NoEligibleWorkCompletesButFlowChecksEvidence`. All three Job types have call/exception/no-work coverage; the Flow evidence case uses the real Runner/Plan and the existing no-work probe. The old Runner stub-exception expectation now checks an actual Core exception and retained incomplete Job.

```csharp
job.Execute();
job.Execute();
Assert.That(probe.CallCount, Is.EqualTo(1));
Assert.That(job.IsCompleted, Is.True);
Assert.That(registeredFollowUpCommandsBeforePresentation, Is.Zero);
```

- [x] Run essential `t10-red` on three matching-call variants and two real Runner/Flow integration cases: 5/5 failed because of existing unconnected Job exceptions, with compilation successful. Replace the three throw stubs with their service calls. Preserve preparation wiring and Flow's separate bookkeeping verification, including superseded previews; Flow/Plan/Runner infrastructure required no production modification.
- [x] Exercise real Runner/Plan plus a real processing service and Buffers for a move, stale-context rejection, preview, and automatic turn start. The preview Job reads a replacement request at execution; a new preview can publish during movement presentation. Queued state-changing work remains unregistered until the test's overall View owner acknowledges the prior result. A service boolean is not a replacement acknowledgement. Jobs finish calculation/publication only; the single overall View result owner in T11 acknowledges after all movement and non-movement events and final reconciliation.
- [x] `t10-final-verified`, using the planned CombatPlan/CombatFlow/MovementPublication filter: 70/70 passed, zero skips, editor exit 0 (22 Plan, 32 Flow, 16 movement publication cases). Earlier green attempts exposed only automatic-turn fixture admission mistakes: the reused preview fixture was already in Combat phase, and an explicit preparation index of 0 violated the existing minimum of 1. Corrected fixture setup to Deployment with the existing typed defaults; no gameplay rule change. No full-suite run, commit or push.

**Deliverable:** The existing scheduler reaches Core; no Core-owned execution or input acknowledgement was added.

Execution scope: T10 complete. Only the three existing Job implementations and two existing test fixtures changed, plus plan/evidence records. Normal service return (including false/no eligible work) completes a Job exactly once; exceptions leave it incomplete and propagate unchanged. Flow still checks actual Buffer evidence independently, and retains command priority, canceled/replaced-preview handling and presentation gates. T11 remains responsible for scene input/lifetime integration, ordered whole-result presentation and acknowledgement; the documented scene-transition/queued-Job exit boundary remains an integration prerequisite. See the [verification handoff](../handoffs/2026-10-04-drag-path-movement-verification.md#t10-existing-combat-job-service-wiring).

## T11: Integrate View playback, return UI, and scene lifetime

**Files:** Create `V/Combat/ICombatMovementPresenter.cs`, `CombatMovementPlayback.cs`, `TF/CombatMovementViewTests.cs`. Integrate `CombatMovementInteraction.cs` into the agreed `V/Combat/CombatViewSystem.cs` and `V/Combat/CombatSceneRoot.cs` if they exist at execution time. If still absent, create those two minimal adapters with the interfaces below; modify `F/View/InGameViewSystemFactory.cs` and `V/InGameViewSystemGroup.cs` only for their registration/context requirements. Coordinate exit with `L/CombatLifeCycle.cs` through the existing transition owner.

**Interfaces:**

- `ICombatMovementPresenter`: `ShowDraft(MovementPath path, MovePreview? preview, bool pointerValid)`, `ClearDraft()`, `BeginStep(CombatCharacterEvent step)` (Moved only), `ShowReturn(CombatCharacterEvent returned)` (field-to-field Returned only, immediate snap), `bool IsMovementComplete`, `CancelMovement()`, and `SetReturnState(CheckpointReturnPreview preview)`. A null quote means pending; pointer validity is separate from the retained valid prefix. Completion concerns the current BeginStep only; cancellation must invalidate prior animation callbacks. It presents movement/Return only; no catch-all `BeginEvent`, unrelated combat policy, whole-result reconciliation, or acknowledgement. Night recall to hand/reserve belongs to the non-movement presenter even though its event kind is Returned.
- `CombatMovementPlayback.BeginStep(CombatCharacterEvent step)`, `ShowReturn(CombatCharacterEvent returned)`, `Tick(float deltaTime)`, `Detach()`, and `bool IsComplete` form a **movement-specific participant**. It holds one immutable movement event and its animation progress, not a whole `CombatResult` or result sequence. Aggregate path/return payloads supply UI detail, never duplicate the event stream.
- The existing/agreed `CombatViewSystem` is the **single whole-result owner**, consistent with [Combat View design, Sections 4-5](../specs/2026-09-30-combat-view-design.md). It captures result sequence and battle/attachment lifetime, walks the complete ordered event stream regardless of top-level intent, delegates movement to `CombatMovementPlayback` and other events to their existing presenters, waits for each handler, then reconciles all displayed Buffer state, removes handled commands, and calls `AcknowledgePresented(sequence)` exactly once. Rejected/empty results use the same completion path. A movement participant completing cannot acknowledge the result.
- If the agreed owner is still absent, the already-planned minimal `CombatViewSystem` adapter supplies only that dispatch/wait/reconcile/acknowledge coordination, input binding, and required lifecycle hooks. Use the existing immediate presentation hook for non-animated events only where it explicitly handles their display/update; do not silently skip unsupported attack/death/turn events. Missing non-movement presenters are an integration gate, not a reason to implement their policies inside movement playback or acknowledge early. This result cursor is presentation sequencing, not another Flow/Job scheduler. `CombatSceneRoot` only exposes cached mapping/presenter references; extend an existing owner if delivered.
- Historical T11/T12 integration ports (superseded by the [Stage merge integration](2026-10-04-stage-merge.md) and [current handoff section 8](../guides/unit-movement-attack-core-handoff.md#8-current-drag-path-movement-integration)): `ICombatPointerMapping` supplies configured `InnerCellInset` and screen-to-grid/cancel-area mapping. `ICombatResultPresenter` supplies `CanPresent`, `BeginEvent(event, completed)`, read-only Buffer `Reconcile`, and `Detach`. The owner preflights all non-movement events; unsupported events block completion. Duplicate/late callbacks are guarded by attachment, battle, captured result and event index. `IInGameViewSystemGroupService.Combat` exports `ICombatViewService` through the existing View group; current `Attach(CombatSceneRoot, IStageInputReceiverSlot, IInputLayerService)` requires all three scene providers and both existing input services. PostBind requires Buffer ports only; the entry owner supplies input services at Attach. T12 corrected the initial eager Global input dependency after full-suite Core-only composition failures. The production entry owner must configure providers before attachment; Factory construction does not create a scene or production presenters. Player input is admitted only in PlayerInput with no pending command or presentation.

- [x] Add `Playback_DoesNotSnapToFinalAnchorEarly`, `DelayedPresentation_BlocksNextMutation`, `RejectedResult_IsAcknowledged`, `Return_SnapsWithoutTraversal`, and `LateCallbackAfterDetach_CannotAcknowledgeNewBattle` using a presenter probe and real input/result Buffers.

```csharp
Assert.That(presentedSequenceBeforeLastEvent, Is.LessThan(result.Sequence));
Assert.That(presentedSequenceAfterReconcile, Is.EqualTo(result.Sequence));
Assert.That(gameplayWritesFromPresenter, Is.Zero);
Assert.That(returnTraversalAnimations, Is.Zero);
```

- [x] Run essential RED and implement movement-step playback under the overall View owner's ordered result dispatch. Block new move/return gestures until the complete result is reconciled; show actual entered cells only. Retain a death cell even when Buffer final facts have no anchor. Evidence includes `t11-red` (two behavioral failures; one fixture failure), corrected `t11-composed-red-verified`, `t11-recall-red`, and `t11-input-phase-red`; see the handoff for exact distinctions.
- [x] Extend the existing composed automatic-turn/playback case in `CombatMovementViewTests`: an Advance result contains movement followed by attack/health/turn events. Finish movement while delaying the non-movement handler; assert zero acknowledgement/reconciliation until that handler completes, then one final reconciliation and exactly one acknowledgement of the captured result. Repeated ticks/callbacks must not duplicate it. Reuse the presenter probe and real Buffers; also assert Flow remains gated throughout. The night-ending variant routes non-field recall through the result presenter; automatic-phase input cannot leave a queued player command. A separate actual enemy entry-death case delays Died with its Expected cell while the final Buffer anchor is null.
- [x] Wire immutable draft/nullable quote/pointer-validity and Core Return preview/intent through scene presenter ports. Cache configured references once, forward actual StageInput/lifetime signals and bind/unbind through the existing slot/layer. No animation callback deducts AP, changes occupancy, or applies damage. Factory/group registration, routed input, ticking and shutdown are verified with real Buffers and supplied presentation/mapping probes.
- [ ] Bind production ghost/path/cost/risk/return visuals through authored View configuration and Render APIs. **Production gate:** migrated Field/Unit/Hand/Ghost/HUD presenters, mapping/configuration and a bootstrapped scene were absent at T11 execution. No production visuals, default inset or scene assets were invented. The completed ports do not prove rendered UI or usability.
- [ ] Integrate scene exit before input Buffer clearing: stop new Flow registration, let already-registered processing finish, complete or reconcile the pending result through the existing presentation owner, then detach/clear inputs. `CombatFlow.Stop()` alone does not drain Jobs. If the transition owner/F2 boundary is not available, retain this as an explicit production gate; do not clear the shared global Runner or add a substitute transition manager.
- [x] Run focused Unity verification. `t11-green-compiled`: 64/64 passed. Final `t11-final-input-verified` uses `CombatMovementViewTests;MovementDraftTests;CombatFlowTests;StageInputTests;UnityStageInputTests` (all prefixed `ProjectL.Tests.Framework.`): **103/103 passed, zero skips, editor exit 0**. It verifies actual Factory registration/lifecycle cleanup as well as direct constructor cases. No full-suite or manual scene claim; no commit/push.

**Deliverable:** Minimal movement/whole-result View integration is implemented and verified through its ports. Real playable completion additionally requires production bootstrap, scene, presenters/assets, and entry/exit ownership. `Detach` cancels input/animation and invalidates callbacks, but deliberately does not acknowledge unfinished results or drain/clear the shared Runner. The existing transition/F2 owner was absent and remains a production gate; the adapter/probe cannot close it. T12 evidence is recorded below.

## T12: Verify the full path and publish the implementation handoff

**Files:** Extend existing `TF/CoreStageCombatCycleTests.cs` or `TF/MovementPublicationTests.cs` for the registered cycle; update `docs/superpowers/handoffs/2026-10-04-drag-path-movement-verification.md`. Update current movement/View handoff guidance only after implemented APIs are final; preserve historical specifications.

**Interfaces:** Use real exported services, Buffers, Flow/Plan/Runner/Jobs, and the View interaction/presentation ports. Fixtures may provide board/content definitions and presentation timing, with each substitution stated explicitly.

- [x] Add `RegisteredDragCycle_PreviewMoveBarrierReturnAttackEndTurn` in CoreStageCombatCycleTests: pure preview, four-step detour, safe Return, failed attack/mode cancellation, hazard/soul sealing, later safe move/Return, successful attack sealing, sealed Return rejection, turn end without duplicate hazards/souls, enemy activation and composed enemy movement/attack in one service lifetime. Reuse T11's existing probes; no new harness. Initial active turn/content are fixture inputs, operations use actual registered groups and Jobs.
- [x] Run the stated cycle filter. `t12-cycle-verified`: **68/68 passed**, zero skips, exit 0. The first cycle run's remaining failure was the test expecting movement in the existing separate TurnStarted Advance; the fixture now acknowledges activation before scheduling enemy action.
- [x] Run full verification and compare baseline. Initial `final`: 170 additional missing-Global binding failures plus 5 baseline failures. Corrected T11 input binding to explicit mandatory Attach ports; `t12-bind-fix`: **196/196 passed**. Final `final-verified`: **2,001/2,006 passed**, exactly 5 original Economy failures, zero skips, exit 2. Failure identities/messages match baseline; 184 added and 23 retired case identities are recorded, with intentional route/deferred-settlement/AI/Job replacements explained in the handoff. No occupancy/atomicity/trait coverage removed to hide failures.
- [ ] Perform actual Unity low/high zoom/FPS and pending-scene-exit interaction checks. **Production gate:** migrated presenters/Render/config/scene/bootstrap and F2 transition owner are still absent. Managed geometry/presentation probes pass but do not close this step. No manual observation is claimed.
- [x] Recheck live content/source integration. Factory composes ThornMovementTileRule; live DB/source adapters do not populate mine/Object/destination-modifier source facts. **Core behavior verified with fixtures; authored environment integration pending.** Actual bootstrap/scene/presenters/F2 ownership remains separately gated; no production values or new Buffer invented.
- [x] Inspect changed/untracked sources and imported `.meta` GUIDs, audit dependency direction and record V01-V36 evidence in the handoff. Current movement/View guides now describe explicit route/Return, scene Attach and full-result acknowledgement, and mark older StageSession examples as legacy reference. Whitespace verification uses `git -c core.whitespace=cr-at-eol -c core.safecrlf=false diff --check`. No commit/push.

**Deliverable:** Managed cycle/full regression verification and English handoff are complete with the five unchanged Economy-content baseline failures. Core/Buffer, scheduling and View ports have evidence; authored environment, rendered interaction and production entry/exit remain open gates. Final independent source review found no Critical/Important issue in its snapshot; the later full-suite binding regression was corrected with test evidence. One minor combined buff-departure/hazard/interruption/soul ordering test gap is deferred and recorded. Close only the levels proven by their own evidence.

## Spec coverage and final review checklist

| Spec acceptance cases | Implementing and verifying tasks |
|---|---|
| V01, V24-V29 | T01-T02, T07, T09, T12 |
| V02-V06, V19 | T03, T05-T07 |
| V07-V08, V12-V15 | T05-T06 |
| V09-V11 | T04, T06, T11 |
| V16-V18 | T06, T12 |
| V20-V23 | T09, T11-T12 |
| V30-V31 | T03, T06 |
| V32-V34 | T08, T12 |
| V35 | T10-T12 |
| V36 | T05-T06, T12 |

- [x] G01-G20 code contracts remain intact; no confirmed gameplay decision reopened. Production manual acceptance remains gated above.
- [x] Route, ledger, context, modifier, and event types use the same names and meanings across all tasks.
- [x] Tile rules receive current narrow facts and cannot return command state, barrier claims, or publication decisions; sequential resolved changes preserve earlier entity/environment updates.
- [x] Destination source facts reach `ResolveAt`, actor contribution replacement, and real Buffer publication; Return shares only modifier resolution, not entry/arrival acquisition.
- [x] Draft correlation, public expected facts, and the actual execution read set remain distinct; hidden-only changes neither leak into context nor invalidate it.
- [x] T08 removes endpoint APIs only after the final AI producer migrates; each compiling task boundary has all required call-site updates.
- [x] Effects are resolved in Core projections; one battle publication installs all related authoritative changes.
- [x] Input, correlation and presentation stay View-owned; one whole-result owner acknowledges only after all events/reconciliation. Core validates gameplay/read dependencies. Normal production scene-exit coordination remains explicitly incomplete, not reassigned to Core or a new scheduler.
- [x] Attack-refund compatibility behavior has explicit scenario evidence and review; checkpoint-return barriers do not accidentally redefine category balance.
- [x] Production content/scene readiness is not inferred from test adapters or generated DTOs.
- [x] The final handoff records remaining dependencies honestly and contains no claim of tests that were not run.

## Execution handoff

The user's request authorizes this implementation plan, not product changes. Review it together with the spec before execution. Suggested execution is native sequential implementation because the ledger, environment facts, and result contracts change together; use `superpowers:executing-plans` if selected. Subagent-driven execution is also suitable if explicitly chosen, with the same dependency order and isolated task reviews. Do not start either method until the user selects it.
