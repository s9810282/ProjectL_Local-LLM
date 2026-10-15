# Recovery Stage Core Connection Design

Date: 2026-10-03  
Status: P1–P3 implemented, verified and reviewed on 2026-10-03; Core gate C complete, including the review-driven entry metadata fix. See the [final handoff](../handoffs/2026-10-03-recovery-stage-core-connection-handoff.md), [P1 checkpoint](../handoffs/2026-10-03-recovery-stage-core-connection-p1-verification.md) and [P2 checkpoint](../handoffs/2026-10-03-recovery-stage-core-connection-p2-verification.md). Authored DB and View/Flow activation remain external.  
Source baseline: `e55348ff`, with unrelated incoming working-tree changes preserved.

## 1. Purpose and authority

Connect the existing recovery definitions to the current Framework-based Core/Buffer runtime. Core must expose a complete, callable recovery path: enter a legal node, prepare its options, apply one option, and finish the node. View/Flow owns when to call these operations and how to present them.

The user first requested that Core scope be determined and documented, then authorized P1, P2 and P3. The architecture below remains the package design. Entry, installed options, guarded application, node completion and measured full-cycle integration are delivered. The user explicitly confirmed on 2026-10-03 that completion without recovery is allowed; this policy is implemented and tested, but does not establish production readiness. Implementation documentation is English; new code comments must be Korean.

Read these sources together:

- [Framework development guidelines](../framework/Framework%20Development%20Guidelines.md): responsibility, Factory composition, DB/runtime separation.
- [Framework overview](../framework/01_Framework_Overview.md): assembly and group hierarchy.
- [Framework services](../framework/02_Framework_Services.md): registered interfaces and Context access.
- [System creation, registration and query](../framework/03_System_Creation_Registration_Query.md): Factory, ExportServices, lifecycle propagation.
- [Framework programmer guide](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md): FW-001/002/003/004/016/101/205 and sections 7–9.
- [Framework architecture](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md): composition and SO → Query → Core → Buffer boundaries.
- [Completed battle-entry handoff](../handoffs/2026-10-03-stage-entry-battle-preparation-handoff.md): explicit target and prepare-before-install precedent.
- Earlier recovery design (Git 이력: `2026-09-19-main-character-recovery-stage-design.md`): historical context only. Its RunSession/Revision/CheckpointHealth architecture and old paths are not the current implementation. Its no-purchase completion proposal is now confirmed by the user on 2026-10-03; its historical architecture is not reinstated.

The framework documents describe a 2026-09-22 snapshot. Use their ownership and registration contracts, but verify current signatures against source. In particular, their old statements about placeholder combat Jobs are not current execution evidence. This package does not re-audit the 77-slide planning deck or infer new balance rules from it.

## 2. Pre-package implementation and original gap

The inventory below records the `e55348ff` baseline before this package. P1/P2 close its Core preparation and completion gaps; their verification checkpoints describe the delivered code. All paths are relative to the repository root.

| Existing component | Evidence and consequence |
| --- | --- |
| `CombatProcessingService.EnterBattle(int, StageNodeId)` | Resolves an explicit target, prepares definitions, revalidates navigation and publishes matching selection/Character/Combat state. The battle entry ordering defect is addressed. |
| `DatabaseBattleInitializationSource.Read(database, chapterKey, stageKey)` | No longer requires Current selection. Still intentionally rejects missing Method_Key authoring. Recovery must not depend on this source. |
| `EventStageDetail.RecoveryOptionKeys` | Current DB convention identifies recovery as `StageType.Event`; the ordered keys identify options. Empty and duplicate key lists are invalid. |
| `IRecoveryOptionTableQuery.GetRecoveryOption(int)` | Provides `OptionId`, `Cost`, `HealMode`, `HealValue`. The runtime Core has no consumer of this query for recovery preparation. |
| `CurrentProgressBuffer.SetRecoveryOptions(...)` | Installs options only after a selection exists and neither Shop nor Recovery is installed. No production caller currently bridges DB data into this port. |
| `CurrentRecoveryData` | Owns detached options and applied-option/payment/heal facts. No second recovery session is needed. |
| `CombatProcessingService.Recover(RecoveryOptionId)` | Uses `CharacterRecoveryCalculator` and `PublishRecovery`; it does not read option definitions from DB or mark a stage clear. |
| `BufferSystemGroup.PublishRecovery(...)` | Checks selection, retained battle, option and source facts, and applies health/currency/receipt. Reuse this boundary. |
| `ChapterProgressionCoordinator.ResolveStage(...)` | Accepts combat outcomes. There is no dedicated recovery completion path; `StageOutcome` has no skip value. |
| `CharacterShopService.PrepareShop/Purchase` | Existing shop implementation. It is a reference, not a feature to rebuild in this package. |

