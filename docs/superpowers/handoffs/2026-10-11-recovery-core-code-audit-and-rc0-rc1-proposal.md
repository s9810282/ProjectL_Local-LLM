# Recovery Core audit and RC0 RC1 contract proposal

Date: 2026-10-11. Status: proposal for Core and View review; no contract agreement or runtime implementation is claimed.

Core already implements recovery entry, healing and payment, one application per entry, and completion without selecting an option. The remaining work is to expose a safe UI contract and connect numbered requests to those operations. RC0 should settle that contract; RC1 should supply independently compilable types before View implements its input Buffer.

## 1 Scope and comparison baseline

This review compares the supplied [recovery event Core contract request](2026-10-09-recovery-core-handoff-Hojin.md#2부-회복-이벤트-ui-연결을-위한-core-계약-요청) with the current working tree on `feature/Cheon/refactor/core-system`, HEAD `2d0509dffc6c249965dc10855074f16ac49091af`. Existing uncommitted changes, including Run gold unification, are part of the baseline. The supplied request is a statement of View requirements, not evidence that its named APIs or asset changes exist here.

The request file was read from a local copy outside the repository; it is now [part 2 of the recovery Core handoff](2026-10-09-recovery-core-handoff-Hojin.md#2부-회복-이벤트-ui-연결을-위한-core-계약-요청). Its linked View design and plan are not present in this checkout. Proposed signatures below require View comparison against those documents before V4 starts.

This task changes only this English document. Existing source, assets, tests and handoff documents remain untouched. No commit, push, external message, compilation or Unity test execution is included. Existing test cases below establish coverage in source, not fresh passing results. The request's reported 695/695 result is not a verification result for this working tree.

## 2 What already exists and what is missing

| Requirement | Current code evidence | Additional work |
|---|---|---|
| Recovery entry | `IRecoveryStageService.EnterRecovery(int, StageNodeId)` validates progression, reads DB options in authored order, revalidates the target and publishes selection with options | Publish a Core-owned entry context with that same entry transaction; connect the UI entry path |
| Frozen choices | `CurrentRecoveryData` copies its nonempty, unique option list; each `RecoveryOptionSpec` exposes ID, cost and original heal mode/value | Preserve this source and order; no View DB rule lookup |
| Healing and payment | `CombatProcessingService.Recover` uses installed choices, current main state, main soul count and `CurrencyReadSink.Amount`; `CharacterRecoveryCalculator` prepares the change | Return typed ordinary rejections; expose the same calculations for display |
| Healing cap | Full, flat and percentage healing are capped at the current deficit; percentage uses integer truncation; maximum health includes main souls | Publish actual projected healing and availability; keep calculation in Core |
| One application | Same option returns `false` without another payment; another option after application throws | Preserve no second payment; map these cases explicitly for UI |
| Failure leaves gameplay facts unchanged | Calculation precedes publication; transactions compare source facts; operations and publication tests assert unchanged health, currency, receipt and progression | Keep these regressions under the typed API and the numbered processor |
| Completion | `CompleteRecovery` delegates to `CompleteRecoveryStage`; no selection is required; only the node is cleared; duplicate completion returns `StageAlreadyCleared` | Keep selection and completion separate; add entry-context validation and a numbered result |
| Current receipt | `CurrentProgressReadSink.Recovery` exposes `AppliedOptionId`, `PaidAmount`, `HealedAmount` | Project these existing facts, without a second mutable receipt |
| Runtime assembly | Core factory constructs `RecoveryStageService`; Core group exposes `Recovery`; Buffer group exposes Core recovery transactions | Register the new query/processor/results contracts through their actual groups |
| Display and result candidates | `RecoveryOptionSnapshot`, `RecoveryStageSnapshot`, `RecoveryCommandResult`, `RecoveryFailureReason` and related contract tests exist | They are not produced by the current runtime recovery path |
| Entry identity | `RunId` and `StageEntryId` value types exist; candidate snapshots require RunId/EntryId/RunRevision | Runtime Run and Current Buffers do not issue or expose those values |
| Numbered input and output | `StageViewBuffer` records numbered node choices; Combat Buffer has its own command/result machinery | No RecoveryView input Buffer, recovery processed number or recovery request result Store exists |

### 2.1 Corrections to the supplied request

1. `StageCommandProcessingService`, `IStageCommandResultReadSink`, `UnsupportedStageType`, and the named `StageCommandProcessingTests.cs` do not exist in this checkout. They cannot be extension points here. The runtime has `StageViewBuffer` and a View `StageMapEntryFlow`, which currently accepts Combat nodes and removes other node requests with a report. Shop and Event are both excluded in that flow. Entry routing is therefore also a View integration dependency.
2. `Event_Recovery.asset` currently references options 1 and 2, in that order. Its GUIDs resolve to `Stage/Event/Recovery/Item/RecoveryOption_1.asset` and `_2.asset`. Option 3 exists, but is not referenced by this event. The request's three-choice change must be brought over separately; this review does not modify the asset. Core must display the actual published list and must not invent a third choice.
3. Currency is currently the single Run gold balance (`CurrencyBuffer.Amount`) in the working tree. Recovery still uses that same balance. The new contract must not introduce a second recovery or stage wallet.

### 2.2 Source and test references

Paths are relative to the repository root. These are the actual files inspected, rather than the request's candidate file list.

| Subject | Source |
|---|---|
| Public operations and validation | [RecoveryStageService.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/RecoverySystem/RecoveryStageService.cs), [ChapterProgressionCoordinator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/ChapterProgressionCoordinator.cs) |
| Calculation and publication | [CombatProcessingService.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatProcessingService.cs), [CharacterRecoveryCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CharacterRecoveryCalculator.cs), [RecoveryHealCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Run/RecoveryHealCalculator.cs), [StageServiceTransactions.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/StageServiceTransactions.cs) |
| Current facts and resets | [CurrentRecoveryData.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Current/CurrentRecoveryData.cs), [CurrentProgressBuffer.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Current/CurrentProgressBuffer.cs), [RunProgressBuffer.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Run/RunProgressBuffer.cs), [ProgressTransactions.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/ProgressTransactions.cs) |
| Display candidates | [RecoveryOptionSnapshot.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/RecoveryOptionSnapshot.cs), [RecoveryStageSnapshot.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/RecoveryStageSnapshot.cs), [RecoveryCommandResult.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Results/RecoveryCommandResult.cs) |
| Actual map entry and asset | [StageMapEntryFlow.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapEntryFlow.cs), [Event_Recovery.asset](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Event/Event_Recovery.asset) |
| Existing Core query pattern | [ResultQueryService.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/ResultQueryService.cs) |

Existing coverage includes:

- `RecoveryStageServiceTests.EnterRecovery_UnconfirmedTargetPublishesOptions`, `RepeatEntry_DoesNotResetReceiptAndDifferentNodeSameKeyGetsFreshOptions`, and `FinalRecoveryLookupEndsRun_PreventsPublication`.
- `RecoveryStageOperationsTests.cs` is another part of the `RecoveryStageServiceTests` fixture. Its tests cover pay/heal once using installed options, other-option rejection, invalid application without writes, skip completion at full health or without funds, duplicate completion, failed completion preserving an applied receipt, stale successors, and soul-dependent rounding.
- `RecoveryEntryPublicationTests` covers detached choices, invalid/stale publication, retaining a consumed receipt, and retained battle rejection.
- `CharacterRecoveryRuleTests` covers capped healing, payment, no-effect/death/funds rejection, percentage rounding and soul contributions.
- `RecoveryStageCycleTests.cs` belongs to `CoreStageCombatCycleTests`; it checks persistent HP into the next battle, both with recovery and with a skipped choice.
- `RecoveryRuntimeCompositionTests.cs` belongs to `FrameworkRuntimeCompositionTests`; it exercises actual factory assembly, but invokes recovery service methods directly. It does not demonstrate numbered UI request processing.
- `RecoveryStageContractTests` constructs candidate snapshots and a separate internal `RecoveryStageState`. That state is not the runtime `CurrentRecoveryData`; installing both as mutable authorities would duplicate recovery facts.

## 3 Recommended contract approach

Use a recovery entry context containing the existing `StageEntryId` plus chapter/node/stage keys. Core issues it on successful entry and stores it with `CurrentRecoveryData`. A fresh GUID for each successful entry distinguishes the same node in a new Run without requiring a project-wide RunId/Revision rollout.

Expose display state through a narrow Core read query that projects the current Buffers. Keep numbered request results in a Core-written Buffer Store. This follows the existing `ResultQueryService` pattern while separating current display state from historical request results.

| Alternative | Assessment |
|---|---|
| Minimal entry context with a read query | Recommended. Reuses existing ID and leaf contracts, preserves current state ownership, supports fresh reads after external changes |
| Activate the full candidate RunId/RunRevision model | Requires issuance, reset and revision contracts beyond recovery and coordination with other runtime systems; only choose if Core already plans that rollout |
| Validate chapter/node alone | Insufficient: an old request can match a new Run's same node |

The query is an explicit RC0 proposal, not an assumption that the View design already accepts it. If View requires a stored display snapshot instead, RC0 must specify every health, soul, currency and progress mutation that republishes or invalidates it. A snapshot published only on entry and selection is insufficient for reconnecting after other source changes.

## 4 RC0 proposed agreement

All names below are proposed additions. Existing `EnterRecovery(int, StageNodeId)` remains the entry operation. View must compare these signatures with V4 through V6 before implementation.

### 4.1 Entry context and input

`RecoveryEntryContext` is a readonly value with `StageEntryId EntryId`, `int ChapterKey`, `StageNodeId NodeId`, and `StageKey StageKey`. Its public constructor validates nonempty IDs and a positive chapter. Constructing a value does not grant authority: Core compares all fields with the installed context before acting.

Core creates `StageEntryId(Guid.NewGuid())` only for a successful new recovery entry. The ID and choices are installed together. Failed entry leaves the old context unchanged. Querying, hiding, reconnecting, or repeating an entry request never replaces the context or clears its receipt. Leaving that selection, clearing Current, or resetting the Run removes the active context. The next entry receives a fresh ID even if chapter/node/stage keys repeat. IDs are transient runtime values; save/resume is outside this contract.

The proposed runtime field is `CurrentRecoveryData.Context : RecoveryEntryContext`; `WithRecovery` preserves it. UI receives the same value through `RecoveryStateSnapshot.Context`. It does not construct a new identity to make a snapshot valid.

RC1 supplies `RecoveryCommandKind` with only `Select` and `Complete`, and immutable `RecoveryCommandData` with `Kind`, `Context`, and nullable `OptionId`. Construction uses these exact factories:

```csharp
RecoveryCommandData.Select(RecoveryEntryContext context, RecoveryOptionId optionId)
RecoveryCommandData.Complete(RecoveryEntryContext context)
```

Select requires a nonempty option ID. Complete forbids an option ID. No input contains cost, healing, wallet, health, availability or a View-issued Run identifier. Chapter/node/stage are inside Context and are not duplicated as separate command fields.

### 4.2 Display state and fresh reads

Proposed read contract in Systems:

```csharp
public interface IRecoveryStateReadSink : IServiceModule
{
    bool TryGetCurrent(out RecoveryStateSnapshot state);
}
```

It is exposed as `SystemGroupsService.InGame -> CoreGroup.RecoveryState`, implemented by a Core `RecoveryStateQueryService`. The query owns no mutable domain state. It reads Current, Character, Currency, Stage, Map and Run facts, then calls pure Core calculation for projections. It must not use the exception message from the existing planner to infer funds or no-effect reasons.

`RecoveryStateSnapshot` contains Context, `HealthStateSnapshot MainCharacterHealth`, `int Gold`, ordered immutable option states, `RecoveryReceiptSnapshot Receipt`, and `bool IsCompleted`. `IsCompleted` is projected from Stage progress. `CanComplete` is derived from `!IsCompleted`. Receipt amounts come from `CurrentRecoveryData.PaidAmount/HealedAmount`; its `SpentAmount` name is a projection alias, not another wallet.

Each `RecoveryOptionState` contains the existing immutable `RecoveryOptionSpec Option`, `int ProjectedHealAmount`, and `RecoveryFailureReason UnavailableReason`. ID, cost and original heal mode/value are read from Option. `CanSelect` is derived from `UnavailableReason == None`. List position is authored order; no independent order or weight field is introduced.

Every successful query reflects the facts current at that synchronous read, including changed gold, main health and main soul count. It does not reread installed option rules from DB. No ordinary external source change is prohibited solely to keep a display cache valid. Start with projection on read; any later cache must track every input fact and cannot become a second mutable source.

Return `false` and null when there is no current recovery selection, when it was replaced/cleared, or when the Run has ended. A still-selected completed node may return its completed snapshot for UI recovery, with all choices disabled. Missing main state, impossible health, broken graph binding, or missing lifecycle dependencies are faults and must not be disguised as `false` or an ordinary rejection.

View refreshes through this read contract on attach, after a request result, and during its visible state refresh. A previously retained snapshot remains historical and immutable. Core revalidates current facts at execution even if the displayed snapshot said selection was possible.

### 4.3 Operations and result classification

Proposed typed additions to the existing service:

```csharp
RecoveryOperationResult SelectRecovery(RecoveryEntryContext context, RecoveryOptionId optionId)
RecoveryOperationResult CompleteRecovery(RecoveryEntryContext context)
```

Keep legacy `Recover(int, StageNodeId, RecoveryOptionId)` and `CompleteRecovery(int, StageNodeId)` for existing callers during migration. New numbered UI processing uses the context-aware operations. Both paths must share the same calculation/publication implementation; retain no independent legacy calculation path. The old `bool` remains an old API and is not a UI success contract.

`RecoveryOperationResult` has `RecoveryResultKind Kind` (`Accepted`, `TradeRejected`, `StateMismatch`), `RecoveryFailureReason FailureReason`, `bool AlreadyApplied`, nullable `RecoveryStateSnapshot StateAfter`, and nullable `ChapterProgressionResult Progression`. Its factories are `Selected(state, alreadyApplied = false)`, `Completed(state, progression)`, `TradeRejected(reason, state)` and `StateMismatch(reason)`. Factory validation must reject contradictory fields.

| Case | Proposed classification and reason | View action and gameplay effect |
|---|---|---|
| First valid selection | Accepted / None | Show receipt and updated state; one payment and heal |
| Same option after application, same open context | Accepted / None, AlreadyApplied=true | Show existing receipt; no payment or heal |
| Another option after application | TradeRejected / AlreadyResolved | Keep receipt and refresh; no writes |
| Funds insufficient | TradeRejected / InsufficientFunds | Refresh choices; no writes |
| Full HP, flat zero, or percentage truncating to zero | TradeRejected / NoEffect | Refresh choices; no writes |
| Context differs, including the same keys in a new Run | StateMismatch / EntryMismatch | Release stale UI request and reread current state; no writes |
| No current recovery, ended Run, or retained battle | StateMismatch / WrongPhase | Reread/close recovery UI; no writes |
| Selected recovery node or chapter already cleared | StateMismatch / StaleEntry | Prevent more selection; show completion or current destination |
| Option absent from the installed list | StateMismatch / UnknownOption | Reread rather than use stale choices; no writes |
| Complete without a choice, or after a choice | Accepted / None | Clear only this node; no payment or heal; Progression is accepted with ClearStage=true |
| Duplicate Complete under a new request number | StateMismatch / StaleEntry | No new clear; retain original completion facts |
| DB corruption, missing bindings/main, impossible values, overflow, source inconsistency inside publication | Exception to runtime fault boundary | Stop the affected flow; do not return an ordinary trade rejection |

Validate structure first, then current Run/phase, then Context, then node/chapter completion, then option membership, then prior application, then healing effect, then funds. This makes a zero-effect choice report NoEffect even if funds are also insufficient. Within the same open context, a same-option retry succeeds without recalculating or repaying. After node completion it reports StaleEntry.

Selection availability uses the same option/effect/funds classification. All options are unavailable after any application (`AlreadyResolved`) or completion (`StaleEntry`), even though a repeated same-option request remains idempotent while the node is open. `RunMismatch`, `RevisionOverflow` and `Disposed` in the existing enum are not invented runtime outcomes for this minimal context proposal.

Accepted Select requires StateAfter with the matching applied option and receipt. Accepted Complete requires a matching completed StateAfter and an accepted node-only Progression result. TradeRejected requires an unchanged current StateAfter. StateMismatch carries no StateAfter for the requested entry; View uses the current-state reader to recover. Historical snapshots in results never replace authoritative gameplay state.

### 4.4 Numbered requests and result retention

View owns `RecoveryViewCommand` (positive Number plus the RC1 command data), `RecoveryViewBuffer`, its read/write interfaces and input removal. Core owns processing, handled number and a result Store. Proposed interfaces are:

```csharp
public interface IRecoveryViewBufferReadSink : IServiceModule
{
    int AcknowledgedResultNumber { get; }
    bool TryGetCommand(int expectedNumber, out RecoveryViewCommand command);
}

public interface IRecoveryViewBufferWriteSink : IServiceModule
{
    int Submit(RecoveryCommandData command);
    bool RemoveCommand(int number);
    void AcknowledgeResult(int number);
    void Clear();
}

public interface IRecoveryResultReadSink : IServiceModule
{
    int HandledCommandNumber { get; }
    bool TryGetLatest(out RecoveryProcessedResult result);
}

public interface IRecoveryCommandProcessingService : IServiceModule
{
    bool ProcessNextRecoveryCommand(int expectedNumber);
}
```

`RecoveryProcessedResult` is an immutable positive request Number, the full `RecoveryCommandData Command`, and the typed `RecoveryOperationResult Result`. This echoes kind, chapter/node/stage, EntryId and OptionId without a second copy of every field. Its constructor verifies accepted/trade result snapshots belong to Command.Context and that operation-specific fields match Command.Kind. Complete has no selection ID.

Keep one input pending and one result slot. View acknowledges only a result it has read and correlated in full. Acknowledgement is stored in the View input Buffer; it does not write Core results. Core must refuse a new operation while its latest result is unacknowledged, independently of View's button state. This prevents silent overwrite after Hide or Clear. Core never removes input.

Expose results through `IBufferQuery.RecoveryResultReadSink`; expose the processor through `CoreGroup.RecoveryCommands`. A View request-lifetime owner continues reading and acknowledging completed results while the popup is hidden. After a Run/entry change it correlates the echoed old command, skips displaying it on the new popup, and acknowledges it before permitting new work. Hide/Clear alone never serves as acknowledgement.

Number issuance, handled number, latest result and acknowledgement survive input Clear, node changes and new Run. Number zero means none; numbers are strictly increasing within one live runtime Buffer instance. Reject issuance at int.MaxValue before mutating input. Acknowledgements are monotonic, cannot exceed the last issued number, and do not skip an unread published result. Full runtime replacement ends the old request/job lifetime; carrying old jobs into a replacement runtime is unsupported.

`ProcessNextRecoveryCommand` returns true only when a previously unhandled exact-number request receives a published result, including TradeRejected and StateMismatch. It returns false without a new transaction or result when that input is missing/cancelled, that number was handled, or an older result is unacknowledged. Nonpositive expected numbers are argument errors. A numbering gap due to cancellation is legal; handled number is not a count of issued requests.

View schedules processing only after prior result acknowledgement. On false, it rereads input and result state: missing/already-handled requests finish normally; a retained older result must be consumed before retrying a present input. It must not interpret backpressure as a successful recovery or permanently discard a still-present command.

All operation/query/submit/clear/result methods run synchronously on the game runtime thread. Parallel or reentrant recovery processing is rejected. Before transaction commit, revalidate the exact input and Core context after external lookups. If Clear or replacement removed the input before commit, return false without a transaction/result. After an applied transaction, finish result publication regardless of subsequent input Clear/Hide; never reread input as a condition for posting the applied result.

Prepare result capacity, DTOs, projected final state and all fallible validation before gameplay writes. The transaction installs the accepted gameplay facts, final receipt and matching processed result/handled number through a coordinated prepare/install path, without callbacks between assignments. A failure before installation leaves gameplay and result state unchanged. If a fault occurs after commit, do not retry the transaction automatically; it is a fault-boundary incident, not cancellation.

### 4.5 Entry routing boundary

`EnterRecovery(int, StageNodeId)` already publishes a legal selection and its choices. It must not be preceded by `ConfirmStageEntry`: that preselects the node and can defeat existing recovery entry validation/publication.

RC3 needs a real numbered Stage node entry endpoint in this branch or an explicitly agreed equivalent. Do not write an adapter against the absent `StageCommandProcessingService`. Core owns legal Event/recovery validation and entry-result publication; View owns node routing, Jobs and screen opening. The entry endpoint's origin context/numbering contract must be settled separately if a queued entry can survive a Run reset. The Select/Complete context proposed here is issued after entry and does not validate a pre-entry node click.

This scope treats the currently supported `EventStageDetail` as recovery. Future event types require explicit type/input/result contracts; no generic event engine or future-event enum is part of RC1.

## 5 RC1 type delivery

RC1 supplies definitions and constructor/factory tests only. It must compile without RecoveryViewBuffer, Core runtime publication, Unity UI or any unimplemented processor.

| File under CoreCaculator/Contracts | Proposed public contract |
|---|---|
| `Commands/RecoveryEntryContext.cs` | Value constructor `(StageEntryId entryId, int chapterKey, StageNodeId nodeId, StageKey stageKey)`; value equality and validation |
| `Commands/RecoveryCommandKind.cs` | Select and Complete only |
| `Commands/RecoveryCommandData.cs` | Immutable Context/Kind/OptionId; the two factories in 4.1 |
| `Snapshots/RecoveryOptionState.cs` | Constructor `(RecoveryOptionSpec option, int projectedHealAmount, RecoveryFailureReason unavailableReason)`; derived CanSelect |
| `Snapshots/RecoveryStateSnapshot.cs` | Constructor `(RecoveryEntryContext context, HealthStateSnapshot mainCharacterHealth, int gold, IReadOnlyList<RecoveryOptionState> options, RecoveryReceiptSnapshot receipt, bool isCompleted)` |
| `Results/RecoveryResultKind.cs` | Accepted, TradeRejected, StateMismatch |
| `Results/RecoveryOperationResult.cs` | Validated factories in 4.3; derived success rather than a separately set bool |
| `Results/RecoveryProcessedResult.cs` | Constructor `(int number, RecoveryCommandData command, RecoveryOperationResult result)` |

Reuse `StageEntryId`, `StageNodeId`, `StageKey`, `RecoveryOptionId`, `RecoveryOptionSpec`, `RecoveryHealSpec`, `RecoveryHealMode`, `HealthStateSnapshot`, `RecoveryReceiptSnapshot`, `RecoveryFailureReason` and `ChapterProgressionResult`. Do not fabricate RunId or revision fields to instantiate the existing `RecoveryStageSnapshot`.

The existing top-level `RecoveryStageSnapshot` and `RecoveryCommandResult` remain candidate APIs, outside this proposed runtime route. Their fields force the unimplemented global identity/revision model. The new runtime DTOs reuse their useful leaf contracts without editing legacy constructors or creating a second `RecoveryStageState`. If Core prefers migrating those candidates instead, revise RC0 explicitly before RC1 delivery; do not maintain two published runtime formats for the same consumer.

RC1 also provides the Systems read/result interface declarations in `BufferSystems/Recovery/`, including `IRecoveryStateReadSink` and `IRecoveryResultReadSink`. Implementations and registrations belong to RC2/RC3. RecoveryView read/write interface declarations and the numbered input wrapper remain View-owned V4 work; their agreed signatures are recorded in 4.4. The processor declaration belongs to Core's RecoverySystem and is implemented only after V4.

### 5.1 Required type validation

- Default/empty context, invalid chapter, invalid command enum, missing Select option, and a Complete option are rejected before issuance. A readonly struct's default must also be rejected by downstream factories; constructor validation alone is insufficient.
- Option and snapshot constructors reject nulls, duplicates, negative gold/projections/amounts, invalid reason combinations and projection above health deficit. Choices are nonempty and retain authoring order.
- Availability supports only None, InsufficientFunds, NoEffect, AlreadyResolved and StaleEntry. None requires a positive projection, adequate gold and an open, unapplied entry. Rejection/availability priority matches 4.3.
- Receipt's applied ID must belong to the options; paid amount matches its installed cost; no applied ID means zero payment/healing; an applied selection has positive healing. Do not compare historical HealedAmount to the current deficit after healing.
- Accepted Select/Complete, same-option success, TradeRejected and StateMismatch cannot construct contradictory ResultKind, reason, snapshot, AlreadyApplied or Progression combinations. A Complete never returns AlreadyApplied=true.
- Lists are defensively copied and publicly read-only; leaf objects are immutable. Caller list mutation and later receipt/source changes cannot alter retained state/results.
- No DTO or pure calculation references UnityEngine, Buffer implementations, Framework services or View. Preserve existing Core asmdef boundaries and folder-derived namespaces.

RC1 handoff includes the exact API/file list, changed files and `.meta` files, compile output, and focused contract-test XML. This proposal contains none of those implementation artifacts and does not satisfy RC1 acceptance.

## 6 Ownership and implementation dependencies

| Area | Owner and next stage |
|---|---|
| Pure contract types and tests above | Core, RC1 |
| Context in CurrentRecoveryData and entry prepare/install | Core, RC2; preserve context through WithRecovery |
| Shared evaluator and typed service operations | Core, RC2; preserve existing transaction regressions |
| Fresh state query and `CoreGroup.RecoveryState` | Core, RC2; public read-only interface, PostBindService dependencies |
| RecoveryViewBuffer, wrapper, sinks, issuance, acknowledgement and removal | View, V4 after accepted RC1 types |
| Core result Store and internal Store/write contract | Core, RC2/RC3; only Core processing/publication writes it |
| Exact-number processor and `CoreGroup.RecoveryCommands` | Core, RC3 after RC2 and V4 |
| Numbered node entry endpoint | Core contract with View entry owner, RC3; absent service must not be assumed |
| Node routing, Flow/Jobs, UI and scene connection | View, V5 through V8 after RC4; StageMapEntryFlow changes stay with View |
| BufferSystemGroup, BufferSystemFactory, Core group/factory and ServiceKeys | Serial shared changes: V4 input registration, Core output/processing registration, then composition tests; record Framework sharing |

Follow-up changes must be planned in phases. RC1 can be delivered without input implementations. RC2 query/calculation may run alongside V4 after RC1. RC3 requires both. Existing modified Core/Buffer/factory files must not be edited concurrently with their current owner.

## 7 SSOT SOLID and Framework review

SSOT: Current owns entry choices and receipt, Character owns persistent main HP and souls, Currency owns Run gold, Stage owns node completion, and the new Core result Store owns request processing facts. Display and result snapshots are immutable projections. Core does not install `RecoveryStageState` alongside CurrentRecoveryData. Context is stored once with the current recovery entry.

Single responsibility: pure evaluation computes heal/availability/rejection; the recovery service validates and coordinates; transactions compare and install; the reader projects; the processor correlates input/output; View owns UI and request lifetime. Factory/Group only assemble/expose these roles. Add the concrete recovery operation without a speculative event framework.

Interface separation and dependency direction: View receives input write and output read contracts; Core receives input read and internal output Store access. The actual group interfaces expose narrow services. Pure Application/Contracts do not reference Buffers or Unity. `RecoveryStateReadSink` is a read boundary even though its implementation belongs to Core.

Framework contracts: FW-001 registers service interfaces, FW-002 adds InitData/PostBindService modules, FW-003 builds each Builder once, FW-004 preserves internal Store access boundaries, and FW-016 requires completed runtime preparation before use. View integration additionally follows FW-005 through FW-009 and Assets/AGENTS.md input/UI conventions. Service keys stay under Systems/ServiceKeys; no game-specific types are added to Framework.

Review basis: [Framework programmer guide](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), [Framework architecture](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md), and [project SSOT SOLID application criteria section 4](2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙).

## 8 Review and acceptance checkpoints

For RC0, Core and View should agree on the minimal context, the query read location, typed failure table, same-option/duplicate-completion behavior, exact-number/acknowledgement semantics, retained results across reset, file owners and the missing entry endpoint. These are the concrete proposed decisions, not already accepted contracts.

For RC1, verify pure DTO compilation and focused constructor/factory tests independently of input Buffer implementation. View then copies the actual accepted signatures into its V4 through V6 plan. Changes to this proposal's signatures must precede dependent implementation.

RC2 through RC4 must demonstrate current-state freshness after gold/HP/soul changes and reconnect, same-node/new-Run rejection, unchanged gameplay state on every ordinary rejection, duplicate Select and Complete behavior, input disappearance before commit, result retention after commit/Clear, overwrite backpressure, legal numbering gaps, and real factory input/output assembly. Preserve the existing recovery-to-next-battle cycle. Check the three-choice DB change separately with DataBaseAssetTests when it is brought into this branch.

Current delivery status: code comparison and contract proposal are documented; RC0 remains unagreed and RC1 remains unimplemented. No Core or View runtime completion is implied.
