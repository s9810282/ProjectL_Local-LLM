# Chapter Continuation and Final Run Completion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` for sequential implementation, or `superpowers:subagent-driven-development` if the user selects delegation. Steps use checkboxes. This document does not start implementation or select delegation.

**Goal:** Continue from a genuinely cleared boss chapter to its configured successor, or commit final Run victory, preserving owned facts and enforcing terminal gameplay admission.

**Architecture:** Extend the existing chapter coordinator and inject read-only continuation definitions. Store terminal outcome/receipt in one minimal Buffer; publish graph/selection only after preparation and live validation succeed. Reuse combat completion, reset and development targeting.

**Tech Stack:** C#, Unity 6000.0.73f1, existing Framework composition and NUnit EditMode tests; no new dependency or assembly.

**Spec:** [Chapter Continuation and Final Run Completion — Core Design](../specs/2026-10-03-chapter-continuation-run-completion-design.md).

**Status:** P1–P3 implemented and verified, 2026-10-04; P3 independent review complete. See [P1 verification](../handoffs/2026-10-03-chapter-continuation-run-completion-p1-verification.md), [P2 verification](../handoffs/2026-10-03-chapter-continuation-run-completion-p2-verification.md) and the [current consumer handoff](../handoffs/2026-10-03-chapter-continuation-run-completion-handoff.md). No commit or push; incoming recovery/shop work is preserved.

## Global constraints

- Implementation documentation is English; new code comments are Korean.
- Follow the six Framework references in the spec. Systems orchestrates, pure Core commands decide, Buffers own mutable facts, Factory builds once.
- Reuse RunOutcome. Add no Core-owned graph/session, Run revision, generic transaction or duplicate progression service.
- Keep existing Create names unique. Preserve both current temporary scenarios and explicitly inject temporary sources under `UNITY_EDITOR || DEVELOPMENT_BUILD`. Production composition never falls back to them.
- Real DB replacement removes temporary definitions/scenario tests, not permanent Core APIs and service tests.
- View/Flow owns presentation, acknowledgement, EndBattle timing and pending-input cleanup. No production Job/Flow, scene, reward, save/restart-policy or importer work.
- Continuation never initializes roster/wallet/Artifact board or calls ResetRunProgress. Explicit reset preserves existing Artifact ownership and map registrations.
- Operations are synchronous/serialized. Rejected operations add no own writes; query-fixture mutations are not rolled back. Cross-Run stale identity after explicit reset remains outside this contract.
- Keep raw XML/logs/launchers/ledgers outside the repository. Preserve incoming uncommitted work. Do not commit or push without explicit instruction.

## Review focus

1. Final external lookup changes selection, battle, Run outcome or either graph: no continuation graph/selection/receipt installs. P2.4/P2.5.
2. Missing/invalid terminal data: never infer victory or a numeric successor. P2.1/P2.4.
3. Legacy APIs bypass RunEnded/route admission or change boolean meanings: guard every listed path. P1.4/P2.5.
4. Defeat reaches command-driven or explicit completion: both publish outcome atomically. P1.3.
5. Failed preparation/retry leaves a graph behind or repeats initialization/settlement: preserve committed source facts and retry only continuation. P2.3/P3.2.

## File map

Aliases are exact repository-relative prefixes:

| Alias | Prefix |
| --- | --- |
| `Core/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/` |
| `Calc/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` |
| `Buffers/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/` |
| `Factory/` | `Assets/ProjectL/Script/Systems/Factory/Runtime/` |
| `Keys/` | `Assets/ProjectL/Script/Systems/ServiceKeys/` |
| `Tests/` | `Assets/Tests/EditMode/Framework/` |