Recovery table and Event-stage SO classes exist, but no `.asset` references to their script GUIDs were found in the inspected Assets tree. View recovery labels exist. This is an authoring-readiness observation, not proof that an external DB delivery is unavailable.

## 3. Scope and ownership

### Included in the Core package

1. Explicit recovery entry using chapter/node intent and live progression.
2. DB option lookup and conversion with identity, type, chapter, duplicate and value validation.
3. Publication of target selection and prepared options in one synchronous prepare-before-install operation.
4. A recovery service in the existing Core group, including Factory, key, Context and lifecycle wiring.
5. A guarded entry point that reuses existing recovery calculation/payment/publication.
6. Recovery completion that clears only the selected node, without manufacturing a combat outcome.
7. A user-approved no-purchase completion path using the same completion API.
8. Focused tests through real Framework composition and Buffer stores, plus an external-owner handoff.

### Outside this package

- Recovery UI, labels, animations, result windows, input binding, Job/Plan/Flow/Runner/LifeCycle implementations and scene transitions.
- Production SO authoring, catalog repair, option prices/amounts, a fixed number of options, and recovery node placement/balance.
- Combat caller migration, Method_Key schema, Economy assets or production battle activation.
- Shop redesign, generic event processing, growth/exchange/random events, character enhancement, consumable items or deck synergy.
- Next-chapter routing, final Run victory, rewards, aggregate gold accounting, persistence or Run reset.
- A new RunSession, revision counter, event bus, generic transaction framework, runtime service registry or pending-entry queue.

DB owns authored definitions. Core owns validation, conversion and command decisions. Buffer owns Current/Character/Currency/Stage facts and mutation boundaries. View/Flow owns orchestration and presentation. New Buffer support is part of this package because safe recovery entry requires it; it must remain narrowly scoped.

## 4. Selected approach

| Approach | Assessment |
| --- | --- |
| Select first, then add a parameterless `PrepareRecovery()` | Smallest patch, but DB failure can leave the target selected with no recovery options. It repeats the ordering problem just fixed for battle entry. Not selected. |
| Explicit `EnterRecovery(chapterKey, nodeId)` plus a narrow Current publication operation | Selected. Reuses progression, DB queries and existing stores; validates before mutation. |
| Generic noncombat manager or new Run-session subsystem | Adds state owners and migration unrelated to this gap. Deferred. |

Create `RecoveryStageService` under `CoreSystem/RecoverySystem`, exposed as `CoreGroup.Recovery`. It holds dependencies only, not recovery state. Keep existing `Combat.Recover` as a compatibility operation and delegate to it from the new guarded service. Moving the settlement implementation and renaming `IBattleBufferWriteSink.PublishRecovery` can be a separate cleanup; neither is required to close this connection.

## 5. Consumer contract

```csharp
public interface IRecoveryStageService : IServiceModule
{
    ChapterProgressionResult EnterRecovery(int chapterKey, StageNodeId targetNodeId);
    bool Recover(int chapterKey, StageNodeId nodeId, RecoveryOptionId optionId);
    ChapterProgressionResult CompleteRecovery(int chapterKey, StageNodeId nodeId);
}
```

`IInGameCoreSystemGroupService.Recovery` and the concrete group expose this callable interface. P3 verifies composition and the full service cycle; authored-game acceptance remains separate from API delivery.

