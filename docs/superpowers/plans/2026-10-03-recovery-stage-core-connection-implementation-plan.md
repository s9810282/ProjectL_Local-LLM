# Recovery Stage Core Connection Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` for sequential implementation, or `superpowers:subagent-driven-development` if the user selects delegation. Steps use checkboxes for tracking. This document does not start implementation or select an execution method on the user's behalf.

**Goal:** Provide a Framework-registered Core recovery path from explicit node intent and DB options through application and node completion.

**Architecture:** Add a small Recovery service to the existing Core group. Reuse DB queries, Current/Character/Currency/Stage stores and existing recovery calculation/publication. Add a narrow prepare-before-install entry operation and outcome-free recovery completion; retain View/Flow and authored-data ownership boundaries.

**Tech Stack:** C#, Unity 6000.0.73f1, existing Framework composition, NUnit EditMode tests; no new dependencies or assemblies.

**Spec:** [Recovery Stage Core Connection Design](../specs/2026-10-03-recovery-stage-core-connection-design.md).

**Status:** P1–P3 complete as reviewed, uncommitted checkpoints on 2026-10-03 against `e55348ff`; Core gate C complete. Final review's entry metadata finding is fixed with six RED → GREEN cases, focused 283/283 and broad 1525/1530 with the same five baseline Economy failures. See the [final handoff](../handoffs/2026-10-03-recovery-stage-core-connection-handoff.md), [P1 verification report](../handoffs/2026-10-03-recovery-stage-core-connection-p1-verification.md) and [P2 verification report](../handoffs/2026-10-03-recovery-stage-core-connection-p2-verification.md). No-purchase completion, approved by the user on 2026-10-03, is implemented and tested. Production acceptance remains separate.

## Global constraints

- Implementation documentation is English; new code comments must be Korean.
- Follow the six Framework references in design section 1 and the contract mapping in section 8.
- Keep Framework free of Systems/Core references. Preserve existing Core asmdef boundaries and folder-based namespaces.
- Runtime facts remain in Buffer. No new RunSession, revision system, generic event manager or transaction framework.
- Use existing Factory → ExportServices → Builder → group composition. No runtime registration or scene searches.
- Preserve `EnterBattle`, the old `Combat.Recover` and `PrepareBattle` compatibility contracts, shop behavior and existing public chapter progression interface.
- No production Job/Plan/Flow/LifeCycle/View changes, SO authoring, combat DB repair, reward policy or Run-ending work.
- Entry is synchronous and serialized. Failures preserve facts written by this operation; external mutations performed by injected queries are not rolled back.
- The handoff must record user approval of no-purchase completion on 2026-10-03 separately from implementation and production acceptance.

## Review focus

1. Same StageKey at different nodes must not share consumed recovery state; test in P1.
2. Query failure after one valid option must not select a target or partially install options; test in P1.
3. A terminal battle retained after settlement still blocks recovery; test in P1/P2 and extend P3.2 to prove the same services accept recovery after legitimate EndBattle cleanup without replaying settlement.
4. A delayed click/finish for a previous node must not affect the newly selected recovery node; test in P2. Also pin recovery-success/completion-failure retry in P2.3: committed recovery survives and only completion is retried.
5. Full health or insufficient funds must still permit the approved no-purchase exit, while healing remains rejected; test in P2.

## File inventory

Path aliases below are exact prefixes, relative to the repository root:

| Alias | Prefix |
| --- | --- |
| `Core/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/` |
| `Calc/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` |
| `Buffers/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/` |
| `Systems/` | `Assets/ProjectL/Script/Systems/` |
| `Tests/` | `Assets/Tests/EditMode/Framework/` |

