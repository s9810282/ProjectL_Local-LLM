# Chapter Continuation and Final Run Completion — Consumer Handoff

Date: 2026-10-04. P1–P3 implementation, Unity verification and independent review are complete. Changes remain uncommitted. No push.

References: [design](../specs/2026-10-03-chapter-continuation-run-completion-design.md), [implementation plan](../plans/2026-10-03-chapter-continuation-run-completion-implementation-plan.md), [P1 evidence](2026-10-03-chapter-continuation-run-completion-p1-verification.md), [P2 evidence](2026-10-03-chapter-continuation-run-completion-p2-verification.md), [temporary runtime](2026-10-03-temporary-core-runtime.md). P1/P2 documents retain their historical phase status and results; this document records current delivery.

## Available development path

`TemporaryCoreRuntimeFactory.CreateWithChapterContinuation()` is a separately named, disposable entry point compiled under `UNITY_EDITOR || DEVELOPMENT_BUILD`. Resolve Core through `runtime.GetService(SystemGroupsService.InGame).CoreGroup`. Creation already runs InitData, PostBindService and Initialize, initializes the starting roster/wallet/empty Artifact board, and enters chapter 1. Keep the same runtime/services through the sequence; call Shutdown when finished. It does not compose Global/LifeCycle/Jobs, Unity input, scenes or production View definitions.

| Chapter | Path | Definition |
| --- | --- | --- |
| 1 | Start 1 → battle 2 (101) → shop 5 (30) → boss 4 (102) | Next(1, 2) |
| 2 | Start 1 → battle 2 (201) → boss 3 (202) | Final(2) |

Constants `SecondChapterKey`, `SecondChapterFirstBattleNodeId` and `SecondChapterBossNodeId` are 2, 2 and 3. Both maps register through normal Buffer InitData. Chapter 2 has map key 2, depth 3, width 1 and seed 42, with distinct Regular/Boss combat roots. Its graph is initially absent: CompleteChapter prepares and publishes it. No target preselection, roster/wallet reinitialization or reset occurs at continuation.

The original Create() recovery path and CreateWithShop() shop path retain their one-map defaults and unavailable continuation source. Normal FrameworkRuntimeFactory composition also remains unchanged: invalid/missing production definitions do not select temporary data. Initial values remain main plus three Subs, wallet 50, empty 4×4 Artifact board and existing temporary HQ/field/economy/targeting definitions. Fixtures are not balance approval or the production Method_Key schema.

## Gameplay calls and ownership

1. Enter a battle with `core.Combat.EnterBattle(chapterKey, StageNodeId)` and use the existing commands/settlement API to resolve genuine victory. A cleared boss requests stage/map clear but leaves RunOutcome.InProgress until continuation succeeds.
2. Present/acknowledge results and call `core.Combat.EndBattle()` at the caller's lifecycle boundary. A retained active or terminal battle blocks CompleteChapter with BattleRetained. Core never forces EndBattle to enter another stage/chapter.
3. Call `core.ChapterProgression.CompleteChapter(int chapterKey, StageNodeId bossNodeId)` using the source chapter and selected boss. It requires matching real Victory completion, clear facts and Combat/Boss root/graph identity.
4. On accepted `ChapterContinuationKind.NextChapterSelected`, read NextChapterKey. Its graph and Current map are published together, with no selected node. Later call normal EnterBattle/EnterRecovery/EnterShop for the chosen target. The transition preserves the prior completion; installing a new battle clears it according to the existing lifecycle.
5. On accepted `ChapterContinuationKind.RunCompleted`, read `core.ChapterProgression.Outcome` and `.Completion`, or `IBufferQuery.RunProgressReadSink`. One immutable Victory receipt holds Outcome, ChapterKey, NodeId and StageKey. The final selected boss, clears and owned facts remain available. Core does not select another chapter, change scene or reset the Run.