### Entry

- Requires existing map/graph registration and an entered chapter. Does not bootstrap roster, currency or a chapter.
- Returns the existing rejected progression result for illegal navigation. No option lookup or entry write follows a rejected preflight.
- `Accepted` means Current identifies the requested legal node and contains its detached recovery options before return. It does not mean a UI or Job completed.
- Invalid arguments, missing dependencies, a retained active or terminal battle, malformed definitions and publication invariant violations throw, following the explicit battle-entry pattern.
- After battle settlement, the existing caller must legitimately invoke `Combat.EndBattle()` before recovery entry. The same registered services must then accept a legal recovery target. Recovery never invokes EndBattle/reset APIs to force entry or replays settlement effects; it preserves settled persistent HP, currency, progress and retained combat facts under their existing contracts.
- An entry fault leaves all pre-call Buffer facts unchanged by this operation. No selection-first fallback or catch-and-reset rollback is allowed.
- A repeated request to the same unresolved current node follows existing navigation rejection. It must never reset options or the consumed receipt. A caller reopening a screen reads Current instead of re-entering.

### Recovery application

- Validate requested chapter/node against live Current, the registered node/stage binding, map/node clear state, installed Recovery and the absence of a retained battle.
- Verify the root still identifies the same non-boss Event stage. Do not reload option values: the installed options remain authoritative for this visit.
- Delegate to existing `ICombatProcessingService.Recover(optionId)` after these guards.
- `true` means newly applied. `false` means that same option was already applied in this unresolved visit; it does not mean insufficient funds.
- A different option after application, unknown option, stale target, dead/missing main, insufficient currency or zero healing follows existing rejection/exception behavior with no changes. Do not catch all exceptions and translate them to `false`.
- Complete state rejects later application, including a duplicate, before invoking the compatibility operation.
- Preserve existing soul-adjusted maximum health, integer rounding, healing cap and cost behavior. The shared `Currency` Buffer is the payment source; battle-local economy is not involved.

The old `Combat.Recover(optionId)` does not carry caller identity. Keep its signature and semantics for compatibility; new recovery callers use `CoreGroup.Recovery`. This package does not claim stale-request protection for arbitrary legacy direct calls or commands retained across a Run reset with reused identities.

### Completion and approved skip policy

- `CompleteRecovery(chapterKey, nodeId)` finishes the currently prepared, matching, non-boss recovery node. It preserves Current options and receipt for readers until normal selection change.
- After an applied option, completion changes only the node's clear flag. It does not heal or charge again.
- `Recover` and `CompleteRecovery` are separate commits. If completion fails before its stage-clear write, the healed HP, paid currency and applied-option receipt remain committed, and the node remains uncleared. After correcting that failure, retry only `CompleteRecovery` for the same current node. Do not roll back recovery, re-enter the stage, reset options or repeat payment to recover from a completion fault. If the current identity no longer matches, reject the stale retry under the existing identity checks.
- **User-approved policy (2026-10-03):** completion without applying an option is allowed. It costs zero, heals zero and clears the node. This prevents full HP or an unaffordable menu from blocking progression. No additional policy approval is required to implement this path.
- Both paths use one API. A dedicated Skip enum, synthetic recovery receipt or second completion-state store is unnecessary: `AppliedOptionId` distinguishes application, and existing stage progress distinguishes completion.
- A duplicate completion returns `StageAlreadyCleared`, not another accepted mutation. Stale chapter/node requests return the matching existing progression rejection.
- Never clear the map, end a Run, award rewards, publish a combat result or fabricate `StageOutcome.Victory`/an HQ-destruction reason.
- Permit completion without an applied option regardless of full health or insufficient funds, while retaining all normal prepared-stage, identity and phase checks.

## 6. Data conversion and entry publication

### DB adapter

Implemented internal adapter:

```csharp
RecoveryStageSpec RecoveryDefinitionAdapter.Read(
    IDataBaseQueryContext database, int chapterKey, StageKey stageKey);
```