| Phase | Create / modify | Responsibility |
| --- | --- | --- |
| P1 | Create `Calc/Contracts/Results/RunCompletionReceipt.cs`, `Buffers/Run/RunProgressBuffer.cs` | Immutable terminal identity and minimal read/store interfaces/storage. |
| P1–P2 | Modify `Buffers/Core/BufferSystemGroup.cs`, `Core/ChapterProgressionCoordinator.cs` | Terminal publication/reset/guards/query; later continuation. |
| P1 | Modify `Factory/Buffer/BufferSystemFactory.cs`, `Keys/DataBaseGroupServiceKey.cs` | Register Run store and internal Read/Store keys. |
| P1 | Modify `Core/CombatSystem/CombatProcessingService.cs`, `Core/CharacterSystem/CharacterRosterService.cs`, `Core/ShopSystem/CharacterShopService.cs`, `Core/RecoverySystem/RecoveryStageService.cs` | Terminal admission, including late-query guards. |
| P1–P2 | Modify `Core/StageSystem/StageGeneratorSystem.cs` | Terminal Generate guard; later detached preparation. |
| P1–P2 | Modify `Calc/Contracts/Results/ChapterProgressionResult.cs`, `Calc/Contracts/Results/ShopFailureReason.cs`, `Calc/Contracts/Roster/RosterAcquireFailureReason.cs` | Append RunEnded; P2 also adds chapter ContinuationRejected. Do not renumber. |
| P1 | Create `Tests/RunProgressBufferTests.cs`, `Tests/RunTerminationAdmissionTests.cs`; extend `Tests/CombatCommandPublicationTests.cs`, `Tests/CombatCommonPublicationTests.cs`, `Tests/MapProgressBufferTests.cs`, `Tests/CharacterBattleSettlementTests.cs`, `Tests/CharacterBattlePreparationTests.cs`, `Tests/CoreStageCombatCycleTests.cs`, `Tests/ChapterGraphPreparationTests.cs` | Storage, defeat, reset and admission evidence; migrate only the compatibility expectations described in P1.3/P1.4. |
| P1–P2 | Modify `Tests/ChapterResetSpy.cs`, `Tests/ArtifactEditingServiceTests.cs`, `Tests/CombatCommandPublicationTests.cs` | Mechanical forwarding of new query/coordinator members; search all implementers. |
| P2 | Create `Calc/Contracts/StageMap/ChapterContinuationDefinition.cs`, `Calc/Contracts/StageMap/ChapterContinuationFacts.cs`, `Calc/Contracts/Results/ChapterContinuationResult.cs` | Pure contracts below. |
| P2 | Create `Calc/Application/Chapter/ChapterContinuationCommands.cs`, `Calc/Domain/StageMap/ChapterContinuationRules.cs` | Pure route/final decision. |
| P2 | Create `Core/StageSystem/IChapterContinuationSource.cs`, `Core/StageSystem/UnavailableChapterContinuationSource.cs`; modify `Core/StageSystem/StageServiceManager.cs` | Injected route lookup/default and internal root-definition access. |
| P2 | Modify `Buffers/Stage/StageProgressBuffer.cs`, `Buffers/Current/CurrentProgressBuffer.cs`; create `Buffers/Current/ChapterContinuationPreparation.cs` | Detached registration/selection and narrow publication DTO. |
| P2 | Modify `Factory/InGame/Core/InGameCoreSystemFactory.cs`, `Factory/InGame/InGameSystemFactory.cs` | Named source injection. |
| P2 | Create `Assets/Tests/EditMode/ChapterContinuationCommandsTests.cs`, `Tests/ChapterContinuationServiceTests.cs`, `Tests/ChapterContinuationTestDatabase.cs`, `Tests/ChapterContinuationPublicationTests.cs` | Decisions, real composition and query-fault seams. |
| P2 | Extend `Tests/ChapterGraphPreparationTests.cs`, `Tests/ChapterGraphGenerationTests.cs`, `Tests/FrameworkRuntimeCompositionTests.cs` | Explicit route fixtures, graph equivalence and initialization orders. |
| P3 | Modify `Factory/Development/TemporaryCoreRuntimeFactory.cs`, `Factory/Development/TemporaryCoreDatabase.cs`; create `Factory/Development/TemporaryChapterContinuationSource.cs` | Additional disposable two-chapter scenario. |
| P3 | Create `Tests/TemporaryCoreChapterContinuationTests.cs` | Real multi-chapter commands and retention. |
| P3 | Create `docs/superpowers/handoffs/2026-10-03-chapter-continuation-run-completion-handoff.md`; modify `docs/superpowers/handoffs/2026-10-03-temporary-core-runtime.md` | Actual evidence, consumer and replacement steps. |

Create unique `.meta` files for new Assets files/directories. No asmdef changes are intended. Store helpers stay beside existing stores; no unrelated moves/refactoring.

## Verification procedure

Capture a fresh broad baseline before coding and check no Unity Editor owns the checkout. Use the approved AppData-capable execution path; the known sandbox CurlRequestCache error does not justify cache deletion or a Unity upgrade. Keep evidence outside the repo and record arguments/exit/XML for each run:

```powershell
$chapterEvidence = Join-Path ([IO.Path]::GetTempPath()) ('ProjectL-chapter-run-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $chapterEvidence | Out-Null
$chapterRunName = 'baseline'
$chapterFilter = ''
$chapterArgs = @('-batchmode', '-nographics', '-projectPath', 'D:/My_ProjectWork/Github_desktop/Project_L',
    '-runTests', '-testPlatform', 'EditMode',
    '-assemblyNames', 'ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary',
    '-testResults', (Join-Path $chapterEvidence ($chapterRunName + '.xml')),
    '-logFile', (Join-Path $chapterEvidence ($chapterRunName + '.log')))
if ($chapterFilter) { $chapterArgs += @('-testFilter', $chapterFilter) }
$chapterProcess = Start-Process -FilePath 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -ArgumentList $chapterArgs -WindowStyle Hidden -PassThru
```

Poll only the created process in bounded intervals. Change RunName/filter before rebuilding arguments for later runs; never overwrite baseline. Zero discovered tests, missing XML or compile/setup errors are not passing or behavioral RED evidence. Final tests use typed APIs; remove any assertion-based missing-contract bootstrap after RED.

