# Chapter Continuation and Run Completion — P1 Verification

Date: 2026-10-03. P1 is implemented and verified; P2 and P3 remain pending. No commit or push.

Plan: [implementation plan](../plans/2026-10-03-chapter-continuation-run-completion-implementation-plan.md). Design: [selected design](../specs/2026-10-03-chapter-continuation-run-completion-design.md).

## Delivered contracts

- One Buffer owns an immutable terminal `RunCompletionReceipt` containing Outcome, ChapterKey, NodeId and StageKey. No receipt means InProgress. `IBufferQuery.RunProgressReadSink` and the coordinator's Outcome/Completion expose live reads. Owner keys and the prepare/install/reset port remain internal; there is no arbitrary public outcome setter.
- Existing command settlement and explicit CompleteBattle share the group's PrepareCompletion/InstallCompletion boundary. An accepted EndAttempt prepares and installs Run Defeat with the existing health, soul, field and battle completion facts. Invalid preparation leaves all operation-owned facts unchanged. Victory settlement continues to clear the stage/chapter without terminating the Run in P1.
- Terminal entry, purchase, recovery, roster acquisition and battle mutations reject according to the plan's admission matrix, including after external lookup callbacks. Enum members are appended without renumbering. Exact duplicate CompleteBattle/ResolveBattle/AbandonBattle still return false.
- Parameterless Combat.PrepareBattle requires a committed selection matching an existing uncleared node/key in its registered uncleared map. It validates again after definition reads, before publication/cache replacement. It does not run fresh-click navigation. A cleared boss cannot be prepared again after EndBattle, so its completion remains available to P2.
- PlanStageResolution remains read-only. Direct ResolveStage(Defeat) is unsupported even while InProgress: it throws before writes/accepted return when the planned result ends the attempt. Legacy victory clear behavior remains, but clear flags alone are not proof of genuine combat completion.

No Framework assembly, production Job/Flow, scene, importer or authored DB asset change was needed. Factory creates the Run owner once; services orchestrate existing calculators and Buffer ports. No new session, revision, rollback or independent defeat calculation was introduced.

## Consumer handoff

Read `core.ChapterProgression.Outcome` and `.Completion`, or the corresponding Buffer read sink. Defeat is authoritative when a supported settlement API succeeds; an accepted EndAttempt returned by PlanStageResolution is only a proposal.

Supported defeat publication routes are ProcessNextCommand, AdvanceCombat, ResolveBattle, AbandonBattle and terminal FinalizeTurn. Use those existing full settlement routes instead of direct ResolveStage(Defeat). ProcessNextCommand/AdvanceCombat return false without processing when terminal; ResolveBattle/AbandonBattle return false without another completion. This also holds if the final stage lookup observes Run termination. Void FinalizeTurn preserves its RunEnded exception contract.

EndBattle remains caller-owned and usable after termination. It retains completion/results/counters and does not reopen the Run. Result acknowledgement and pending-input removal remain View/Flow responsibilities. Query/preview and existing no-battle behavior are preserved; GetTimeline/GetDrawAvailability still require an active battle. Explicit ResetRunProgress clears the receipt along with existing temporary Run facts and retains the existing Artifact/map-registration ownership policy. Stale identity across explicit Run reset is outside this increment.

After genuine boss victory and EndBattle, Outcome remains InProgress in P1, but re-preparing the cleared selected boss is rejected. P2 will add CompleteChapter for next/final routing; that API is not yet available. No gameplay final Victory is implemented in P1. Tests seed Victory only through trusted internal fixture ports to verify terminal admission.

## Executed evidence

Unity 6000.0.73f1 EditMode ran through the approved AppData access path. Raw XML, logs, launcher, baseline patch/status, review package and execution ledger are outside the repository in `C:/Temp/ProjectL-chapter-run-p1-20261003`.

The launcher uses `-batchmode -nographics -projectPath D:/My_ProjectWork/Github_desktop/Project_L -runTests -testPlatform EditMode -assemblyNames "ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary"`, with external testResults/logFile paths and the plan's focused filters where applicable.