| Action | File | Responsibility |
| --- | --- | --- |
| Create | `Core/RecoverySystem/RecoveryStageService.cs` | Public service interface plus internal orchestration implementation, following neighboring service convention. |
| Create | `Core/RecoverySystem/RecoveryDefinitionAdapter.cs` | Read and convert ordered DB recovery definitions. |
| Create | `Buffers/Current/RecoveryEntryPreparation.cs` | Immutable expected-origin/target/options publication data. |
| Modify | `Buffers/Current/CurrentProgressBuffer.cs` | Internal recovery selection preparation; reuse existing install boundary. |
| Modify | `Buffers/Core/BufferSystemGroup.cs` | Recovery entry write port, live storage checks and Current publication. |
| Modify | `Core/InGameCoreSystemGroup.cs` | Recovery Context property and child lookup. |
| Modify | `Systems/Factory/Runtime/InGame/Core/InGameCoreSystemFactory.cs` | Construct/export the Recovery service with existing Combat/progression instances. |
| Modify | `Systems/ServiceKeys/SystemGroupsService.cs` | Add `InGameSystemServiceKey.Recovery`. |
| Modify | `Core/ChapterProgressionCoordinator.cs` | Internal outcome-free recovery completion and one accepted stage write. |
| Modify | `Calc/Application/Chapter/ChapterProgressionCommands.cs` | Pure recovery completion command. |
| Modify | `Calc/Domain/StageMap/ChapterProgressionRules.cs` | Pure completion validation, sharing existing validation only where behavior stays unchanged. |
| Create | `Tests/RecoveryDefinitionAdapterTests.cs` | Conversion/invalid-definition tests. |
| Create | `Tests/RecoveryStageServiceTests.cs` | Registered-service entry, application and completion tests. |
| Create | `Tests/RecoveryStageOperationsTests.cs` | P2 application/completion tests in the same partial registered-service fixture. |
| Create | `Tests/RecoveryEntryPublicationTests.cs` | Real-store publication and preservation tests. |
| Create | `Tests/RecoveryStageTestDatabase.cs` | Explicit DB query fixture; no production fallback or duplicate runtime state. |
| Modify | `Assets/Tests/EditMode/ChapterProgressionCommandsTests.cs` | Pure recovery completion and combat regression coverage. |
| Modify | `Tests/FrameworkRuntimeCompositionTests.cs` | Real Factory/module/Context coverage. |
| Modify | Existing test implementations of `IBufferQuery` / `IInGameCoreSystemGroupService` | Mechanical new-property forwarding; identify by search before editing. |
| Create | `docs/superpowers/handoffs/2026-10-03-recovery-stage-core-connection-handoff.md` | Consumer sequence, evidence and external readiness checklist. |

Create Unity `.meta` files for new Assets files and the new folder without regenerating existing GUIDs. No DB query interface/schema change is planned. No production edit to `CombatProcessingService` or `CharacterShopService` is required.

## Verification setup for implementation

Capture a fresh baseline before production edits. Use the existing Unity test mechanism after checking Editor ownership of the checkout; do not start another Editor against a project already in use. The previous entry package's `run-tests.ps1` is a reference for the licensed launcher, not a reason to overwrite old evidence.

Save this package's XML/log/arguments/exit codes under `.superpowers/verification/recovery-stage-core-connection/`. For an available licensed Editor and an unowned checkout, the command shape is:

```powershell
$recoveryEvidence = Join-Path (Get-Location) '.superpowers/verification/recovery-stage-core-connection'
New-Item -ItemType Directory -Force -Path $recoveryEvidence | Out-Null
$recoveryArgs = @('-batchmode', '-nographics', '-projectPath', (Get-Location).Path,
    '-runTests', '-testPlatform', 'EditMode',
    '-assemblyNames', 'ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary',
    '-testResults', (Join-Path $recoveryEvidence 'baseline.xml'),
    '-logFile', (Join-Path $recoveryEvidence 'baseline.log'))
$recoveryArgs | Set-Content (Join-Path $recoveryEvidence 'baseline.args.txt')
$recoveryProcess = Start-Process -FilePath 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -ArgumentList $recoveryArgs -WindowStyle Hidden -PassThru -Wait
$recoveryProcess.ExitCode | Set-Content (Join-Path $recoveryEvidence 'baseline.exit-code.txt')
```

The path/version are grounded in this checkout and the existing launcher; recheck availability at execution. If the host needs a licensed user-profile launcher, use that existing mechanism with the same arguments. Do not treat a licensing failure, missing XML, compile error or zero-discovery run as test evidence.