| Phase | Filter set (semicolon-separated fully qualified fixture names) |
| --- | --- |
| P1 | `ProjectL.Tests.Framework.RunProgressBufferTests;ProjectL.Tests.Framework.RunTerminationAdmissionTests;ProjectL.Tests.Framework.CombatCommandPublicationTests;ProjectL.Tests.Framework.CombatCommonPublicationTests;ProjectL.Tests.Framework.MapProgressBufferTests;ProjectL.Tests.Framework.CharacterBattleSettlementTests;ProjectL.Tests.Framework.CharacterBattlePreparationTests;ProjectL.Tests.Framework.ChapterGraphPreparationTests;ProjectL.Tests.Framework.CoreStageCombatCycleTests;ProjectL.Tests.Framework.RecoveryStageServiceTests;ProjectL.Tests.Framework.ShopStageServiceTests;ProjectL.Tests.Framework.CharacterRosterServiceTests` |
| P2 | `ProjectL.Tests.Core.ChapterContinuationCommandsTests;ProjectL.Tests.Framework.ChapterContinuationServiceTests;ProjectL.Tests.Framework.ChapterContinuationPublicationTests;ProjectL.Tests.Framework.ChapterGraphPreparationTests;ProjectL.Tests.Framework.FrameworkRuntimeCompositionTests;ProjectL.Tests.Core.ChapterProgressionCommandsTests` plus new P1 fixtures |
| P3 | `ProjectL.Tests.Framework.TemporaryCoreChapterContinuationTests;ProjectL.Tests.Framework.TemporaryCoreRuntimeTests;ProjectL.Tests.Framework.TemporaryCoreShopRuntimeTests;ProjectL.Tests.Framework.CoreStageCombatCycleTests;ProjectL.Tests.Framework.CharacterRosterBattleCycleTests;ProjectL.Tests.Framework.BattleDefinitionAdapterTests;ProjectL.Tests.Framework.BattleEntryPublicationTests;ProjectL.Tests.Framework.CharacterBattlePreparationTests` plus new P1/P2 fixtures |

RED must fail named new assertions for missing behavior. GREEN must pass all selected cases without skips. Run broad baseline once before changes and broad regression at final delivery; repeat only after code changes/failures/concrete concerns. Historical shop P3 result was 1666/1671 with five Economy failures. Compare fresh case names/outcomes/failure messages/stacks; no new failures or missing tests. Document intentionally migrated fixture expectations for tightened cross-chapter/terminal contracts while retaining their preservation assertions.

## P1 — Authoritative Run outcome and terminal admission

**Deliverable:** One Buffer terminal fact, atomic existing defeat mapping, preserved reset/cleanup and full gameplay admission coverage. Final Victory is published by P2, not by a public arbitrary setter.

**Interfaces produced:**

```csharp
RunCompletionReceipt(RunOutcome outcome, int chapterKey, StageNodeId nodeId, StageKey stageKey);
IRunProgressBufferReadSink.Outcome : RunOutcome;
IRunProgressBufferReadSink.Completion : RunCompletionReceipt;
IRunProgressBufferStore.PrepareCompletion(RunCompletionReceipt receipt) : RunCompletionReceipt;
IRunProgressBufferStore.InstallCompletion(RunCompletionReceipt prepared) : void;
IRunProgressBufferStore.Reset() : void;
IBufferQuery.RunProgressReadSink : IRunProgressBufferReadSink;
IChapterProgressionCoordinator.Outcome : RunOutcome;
IChapterProgressionCoordinator.Completion : RunCompletionReceipt;
```

These are signature listings, not compilable declarations. Receipt properties match constructor inputs; reject InProgress/undefined outcome, nonpositive chapter or empty IDs. Store/read interfaces live in RunProgressBuffer.cs; store stays internal. Outcome is derived from Completion or InProgress, avoiding independently mutable duplicate fields. Prepare rejects any existing terminal receipt without writing; Install takes an already prepared immutable receipt. Reset clears it. Keep existing IRunProgressBufferWriteSink.ResetRunProgress in BufferSystemGroup; do not introduce a conflicting interface name. Coordinator query properties forward live Buffer values with no cache/new Core Context property.

**Admission matrix:** preserve argument/readiness validation, gate before DB queries and recheck after external lookups before writes. The two InProgress compatibility checks below also apply before a terminal receipt exists.

| Public operation | Terminal behavior |
| --- | --- |
| EnterChapter, PrepareStageEntry, ConfirmStageEntry, PlanStageResolution; Combat.EnterBattle; Shop.EnterShop/CompleteShop; Recovery.EnterRecovery/CompleteRecovery | Reject with ChapterProgressionFailureReason.RunEnded, no writes. PlanStageResolution remains read-only while InProgress; an accepted EndAttempt is a proposal, not publication. |
| ResolveStage | Terminal: RunEnded. InProgress: preserve planning rejections and legacy victory clear behavior; an accepted EndAttempt must throw InvalidOperationException identifying that complete battle settlement is required, before any writes or accepted return. Direct defeat resolution is unsupported. Legacy victory clears alone do not qualify for CompleteChapter. |
| Shop.PrepareShop and both Purchase overloads | ShopFailureReason.RunEnded through existing result shapes. |
| Characters.InitializeRoster / AcquireSub | RosterAcquireFailureReason.RunEnded; no late publication after query mutation. |
| Combat.PrepareBattle() | Terminal: InvalidOperationException identifying RunEnded. InProgress: require the committed selection to match an existing uncleared node/key in its current registered, uncleared map; otherwise reject before reading definitions or publishing. Recheck these facts and unchanged selection after definition queries. Never apply fresh-click navigation to an already committed selection. |
| Combat.ApplyHealth, ApplyPersistentMain, FinalizeTurn; Combat.Recover and Recovery.Recover | InvalidOperationException identifying RunEnded. Recover(false) retains its already-applied meaning. |
| Combat.ProcessNextCommand / AdvanceCombat | False, including when the final completion lookup observes RunEnded; do not consume input or publish a result. View decides how to discard terminal input. |
| Combat.ResolveBattle / AbandonBattle | Preserve duplicate completion false first; otherwise false when terminal, including termination during the final completion lookup; no additional completion. |
| StageGenerator.Generate | InvalidOperationException identifying RunEnded before and after external lookups. |
| Reads, PlanInitialDraw, GetTimeline/GetDrawAvailability, ProcessLatestPreview | Preserve query/preview reporting and no-battle behavior. Preview is not a gameplay commit or reset. |
| EndBattle, result acknowledgement, View Clear, Shutdown, explicit ResetRunProgress | Remain usable; no implicit Run reopening. |
| Persistent Artifact management | Existing battle-lock/ownership contract; no new terminal meta-progression lock. |

