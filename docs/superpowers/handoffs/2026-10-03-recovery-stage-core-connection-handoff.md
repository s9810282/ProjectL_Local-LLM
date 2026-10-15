# Recovery Stage Core Connection — Final Handoff

Date: 2026-10-03  
Base: `e55348ff`, branch `feature/Cheon/refactor/core-system`  
Status: P1–P3 implemented, verified and reviewed as an uncommitted Core package. The final review finding is fixed with RED → GREEN evidence. Core gate C is complete; authored DB and View/Flow activation remain external. No commit or push was made.

## Consumer API and ownership

Resolve the existing InGame Context, then use `inGame.CoreGroup.Recovery`:

```csharp
ChapterProgressionResult EnterRecovery(int chapterKey, StageNodeId targetNodeId);
bool Recover(int chapterKey, StageNodeId nodeId, RecoveryOptionId optionId);
ChapterProgressionResult CompleteRecovery(int chapterKey, StageNodeId nodeId);
```

Factory exports one `IRecoveryStageService` instance as a Core child service/module, with the same registered Combat and progression dependencies. The root does not export Recovery, Combat or Core child keys. Both InitData → PostBindService and PostBindService → InitData are supported before execution; missing either fails by `InvalidOperationException`. Chapter/map/graph, roster and currency initialization remain the caller's existing prerequisites.

| Operation | Accepted behavior | Rejection/fault meaning |
| --- | --- | --- |
| `EnterRecovery` | Resolves the explicit legal target, reads ordered DB options, validates/detaches definitions, repeats live preflight and publishes selection/options together. DB root and registered target must both be non-boss recovery stages. HP, currency and clear flags are unchanged. | Illegal navigation returns the existing rejected progression result before option lookup. Missing/invalid definitions, unsupported DB or registered graph type/role, missing dependencies or any retained battle throw. Changed origin/target rejects without recovery-owned publication. |
| `Recover` | Validates the current chapter/node/key, unresolved map/node, non-boss Event root and installed Recovery before and after root lookup; delegates to existing `Combat.Recover`. `true` means newly applied. | `false` means the same option was already applied in this unresolved visit. Insufficient funds, different/unknown option, dead/missing main, no effect, stale identity and post-completion use throw; they are not payment-failure `false` results. |
| `CompleteRecovery` | Clears only the current node, with `ClearStage=true`; `ClearMap`, `EndAttempt`, `SelectMap` and `SelectStage` remain false. Supports applied and unapplied Recovery. | Duplicate completion returns `StageAlreadyCleared`; stale chapter/node/key or graph binding returns the corresponding mismatch/not-found result; cleared map returns `ChapterCompleted`. Unprepared/non-recovery/Boss state, retained battle and invalid definitions throw. |

Invalid scalar arguments throw `ArgumentException` subclasses. Installed options are authoritative for application; `Recover` does not reload option values. Selection changes clear the previous options/receipt through the existing Current store. Completion retains options/receipt until that change and emits no combat outcome or reward. Legacy direct `Combat.Recover(optionId)` stays compatible but does not acquire the guarded service's caller-identity checks.

## Caller sequence and failure recovery

1. Complete the existing battle settlement, then call `Combat.EndBattle()` through its normal caller contract. A terminal battle retained after settlement still blocks recovery. Recovery never forces cleanup, calls reset APIs or replays settlement.
2. Call `EnterRecovery(chapterKey, targetNodeId)`. On acceptance, display `CurrentProgressReadSink.Recovery.Options` using View-owned labels. Do not separately call `ConfirmStageEntry`, `SetRecoveryOptions` or another preparation step.
3. Retain the chapter/node identity with the UI intent and call `Recover(chapterKey, nodeId, optionId)` if an option is chosen. A duplicate `false` is an already-applied receipt, not an unsuccessful payment. A failed application does not finish the node.
4. Call `CompleteRecovery(chapterKey, nodeId)` after recovery or directly to exit without recovery. **Completion without recovery was approved by the user on 2026-10-03 and is implemented/tested.** It costs zero, heals zero and records no applied option, including full-health or unaffordable menus.
5. If recovery/payment succeeded but completion faults before its stage-clear write, retain the healed HP, paid currency and receipt while the node stays uncleared. Correct the fault and retry **only `CompleteRecovery`** for the same current identity. Do not repeat `Recover`, re-enter, reset options or roll back payment/healing. A stale identity still rejects retry. A rejected progression result also requires handling its reason before transition.
6. After accepted completion, View/Flow closes the screen and selects the next transition. Use `Combat.EnterBattle` for the next combat or `Recovery.EnterRecovery` for the next recovery. Shop keeps its existing `PrepareShop`/`Purchase` contracts; no shop redesign was added.

