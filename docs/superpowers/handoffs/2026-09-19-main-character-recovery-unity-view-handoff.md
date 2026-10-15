# Main Character Recovery — Unity/View Handoff

> 과거 기록 (2026-10-06 정리): 아래 내용은 Framework/Buffer 전환 전 구현이며 현재 구현 지침이 아닙니다. 삭제된 계획·설계 원문은 Git 커밋 `606fdf4c`에서 확인할 수 있습니다. 현재 구조와 API는 [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)와 실제 코드를 기준으로 확인합니다.

Date: 2026-09-19

Source baseline inspected: `a48e0a1`

Status: recovery Core T1–T6 work is implemented. **T1's Unity caller migration and Unity integration acceptance remain pending.** Recovery is not yet connected to playable Unity screens.

This document records the implemented Core contract and the remaining integration work. It does not introduce a Core T7 or authorize unrelated refactoring. This handoff updates the historical progress statements and provisional D4–D6 policy notes in the earlier documents; it does not rewrite those documents.

References:

- Recovery implementation plan (Git 이력: `2026-09-19-main-character-recovery-stage-implementation-plan.md`)
- Recovery design (Git 이력: `2026-09-19-main-character-recovery-stage-design.md`)
- [T1 classification handoff](2026-09-19-stage-execution-t1-core-handoff.md)
- [Stage Gold and Character Draw handoff](2026-09-19-stage-gold-and-draw-view-handoff.md)

## 1. Ownership and remaining scope

The receiving owner needs Unity integration responsibility, not only screen rendering responsibility.

| Owner | Responsibility |
|---|---|
| Core, implemented | Entry validation and activation; option availability; atomic Run-currency payment, healing, and applied-choice record; completion receipt; progression; duplicate-command handling; HP handoff to subsequent combat. |
| Unity Data / authoring | Explicit execution classification; immutable map inputs; authored recovery choices and conversion to Core Specs; validation and asset assignments. |
| Unity Runtime / Setup / Host | Run construction prerequisites; branching from map selection; pending entry and screen lifetime; command ports; snapshot publication; completion and return to map. |
| View / Presenter | Display Core snapshots, send identifiers through the Host, show rejection reasons, and provide an agreed reachable completion interaction. |
| Product / content | Actual prices and healing values; labels and visuals; exit interaction and result presentation. These values are not established by test fixtures. |
| Core + Unity coordination | The earlier Draw/Gold Task 9 still calls for restricting raw Stage factory overloads and `Setup.CreatePlayerReserveUnit` after Unity consumers migrate. This is a separate Core access change, not a missing recovery algorithm or work to perform in a Presenter. |

Do not add a second mutable HP, Wallet, recovery-resolution, or deck owner in Unity. Recovery uses Run currency, not Stage Gold. No recovery command creates combat entities, Draw state, a battlefield, or a combat outcome.

## 2. Confirmed gameplay and retry rules

These policies were confirmed during Core implementation:

| Rule | Implemented behavior |
|---|---|
| Purchase count | At most one successful choice per recovery entry. A different choice after resolution returns `AlreadyResolved`. |
| Zero effect | `NoEffect`; no payment, purchase consumption, healing, or Revision increment. This includes full HP, Flat zero, and percentages that truncate to zero. |
| Purchase-free completion | `TryCompleteRecoveryStage` permits completion without a choice, including insufficient-funds and full-HP states. The receipt has a null option and zero amounts. |
| Percentage | Uses the current checkpoint **MaxHealth**, truncates fractional results, and caps healing at the current deficit. Core uses a wide multiplication intermediate. |
| Flat / Full | Flat accepts zero and rejects negatives. Full heals the current deficit and ignores its `Value`. |
| Duplicate selection while active | Same Entry/option returns success with `AlreadyApplied = true`; no additional payment, healing, or Revision increment, even if the first purchase filled HP or exhausted funds. |
| Selection after completion | Rejected. The latest retained recovery receipt yields `StaleEntry`. Older commands are also rejected by current phase/identity checks; there is no unlimited history or guaranteed `StaleEntry` classification after that receipt is replaced. |
| Duplicate completion | Returns success with `AlreadyApplied = true` and the **current** Run snapshot while that recovery remains `LastCompletedStage`. It can be retried during subsequent combat or recovery. Later completion replaces that retention window. |