At narrow Buffer publication boundaries, terminal gameplay attempts throw before installation: both PrepareBattle overloads, CharacterChanges/SoulActionChanges/CombatCommandTransition Apply, PublishRecovery, PublishPurchase, PrepareShop and PrepareRecovery. Preserve exact duplicate CompleteBattle(false) before the terminal gate. Do not reject the already prepared InProgress -> Defeat transaction after its receipt installs. Raw owner-side setup/reset ports remain trusted infrastructure.

- [x] **P1.1: Add storage/reset tests.** `RunProgress_DefaultIsInProgress_AndTerminalReceiptIsImmutable` pins null initial receipt and invalid values. `TerminalReceipt_RejectsSecondWrite_UntilExplicitReset` preserves the first receipt on duplicate/conflicting writes. Extend `ResetRunProgress_ClearsTemporaryDataAndPreservesArtifactsAndMapRegistrations` with InProgress/null assertions and its existing Artifact/map expectations.
- [x] **P1.2: Run storage RED, then implement storage/composition.** Run the new P1 storage fixture and observe missing-storage assertions. Add receipt/store, internal RunProgressBufferKey.Read/Store in DataBaseGroupServiceKey.cs, factory registration, group binding and coordinator forwarding. Forward all new interface members in discovered test doubles. Both supported InitData/PostBind orders must resolve the same store.
- [x] **P1.3: Pin and implement atomic defeat.** `DefeatPublication_CommitsRunReceiptWithExistingSettlement` covers command transition and explicit CompleteBattle, including voluntary defeat with a living main. Assert Defeat once, matching chapter/node/key, no victory clears, unchanged wallet and settlement once. `RejectedDefeatPublication_LeavesRunInProgress` uses invalid character/field/selection/result input from existing fixtures: all stores/counters/outcome unchanged. Run RED before extending PrepareCompletion/PreparedCompletion/InstallCompletion with a prepared receipt only for Progression.EndAttempt; validate outcome Defeat. Both paths consume that same extension.

  **Pre-P1 source audit and selected contracts (historical line references, not execution evidence):**

  | Path | Current source and planned contract |
  | --- | --- |
  | Pure defeat decision | `Calc/Domain/StageMap/ChapterProgressionRules.cs:115` constructs `ChapterProgressionResult.Defeat`; the result carries EndAttempt but stores nothing. `ChapterProgressionCoordinator.PlanStageResolution` returns that proposal. Both remain read-only while InProgress. |
  | Command completion | `CombatProcessingService.Publish` (line 466), reached by ProcessNextCommand/AdvanceCombat, attaches progression to already calculated settlement, then calls `BufferSystemGroup.Apply(CombatCommandTransition)`. Add the Run receipt at its existing prepared completion boundary; do not calculate settlement again. |
  | Explicit completion | `CombatProcessingService.Complete` (line 483), reached by ResolveBattle, AbandonBattle and terminal FinalizeTurn, calculates complete settlement and calls `BufferSystemGroup.CompleteBattle`. Use the same PrepareCompletion/InstallCompletion extension as command publication. Every supported accepted defeat must publish Defeat before returning. |
  | Legacy direct resolution | `ChapterProgressionCoordinator.ResolveStage:119–125` plans then directly writes only ClearStage/ClearMap. Defeat has neither clear flag: it currently returns accepted EndAttempt **without a progress write or battle settlement**. No production caller of this wrapper was found under `Assets/ProjectL`; direct defeat callers are in ChapterGraphPreparationTests. Reject accepted EndAttempt before the wrapper's writes/return as specified in the matrix; do not add an independent Outcome setter. |
  | Owner-side storage | `CombatStateBuffer.ICombatStateBufferStore.InstallCompletion` is an internal install port; the group calls it only from InstallCompletion after preparation. Direct fixture setup and explicit reset are trusted infrastructure, not alternate supported defeat APIs. Old RunProgressSnapshot is a value contract, not a live publisher. |

  Reuse `CharacterBattleSettlementTests.VoluntaryExit_RecordsDefeatOnce`, `TurnCompletion_PublishesDeathAndOutcomeTogether_OrRejectsEverything` and `DuplicateCompletion_HealsOnceAndLeavesRosterUntouched`; add Run assertions to their existing settlement/duplicate checks. Extend `ChapterGraphPreparationTests.CombatService_TurnDeathPublishesFinalHealthAndDefeatTogether` for the FinalizeTurn service route, and `CoreStageCombatCycleTests.RegisteredResolution_MainDeathTakesPriorityOverDestroyedHeadquarters` / `RegisteredCycle_AbandonmentSettlesOnceBeforeCleanup` for ResolveBattle/AbandonBattle. Preserve duplicate CompleteBattle/ResolveBattle/AbandonBattle false and usable EndBattle. Migrate the latter cycle's old post-defeat PrepareBattle success expectation to rejection until explicit reset.

  Replace `ChapterGraphPreparationTests.StageDefeat_RequestsAttemptEndWithoutClearingProgress` with one focused legacy-path case: while InProgress, PlanStageResolution(Defeat) returns accepted EndAttempt without writes, but ResolveStage(Defeat) throws the specified exception and preserves outcome/receipt, completion, selection, persistent facts, clears and counters. Keep pure command/rule defeat coverage. Migrate `Runtime_DefeatThenExplicitNewRun_RebuildsGraphAndPreservesArtifact` to the minimal battle setup already used by that fixture's `CombatService_UsesBufferValuesAndRecordsAbandonmentBeforeCleanup`, then call AbandonBattle and EndBattle before its existing NewRun assertions. Assert Defeat before reset and InProgress afterward. A supported caller needing defeat must use complete combat settlement; no production Job changes are required.