For each run below, use a distinct basename instead of `baseline`, retaining the same log/args/XML/exit-code convention. Append `-testFilter` and the specified fully qualified fixture names separated by semicolons for focused runs. Run broader suites once at baseline and once after the package, repeating only for an actual change or failure investigation.

## P1: Safe recovery entry from DB

**Deliverable:** A registered `CoreGroup.Recovery.EnterRecovery` prepares and publishes the correct target and options without preselection.

**Files:** New adapter/service/publication DTO/tests/fixture; Current and Buffer group; Core Factory/Context/key; affected interface test doubles from the inventory. Add completion/application public methods in P2, so P1 does not ship throwing placeholder methods.

**Interfaces produced:**

```csharp
ChapterProgressionResult IRecoveryStageService.EnterRecovery(int chapterKey, StageNodeId targetNodeId);
IRecoveryStageService IInGameCoreSystemGroupService.Recovery { get; }
RecoveryStageSpec RecoveryDefinitionAdapter.Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey);
IRecoveryStageBufferWriteSink IBufferQuery.RecoveryStageWriteSink { get; }
void IRecoveryStageBufferWriteSink.PrepareRecovery(RecoveryEntryPreparation preparation);
PreparedCurrentSelection ICurrentProgressBufferStore.PrepareRecoverySelection(RecoveryEntryPreparation preparation);
```

Use the exact DTO properties and constructor from design section 6. `RecoveryStageService(ICombatProcessingService combat, ChapterProgressionCoordinator progression)` is constructed with the existing instances. No new source-injection interface is needed; inject explicit DB query fixtures through normal InitData.

- [x] **P1.1 — Add failing conversion tests.** `Read_PreservesAuthoredOrderAndValues` uses keys `[2, 1]`, option 2 = cost 3 / Flat 2, option 1 = cost 0 / Full 0; assert order, IDs, costs, modes, values and stage key. These numbers are test data only. Add `Read_RejectsMalformedDefinition` for missing/null root/detail/option/query, wrong returned root or option ID, chapter mismatch, Combat/Shop/Boss root, empty/duplicate keys and invalid heal/cost data. Assert DB input collections remain unchanged and returned specs do not alias mutable collections.
- [x] **P1.2 — Run RED conversion tests.** Filter `ProjectL.Tests.Framework.RecoveryDefinitionAdapterTests`, basename `p1-adapter-red`; establish missing adapter behavior, not an unrelated setup failure.
- [x] **P1.3 — Implement adapter.** Apply design section 6 with existing queries and spec constructors; no fallback definitions, SO lookup or new schema.
- [x] **P1.4 — Add failing publication/service tests.** `EnterRecovery_UnconfirmedTargetPublishesOptions` starts at chapter 1 with no stage selected, enters legal node 2/key 20 and verifies both selection and options. `EntryFault_PreservesAllFacts` snapshots Current, Shop/Recovery identity, health, currency, combat history and progress; inject a failure on the second option lookup and verify no owned mutation. Add wrong expected origin, changed live target binding, cleared map/node, and active/terminal retained-battle cases at the real Buffer boundary.
- [x] **P1.5 — Add retry and identity cases.** `RejectedNavigation_DoesNotReadOptions`, `ChangedOriginOrTarget_DoesNotPublish`, `RepeatEntry_DoesNotResetReceipt`, `DifferentNodeSameKey_GetsFreshOptions`, `FailedEntryFromShop_PreservesOffers`. Distinguish query-induced external mutation from writes by entry. A correction followed by a legal retry must succeed with the same service instance.
- [x] **P1.6 — Run RED entry tests.** Filter `ProjectL.Tests.Framework.RecoveryEntryPublicationTests;ProjectL.Tests.Framework.RecoveryStageServiceTests`, basename `p1-entry-red`.
- [x] **P1.7 — Implement publication and service.** Build detached Current replacements before install. Validate canonical graph binding and retained battle in Buffer, navigation/type/DB in Core. Revalidate after DB read. Preserve battle DTO and ports. Register Factory/service/module/key/Context together and forward new properties on test doubles discovered by `rg -n 'IBufferQuery|IInGameCoreSystemGroupService' Assets/Tests`.
- [x] **P1.8 — Run GREEN and entry regressions.** Filter the three recovery fixtures plus `ProjectL.Tests.Framework.BattleEntryPublicationTests;ProjectL.Tests.Framework.CoreStageCombatCycleTests;ProjectL.Tests.Framework.ChapterGraphPreparationTests`, basename `p1-green`. Require all discovered focused cases passing, no skips or compilation errors. Review the scoped diff and record a coherent P1 checkpoint.

