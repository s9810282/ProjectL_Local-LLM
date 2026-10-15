# Recovery Stage Core Connection — P1 Verification

Archive note (2026-10-03): raw execution artifacts and task scratch files referenced below were backed up outside the repository during user-requested cleanup. Commands and paths below record the original execution; the verification summary remains in this document.

Date: 2026-10-03  
Base: `e55348ff`, branch `feature/Cheon/refactor/core-system`  
Status: P1 complete as a reviewed working-tree checkpoint. P2/P3 and complete recovery gate C remain pending. No commit, push or production activation is claimed.

## Delivered contract

Use `inGame.CoreGroup.Recovery.EnterRecovery(chapterKey, nodeId)` for explicit recovery entry. An accepted result installs the target selection and validated DB options together. Read the menu from `CurrentProgressReadSink.Recovery.Options`; do not follow acceptance with `ConfirmStageEntry` or `SetRecoveryOptions`.

The service checks live navigation, reads the explicit stage's ordered `EventStageDetail.RecoveryOptionKeys`, validates root/option identity, chapter, non-boss role and recovery values, then repeats entry checks. A rejected navigation decision performs no option lookup. Query/conversion/publication faults preserve prior selection, Shop/Recovery objects, HP, currency, progress and combat records. Changed live origin or target prevents publication; external changes made by a query are not rolled back.

Buffer owns the narrow `PrepareRecovery(RecoveryEntryPreparation)` boundary. It validates expected origin, canonical node/key binding, uncleared map/node and absence of any retained battle before one Current installation. An identical selected target cannot reset an applied receipt. Different nodes sharing a StageKey receive fresh options. No HP, currency or progress write occurs during entry.

Both active and terminal retained battles block entry. The existing battle caller must complete settlement and call `Combat.EndBattle()` legitimately before entering recovery. Recovery never performs cleanup or settlement itself. Factory constructs one existing Combat service and passes it with the existing progression coordinator into Recovery; service/module/key/Context registration follows existing Framework contracts. Both InitData/PostBindService orders before execution are covered.

P1 exposes entry only. The legacy `Combat.Recover(optionId)` remains unchanged, but it does not provide the planned caller-identity guards. P2 adds `Recovery.Recover(chapterKey, nodeId, optionId)` and `CompleteRecovery(chapterKey, nodeId)`, including completion-only retries. Completion without recovery was approved by the user on 2026-10-03 and requires no further policy approval. P1 does not implement that completion route. P3 must still prove the full settlement → cleanup → recovery → completion → next-battle cycle with measurable prevention of settlement replay; the narrower P1 cleanup evidence does not close P3.

## Changed files

- New production files: `RecoverySystem/RecoveryDefinitionAdapter.cs`, `RecoverySystem/RecoveryStageService.cs`, `BufferSystems/Current/RecoveryEntryPreparation.cs`, with Unity metadata.
- Existing production files: `BufferSystemGroup.cs`, `CurrentProgressBuffer.cs`, `InGameCoreSystemFactory.cs`, `InGameCoreSystemGroup.cs`, `SystemGroupsService.cs`.
- New tests: `RecoveryDefinitionAdapterTests.cs`, `RecoveryEntryPublicationTests.cs`, `RecoveryStageServiceTests.cs`, shared `RecoveryStageTestDatabase.cs`, with metadata.
- Mechanical test-double forwarding: `ArtifactEditingServiceTests.cs`, `ChapterResetSpy.cs`, `CombatPlanTests.cs`.
- Design/plan status and this checkpoint report. No production Job/Flow/View or authored DB assets changed.

## Verification evidence

Unity 6000.0.73f1 executed through the approved user-profile path that permits AppData access. The initial restricted run reached no project tests and is not RED evidence. No cache deletion or Unity upgrade was needed.

Evidence: `.superpowers/verification/recovery-stage-core-connection/`; launcher: `run-tests.ps1`. Every run uses EditMode assemblies `ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary`.

| Run | Result | Unity exit | Meaning |
| --- | --- | --- | --- |
| `p1-baseline-user` | 1371/1376 | 2 | Fresh pre-change baseline; five Economy asset failures. |
| `p1-adapter-red-valid-tests` | 0/27 | 2 | Missing adapter asserted outside exception-catching tests. |
| `p1-adapter-green` | 27/27 | 0 | Ordered conversion and malformed-data coverage. |
| `p1-entry-red-compiled` | 0/43 | 2 | Missing entry service/publication APIs. Compilation failures in earlier runs are not RED evidence. |
| `p1-green-corrected-filter` | 175/175 | 0 | All six intended fixtures discovered, no skips or inconclusive cases. |
| `p1-broad-final` | 1449/1454 | 2 | 78 added cases, none missing; identical five failure names, messages and stacks. |

Final focused counts: adapter 27, publication 26, recovery service 25, battle publication 30, registered combat cycle 27, chapter graph preparation 40. The source file `ChapterGraphGenerationTests.cs` contributes to the partial fixture `ChapterGraphPreparationTests`; P1.8's filter was corrected accordingly. These are service/Buffer tests, not production scene or Job/Flow acceptance.

The final broad run has Core 918/918, Framework 525/530 and CombatViewBoundary 6/6. All five existing failures report `[CombatStageDetail] Combat stage requires Economy` during catalog construction:

- `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `DataBaseAssetTests.EveryStage_HasNodeVisual`
- `DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

No failure is suppressed or classified as passing. `p1-broad-comparison.json` and failure JSON retain the baseline comparison. A fresh read-only reviewer inspected the actual uncommitted production/test package and found no Critical, Important or Minor actionable issue; Unity execution evidence belongs to this verification, not the reviewer's own run.

## Remaining gates and execution decisions

P1 is complete; gate C requires P2/P3. Authored DB readiness (D), View/Flow consumer adoption (V) and production activation (A) remain external/pending. Gameplay policy (P) permits no-purchase completion, with implementation still pending in P2. Concurrent/reentrant writers remain outside the approved serialized synchronous entry guarantee.

Work stayed in the existing feature checkout so approved uncommitted documents accompanied implementation. PowerShell orchestration replaced POSIX skill scripts for this Windows environment; evidence remains available for P2/P3. These decisions cost checkout isolation and leave local verification artifacts to manage, but do not change runtime contracts. Incoming FMOD changes and attachments are preserved; only Unity's additional scan timestamp is restored to the incoming saved bytes after verification.