- [x] **P1.4: Pin and implement admission.** `EndedRun_RejectsGameplayWithoutWrites` is parameterized over every matrix operation and both outcomes, with literal result/exception/false assertions and Buffer/counter snapshots. Use internal store seams to seed Victory until P2 exists. `FinalUnitLookupEndsRun_PreventsRosterOrPurchasePublication` and analogous recovery/battle query cases prove after-query guards; injected mutations remain, operation-owned changes do not. Run RED, then add service/group guards and append enum values. Generate rechecks before RegisterStages after its last query.

  **Pre-P1 InProgress compatibility preparation trace:** `CombatProcessingService.PrepareBattle:128–133` checks readiness/HasBattle, reads the committed definition and publishes. `ReadCommittedDefinition:156–161` checks only that map/selection exist. `CombatStateBuffer.EndBattle:146–150` drops the active battle while retaining Completion; `InstallBattle:106–115` clears Completion on successful preparation. `BufferSystemGroup.PrepareBattleStores:268–286` has no node/map-clear admission check. Its uncleared-node/map check at `PrepareCompletion:379–384` protects completion publication, not preparation. Therefore valid injected definitions can reach the completion-clearing install after boss cleanup; no current guard establishes the requested protection. The default DatabaseBattleInitializationSource's Method_Key exception is a separate data limitation, not this admission guarantee. This is a source trace, not a reproduced runtime result.

  Add only the service-level committed-selection check in the matrix, before source reads and again before publication/cache assignment after external lookups. Use existing Current/Stage/Map reads and leave `_definitions` unchanged on rejection. Do not call PrepareStageEntry/ConfirmStageEntry: the selected uncleared node has already been committed and must remain preparable. Reuse `CharacterBattlePreparationTests.StartPreparation_UsesBufferStateWithoutLegacyFactory` as the positive compatibility witness, adding matching registered uncleared map/node facts to its current selection-only setup through existing fixture ports; retain `CoreStageCombatCycleTests.EnterBattle_RetainedBattleRejectsWithoutReinitialization` for the earlier HasBattle guard. Add a focused cleared-selection rejection in the existing service fixture; P2.5 extends it through next/final continuation rather than creating another suite. No pending-continuation flag or Buffer is needed.
- [x] **P1.5: Verify cleanup/reporting and GREEN.** `TerminalCleanupAndQueries_DoNotResetOutcome` proves EndBattle/read/acknowledgement/View Clear retain receipt and existing counters. Duplicate settlement does not republish. Gameplay rejection does not clear pending inputs. Run full P1 filters: all pass/no skips. Record changed contracts/results; P1 alone does not deliver routing.

## P2 — Defined continuation and atomic graph/selection publication

**Deliverable:** CompleteChapter, constrained legacy entry, detached graph preparation, final Victory and retry-only continuation after committed settlement.

**Contracts produced:**