| Evidence | Result | Meaning |
| --- | --- | --- |
| baseline.xml | 1666/1671; 5 failures, 0 skips | Fresh pre-P1 baseline; all failures require missing CombatStageDetail Economy. |
| storage-red.xml | 0/1; exit 2 | Named missing-storage assertion, before implementation. Bootstrap reflection test was replaced by typed tests. |
| behavior-red.xml | 19/80; exit 2 | 61 missing-publication/admission failures, including legacy paths; typed storage tests passed. |
| review-late-completion-red-3.xml | 1/5; exit 2 | Four bool completion paths threw RunEnded instead of returning false. The void FinalizeTurn preservation witness passed. |
| review-late-completion-green.xml | 5/5; exit 0 | Correct bool rejection and unchanged void exception behavior. |
| p1-focused-delivery.xml | 443/443; exit 0, 0 skips | Final storage, publication, admission, reset, cleanup, compatibility and composition acceptance. |
| broad-delivery.xml | 1760/1765; exit 2, 0 skips | Final three-assembly regression; same five baseline failures. |

Broad comparison found zero changed/new failure signatures using exact full name, message and stack. The only removed case name was ResultCounterOverflow_FaultsBeforeHealthPublication, replaced by its two parameterized victory/defeat cases with original preservation assertions retained. There are 95 added case names, a net increase of 94. The overall suite is not green.

The unchanged failures are ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews and DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups, CombatStages_ReferenceExistingInGameData, EveryStage_HasNodeVisual and EveryUnitAndStructure_HasUnitVisual. Each reports `[CombatStageDetail] Combat stage requires Economy.` Authored Economy data is a separate dependency.

Intermediate runs exposed test-only namespace/import and duplicate-registration mistakes; these were corrected before the final runs. The first automatic late-completion fixture did not reach completion because it killed a scheduled actor directly. Its corrected witness uses the supported Railway gimmick and reaches the same final lookup as production automatic settlement. Compiler/setup failures are not counted as missing-behavior RED evidence.

## Coverage reused and migrations

Existing settlement, duplicate, counter-overflow, health, cleanup, roster/deck, recovery, shop and graph tests remain. New Run assertions extend those witnesses rather than adding a second settlement framework. RunProgressBufferTests and RunTerminationAdmissionTests cover the minimal new owner and admission matrix.

Selection-only compatibility fixtures now register matching uncleared map/node facts. Obsolete post-defeat prepare/recovery success expectations now require RunEnded. The direct legacy defeat test proves planning is read-only and direct publication rejects; the explicit NewRun test uses legitimate AbandonBattle/EndBattle before reset. Manually assembled Buffer fixtures include the new owner; forwarding test doubles expose the new read members. These are intentional contract migrations, not relaxed checks.

CoreStageCombatCycleTests.ClearedBoss_CompatibilityPreparationPreservesCompletionAfterCleanup produces genuine boss victory, calls service EndBattle and rejects compatibility preparation before a definition read. It preserves completion, selection, persistent facts, clears, results/counters and the null definition cache. Next/Final CompleteChapter success after this rejection is explicitly deferred to P2.5; P1 cannot verify an API that has not been implemented.

## Review and rulings

One independent final P1 review found one Important return-contract issue at the final completion lookup. It was reproduced in four focused cases, fixed by returning false for RunEnded before settlement publication, and verified by focused GREEN plus both final suites. FinalizeTurn keeps its exception mapping. No unresolved P1 review finding or deferred minor fix remains.

Execution stayed in the current feature checkout because this increment consumes incoming uncommitted recovery/shop/temporary-runtime composition; those changes were preserved. The ledger/evidence stayed external to avoid worktree clutter. Nested owner keys were preserved instead of exporting them at the root. Trusted owner setup/reset/install seams were not turned into gameplay APIs. No generic hardening or additional review cycle was added.

## Remaining dependencies and next phase

P2 adds pure continuation decisions, route source, detached target preparation, query-free graph/selection publication, CompleteChapter and gameplay final Victory. P3 adds the disposable two-chapter scenario and consumer integration witnesses. These can proceed with explicit temporary/fixture definitions; the architecture pipeline document is not a prerequisite.

Production replacement still requires the authored Chapter_NextChapter/final-definition representation and adapter, Method_Key targeting definition/query connection, and valid Economy assets. Actual Job/Flow/scene acceptance remains separate from these Core/service tests. Temporary definitions and scenario paths are removed only after real adapters and equivalent authored-data acceptance are ready; permanent Run APIs and service/rule tests remain.