Core does not drive result confirmation, input clearing, Jobs/Plans/Flow, scenes or Run termination. `EndBattle` clears active battle/character links, soul field and preview response but retains Completion. Recovery preserves that Completion, the latest combat result, result/command/preview counters and View input/presentation. Successful next-battle preparation clears Completion and the old Recovery while retaining result/counter history before new processing.

## P3 bridge evidence

`CoreStageCombatCycleTests.RecoveryCycle_TransfersPersistentHealthToNextBattle` adds applied and skipped variants to the existing registered combat fixture. It uses actual command processing, real Buffers, DB artifact/recovery queries and supported injected battle definitions throughout; it does not claim default Method_Key battle preparation readiness.

The non-boss victory settles a StageCleared heal of 3: persistent HP 7 → 10, while soul-adjusted maximum is 12 and currency remains 91. This leaves observable missing HP; replaying settlement during recovery would incorrectly heal it. Before caller cleanup, entry throws and a full Buffer snapshot remains unchanged. A nonempty soul field and preview response are cleared by `EndBattle`, with Completion and history retained. Entry after cleanup installs ordered options without further healing or settlement.

| Path | After recovery/application | After completion and next battle |
| --- | --- | --- |
| Apply Full option costing 3 | HP 12, currency 88, receipt healed 2/paid 3, option 2 applied. | Only recovery node 3 joins combat node 2 as cleared; map remains uncleared. Next battle uses stage 102 with HP 12/12 and currency 88. |
| Exit without application | HP 10, currency 91, no applied option or payment/healing receipt. | Same node-only completion; next battle uses HP 10/12 and currency 91. |

Both paths preserve roster/souls/artifact, latest combat result, `LatestResultSequence`, `HandledCommandNumber`, `AnsweredPreviewNumber` and pending View commands/preview intent/presentation acknowledgement. Recovery retains Completion and installed options/receipt until next selection; next preparation clears Completion and Recovery. These are Core/service assertions, separate from actual Job/Flow/scene acceptance.

`FrameworkRuntimeCompositionTests` adds four cases through the real Buffer and InGame factories, including actual Core/View hierarchy. Both lifecycle orders propagate dependencies, resolve the same Recovery instance and complete an application/clear through shared Buffers. Missing InitData or PostBindService fails before publication. Global rendering/input queries are deliberately excluded from this focused composition fixture; existing full-root hierarchy tests still run.

Existing coverage is reused: registered victory/cleanup/preparation and retained-battle tests in `CoreStageCombatCycleTests`; multiple-chalice settlement and one-use paid healing in `CharacterBattleSettlementTests`; combat history retention in `CombatCommandPublicationTests`; P1 adapter/publication failures and P2 stale intent, duplicates and completion-only retry. P3 adds only the missing recovery bridge and InGame composition assertions, without a new test framework or production fault hook.

## Verification

Unity 6000.0.73f1 ran through the approved user-profile path with AppData access. No cache deletion or Unity upgrade was needed. Raw evidence, runner scripts and scratch files originally stored under `.superpowers/` were backed up outside the repository and removed from the working tree during user-requested cleanup on 2026-10-03. The summaries below remain the repository handoff; raw arguments, logs, XML and exit codes are in that local backup.

| Run | Passed / total | Unity exit | Meaning |
| --- | --- | --- | --- |
| `p3-baseline` | 1513/1518 | 2 | Fresh P1/P2 checkpoint baseline with five existing Economy failures. |
| `p3-cycle-composition-typed` | 40/40 | 0 | New bridge/composition cases plus existing cases in both fixtures. |
| `p3-focused` | 277/277 | 0 | All eleven intended fixtures discovered; no skips/inconclusive cases. |
| `p3-regression` | 1519/1524 | 2 | Six added cases; none missing; no changed outcomes or new failures. |
| `p3-review-red` | 0/6 | 2 | Initial and query-mutated graph type/role mismatches incorrectly publish entry; all expect an exception but receive none. |
| `p3-focused-final` | 283/283 | 0 | Review fix and all eleven intended fixtures; no skips/inconclusive cases. |
| `p3-regression-final` | 1525/1530 | 2 | Twelve cases added against fresh P3 baseline; none missing; identical baseline failures. |

The initial P3 runs with missing test namespace imports did not discover tests and are excluded from acceptance/RED evidence. The original six P3 acceptance cases validate existing P1/P2 behavior and passed once compiled; no artificial RED was introduced. Final review then identified a real entry gap, reproduced by six additional failing cases before the production validation fix.

The recorded focused command used the runner now stored in that backup:

```powershell
& '.superpowers/verification/recovery-stage-core-connection/run-tests.ps1' -RunName p3-focused-final -Filter 'ProjectL.Tests.Framework.RecoveryDefinitionAdapterTests;ProjectL.Tests.Framework.RecoveryEntryPublicationTests;ProjectL.Tests.Framework.RecoveryStageServiceTests;ProjectL.Tests.Framework.FrameworkRuntimeCompositionTests;ProjectL.Tests.Core.ChapterProgressionCommandsTests;ProjectL.Tests.Core.CharacterRecoveryRuleTests;ProjectL.Tests.Core.RecoveryStageContractTests;ProjectL.Tests.Framework.BattleEntryPublicationTests;ProjectL.Tests.Framework.CoreStageCombatCycleTests;ProjectL.Tests.Framework.CharacterBattleSettlementTests;ProjectL.Tests.Framework.CharacterRosterServiceTests'
```

Baseline/regression use the same runner with `-RunName p3-baseline` / `p3-regression-final` and no filter. The broader run was repeated after the review-driven production fix; the earlier unchanged-code run remains historical evidence. All runs target EditMode assemblies `ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary`. Final focused counts are adapter 27, recovery publication 28, recovery service 77, runtime composition 11, pure chapter 19, recovery rules 10, recovery contracts 24, battle publication 30, registered cycle 29, settlement 18 and roster 10.

Final broad counts: Core 934/934; Framework 585/590; CombatViewBoundary 6/6. **The whole suite remains failing.** The following five catalog failures all report `[CombatStageDetail] Combat stage requires Economy`. Their names, messages and stacks match this P3 baseline exactly, as recorded in `p3-comparison.json` and `p3-failures.json`:

- `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `DataBaseAssetTests.EveryStage_HasNodeVisual`
- `DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

[P1 evidence](2026-10-03-recovery-stage-core-connection-p1-verification.md) and [P2 evidence](2026-10-03-recovery-stage-core-connection-p2-verification.md) are historical checkpoint runs, not substitutes for the fresh P3 baseline or final run. P1 delivered 78 cases, P2 64 and P3 12; this package adds 154 cases against the original 1376-case baseline. P2's completion-fault retry already verifies committed healing/payment/receipt, one clear transition after correction and no duplicate application; P3 does not duplicate it.

## Changed files

Paths below are repository-relative. All new C# files/folders include Unity metadata. P3 adds tests/docs and one review-driven Buffer validation fix; other production changes belong to P1/P2.