| Type / signature | Required shape |
| --- | --- |
| ChapterContinuationDefinition | Get-only ChapterKey, Kind (`NextChapter`, `FinalChapter`), NextChapterKey (zero only for Final). Static `Next(int chapterKey, int nextChapterKey)` and `Final(int chapterKey)` validate positive/distinct identity. |
| ChapterContinuationFacts | Constructor/get-only inputs in order: `RunOutcome outcome, int requestedChapterKey, int currentChapterKey, StageNodeId requestedBossNodeId, StageNodeId selectedNodeId, StageKey selectedStageKey, bool hasSelection, bool hasBattle, bool sourceMapClear, bool sourceNodeClear, bool isCombatBoss, bool matchingVictory, bool targetRegistered, bool targetClear`. Command-local values, never stored. |
| ChapterContinuationResult | Accepted, FailureReason, Kind (`None`, `NextChapterSelected`, `RunCompleted`), ChapterKey, NodeId, StageKey, NextChapterKey. Rejected Kind None/NextChapterKey zero. Static `Reject(ChapterContinuationFailureReason reason)`, `SelectNext(int chapterKey, StageNodeId nodeId, StageKey stageKey, int nextChapterKey)`, `FinishRun(int chapterKey, StageNodeId nodeId, StageKey stageKey)`. FinishRun means Victory only. |
| ChapterContinuationFailureReason | `None, RunEnded, BattleRetained, ChapterMismatch, StageNotFound, StageMismatch, ChapterNotCleared, InvalidBoss, CompletionMismatch, DefinitionUnavailable, InvalidDefinition, TargetNotRegistered, TargetAlreadyCleared`. |
| Pure entry | `ChapterContinuationCommands.Complete(ChapterContinuationFacts facts, ChapterContinuationDefinition definition) : ChapterContinuationResult`; domain `ChapterContinuationRules.Evaluate` takes the same inputs. |
| Definition source | Internal `IChapterContinuationSource.Read(int chapterKey) : ChapterContinuationDefinition`. Default UnavailableChapterContinuationSource throws NotSupportedException; no fake data/schema. |
| Consumer | `IChapterProgressionCoordinator.CompleteChapter(int chapterKey, StageNodeId bossNodeId) : ChapterContinuationResult`. |
| Named composition | `InGameSystemFactory.CreateWithSources(IBattleInitializationSource battleSource, IChapterContinuationSource continuationSource) : IExportSystemServices`; optional continuation-source parameter on internal InGameCoreSystemFactory.Create. Existing Create/CreateWithBattleSource delegate using unavailable default. |
| Detached graph | `IStageGeneratorSystem.PrepareGraph(int mapKey) : IReadOnlyList<StageProgressData>`; Generate remains prepare-and-publish. |

Enums live alongside their definition/result. Name them ChapterContinuationDefinitionKind and ChapterContinuationKind respectively. Receipt properties are Outcome, ChapterKey, NodeId and StageKey; Facts properties use PascalCase constructor names. Constructors validate numeric/enum domains but allow absent selection/current chapter so pure commands can return the specified rejection. A null definition in the pure command means DefinitionUnavailable, not terminal. DefinitionUnavailable covers absent route/default unavailable source. Malformed/mismatched definitions return InvalidDefinition; unrelated injected lookup exceptions propagate without own writes. Never interpret lookup failure as FinalChapter. Constructor collections are copied/read-only.

**Publication interfaces:**

- `IStageProgressBufferStore.PrepareRegistration(int mapKey, IReadOnlyList<StageProgressData> stages) : PreparedStageRegistration` and `InstallRegistration(PreparedStageRegistration prepared) : void`. Extract current RegisterStages validation; keep RegisterStages as wrapper.
- `ICurrentProgressBufferStore.PrepareMapSelection(int mapKey) : PreparedCurrentMap` and `InstallMapSelection(PreparedCurrentMap prepared) : void`. Preserve same-map no-op/changed-map node-shop-recovery clearing; SelectMap reuses them.
- `IChapterContinuationBufferWriteSink.PublishContinuation(ChapterContinuationPreparation preparation) : void` in BufferSystemGroup.cs; add `IBufferQuery.ChapterContinuationWriteSink`.
- ChapterContinuationPreparation constructor/get-only properties: `ChapterContinuationResult decision`, `StageSelectionData expectedSelection`, `BattleCompletionChanges expectedCompletion`, `IReadOnlyList<StageProgressData> expectedSourceStages`, `IReadOnlyList<StageProgressData> expectedTargetStages`, `IReadOnlyList<StageProgressData> generatedTargetStages`. Lists are copied. Final has empty target lists. Next reuses nonempty expected target with empty generated list, or requires a nonempty generated list when target is absent.

The group verifies InProgress, no battle, current chapter/selection, source clear and matching completion reference/content; target must still be registered/uncleared. Compare graph values including keys/IDs/depth/width/type/role/clear/edges, not read-only-wrapper reference identity. Prepare registration/selection or Victory receipt fully, then install without further queries. Publication mismatch throws before own writes; no generic snapshot/rollback framework. Before the route lookup, copy current map registration/clear values and graph value arrays for registered maps into command-local data; after the target is known, select its original captured graph. This lets a route callback mutation of the future target be detected. Keep only source/target arrays in the publication DTO; no persistent second graph or revision is added.