Authoring specifies three choices. Core structurally accepts any nonempty valid list; authoring/UI validation should enforce three without changing Core's minimum.

The purchase-free **API** is approved. Button visibility, exact exit interaction, and result presentation are still presentation decisions. Before player-facing acceptance, provide an actual reachable completion path at zero balance and full HP. A hidden API alone does not satisfy the no-soft-lock requirement (SL-1).

## 3. Integration prerequisites — resolve before scene acceptance

Paths in this table are relative to `Assets/Scripts/ProjectL/` unless otherwise stated. These are verified current gaps, not already available adapters.

| Boundary | Current gap and required delivery |
|---|---|
| D1: `Unity/Runtime/Run/RunSessionHost.cs` | `InitializeProgression` still invokes the removed three-argument Run constructor. Supply the initial `RunCharacterDeckSpec`, full character catalog, required `RunNonCombatSpec` (explicit Recovery/Shop bindings and shop base seed), and intended Run balance. Preserve these initial inputs when starting a new Run. Do not restore implicit deck or feature-binding overloads. |
| Existing combat preparation | Complete the Draw/Gold handoff's Task 8–9 consumer migration: frozen `request.Draw`, explicit economy input, one prepared-services creation/adoption, and removal of the obsolete Unity deck/selection ownership paths. This handoff does not replace that checklist. |
| D2: `Unity/Data/ScriptableObjects/Stage/StageDefinitionSO.cs` | No `ExecutionKind` producer exists yet. Add authored `StageExecutionKind` metadata plus explicit Recovery/Shop content bindings and profile references, preserving existing Combat assets. Do not classify by key ranges, names, or fallback. |
| `Unity/Data/Runtime/StageMapCatalog.cs` | Still exposes `RegularStageKeys`. Replace it with `IReadOnlyList<StageContentRef> RegularStages`, copying each definition's Key/kind at catalog construction. Preserve source order, duplicate candidate weights, defensive copies, and conflicting-definition rejection. Reject a NonCombat boss definition. |
| Map callers | `RunSessionHost.Initialize` must pass `catalog.RegularStages` to the factory. `Unity/Setup/StageSelect/StageSelectSceneInstaller.cs` must iterate each ref's `Key` for visual coverage. `StageMapDefinitionSO.BuildCatalog(int)` already passes definitions; it does not need another parallel input list. |
| Unity tests | Migrate `StageMapDataLayoutTests` and `StageDatabaseAssetTests` away from `RegularStageKeys`, including reflection. Audit `StageResultFlowTests`' `RunProgressSnapshot` construction for the required Wallet and appended nullable `CurrentNonCombatKind` arguments. Use null outside active non-combat entries. Resolve the prior handoff's affected consumers/tests together. |

The Core factory now takes `IReadOnlyList<StageContentRef>` as `regularStages`:

```csharp
StageMapSession map = StageMapSessionFactory.Create(config, regularStages, bossStageKey);
```

There is no old StageKey-list overload. `StageExecutionKind` is exactly `Combat = 0, NonCombat = 1`; there is no `Recovery` enum member. The non-playable start has an empty key; the final/boss node is Combat-only. Do not change random-call order, candidate weighting, graph repair, node IDs, or connection ordering during this conversion.

NonCombat now requires an explicit `NonCombatStageKind.Recovery` or `Shop` binding in `RunNonCombatSpec`. Every actual NonCombat node must be bound; unused authored bindings are allowed. A Combat node remains Combat even if its key has a NonCombat binding. Never route an unbound node to recovery. See the [shop Core handoff](2026-09-19-sub-character-shop-core-handoff.md) for the constructor and seed contract. A generic non-combat registry or framework is not required.

## 4. Actual Core API and immutable inputs

The following are existing Core members. Unity recovery wrappers, command interfaces, SOs, screens, and snapshot feeds still need to be implemented by the integration owner; do not assume they already exist.

On `ProjectL.Core.Application.Run.RunProgressionService`:

```csharp
public RunRecoveryService Recovery { get; }
public RecoveryStageEntryRequestResult TryCreateRecoveryEntry(StageNodeId nodeId);
public RunCommandResult TryActivateRecoveryStage(
    RecoveryStageEntryRequest request, RecoveryStageSpec spec);
public RunCommandResult TryCompleteRecoveryStage(RunId runId, StageEntryId entryId);
public RunProgressSnapshot GetSnapshot();
```