New shop consumers use `EnterShop(chapterKey, nodeId, seed)` → optional `Purchase(chapterKey, nodeId, offerId)` → `CompleteShop(chapterKey, nodeId)`. No-purchase completion is supported. Compatibility PrepareShop(int)/Purchase(ShopOfferId) retain their separately documented stale-node limitation. Recovery follows its existing explicit entry/application/completion contract. The continuation API does not take over either subsystem's settlement or completion.

EndBattle clears active combat characters/field/definition cache but retains completion/results/counters according to existing contracts. View/Flow owns result presentation/acknowledgement, transition timing and pending View command/preview cleanup. Neither continuation nor the next battle clears View inputs implicitly. Complete the caller's input cleanup before issuing new battle commands. Queries such as GetTimeline/GetDrawAvailability still require an active battle; terminal readback uses the coordinator/Run read sink.

## Failure, duplicate and termination contracts

For a missing route or fallible definition/graph query, preserve the committed boss settlement and correct the dependency, then retry **only CompleteChapter**. Do not replay victory, re-enter/reprepare the boss, reset the Run, reinitialize options/roster/wallet or reroll an installed target. Rejections publish no operation-owned partial graph/selection/receipt; deliberate query-fixture mutations are not rolled back. Existing schema/query exceptions can propagate and never mean Final/Victory. Missing continuation returns DefinitionUnavailable.

After boss settlement and EndBattle, InProgress does not authorize parameterless PrepareBattle on that cleared selected boss. It rejects before a source read and preserves the completion needed by CompleteChapter. Parameterless preparation remains supported for a legitimately committed, matching uncleared selection; it does not run fresh-click navigation against that selection. Next/Final cleanup witnesses are reused from P2.

After a successful Next result, a stale source CompleteChapter request returns ChapterMismatch. Duplicate final completion returns RunEnded without another receipt or settlement. After terminal Victory/Defeat, gameplay entry/mutation paths follow the plan's admission matrix. Supported ResolveBattle/AbandonBattle and exact duplicate CompleteBattle retain false semantics; cleanup, reporting and reads remain available.

Supported defeat publication uses existing ProcessNextCommand/AdvanceCombat or ResolveBattle/AbandonBattle/terminal FinalizeTurn full settlement boundaries. Attempt-ending progress and authoritative Run Defeat commit together. PlanStageResolution(Defeat) is read-only planning even when it returns EndAttempt. Direct ResolveStage(Defeat) is unsupported while InProgress and throws before gameplay writes; after termination it returns RunEnded. The legacy victory-clear wrapper cannot manufacture the genuine completion required by CompleteChapter. Trusted owner setup/install/reset ports are infrastructure, not alternate gameplay APIs.

Only the explicit existing `IBufferQuery.RunProgressWriteSink.ResetRunProgress()` boundary reopens the Run: it clears the receipt/outcome, roster, currency and temporary selection/graph/combat facts, clears map clear flags while retaining map registrations, and preserves Artifact ownership. New-game roster/economy initialization remains caller-owned. No implicit reset is added. Operations are synchronous/serialized; numeric chapter/node identities are not protected against reuse across a separately reset Run. A cross-Run identity contract remains separate work.

## Executed acceptance

Unity 6000.0.73f1 EditMode used the approved AppData-capable execution path. Raw logs/XML/arguments/launcher, baseline patch/status, ledger, comparison and review package are outside the repository at `C:/Temp/ProjectL-chapter-run-p3-20261003`.

Launcher arguments: `-batchmode -nographics -projectPath D:/My_ProjectWork/Github_desktop/Project_L -runTests -testPlatform EditMode -assemblyNames "ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary"`, with external testResults/logFile paths. Focused delivery uses the P3/default temporary cycles, existing combat/deck/adapter/preparation/graph/composition fixtures and P1/P2 Run/continuation fixtures; exact filter is stored in `p3-focused-final-args.json`.