| Area | Files |
| --- | --- |
| Core registration/context | `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/Core/InGameCoreSystemFactory.cs`; `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/InGameCoreSystemGroup.cs`; `Assets/ProjectL/Script/Systems/ServiceKeys/SystemGroupsService.cs` |
| Recovery runtime | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/RecoverySystem/RecoveryDefinitionAdapter.cs`; `RecoveryStageService.cs` in the same folder |
| Current/Buffer publication | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/BufferSystemGroup.cs`; `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Current/CurrentProgressBuffer.cs`; `RecoveryEntryPreparation.cs` in the same Current folder |
| Node completion | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/ChapterProgressionCoordinator.cs`; `CoreCaculator/Application/Chapter/ChapterProgressionCommands.cs` and `CoreCaculator/Domain/StageMap/ChapterProgressionRules.cs` under the same CoreSystem folder |
| Recovery fixtures | `Assets/Tests/EditMode/Framework/RecoveryDefinitionAdapterTests.cs`; `RecoveryEntryPublicationTests.cs`; `RecoveryStageServiceTests.cs`; `RecoveryStageOperationsTests.cs`; `RecoveryStageTestDatabase.cs` in the same Framework test folder |
| P3 composition/cycle | `Assets/Tests/EditMode/Framework/RecoveryRuntimeCompositionTests.cs`; `RecoveryStageCycleTests.cs`; partial declarations and recovery DB query support in `FrameworkRuntimeCompositionTests.cs` / `CoreStageCombatCycleTests.cs` in the same folder |
| P3 review fix | `BufferSystemGroup.cs` validates registered Event/non-boss metadata before entry installation; `RecoveryEntryPublicationTests.cs` and `RecoveryStageOperationsTests.cs` add six initial/query-mutation boundary regressions. |
| Pure tests/test doubles | `Assets/Tests/EditMode/ChapterProgressionCommandsTests.cs`; `Assets/Tests/EditMode/Framework/ArtifactEditingServiceTests.cs`, `ChapterResetSpy.cs`, `CombatPlanTests.cs` (mechanical Context additions) |
| Documentation | [Design](../specs/2026-10-03-recovery-stage-core-connection-design.md), [implementation plan](../plans/2026-10-03-recovery-stage-core-connection-implementation-plan.md), P1/P2 checkpoint reports and this handoff |

No production Job/Flow/View, Combat implementation, Framework API/assembly definition or authored DB asset changed. Incoming FMOD content is preserved; only Unity's additional lastScanTime mutation was restored after verifying it was the sole new asset change. Raw evidence, the task ledger/snapshots and the local feedback attachment copy were subsequently backed up outside the repository, verified by SHA256 and removed from the working tree at the user's request.

## DB authoring checklist and acceptance gates

DB owners must provide a registered recovery table/query and validated items with positive unique IDs, nonnegative costs and supported Flat/PercentOfMax/Full values. Every authored non-boss Event recovery root must match its chapter/key and carry nonempty ordered unique EventKeys referencing those items. Graph facts must agree with root type/role and allow the intended successor. Option counts, prices, healing amounts, visual labels and recovery placement are data/product decisions; the fixture values are not approved balance.

Catalog construction and the existing Economy failures need separate DB resolution. Production battle preparation still requires supported Method_Key definitions/query integration and Economy data; injected battle definitions establish only this service-cycle evidence.

| Gate | Status and remaining owner work |
| --- | --- |
| C — Core | Complete: implementation, fresh P1–P3 verification and final review fix delivered. Entry → apply or skip → complete and completion-only retry are callable. |
| D — DB | Pending authored-table/catalog readiness, matching roots/options/graph and data-owner values. |
| P — Product | No-purchase completion approved on 2026-10-03 and implemented/tested. Remaining UI/content choices are separate. |
| V — View/Flow | Pending caller adoption, installed-option presentation, fault/result handling and result confirmation/cleanup/transition ordering. |
| A — Activation | Pending an authored runtime/scene demonstration after external prerequisites. |

The existing serialized synchronous execution model is retained. General concurrent/reentrant writers, process/OOM failures, direct compatibility API bypass and chapter/node identity reuse after Run reset remain outside guarantees; existing query-mutation test seams are guarded by repeated validation. Direct Buffer publication remains a storage boundary, not a navigation authorization. No new Buffer state owner, session, revision or generic transaction was added. Next-chapter/final Run results, rewards, persistence and unrelated event systems remain outside this package.

## Decisions and review record

Continue in the approved feature checkout to preserve uncommitted P1/P2 changes; use PowerShell in place of POSIX runner scripts and retain evidence without commit/push. This shares checkout state and leaves evidence to manage. Keep recovery completion separate from combat outcomes to preserve old validation ordering; a small amount of identity validation remains duplicated. Correct the old graph-fixture filter to `ChapterGraphPreparationTests`, because the filename is not the runtime fixture name. Use the existing cycle fixture and real Buffer/InGame composition for P3 instead of introducing another harness; unrelated Global dependencies are excluded only from the focused composition fixture. These decisions do not establish DB readiness or production scene acceptance.

The fresh read-only final reviewer inspected the actual tracked/untracked package and found one Important issue, with no Critical/Minor issues: entry could accept a DB recovery root whose registered graph metadata was Combat or Boss, leaving an installed state that application/completion rejected. Six initial, query-mutated and direct-publication cases reproduced this as `p3-review-red` 0/6. The existing canonical Buffer predicate now checks Event/non-boss metadata before installation. All six cases pass inside `p3-focused-final` 283/283, and final broad comparison adds no failures. This was one fix pass; no second review was dispatched. Reviewer source inspection is separate from the parent's Unity execution evidence.

The reviewer set aside external DB/catalog/Method_Key readiness; actual View/Flow/Job/scene activation; synchronous-model exclusions and direct compatibility bypass; Run-reset identity reuse; balance/option counts/placement; next-chapter/Run/rewards/persistence; and temporary status finalization. These remain excluded for the ownership/scope reasons above. The cost of broadening those claims without owner evidence would be misleading production/concurrency readiness; none is claimed. The P1/P2 checkpoint boundary was preserved until P3 authorization, preventing entry-only delivery from being presented as the whole feature.

Structural audit passed: `git diff --check`; eleven unique new metadata GUIDs; Korean new comments; five English package documents and 21 resolving local Markdown links; no Core Framework/Unity imports or changed assembly definitions; no production View/Flow/Job/asset changes. Incoming FMOD bytes match saved SHA256 `34E829A9CD73909C5CAB98004238244E38E196CF1496759283DEBBE0207104F8`, and no Unity process remains. The fixture/filter decisions above risk incomplete evidence if misapplied; all intended fixtures were explicitly discovered and the broad run independently checked case identities.