On `ProjectL.Core.Application.Run.RunRecoveryService`:

```csharp
public bool TryGetSnapshot(out RecoveryStageSnapshot snapshot);
public RecoveryCommandResult TrySelectOption(
    RunId runId, StageEntryId entryId, RecoveryOptionId optionId);
```

Input types live in `ProjectL.Core.Contracts.Commands`; identifiers in `.Ids`; modes/phases in `.Common`; result and snapshot types in `.Results` and `.Snapshots`.

```csharp
// mode: RecoveryHealMode; value and cost: int; optionId: RecoveryOptionId.
var heal = new RecoveryHealSpec(mode, value);
var option = new RecoveryOptionSpec(optionId, cost, heal);
// stageKey: StageKey; options: IReadOnlyList<RecoveryOptionSpec> built from authored choices.
var spec = new RecoveryStageSpec(stageKey, options);
```

Create authored recovery data under the existing Unity Data/SO boundary and convert it into these immutable inputs before activation. Keep option IDs positive and unique within a stage; costs are nonnegative; Flat values are nonnegative; percentages are 1–100; Full ignores Value. Reject missing/null choices and missing healing data. Do not invent a free choice or default price to repair invalid assets.

Do not store live HP, balance, or applied choice in an SO. `RecoveryStageSpec` copies the option list; later source edits must not replace an already activated entry's frozen content. Combat field/roster/HQ validation belongs on the Combat preparation path; recovery authoring must not require fake combat data to enter.

Obtain `RecoveryStageEntryRequest` from Core rather than constructing it in the View. It freezes `RunId`, `EntryId`, `ExpectedRunRevision`, `NodeId`, `StageKey`, and checkpoint `Health`. It has no Draw or combat preparation payload.

## 5. Connection sequence

### A. Map selection and activation

1. Extend `Unity/Runtime/StageSelect/StageSelectRuntime.cs` to branch using the selected node snapshot's `ExecutionKind`. It currently always requests/prepares combat. Retain the existing Combat preparation path for Combat nodes.
2. For NonCombat with an explicit Recovery binding, call `TryCreateRecoveryEntry(nodeId)`; Shop uses the separate shop APIs. On success, retain that exact request with its frozen recovery Spec in a Unity-owned pending flow. Request creation does not move the map or change HP, balance, or Revision.
3. Resolve authored content using `request.StageKey`, convert it once, and prepare the recovery screen dependencies. Preserve pending identity, cancellation, and input blocking. The existing `StageSelectionHandoff`/`PreparedStageEntry` are combat-shaped; do not force recovery into them or silently relax their combat requirements.
4. Call `TryActivateRecoveryStage(request, spec)` when the receiving flow is ready. Only successful activation moves the map and changes the Run to `NonCombatStageActive`, incrementing Revision once. Do not call `StageMapSession.TrySelectNode` before or instead of this API.
5. Publish the returned `CurrentRun` and refresh the map, then query `Recovery.TryGetSnapshot` for the screen. Repeating a successful activation request returns `StaleEntry`; it is not an `AlreadyApplied` activation API.

Cancellation/preparation failure before activation leaves Core unchanged. After activation succeeds, a failed screen transition does not undo the entry: recover the existing Run/Entry through current snapshots and reconnect its screen. Do not create a new entry, reset the Run, or retry activation merely because display failed.

### B. Render and select

| UI data | Authoritative field |
|---|---|
| Entry binding | `RecoveryStageSnapshot.RunId`, `EntryId`, `NodeId`, `StageKey` |
| HP / maximum HP | `MainCharacterHealth.CurrentHealth`, `MaxHealth` |
| Run currency | `Wallet.Balance` |
| Choice identity, price, projected effect | `Options[i].Id`, `Cost`, `ProjectedHealAmount` |
| Button availability | `Options[i].CanSelect`, `UnavailableReason` |
| Resolution | `IsResolved`, `AppliedOptionId`, `RunRevision` |

Bind buttons to `RecoveryOptionId`. The Host supplies the originating Run/Entry IDs and calls `TrySelectOption` once. Buttons must not submit a price/heal value, mutate local authoritative HP, call public Wallet spending, or recalculate eligibility.

After a normal result, publish `CurrentRecovery` when present, including on rejection. A successful selection returns post-payment/post-healing values and increments Run Revision once. All choices become unselectable. A retained pre-selection `CanSelect` cannot authorize another purchase.