## P2: Guarded application and outcome-free completion

**Deliverable:** Core consumers can apply an installed option and finish the matching recovery node, including the user-approved no-purchase path.

**Files:** Recovery service; coordinator; pure chapter commands/rules; recovery service tests and `ChapterProgressionCommandsTests`. Reuse existing settlement code without copying it.

**Interfaces consumed:** P1 entry/Context/installed Current; existing `ICombatProcessingService.Recover(RecoveryOptionId)`; existing `StageProgressWriteSink.SetClear`.

**Interfaces produced:**

```csharp
bool IRecoveryStageService.Recover(int chapterKey, StageNodeId nodeId, RecoveryOptionId optionId);
ChapterProgressionResult IRecoveryStageService.CompleteRecovery(int chapterKey, StageNodeId nodeId);
internal ChapterProgressionResult ChapterProgressionCoordinator.CompleteRecoveryStage(
    int chapterKey, StageNodeId expectedNodeId, StageKey expectedStageKey);
```

The pure `ChapterProgressionCommands.CompleteRecoveryStage` signature is defined in design section 7. Its domain operation returns a write decision only:

```csharp
ChapterProgressionResult ChapterProgressionRules.EvaluateRecoveryCompletion(
    int requestedChapterKey, int currentChapterKey, int graphChapterKey,
    StageNodeId selectedNodeId, StageKey selectedStageKey, bool isChapterClear,
    IReadOnlyList<ChapterNodeProgress> nodes);
```

The command rejects missing selection before calling the domain operation, following the existing chapter-command convention. Preserve the existing public coordinator interface and StageOutcome values.

- [x] **P2.1 — Add failing application tests through the registered service.** `Recover_PaysAndHealsOnce` uses HP 7/10, zero souls, balance 8 and Full option cost 3; assert HP 10, balance 5, receipt paid/healed 3, first call true and same-option retry false. `Recover_OtherOptionAfterConsumptionRejects` preserves all three facts. Test missing/dead main, unknown option, full HP/Flat(0), insufficient funds, stale chapter/node, cleared stage/map, unprepared state and active/terminal retained battle. Retain existing soul-adjusted rounding tests.
- [x] **P2.2 — Add failing pure and service completion tests.** `CompleteRecovery_AfterApplyClearsOnlyNode` asserts `ClearStage=true`, `ClearMap=false`, `EndAttempt=false`, unchanged HP/balance/receipt and unchanged combat results. `CompleteRecovery_WithoutPurchaseAllowsExit` uses full HP and separately balance 0 with a priced menu; assert the same progress flags and no receipt/payment/heal. Identify this test as the user-approved skip policy in its Korean comment.
- [x] **P2.3 — Add completion boundary cases.** `CompleteRecovery_DuplicateReturnsStageAlreadyCleared`, `CompleteRecovery_StaleNodeDoesNotClearCurrent`, `Recover_AfterCompletionRejects`, `CompleteRecovery_RejectsCombatShopBossOrUnprepared`, `CompleteRecovery_AllowsLegalSuccessor`. Pure tests cover missing selection, absent graph node, key mismatch and chapter/map completion rejections. No test creates a synthetic battle outcome to finish recovery.

  Add one focused `CompleteRecovery_PreWriteFailurePreservesAppliedRecoveryAndRetriesOnlyCompletion` case in the existing service fixture. Start with HP 7/10, zero souls, balance 8 and a Full option costing 3; successfully call Recover once. Make the existing fixture's stage query fail on the subsequent completion root lookup, before any stage-clear write, without changing Buffer facts. Assert HP 10, currency 5, the same applied receipt (option ID, paid 3, healed 3), installed options unchanged and the node still uncleared. Restore the query and retry only CompleteRecovery on the same service/node: assert acceptance, one uncleared-to-cleared transition, and no further HP/currency/receipt changes. A duplicate completion remains rejected. Do not call Recover again, restart entry or reinstall options; do not add production fault hooks, rollback or another transaction/session. The existing duplicate-Recover test separately preserves `false` as already applied, not payment failure.
