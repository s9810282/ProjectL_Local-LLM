# Recovery RC2–RC4 Core runtime handoff

Prepared on 2026-10-14 (Asia/Seoul). Delivery is through the user's Git integration. This document is not an external message or proof of View runtime acceptance.

## 1 Delivery status and scope

| Checkpoint | Evidence-backed status |
|---|---|
| RC2 state reader and typed operations | Implemented; plan §11–12 |
| RC3 numbered recovery processing and result retention | Implemented; plan §14 |
| RC3 existing Stage endpoint recovery entry | Implemented locally; plan §17 |
| RC4 integrated acceptance | Core recovery boundary accepted: focused 16/16 and full Core 2,444/2,444 passed; wider Framework has 92 unresolved failures (section 6) |
| User merge of final RC3-B/RC4 changes into View branch | Not recorded; current work remains uncommitted |
| View signature acceptance | Existing RC0/RC1/RC2 source acceptance and V4 delivery are recorded in the linked View documents; final RC3-B/RC4 delivery acceptance is not recorded |
| View V5–V8 runtime integration / recovery popup Play acceptance | Not established by this Core acceptance |

Baseline HEAD is `632498c274f75c07032e552d6d92e810d2d97133`, including merged ER1 `662da3dd`. Phase 4 changes were already present locally when Phase 5 began. Phase 5 adds acceptance tests/metadata and documentation only; it adds no production rules, View code, scene or DB changes.

Sources of policy: runtime design (Git history: `2026-10-12-recovery-rc2-rc4-runtime-integration-design.md`), execution plan (Git history: `2026-10-12-recovery-rc2-rc4-runtime-integration-plan.md`), [RC0/RC1 policy](2026-10-11-recovery-core-code-audit-and-rc0-rc1-proposal.md), [RC1 manifest](2026-10-11-recovery-rc1-view-handoff.md), [View contract acceptance](2026-10-09-recovery-core-handoff-Hojin.md), and [V4 delivery](2026-10-09-recovery-core-handoff-Hojin.md). Historical incomplete statuses in those documents are dated evidence; current Core execution is recorded here and in plan §18.

## 2 Public integration contract

Resolve the existing Core group and Buffer query through normal Framework lifecycle/assembly. `InitData`, `PostBindService` and initialization must complete before use. Runtime processing is synchronous on its owning thread; nested/parallel execution is rejected.

| Purpose | Existing public access / signature |
|---|---|
| Stage click input | `buffers.StageViewWriteSink.Submit(StageViewCommandData.SelectStageNode(chapterKey, nodeId))` returns the request number |
| Stage entry processing | `core.StageCommands.ProcessNextNodeCommand(int expectedRequestNumber, Func<int> nextSeed)` |
| Stage entry output | `buffers.StageCommandResults.TryGetMapResult(out StageEntryCommandResult result)`; match RequestNumber, ChapterKey, NodeId and Kind |
| Current recovery display | `core.RecoveryState.TryGetCurrent(out RecoveryStateSnapshot state)` |
| Select/Complete input | `buffers.RecoveryViewWriteSink.Submit(RecoveryCommandData.Select(context, optionId))` or `.Submit(RecoveryCommandData.Complete(context))` |
| Recovery processing | `core.RecoveryCommands.ProcessNextRecoveryCommand(int expectedNumber)` |
| Recovery output | `buffers.RecoveryResultReadSink.TryGetLatest(out RecoveryProcessedResult result)` and `.HandledCommandNumber` |
| Acknowledgement/removal | `buffers.RecoveryViewWriteSink.AcknowledgeResult(number)` followed by `.RemoveCommand(number)` after actual result correlation/consumption |
| Direct typed compatibility path | `core.Recovery.SelectRecovery(RecoveryEntryContext, RecoveryOptionId)` / `.CompleteRecovery(RecoveryEntryContext)` return `RecoveryOperationResult` without numbered input/output |
| Legacy compatibility | Existing `EnterRecovery(int, StageNodeId)`, `Recover(int, StageNodeId, RecoveryOptionId)`, `CompleteRecovery(int, StageNodeId)` and Combat recovery API remain |