- [x] **P2.1: Add pure tests and run RED.** `ClearedBoss_ChoosesConfiguredChapter` pins source1/node4/key102 -> target2; `ExplicitFinal_ProducesRunCompletion` pins source2/node3/key202 with no target. Pin every rejection reason, retained terminal battle, same numeric node in another chapter, clear-only forged victory and absent/mismatched definition. Final needs no target registration; Next does. Constructor tests reject self-link/zero/undefined kind. After RED, implement pure contracts/command/rules.
- [x] **P2.2: Extract detached generation after RED.** `PrepareGraph_DoesNotRegisterUntilPublished` preserves empty target and all Buffers. `Generate_UsesSamePreparedGraphAlgorithm` preserves existing fixed-seed values and three algorithm fixtures; no extra Random or changed call order. Invalid/duplicate registration/map selection leaves state unchanged. PrepareGraph returns detached data and performs no registration; Install performs no DB query.
- [x] **P2.3: Pin publication and implement the port.** `PublishContinuation_InstallsTargetGraphAndSelectionTogether` covers missing/reused target graphs and preserves prior clears, HP/souls/gold/roster/Artifacts/results/counters. `InvalidPreparedTarget_LeavesBothGraphAndSelectionUnchanged` covers invalid edges/duplicate IDs and changed expected source/target. `PublishFinal_KeepsFinalSelectionAndInstallsOneReceipt` retains final selection. Run RED before DTO/group/store changes and mechanical query forwarding. All fallible preparation precedes installation.
- [x] **P2.4: Add service tests, then implement continuation.** Use real Factory/Buffer composition and query-controllable ChapterContinuationTestDatabase. Pin `RetainedBattle_BlocksContinuationUntilCallerEndBattle`, `ClearFlagsWithoutMatchingBossCompletion_AreRejected`, `MissingDefinition_DoesNotMeanFinal`, `ConfiguredTarget_IsTheOnlyAllowedSuccessor`, `LateLookupMutation_AddsNoContinuationWrites`. Last case mutates selection/key, graphs/clears, Run state or active/terminal battle from route/root/final graph lookup. Expose the existing StageServiceManager.GetStage(int stageKey) as internal for coordinator reads; keep its public interface unchanged. Validate root key/map/Combat/Boss and completion Victory/progression identity. Current StageServiceManager.Enter only validates definitions and is not a gameplay publication path. Read one immutable route; validate target map/root definitions and existing graph metadata before reuse, and prepare only an absent graph. Enforce matching target MapKey, valid node edges/depths, one terminal combat Boss and legal start/regular nodes; missing or inconsistent roots cannot be adopted just because a graph was pre-registered. Recheck captured/live facts after final lookup and publish query-free. Definitions contain no mutable Run facts.
- [x] **P2.5: Constrain legacy entry and pin retries.** Cross-chapter EnterChapter uses a private common continuation preparation path with expected target; reject mismatched target or Final before publication. Map failures to chapter RunEnded/ChapterMismatch when exact, otherwise appended ContinuationRejected; accepted next returns existing EnterChapter result. Preserve initial/no-map and same-chapter rules. `ContinuationFault_AfterSettlement_RetryOnlyCompleteChapter` injects route/target-query failure after boss settlement, preserves HP/gold/roster/clears, then retries only continuation: one selection/graph, no reroll/settlement/reset. Pin stale source, duplicate final and terminal bypass attempts. Update existing cross-chapter fixtures to explicit route/matching completion while retaining preservation assertions.

  Extend the boss-cleanup witness in ChapterContinuationServiceTests with `BossCleanup_CompatibilityPreparationRejected_ContinuationStillSucceeds`, parameterized for Next/Final definitions. Reuse real registered services/Buffers and existing supported combat fixture definitions to produce genuine boss victory, then call service EndBattle. While Outcome is still InProgress, attempt parameterless PrepareBattle and require rejection before any source read. Preserve the same completion reference/content, selection, node/map clears, persistent HP/max/souls/roster/wallet/Artifacts, result and command/preview/result counters, and definition cache (null after service EndBattle). Observe the private cache with test-only reflection if necessary; do not add a production accessor or fault hook. Then CompleteChapter must select the configured successor or publish final Victory respectively. No replay of victory, forced cleanup, reset or synthetic completion. Reuse the retention assertions already in `CoreStageCombatCycleTests.RegisteredCycle_VictoryCleanupAndNextPreparationPreservePersistentState`; that existing next-stage test does not cover re-preparing the same cleared boss.
- [x] **P2.6: Verify composition and GREEN.** Inject sources and test default unavailability, both InitData/PostBind orders, one coordinator/store, root lookup and unique Create reflection. Forward new ProgressionProbe members. Run full P2 plus new P1 filters; all pass/no skips. P2 completes permanent continuation APIs; temporary integrated evidence is P3.

Admission order: invalid argument/readiness; RunEnded; BattleRetained; current chapter/selection identity; source clear/boss/completion; route identity; target registration/clear; live graph/publication. Empty selection -> StageNotFound; requested node mismatch -> StageMismatch. Missing route -> DefinitionUnavailable; invalid boss metadata -> InvalidBoss, corrupted root identity -> InvalidDefinition. Existing schema-validation exceptions may propagate; none imply final success. Query-side mutations are excluded from no-own-write assertions.

## P3 — Disposable two-chapter runtime and consumer handoff

**Consumes:** P1 terminal storage/admission; P2 CompleteChapter, CreateWithSources and definitions.

**Produces:** `TemporaryCoreRuntimeFactory.CreateWithChapterContinuation() : FrameworkRuntime`, constants `SecondChapterKey = 2`, `SecondChapterFirstBattleNodeId = 2`, `SecondChapterBossNodeId = 3`, and development-only TemporaryChapterContinuationSource returning Next(1,2)/Final(2). Preserve Create()/CreateWithShop() and unique public names; extend private scenario composition without public Create overloads.