- [x] **P2.4 — Run RED.** Filter `ProjectL.Tests.Framework.RecoveryStageServiceTests;ProjectL.Tests.Core.ChapterProgressionCommandsTests`, basename `p2-red`; require discovery in both assemblies.
- [x] **P2.5 — Implement guarded application.** Validate current chapter/node/registered key, non-boss Event identity, prepared Recovery and unresolved phase, then delegate to existing Combat recovery. Do not reread DB option values, change legacy bool semantics or catch all errors as false.
- [x] **P2.6 — Implement completion.** Add the internal coordinator operation and pure command/rules per design section 7. Verify expected identity again at the coordinator boundary. After acceptance write only the node clear flag. Applied and no-purchase completion use the same API; duplicate completion returns the existing rejection and keeps the receipt.
- [x] **P2.7 — Run GREEN.** Use the P2 fixtures plus `ProjectL.Tests.Framework.CharacterBattleSettlementTests;ProjectL.Tests.Core.CharacterRecoveryRuleTests;ProjectL.Tests.Core.RecoveryStageContractTests`, basename `p2-green`. Require every focused test to pass and unchanged combat progression behavior. Review the scoped diff and record a coherent P2 checkpoint.

## P3: Composition, lifecycle and integration evidence

**Deliverable:** Evidence for Core gate C and a precise consumer handoff, with D/P/V/A tracked separately.

**Files:** Framework composition tests, recovery service fixture, affected test doubles, final handoff. Production changes only if a test identifies an in-scope defect.

**Interfaces consumed:** Final `CoreGroup.Recovery` APIs, existing Buffer read ports and `Combat.EnterBattle` with the supported injected definition source used in existing tests.

- [x] **P3.1 — Add registered composition coverage.** `Factory_ExportsRecoveryAndPropagatesDependencies` retrieves Recovery through the real InGame/Core Context and confirms one shared service instance. Test InitData before PostBindService and the reverse before execution; missing either must fail clearly. Build/module validation and existing root hierarchy remain intact. Update framework fixtures to provide a RecoveryOption query without inventing authored assets.
- [x] **P3.2 — Extend `RecoveryCycle_TransfersPersistentHealthToNextBattle`.** Using the same registered services and real Buffers throughout, exercise a legal non-boss battle → settlement → caller-owned `Combat.EndBattle()` → EnterRecovery → Recover → CompleteRecovery → next battle with supported fixture definitions. Before EndBattle, attempt recovery entry and assert that the retained terminal battle blocks it with no mutation. Then clean up through the existing service contract and retry recovery entry successfully; the recovery service must never call EndBattle/reset APIs to force entry or repeat settlement effects. Reuse the existing registered-cycle setup and settlement assertions; choose a nonzero settlement-heal effect that still leaves missing HP, making replay observable.

  Capture facts after settlement and compare them after blocked entry, cleanup, recovery entry, application, completion and next preparation. Assert persistent HP/progress survive cleanup, currency changes only by the recovery cost, recovery completion clears only its node, and healed HP enters the next battle. Preserve the latest combat result and `LatestResultSequence`, `HandledCommandNumber`, `AnsweredPreviewNumber` until new combat processing. Assert the existing lifetime differences: EndBattle clears the active battle/soul field/preview response but retains Completion; recovery preserves that retained completion; next battle preparation clears it. Recovery must not reset View-owned input or presentation. Recovery receipt/options clear only under the existing next-selection semantics. Retain the approved skip route with HP/currency unchanged from their post-settlement values. This proves a Core/service cycle, not production Job/Flow/scene acceptance or default Method_Key readiness; no new test framework is needed.

  Reuse source coverage already present in `CoreStageCombatCycleTests.RegisteredCycle_VictoryCleanupAndNextPreparationPreservePersistentState` and `EnterBattle_RetainedBattleRejectsWithoutReinitialization`, `CharacterBattleSettlementTests.MultipleChalices_SettleOnceAndPersistHealedHealthAcrossCleanupAndNextPreparation` and `Recovery_IsOneUseAndPaysAndHealsTogether`, and `CombatCommandPublicationTests.ResultNumbersAndLastResult_SurviveEndBattleAndNextPreparation`. These cover constituent contracts, not the intervening recovery bridge or the P2 completion-fault retry. Keep them as existing regression coverage and add only the missing sequence assertions in the planned fixture; source inspection is not a new passing test result.