Read `StageTableQuery.GetStage(stageKey.Value)` and validate:

1. Root exists, `StageKey` equals the requested key, `MapKey` equals the requested chapter, and normal root validation succeeds.
2. Detail is `EventStageDetail`; `StageType` is Event; `MapRole` is not Boss. This is the current recovery convention, not a general promise that every future event is recovery.
3. Validate the ordered, nonempty option key list. For every key read `RecoveryOptionTableQuery.GetRecoveryOption(key)`, require returned `OptionId == key`, and validate its data.
4. Convert each item to `RecoveryOptionSpec(new RecoveryOptionId(id), cost, new RecoveryHealSpec(mode, value))` in authored order. Construct `RecoveryStageSpec(stageKey, options)` and return detached data.

Do not invent a default price, option, heal mode, fixed three-option limit or fallback for missing data. `Full` ignores its value under the current contract; `Flat(0)` is valid data but selection yields no effect. No SO access, scene lookup, randomness or balance authoring belongs in this adapter.

### Entry sequence

```mermaid
sequenceDiagram
    participant Caller as View/Flow caller
    participant Core as RecoveryStageService
    participant Progress as ChapterProgressionCoordinator
    participant DB as Existing DB queries
    participant Buffer as BufferSystemGroup / Current
    Caller->>Core: EnterRecovery(chapterKey, nodeId)
    Core->>Buffer: Capture origin selection; reject retained battle
    Core->>Progress: PrepareStageEntry(chapterKey, nodeId)
    Progress-->>Core: Accepted target or rejection
    Core->>DB: Read root and ordered recovery options
    DB-->>Core: Validated detached definition
    Core->>Progress: Repeat live preflight
    Core->>Buffer: PrepareRecovery(expected origin, target, options)
    Buffer->>Buffer: Validate and prepare all Current values; install once
    Core-->>Caller: Accepted target
```

Repeat progression checks and compare origin chapter, selection presence and all origin selection values, plus the accepted target chapter/node/stage key. Changed origin or changed accepted target returns `StageMismatch`; a new navigation rejection is returned directly. Query-induced external mutations are not rolled back by this operation.

### Narrow Buffer contract

Add `IRecoveryStageBufferWriteSink` to the existing `BufferSystemGroup` and expose `IBufferQuery.RecoveryStageWriteSink`. It has one method:

```csharp
void PrepareRecovery(RecoveryEntryPreparation preparation);
```

`RecoveryEntryPreparation` is a public immutable call-local value under `BufferSystems/Current`, not a registered service or a navigation authorization. Its constructor is:

```csharp
RecoveryEntryPreparation(int expectedChapterKey, bool expectedHasSelection,
    StageSelectionData expectedStage, StageNodeId targetNodeId,
    StageKey targetStageKey, string targetStageName,
    IReadOnlyList<RecoveryOptionSpec> options);
```

Expose corresponding get-only `ExpectedChapterKey`, `ExpectedHasSelection`, `ExpectedStage`, `TargetNodeId`, `TargetStageKey`, `TargetStageName`, `Options`. Defensively copy the options and validate nonempty unique IDs. Preserve existing BattleEntrySelection and its public contract.

Add internal `PreparedCurrentSelection PrepareRecoverySelection(RecoveryEntryPreparation preparation)` to `ICurrentProgressBufferStore`. It checks the full expected origin, rejects an already selected identical target, and prepares a detached Current selection with `Shop = null` and new `CurrentRecoveryData`. Reuse the existing `PreparedCurrentSelection` and `InstallSelection` storage boundary; a private shared origin validator is acceptable, but no generic transaction abstraction is needed.

Before installation, the Buffer group verifies there is no retained battle, chapter/node/stage binding matches registered facts, the registered target is Event/non-boss, and the target node/map is not cleared. Build every replacement object before the single `InstallSelection` call. Buffer does not query DB, select navigation policy, calculate healing or schedule Jobs. Character, Currency, Combat and progression flags are untouched by entry. A failed entry preserves the prior Shop/Recovery objects as well as selection.