| Evidence | Result | Meaning |
| --- | --- | --- |
| baseline.xml | 1835/1840, exit 2, 0 skips | Fresh pre-P3 three-assembly baseline. |
| p3-red-executable.xml | 0/5, exit 2, 0 skips | All five cases fail the missing named factory assertion before implementation. |
| p3-green.xml | 5/5, exit 0, 0 skips | New typed scenario tests pass. |
| default-metadata-red.xml | 0/2, exit 2, 0 skips | Both original scenario metadata values had changed; expected Event but observed Combat. |
| p3-focused-final.xml | 314/314, exit 0, 0 skips | P3 plus default paths and P1/P2 regressions, including corrected default metadata. |
| broad-final.xml | 1840/1845, exit 2, 0 skips | Final complete three-assembly comparison after the default metadata correction. |

`TwoChapters_KeepOwnedFacts_ThenFinishRun` uses actual EnterCombat/Move/Attack commands for all four victories, legitimate EndBattle and the explicit shop APIs. It buys one cost-5 slot, retains wallet 45/five owned records and the acquired Sub exactly once in the chapter-2 full draw order. Main current HP 4, persistent maximum 10, battle maximum 12 from retained souls, main/sub souls 2/1, Artifact contents/identity, old chapter clears and results/counters/View inputs survive according to their ownership contracts. A populated nonempty Artifact pool item is supplied through the existing trusted PublishEdit test seam; it proves ownership retention without inventing temporary placed-effect definitions. Existing CoreStageCombatCycleTests cover placed-Artifact settlement. No fake victory or direct clear write is used.

Two `DefeatInSecondChapter_TerminatesWithoutClearingChapter` cases use the health/ResolveBattle APIs in the second regular and boss battle. Both produce one Defeat receipt with the correct 201/202 identity, retain chapter-1 clear, leave chapter 2 uncleared, block continuation/gameplay, preserve duplicate false semantics and permit EndBattle/input cleanup. Two `NewScenario_DoesNotMutateAnotherRuntime` cases preserve separate default recovery/shop runtimes while the new runtime advances.

P3.2 query-fault acceptance reuses `ChapterContinuationServiceTests.ContinuationFault_AfterSettlement_RetryOnlyCompleteChapter` (route, map:2, stages:2, root:202) and `FinalContinuationFault_RetriesWithoutSettlementReplay` (route/root). All six reran Passed. Both Next/Final `BossCleanup_CompatibilityPreparationRejected_ContinuationStillSucceeds` cases also reran Passed. The existing P1 legacy direct-defeat/admission and P2 publication/late-mutation suites are reused, not duplicated.

The first test-only compile run used a nonexistent persistent-state MaxHealth property. It was corrected to the existing PersistentMaxHealth property before executable RED; that compiler failure is not missing-feature evidence. The missing-factory reflection bootstrap was replaced with the typed API after implementation.

Exact baseline comparison found five added cases, no removed cases and no changed existing outcomes/failure messages/stacks. Whole-suite GREEN is not claimed: the same five authored-data failures report `[CombatStageDetail] Combat stage requires Economy.` They are ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews and DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups, CombatStages_ReferenceExistingInGameData, EveryStage_HasNodeVisual and EveryUnitAndStructure_HasUnitVisual.

## Permanent and disposable delivery

Permanent P1/P2 work remains: minimal RunProgressBuffer/receipt and read contracts; continuation contracts/commands/rules/source interface; ChapterProgressionCoordinator CompleteChapter and admission; detached StageGeneratorSystem preparation; prepared Buffer/Current/Stage publication; source injection/composition and terminal settlement/legacy guards. Generic service/rule/Buffer/admission tests remain, including query-fault fixtures. See the plan's file map and P1/P2 evidence for exact files.

Disposable P3 files are `Factory/Runtime/Development/TemporaryCoreRuntimeFactory.cs`, `TemporaryCoreDatabase.cs`, `TemporaryChapterContinuationSource.cs`, the reused unchanged `TemporaryBattleInitializationSource.cs`, and `TemporaryCoreChapterContinuationTests.cs`, alongside the prior temporary recovery/shop scenario tests and matching metadata. This increment changes only the temporary factory/database, adds the development continuation source and five scenario cases, and updates English documents. No asmdef, production Job/Flow, authored asset, importer or gameplay calculator is changed by P3.