The Stage endpoint routes non-boss Event nodes to the shared recovery entry preparation. `RecoveryDefinitionAdapter` validates `EventStageDetail` and authored option definitions. Support is not hard-coded to stage 1301 and does not implement arbitrary non-recovery events. A recovery entry uses Kind=Node and Seed=0; the seed supplier must be non-null but is never invoked for that route. Shop and Battle keep their existing paths. Do not call `ConfirmStageEntry` first.

An accepted map result and the recovery selection/options/context are installed together. A normally rejected map result has no selection mutation. After accepted entry, read the real `state.Context`; never synthesize an EntryId or reuse a context from an earlier entry. Normal false from `TryGetCurrent` means no active recovery entry; lifecycle or inconsistent source data can throw.

For recovery processing, `true` means an outcome was published, including `TradeRejected` or `StateMismatch`. Read `result.Result` to determine success. `false` means no new transaction/output (missing/cancelled/already-handled input or older output awaiting acknowledgement); it is not permission to drop a still-pending blocked request. Core never removes recovery input.

Match `Number` and the full echoed `Command`: Kind, Context (EntryId, ChapterKey, NodeId, StageKey), and OptionId. Only then acknowledge the actual published number and remove that request. Number gaps are legal. A same-option retry before completion returns Accepted/AlreadyApplied without charging again; another option returns AlreadyResolved. Complete may skip healing and clears only the selected node. A new Complete request after completion returns StaleEntry.

## 3 Lifetime and View responsibilities

- Keep a request/result owner across Hide, Stage reattach and new Run. `Clear` cancels input but preserves issuance/acknowledgement and retained Core output. Clearing is not acknowledgement. An old result must still be correlated/consumed and acknowledged even when it must not be drawn on the new popup. New processing waits until that happens.
- Read current HP/max HP, gold, completion and option availability again on attach/refresh. Core includes current souls in its projection; the snapshot does not expose a raw soul count. Installed option rules and historical snapshots/receipts remain immutable. View reads the projected result and does not recalculate the healing/payment rule or mutate Core buffers.
- For Stage request origin, preserve ER1 Clear-before-Run/chapter replacement and monotonic numbers. Normal Combat departure stops Stage registration, drains/validates pending work or the accepted combat transition, then clears input in View Detach. Core rereads the exact Stage command after definition callbacks. Arbitrary replacement calls bypassing that lifecycle are outside the supported contract. No new global RunId or additional person's approval is required by this implementation.
- Route unexpected definition/lifecycle faults to the existing runtime fault boundary. There is no public fault injection API. The install sequence has no callback between gameplay and output assignments; immediate-after-return Clear retention is tested without inventing a postcommit callback.
- View still owns V5–V8 scheduling, popup presentation, input focus/restore, acknowledgement consumption and production scene/DB acceptance. The test fixture performs the owner actions explicitly; it does not prove that the production popup already performs them.

## 4 Source manifest

Prefixes below are relative to `Assets/ProjectL/Script/Systems/` and correspond to the file map in plan §1. New `.cs` files include `.meta`; existing GUIDs are preserved. Full RC1 immutable DTO/interface signatures remain in the RC1 manifest.