This guarantee assumes the existing serialized synchronous execution model. Concurrent/reentrant writers, process failure and out-of-memory recovery are outside it.

## 7. Completion implementation boundary

Keep navigation and progress interpretation in the existing chapter code. Add an internal coordinator operation, invoked by the recovery service after its type/preparation/phase guards:

```csharp
ChapterProgressionResult CompleteRecoveryStage(
    int chapterKey, StageNodeId expectedNodeId, StageKey expectedStageKey);
```

It rereads Current, Map and graph facts, verifies expected identity, then invokes a new pure command:

```csharp
ChapterProgressionResult ChapterProgressionCommands.CompleteRecoveryStage(
    int requestedChapterKey, int currentChapterKey, int graphChapterKey,
    bool hasEnteredStage, StageNodeId enteredNodeId, StageKey enteredStageKey,
    bool isChapterClear, IReadOnlyList<ChapterNodeProgress> nodes);
```

The command checks the same chapter/selected-node/key/already-clear invariants as normal stage resolution. Share an internal domain validation helper if needed without changing combat behavior. A valid result is `ChapterProgressionResult.FinishStage(chapterKey, nodeId, stageKey, false)`. This result represents a progress write, not a combat victory. The coordinator applies only `_stageWrite.SetClear(chapterKey, nodeId.Value, true)` after acceptance. No Map or Current write is needed.

The recovery service must validate the non-boss Event root and live registered type/role before invoking this method. Keep the new coordinator method internal so general callers cannot use it as an unguarded noncombat completion API. Do not change the existing public progression interface or extend StageOutcome.

## 8. Framework integration and invariants

| Guide contract | Package application |
| --- | --- |
| FW-001 | Export `IRecoveryStageService`; no concrete implementation registration. |
| FW-002 | Register the service as a module as well, because it uses `ISystemInitData<IDataBaseQueryContext>` and `ISystemPostBindService`. |
| FW-003 | Factory creates and exports everything before its single Build. No runtime registration. |
| FW-004 | Call Buffer ports; do not expose private collections or mutate DB definitions. |
| FW-016 | Require DB initialization and Buffer binding. Verify both stage orders supported by the Framework; do not invent a stronger global ordering. |
| FW-101 | Expose the narrow Recovery interface on the existing Core Context. |
| FW-205 / creation guide | Add one Core child service key and Factory/Context wiring. No new root group or Framework dependency. |

Factory constructs the existing Combat service once, exports it, then passes that same `ICombatProcessingService` and the existing `ChapterProgressionCoordinator` to `RecoveryStageService`. Constructor signature: `RecoveryStageService(ICombatProcessingService combat, ChapterProgressionCoordinator progression)`. `InitData` stores DB queries; `PostBindService` gets the existing Buffer group from the root resolver. Do not resolve the Recovery service through the root using its child key.

The service needs no Tick, scene binding, resource scope, event subscription, new assembly or artificial Shutdown state. Its group already propagates InitData and PostBindService. Stateless calculation remains in the separate Core assemblies; DB conversion and orchestration remain in ProjectL.Systems. Public Context additions require mechanical updates to test doubles, not changes to production View/Flow behavior.

## 9. Verification and acceptance gates

### Core gate C