Production replacement order:

1. Supply the actual authored Chapter_NextChapter/explicit-final representation and its IChapterContinuationSource adapter; absence must continue to fail explicitly.
2. Complete Method_Key targeting definitions, queries and unit linkage for DatabaseBattleInitializationSource, and provide valid Economy/stage/map/recovery/shop definitions.
3. Switch callers to normal composition and authored initialization/graph generation. Keep the permanent CompleteChapter and explicit stage/shop/recovery consumer APIs.
4. Run equivalent authored-data acceptance for state carry-over, real next/final victories, defeat and failure/retry, plus consumer Job/Flow/scene acceptance.
5. Remove temporary callers, Development factories/data/sources and disposable scenario tests once no longer needed. Keep permanent APIs and generic tests.

The architecture pipeline document is not a prerequisite. P3 establishes Core/service execution with explicit temporary data; production authored/nondevelopment execution, scene transitions, reward/result aggregation and total framework migration remain separate acceptance.

## Execution rulings and review

- Keep the existing feature checkout because P1/P2 and recovery/shop dependencies are uncommitted; preserve them and put evidence externally. Cost: checkout isolation is weaker than a separate worktree.
- Use a populated Artifact pool item through the existing trusted fixture seam, keeping the default board/definitions empty. Placed-effect settlement remains covered by the existing cycle suite. Cost: P3 itself does not exercise a placed Artifact effect.
- Map next/final continuation faults to the six existing P2 service cases instead of adding a duplicate fault framework. Cost: those witnesses use the supported query-controlled combat fixture rather than the temporary runtime database.
- Mark the first fixed node explicitly Combat only in the new continuation scenario; preserve the historical default Event metadata in Create()/CreateWithShop(). An initial incorrect enum-default assumption was caught, pinned by RED and corrected. Cost: default metadata is retained rather than separately migrated.
- Retain external evidence/ledger because the work is uncommitted; do not delete its only durable verification record. Cost: temporary files remain outside the repository.

One fresh read-only independent reviewer found no Critical, Important or Minor issues and requested no further scoped change. It inspected the corrected default metadata, final XML and exact baseline comparison, publication/terminal integration and assembly/metadata boundaries. Review did not execute Unity. Final git diff --check passed; Unity processes exited; only generated FMOD timestamp drift was restored. No XML/log/launcher or ledger is in the worktree. No branch integration was requested.

The following reviewer boundaries remain explicit scope rulings; they do not imply acceptance outside Core/service execution:

| Boundary | Ruling and reason | Cost if the boundary changes |
| --- | --- | --- |
| Production authored data, Method_Key, nondevelopment and scene execution | Keep normal failure behavior and require real adapters plus replacement acceptance. | Implement adapters and authored/scene acceptance separately. |
| Consumer Job/Flow cleanup timing and policy | Keep caller-owned EndBattle, presentation and View input cleanup. | Integrate and test the real consumer flow. |
| Rewards, save/restart and identity after explicit reset | Preserve existing reset; infer no new policy or cross-Run identity guarantee. | Agree separate result, persistence and identity contracts. |
| Malicious trusted owner ports, concurrency/ABA and allocation-failure rollback | Trust owner infrastructure and serialized operations; do not add revisions or generic rollback. | A changed trust/concurrency boundary needs separate design. |
| Inherited nonterminal legacy query reentrancy and compatibility shop stale-node behavior | Preserve initial/same-chapter behavior and documented compatibility limits; new shop consumers use explicit chapter/node APIs. | Stronger legacy admission requires a separate migration. |
| Placed-Artifact effect in the P3 fixture | Test nonempty owned pool retention; reuse existing placed-effect cycle coverage. | Add equivalent authored placed-effect acceptance at replacement. |
| Existing Economy asset failures | Report the unchanged five failures and preserve authored assets. | Complete authored catalog data before whole-suite success. |

Final status/evidence notes after review are documentation-only and were checked by the executor for consistency and link resolution; the reviewer verdict excludes those subsequent prose edits. No deferred minor issue remains.