| Files | Responsibility |
|---|---|
| `RuntimeSystem/InGameSystem/CoreSystem/RecoverySystem/RecoveryStateProjection.cs`, `RecoveryStateQueryService.cs` | Read authoritative facts and project current immutable display state |
| Same directory: `RecoveryStageService.cs`, `RecoveryOperationPreparation.cs` (also declares RecoveryOperationGuard), `RecoveryEntryOperationPreparation.cs` | Shared direct/numbered preparation, context, owner/scope and non-reentrancy |
| Same directory: `RecoveryCommandProcessingService.cs`, `IRecoveryCommandProcessingService.cs` | Exact-number processing, cancellation, retention/backpressure |
| `RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Run/RecoveryOptionEvaluator.cs`, `Application/Combat/CharacterRecoveryCalculator.cs` | Shared pure option classification and preserved healing rule |
| `RuntimeSystem/BufferSystems/Current/CurrentRecoveryData.cs`, `RecoveryEntryPreparation.cs`, `CurrentProgressBuffer.cs` | Authoritative installed recovery context/options/receipt |
| `RuntimeSystem/BufferSystems/Recovery/RecoveryResultBuffer.cs`, `IRecoveryResultStore.cs`, `IRecoveryResultReadSink.cs`, `IRecoveryStateReadSink.cs` | Retained result slot and public read contracts |
| `RuntimeSystem/BufferSystems/Core/IRecoveryOperationTransaction.cs`, `StageServiceTransactions.cs`, `PreparedRecoveryMutation.cs`, `PreparedRecoveryPublication.cs`, `ProgressTransactions.cs` | Prevalidate and install gameplay facts with already-built output |
| `RuntimeSystem/InGameSystem/CoreSystem/StageCommandSystem/StageCommandProcessingService.cs` | Existing Stage entry endpoint routes supported Event to shared recovery service |
| `RuntimeSystem/InGameSystem/CoreSystem/ChapterProgressionCoordinator.cs`, `CombatSystem/CombatProcessingService.cs` | Shared completion decision and legacy compatibility |
| `Factory/Runtime/InGame/Core/InGameCoreSystemFactory.cs`, `Factory/Runtime/Buffer/BufferSystemFactory.cs`, `RuntimeSystem/BufferSystems/Core/BufferSystemGroup.cs`, `RuntimeSystem/InGameSystem/CoreSystem/InGameCoreSystemGroup.cs`, `ServiceKeys/SystemGroupsService.cs` | Existing composition/access points; shared collaborators and narrow stores |
| `Assets/Tests/EditMode/Framework/RecoveryRuntimeAcceptanceTests.cs` (repository-relative) | Phase 5 public-flow acceptance, real Core/Buffer factories and ER1 StagePlan jobs |

No new persistent domain model, wallet, generic event engine or public mutable result store is introduced. Pure Contracts/Application retain `noEngineReferences`; processing/query lifecycle modules and public interfaces remain in Systems. SSOT/SOLID/FW review found no actionable issue.

## 5 Verification evidence

Evidence root: `D:/Github_LocalFile/Project_L/Logs/RecoveryRC2RC4/p5/`. Unity `6000.0.73f1`, batch EditMode, `-nographics`, installed Unity CLI. Original XML/logs are retained locally; no ZIP was made.

| Run | Selection | Result |
|---|---|---|
| `acceptance`, `acceptance-verified` | Initial acceptance compilation attempts | Unity exit 1, no NUnit results: test-only missing namespace for StageEntryCommandKind; corrected to BufferSystems.Current |
| `acceptance-run` | New RecoveryRuntimeAcceptanceTests plus reused next-battle HP cycle | **16/16 passed**, 0 failed/skipped/inconclusive; exit 0; 14 new cases + 2 reused cases |
| `full-core-framework` | Both entire test assemblies, without name filtering | **6,450 total / 6,358 passed / 92 failed / 0 skipped / 0 inconclusive**, Unity exit 2. Core **2,444/2,444**; Framework **3,914/4,006**, including one Explicit case that this runner also executed and passed |

Focused filter: `^ProjectL.Tests.Framework.(RecoveryRuntimeAcceptanceTests|CoreStageCombatCycleTests.RecoveryCycle_TransfersPersistentHealthToNextBattle)`. Full run uses `-assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.Framework'` with no test-name filter. Discovery showed that this Unity runner also executed the Explicit `ShopStagePlaySmokeTests.Boot_NaturalShop_PurchaseExitAndNextCombat_WithSeparateZeroGoldBoundary` case; it passed. Therefore non-Explicit Framework coverage is **3,913 passed / 92 failed / 4,005 total**, not 4,006 automated cases. There were no skipped cases. This extra shop smoke result does not establish recovery popup acceptance.

| Acceptance requirement | Executed coverage |
|---|---|
| Numbered Stage entry → read → Select → correlate/ack/remove → Complete | New acceptance fixture, all 14 cases enter via the existing numbered Stage endpoint |
| Paid/free/skip, capped heal, full HP/rounded-zero/funds, retry, duplicate Complete | New acceptance fixture |
| Real Run replacement with identical keys, old Select/Complete, retained history | New acceptance fixture uses actual StagePlan NewRun jobs |
| Before/after-commit Clear, hidden owner backpressure, cancelled gaps/replay | New acceptance fixture; existing RecoveryCommandProcessingTests adds detailed publication boundaries |
| Current HP/gold/souls, original rules and immutable retained snapshots | New acceptance fixture and RecoveryStateQueryTests |
| Setup/binding/data faults with no partial publication | New acceptance definition-fault cases plus existing MissingLifecycle/PreparationFault/PreparedPublication tests |
| Queued and in-flight old Stage origin | Existing RecoveryNodeCommandTests.OldOriginSameKeys_CannotEnterNewRun cases through ER1 jobs |
| Next battle keeps recovered/skipped HP | Existing CoreStageCombatCycleTests.RecoveryCycle_TransfersPersistentHealthToNextBattle, both cases |
| RC1 compatibility | Existing recovery contract fixtures in the full Core assembly |