- Ordered real query → adapter → registered recovery service → real Current publication succeeds without preselecting the target.
- Invalid/missing options, wrong stage/chapter/role, unreachable or cleared targets, stale origin and retained battles produce no recovery-owned writes.
- Entry changes only Current selection/options. It does not touch currency, main health, battle results, input, combat numbering or graph clear flags.
- Same-node entry cannot recreate options or reset a receipt. A different node sharing the same StageKey receives fresh options, so identity is chapter/node/key, not just StageKey.
- Applying an option preserves existing payment/healing/duplicate rules. No-effect and insufficient-funds cases leave every related fact unchanged.
- Application and completion reject stale node intent and post-completion use.
- Applied and no-purchase completion clear only that node; duplicates do not clear or pay again; a legal successor becomes enterable.
- A focused recovery-success/completion-failure case retains HP, currency and receipt, leaves the node uncleared, then succeeds by retrying completion alone after the failure is corrected. Exactly one uncleared-to-cleared change occurs, with no additional healing/payment or option reset. Use an existing query/test seam for the pre-write failure, not a production fault hook.
- Extend the existing P3.2 cycle: battle settlement → caller-owned cleanup through `Combat.EndBattle()` → EnterRecovery → Recover → CompleteRecovery → next injected-definition battle, using the same registered services and real Buffers. Before cleanup, the retained terminal battle rejects recovery without mutation; after legitimate cleanup, entry succeeds without forced cleanup or repeated settlement. Use a non-boss battle with a legal recovery successor and a fixture that leaves measurable missing HP after a nonzero settlement effect, so both accidental settlement replay and recovery healing are observable.
- Across that cycle, assert settled persistent HP and progress survive cleanup; currency changes only by the recovery cost; recovery completion clears only its node; healed HP enters the next battle. Retain the combat result and `LatestResultSequence`, `HandledCommandNumber`, `AnsweredPreviewNumber` during cleanup/recovery/next preparation before new combat processing. Existing cleanup clears the active battle, soul field and preview response but retains `Completion`; recovery leaves those retained facts alone, and the next successful battle preparation clears `Completion`. Do not reset View-owned input/presentation from recovery. This is Core/service evidence, not Method_Key readiness or actual Job/Flow/scene acceptance.
- Real Factory, Context and module lifecycle tests pass. Relevant battle-entry and shop behavior do not regress.

Existing source coverage should be reused rather than duplicated: `CoreStageCombatCycleTests.RegisteredCycle_VictoryCleanupAndNextPreparationPreservePersistentState` already checks registered battle settlement, cleanup and next preparation; `EnterBattle_RetainedBattleRejectsWithoutReinitialization` checks retained-battle rejection for battle entry. `CharacterBattleSettlementTests.MultipleChalices_SettleOnceAndPersistHealedHealthAcrossCleanupAndNextPreparation` and `Recovery_IsOneUseAndPaysAndHealsTogether` cover settlement-once and recovery payment-once respectively; `CombatCommandPublicationTests.ResultNumbersAndLastResult_SurviveEndBattleAndNextPreparation` covers result/command numbering retention. These are read-only source observations, not new test-run results. They do not prove the full DB-backed recovery bridge or recovery-success/completion-failure retry; P3.2 and the focused P2 case add those missing assertions within the planned fixtures.

### External gates

| Gate | Owner | Evidence required |
| --- | --- | --- |
| D | DB | Registered recovery table/items, matching root/detail keys and non-boss recovery placement; catalog builds; authored values supplied by data owners. |
| P | Product/design with View/Flow | No-purchase completion approved by the user on 2026-10-03. This policy is no longer pending. UI behavior and option count remain separate external decisions. |
| V | View/Flow | Calls the new API with current chapter/node intent, reads installed facts, handles duplicate/rejected results and drives presentation/transition timing. |
| A | Integration | D/P/V complete and an authored scene/runtime path demonstrates enter → apply or skip → finish → next node. |

Core gate C may complete using DB query fixtures without D/V/A. Record the user approval of no-purchase completion separately from implementation/test completion and remaining UI/content decisions. Do not relabel that as authored-game acceptance.

The battle-entry handoff records 222/222 focused tests and 1371/1376 broader tests, with five catalog failures reporting `Combat stage requires Economy`. Those are historical results, not results of this package. Capture a fresh baseline at implementation time; compare failure identities and stacks instead of assuming any five failures are acceptable.

## 10. Delivery

The [implementation plan](../plans/2026-10-03-recovery-stage-core-connection-implementation-plan.md) delivers three work packages: safe entry, guarded application/completion, and composition/regression/handoff. Each package has its own observable acceptance criteria. The [final handoff](../handoffs/2026-10-03-recovery-stage-core-connection-handoff.md) records implementation and fresh verification separately from external readiness. This document remains the design authority; execution evidence is recorded in the handoffs.