`ProjectedHealAmount` is a projection from the snapshot's current HP, not a record of the heal just applied. For completion totals, use the completion receipt's actual amounts. Selection and completion are separate commands; result presentation does not create another Core result phase.

### C. Complete and resume

Call `TryCompleteRecoveryStage(runId, entryId)` through the Host for the approved completion interaction, with or without a purchase. Success:

- Records a `CompletedStageSnapshot` with `ExecutionKind == NonCombat`, `NonCombatKind == Recovery`, and a `RecoveryReceiptSnapshot` containing `AppliedOptionId`, actual `SpentAmount`, and actual `HealedAmount`. Its Shop payload is null.
- Returns `StageSelection` / `InProgress`, increments Revision once, clears current entry/node/stage/kind, and releases the active recovery state.
- Keeps HP, balance, deck, and map position unchanged. Only the next successful activation advances the map.
- Makes `Recovery.TryGetSnapshot` return `false` with a null snapshot. The same service can later serve another recovery entry.

If completion fails after a purchase, keep the purchased effect and active entry. Retry completion only; do not repay, reselect, or refund. A completed entry does not fabricate Victory/Defeat or a battlefield. Do not use `TryLeaveStageResult`, combat `StageCompletionHandoff`, or combat scene-release confirmation as the recovery exit path.

When inspecting any `CompletedStageSnapshot`, branch on `ExecutionKind` before accessing `Outcome` or `FinalStage`. Recovery has no combat `FinalStage`; its `Outcome` accessor throws explicitly. Use `RunProgressSnapshot` and `RecoveryStageSnapshot`; the older combat-shaped `GameFlowSnapshot` rejects `NonCombatStageActive`.

Within NonCombat, branch on the receipt's explicit `NonCombatKind`. Recovery completion and selection retries recognize only Recovery receipts; a Shop receipt cannot resolve a Recovery command. Matching completion retries return current state during a later active Shop as well as combat/recovery, without releasing that later entry. `RunProgressSnapshot.CurrentNonCombatKind` is present exactly during `NonCombatStageActive`.

The next Combat entry uses the existing `TryCreateStageEntry` path. Preserve `request.Health` (current and maximum HP) and `request.Draw`, and use the full combat factory with explicit `StageEconomySpec` as required by the Draw/Gold handoff. Do not reset HP from the authored UnitSpec or recreate Main after adopting prepared Services.

## 6. Host lifetime, publication, and rejection handling

`RunSessionHost` currently exposes only the combat pending/activation/completion flow. Add recovery forwarding and lifetime handling in Unity; Core adds no event publication or scene management.

- Bind retained command ports to their originating Run instance and Entry IDs. An old button/port must not resolve the global Host and silently act on a new Run or another recovery entry.
- Block reentrant commands and competing confirmations during pending/active transitions. Publish snapshots only after the Core command returns. Subscriber/display failure does not roll back a committed transaction or justify another payment.
- Preserve an active recovery across ordinary screen departure/reconnection. Audit `ResetRun`, `OnDestroy`, `_stageReleased`, `ConfirmStageReleased`, and `StartNewRun`. The current reset guard checks only `StageActive`; a recovery must not be discarded by treating that as the only active phase. Explicit Run teardown still owns disposal.
- Recovery has no borrowed combat Stage to release. Keep existing combat cleanup guarantees while introducing the separate recovery lifecycle; do not satisfy it with a dummy combat release.
- Public Wallet credit/spend and Deck additions remain rejected during `NonCombatStageActive`. Do not weaken those guards; selection already performs payment atomically inside Core.
- Recovery and Run progression mutation APIs return `Disposed` after disposal. Direct Run/recovery reads throw after disposal. A live inactive recovery query returns false/null. A retained service from a disposed Run cannot be used for a replacement Run.