These acceptance tests verify existing behavior; Phase 5 introduced no production behavior requiring a new RED/GREEN fix. Compilation errors above are not behavioral RED. Phase 4's behavioral RED/GREEN remains in plan §17.

Fresh independent read-only review found no actionable source or matrix-coverage issue. The reviewer checked public flow, reused next-battle/lifecycle/origin tests, callback-free publication and SSOT/SOLID/FW. Review is separate from test execution. The final regression outcome and cleanup are recorded in plan §18.

## 6 Wider regression failures and readiness limit

The full run is **not green**. All 1,095 cases in the Phase 4 final XML were found and passed in this full run (`phase4-comparison.json`); the 14 new integration cases also passed. Phase 0 recorded a narrower 195-Core/605-Framework baseline, not these entire assemblies. Its original XML is not present in this checkout, so the 92 wider failures are not labeled collectively as proven pre-existing regressions. No broad repair was attempted in this recovery acceptance phase.

`failures.json` retains all 92 exact names/messages; `failure-groups.json` retains fixture counts. The grouped inventory is:

| Fixture(s) | Failed | Observed boundary |
|---|---:|---|
| AreaOutlineHandleTests | 1 | Missing default constructor in reflected fixture |
| ArtifactHealingChaliceIntegrationTests | 3 | Combat command returns InvalidPrimaryTarget before victory healing; no recovery Event request involved |
| ArtifactPopupBootSmokeTests / LobbyDepartureBootSmokeTests | 2 / 1 | Unexpected FMOD command-buffer growth warning |
| AudioSystemTests | 1 | Missing IGlobalSystemGroupService registration |
| BoomEffectPlaybackTests | 2 | Expected particle emission absent |
| CombatDisplayReuseTests / CombatLogUITests | 6 / 2 | UI null/cast/visibility expectations |
| CombatMortarAssetTests / CombatStandardAttackAssetTests | 2 / 3 | Authored transform/color expectations |
| CombatMovementPlanningDisplayTests | 4 | Color equality assertions |
| CombatPointerMappingTests | 12 | EventSystem SetUp error |
| CombatRailwayContactPlaybackTests / CombatRailwayContactSceneTests | 14 / 11 | Contact/event ordering and count expectations |
| CombatSceneEventPresenterTests | 3 | Presentation assertions, including allocation measurement |
| CoreTableQueryTests | 3 | Missing StageMap/ChapterMaster table fixtures |
| DataBaseAssetTests / TerrainOperationalDisplayTests | 1 / 2 | Stage 1101 wall coordinates differ at index 5: expected (7,3), actual (6,3) |
| DeckButtonUITests / GlobalUITests | 3 / 1 | UI hierarchy, visibility and scale |
| DrawPointLineSystemTests | 3 | Input controller setup / effect counts |
| EnvironmentStageAuthoringTests | 1 | Wall authored inactive/not public to both sides |
| HandCardHoverTests | 1 | RenderTexture creation in headless execution |
| InGameViewFaultCleanupTests | 1 | ArtifactViewSystem services not bound before attach |
| LobbyBootSmokeTests | 2 | Missing Unit_CactusCannon in development fixture; RenderTexture creation |
| StageMapDepthCenteringBootSmokeTests / StageMapNodeIconBootSmokeTests | 5 / 2 | Camera.main null during AwaitStage in headless execution, before click verification |

Graphics-dependent smoke failures need the View owner's appropriate graphics/runtime environment before making a gameplay claim. Also update/review the old `BootGraph_EventNodeClickRejectsAndAllowsRetry` expectation when connecting recovery UI: its intended unconditional Event rejection is obsolete, but this run failed earlier at `AwaitStage`, so this is a source-level follow-up, not the observed failure cause. No View smoke assertion was weakened to claim success.

Core recovery is ready for View integration against the supplied contract, with the wider regression limitations above. Final RC4 user merge and View acceptance remain separate. No commit, push or external message was made.