Chapter1 remains shop path 1 -> 2(101) -> 5(30) -> 4(102). Chapter2 is 1 -> 2(201) -> 3(202), depth0/1/2, map2, Regular/Boss combat roots and existing HQ/field/unit/economy settings. Register both maps through normal DB InitData. Let continuation prepare chapter2 from definitions (StageMapData depth3, width1, seed42, exactly regular201/boss202), without preselection or reinitialization. A constructor option enabling chapter2 definitions defaults off so current temporary database assumptions remain valid. Reuse TemporaryBattleInitializationSource unchanged if its adapter accepts the new valid roots; no production key fallback.

- [x] **P3.1: Write the success cycle and run RED.** `TwoChapters_KeepOwnedFacts_ThenFinishRun` creates one named runtime, resolves first battle, buys one cost5 offer, explicitly completes shop, resolves chapter1 boss, proves retained battle blocks CompleteChapter, calls EndBattle, then selects chapter2. Assert InProgress, CurrentMapKey2/no selection, wallet45, owned5, purchased ID once in chapter2 full draw order, persistent HP/max/souls/Artifact facts and old clears. Seed damage/nonzero souls and a populated real Artifact pool item through the existing trusted PublishEdit fixture seam; existing CoreStageCombatCycleTests retain placed-effect coverage. Never write fake victory/clear flags. Resolve both chapter2 battles through commands; after cleanup CompleteChapter yields one Victory receipt2/3/202 and retained final selection. Check View/result retention before caller-owned input cleanup.
- [x] **P3.2: Add integration failure witnesses.** Next/final retry reuses `ChapterContinuationServiceTests.ContinuationFault_AfterSettlement_RetryOnlyCompleteChapter` (four query sites) and `FinalContinuationFault_RetriesWithoutSettlementReplay` (two query sites), without production fault hooks; these six cases reran in P3 focused delivery. `DefeatInSecondChapter_TerminatesWithoutClearingChapter` resolves real death through existing health/completion inputs, records Defeat, retains chapter1 progress and forbids second-map clear/final Victory/continued gameplay, while cleanup remains usable. Reuse original default recovery/shop fixtures and add `NewScenario_DoesNotMutateAnotherRuntime` for independent stores. Run RED before adding named factory/source/data path.
- [x] **P3.3: Implement temporary data and verify focused GREEN.** Supply chapter2 route/map/root definitions under development guards, retaining initial main+three, wallet50 and default empty Artifact board. Tests supply populated Artifact data through existing seams. No loadout/currency/board initialization during continuation. Run P3 plus new P1/P2 filters: all pass/no skips. Reuse existing deck/recovery/shop retry/graph coverage.
- [x] **P3.4: Broad comparison and independent review.** Run all three assemblies and compare full names/outcomes/messages/stacks with fresh baseline. Record intentional fixture-contract migrations; retain tests and preservation assertions. Investigate every new failure. Review late-query guards, source injection, metadata, unchanged asmdefs, defaults and terminal legacy paths. Existing Economy failures do not become a whole-suite-green claim.
- [x] **P3.5: Handoff and delivery.** Document exact APIs/results, EndBattle/View-input ownership, retry-only continuation, terminal readback, explicit reset and cross-Run identity limit. List permanent/disposable files and replacement: real route/Method_Key adapters -> normal composition -> equivalent authored-data acceptance -> remove temporary callers/factories/scenario tests. Record actual commands/counts/evidence only after execution. Update statuses, run git diff --check, restore only generated Unity timestamp drift, and keep XML/log/launcher files out of the worktree. No commit/push without instruction.

  State explicitly that after boss settlement/EndBattle, InProgress does not permit re-preparing the cleared selected boss: consumers call/retry CompleteChapter and preserve its retained completion. Parameterless PrepareBattle remains available for a legitimately committed uncleared selection. PlanStageResolution/EndAttempt is read-only planning, not a completed defeat; direct ResolveStage(Defeat) is unsupported even while InProgress. Supported command processing, ResolveBattle, AbandonBattle and terminal FinalizeTurn publish full settlement and Run defeat together. Document preserved duplicate false semantics and caller-owned cleanup. The legacy victory-clear wrapper cannot supply the real completion required by continuation. Record the two focused witnesses and migrated expectations above without duplicating existing suites. These checks use current fixture definitions and have no new DB/pipeline prerequisite; production replacement still needs the authored Chapter_NextChapter representation/adapter and Method_Key targeting definitions/query connection.

## Completion gate

P1–P3 complete when configured continuation/final victory work through the same services/real Buffers, listed APIs cannot bypass termination, preparation failure leaves no partial continuation write, and the real two-chapter cycle plus default temporary paths pass without new broad regressions. P1 execution evidence is recorded in its linked verification document; P2 execution evidence is recorded in its linked verification document; P3 executed acceptance is recorded in the current consumer handoff: new cases 5/5, focused 314/314, broad 1840/1845 with the same five Economy failures and no changed baseline outcomes. Final independent review found no outstanding findings.

Acceptance is Core/service-level. Production DB/Method_Key, scene/Job/Flow, nondevelopment player execution, rewards and total framework migration have separate acceptance. P1–P3 now supply CompleteChapter, next-chapter selection, gameplay final Victory and the disposable integrated two-chapter runtime. Production replacement and consumer Job/Flow/scene acceptance remain separate.