| Result boundary | Handling |
|---|---|
| Entry request | `RecoveryStageEntryRequestResult` provides `Succeeded`, `FailureReason` (`RunFailureReason`), and `Request`; use Request only on success. |
| Activation / completion | `RunCommandResult` provides `Succeeded`, `FailureReason`, `AlreadyApplied`, and `CurrentRun`. CurrentRun can be null when disposed, when the current combat query is unreadable, or when an invalid dead recovery checkpoint cannot form a valid snapshot. Do not substitute stale HP. |
| Selection | `RecoveryCommandResult` provides `Succeeded`, `FailureReason` (`RecoveryFailureReason`), `AlreadyApplied`, and `CurrentRecovery`. The snapshot can be null when no active recovery exists or after disposal. |
| Content/selection rejection | `UnknownOption`, `NoEffect`, `InsufficientFunds`, `AlreadyResolved`: render the returned availability without local fallback effects or charges. Zero cost does not bypass no-effect or lifetime checks. |
| Identity/lifetime rejection | Handle `RunMismatch`, `EntryMismatch`, `StaleEntry`, `WrongPhase`, and `Disposed` as stale/invalid input; refresh or invalidate the originating port. Do not retarget the same command to current IDs. |
| Run validation rejection | `StageKindMismatch`, `InvalidRecoverySpec`, map failures, `InvalidStage`, or `InvalidMainCharacter`: keep Core state and report the setup/state problem; do not create fallback combat content. Completion uses `StaleEntry` for an empty/wrong current Entry. |
| Overflow | `RevisionOverflow` is a no-change rejection. Preserve the committed state and report it; never wrap/reset Revision or perform a manual refund. |

Unexpected configuration/invariant exceptions are not ordinary affordability failures. Use the Host's fault path rather than suppressing them and pretending the operation succeeded. A duplicate completion during an unreadable current combat can return `InvalidStage`, because its result must report the current Run rather than cached HP.

## 7. Verification evidence and receiving-owner acceptance

The latest T6 verification executed **76 passing NUnit cases**: `ProjectL.Tests.Core.RunRecoveryTests` (44) and `ProjectL.Tests.Core.MainCharacterRunProgressionTests` (32). Contracts, Domain, and Application compiled as separate `netstandard2.1` assemblies with their actual names/access boundaries; tests ran under .NET 9 with the installed Unity NUnit library. This was **not a Unity EditMode pass or full-project regression run**. The optional local evidence in `.utmp/T6Verification/` is ignored and is not required repository test infrastructure.

That coverage includes real Combat → recovery → completion → Combat HP handoff, purchase-free completion with no available options, completion failure/retry without repurchase, active/completed duplicate semantics, sequential recovery lifetime, and existing Run combat behavior. T5 already covered payment/healing calculations and atomic failures. Do not duplicate trivial constructor/property tests in the integration work.

D1/D2 previously stopped Unity compilation. The current source still contains the old constructor/map-list callers. Unity/SO/scene behavior remains unverified. No tests were rerun merely to author this document.

Minimum receiving-owner checks after migration:

1. Compile Unity and the affected test assemblies. Run the existing recovery/Run tests in Unity and the affected map/catalog/composition tests: `StageMapGeneratorTests`, `StageMapRunStateTests`, `StageMapSessionTests`, `CombatViewBoundary.StageMapViewBoundaryTests`, and `StageComposition` catalog/layout/highlight/result cases. Reuse the fixed map signatures and migration tests; broaden only for additional changed boundaries.
2. With real assets, confirm Combat routing remains intact, explicit Recovery bindings route to recovery, Shop bindings cannot enter recovery, map candidate duplicates remain weighted, and a NonCombat boss is rejected. Validate authored three-choice inputs and catalog/spec freezing against later source edits.
3. Drive an injured Main through actual Host/screen selection, one purchase, completion, and the next Combat. Compare displayed and actual current/max HP, Run balance, receipt amounts, and map progression. Confirm no recovery battlefield, Draw, or Stage Gold activity occurs.
4. Enter with insufficient funds and with full HP. All choices may be disabled, but the agreed player-accessible completion action must still return to map without payment/healing. This is the remaining player-facing SL-1 acceptance gate.
5. Exercise rapid repeated selection, different-choice attempts after resolution, repeated completion before/during another stage, and late selection after completion. Verify no duplicate payment/healing or stale receipt replay beyond the latest completion.
6. Exercise pending cancellation, screen preparation failure, post-commit publication failure, recovery screen departure/reconnection, Run teardown, and retained ports after a fresh Run. Preserve committed state and reject stale commands; do not reset a live recovery just to rebuild its screen.
7. Run the Draw/Gold handoff's integration acceptance for its changed consumers. Coordinate its Core Task 9 access restriction only after those consumers have migrated.

Record Unity results separately from the Core evidence above. Recovery Core completion does not establish asset correctness, Host lifetime correctness, or playable screen completion.