- [x] **P3.3 — Run focused GREEN.** Use basename `p3-focused` and filter `ProjectL.Tests.Framework.RecoveryDefinitionAdapterTests;ProjectL.Tests.Framework.RecoveryEntryPublicationTests;ProjectL.Tests.Framework.RecoveryStageServiceTests;ProjectL.Tests.Framework.FrameworkRuntimeCompositionTests;ProjectL.Tests.Core.ChapterProgressionCommandsTests;ProjectL.Tests.Core.CharacterRecoveryRuleTests;ProjectL.Tests.Core.RecoveryStageContractTests;ProjectL.Tests.Framework.BattleEntryPublicationTests;ProjectL.Tests.Framework.CoreStageCombatCycleTests;ProjectL.Tests.Framework.CharacterBattleSettlementTests;ProjectL.Tests.Framework.CharacterRosterServiceTests`. Require nonzero discovery for every selected fixture and all focused cases passing.
- [x] **P3.4 — Run one broader regression.** Run the three assemblies from verification setup with no filter, basename `p3-regression`. Compare against the fresh baseline by case identities, outcomes and failure details. Require no missing cases or new failures. If known catalog failures remain, report their names/messages and keep the whole suite marked failing; do not call it all-green or fix DB assets as a side task.
- [x] **P3.5 — Audit structure and changes.** Run `git diff --check`, inspect untracked source/docs too, verify unique new meta GUIDs, Korean new code comments, English implementation docs, no new Framework/Unity dependency in Core, and no production View/Flow/Job/asset changes. Preserve unrelated incoming changes. Verify each new Markdown link resolves.
- [x] **P3.6 — Write the handoff.** Include the exact API signatures and return/exception meanings; caller-owned settlement/EndBattle precondition and the completion-only retry sequence below; DB authoring checklist; confirmed skip policy and approval date; test commands/results; actual changed files; reused vs newly added coverage; historical vs new evidence; and separate C/D/P/V/A statuses. User policy approval does not establish implementation or production acceptance. Review the final scoped package and record its completion evidence before delivery.

## Consumer sequence for the handoff

1. After a battle, its existing caller completes settlement and legitimate cleanup through `Combat.EndBattle()` before supplying chapter/node intent to `CoreGroup.Recovery.EnterRecovery`. A retained terminal battle still rejects entry; recovery never performs cleanup or settlement on the caller's behalf.
2. On acceptance, read `CurrentProgressReadSink.Recovery.Options` and display them using View-owned definitions. Do not call `ConfirmStageEntry` or `SetRecoveryOptions` afterward.
3. Apply an option using `Recover(chapterKey, nodeId, optionId)`. `false` means the same option was already applied, not that payment failed; retain the selected node identity with the UI request.
4. Invoke `CompleteRecovery(chapterKey, nodeId)` after an applied option, or without application under the approved skip policy. A failed application does not automatically finish the stage. If application/payment succeeded but completion fails before the clear write, keep healed HP, paid currency and the applied receipt, and leave the node uncleared. Correct the failure and retry only CompleteRecovery for the same current identity; do not repeat Recover, re-enter, reset options or roll back payment/healing. A stale identity still rejects the retry.
5. On accepted completion, View/Flow performs its own closing/transition behavior. Core has cleared the node but has not activated another scene, phase or Run result.
6. For the next combat use `Combat.EnterBattle`; for the next recovery use the new entry operation. Shop's existing API and orchestration remain outside this implementation.

## Completion definition

P1–P3 complete only when Core gate C has evidence and the handoff accurately records outstanding external gates. Do not repeatedly run unchanged broad suites to substitute for DB readiness or View/Flow adoption. This plan ends at a reviewable Core implementation and handoff; authored gameplay activation remains a separate integration result.
