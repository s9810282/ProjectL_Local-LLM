# Core Stage, Combat and Run Integration Verification — C1-C5

Date: 2026-10-03. Scope: C1-C5 in the [verification plan](../plans/2026-10-03-core-stage-combat-run-integration-verification-plan.md), now complete within the stated Core boundary. The C1 matrix and C2-C4 sections retain their historical results and setup classifications; the final C5 section records closure and remaining external obligations.

## Result

**Core integration verified within fixture-backed scope.** C5's final three-assembly run passed **1307/1312**, with the same five Economy authoring failures as C1 and no new failures, missing tests, skips or inconclusive cases. Core passed 918/918 and CombatViewBoundary passed 6/6. All seven added cases and all historical named witnesses passed. Independent final review found no actionable issue in the verification package. This is not an all-green suite or playable Stage/Run acceptance.

C1 established the baseline and coverage matrix. C2 added three registered preparation cases and passed 115/115 focused cases. C3 upgraded existing common-command tests to registered services/real roster paths and passed 88/88. C4 added four service-cycle/priority cases and passed 155/155. C5 completed full regression, responsibility audit and this handoff. No production code was changed by C1-C5.

At C1, the main missing integration witness was one continuous **registered Core service** path through preparation, an accepted terminal command, the real progression coordinator, service cleanup and subsequent preparation. C4 now covers that chain with explicit fixture data. Production DB readiness and actual View/Flow/Runner ordering remain separate dependencies.

## Checkout and evidence attribution

- Branch: `feature/Cheon/refactor/core-system`.
- HEAD: `3a6b4cd326d111588c4a5fd05868a1b3e84493be` (`[천명] 아티팩트 배치 구현`).
- Unity: `6000.0.73f1`, revision `a166abc3bf0e`.
- Baseline target: the current working tree, including the pre-existing uncommitted `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatProcessingService.cs` changes. This is not a clean-HEAD validation or a review/approval of that incoming refactor.
- Also present before C1: modified `Assets/Audio/FMODplus/New Fmod Event Catalog.asset`, untracked `.codex-remote-attachments/`, and the untracked verification plan. They were not attributed to C1 or committed.
- Local evidence directory: `.superpowers/sdd/2026-10-03-core-stage-combat-run-integration-verification-plan/`.
- Evidence includes `incoming-status.txt`, `incoming.patch`, `incoming-hashes.json`, exact protected-file backups under `incoming/`, `head.txt`, `branch.txt`, `started-at.txt`, `baseline.xml`, `baseline.log`, `baseline.exit-code.txt`, `failures.json`, `baseline-comparison.json`, `witness-validation.json`, `preservation-check.json` and `progress.md`.

The first restricted-token Unity launch stalled before producing a log or XML. Only that task-created process was stopped. The successful retry used the same project and three assembly filters with user-profile access. It was a test run, not a second Unity project or a changed test configuration.

Unity changed only the FMOD catalog's `lastScanTime` among the protected files. Its contents were compared against the incoming backup, confirmed to differ only in that timestamp, and restored byte-for-byte. The incoming combat service and protected ProjectSettings files retained their SHA-256 hashes. The plan is intentionally updated after preservation verification to record C1 completion.

## Fresh baseline

The invocation used the editor and flags in the plan, with evidence saved in the directory above:

```text
-batchmode -nographics
-projectPath D:/My_ProjectWork/Github_desktop/Project_L
-runTests -testPlatform EditMode
-assemblyNames ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary
-testResults <evidence-directory>/baseline.xml
-logFile <evidence-directory>/baseline.log
```

| Assembly | Passed / total | Failed | Skipped |
| --- | --- | --- | --- |
| ProjectL.Tests.Core | 918 / 918 | 0 | 0 |
| ProjectL.Tests.Framework | 376 / 381 | 5 | 0 |
| ProjectL.Tests.CombatViewBoundary | 6 / 6 | 0 | 0 |
| Total | 1300 / 1305 | 5 | 0 |

Unity exit code: **2**. Inconclusive: **0**. The full suite is **not green**.

Comparison input: `.superpowers/sdd/2026-10-02-artifact-core-migration-implementation-plan/A4/commit-validation/results.xml`. The complete test-name sets match, and the failed test identities and full messages match exactly (zero differences). This is stronger than comparing only the failure count, but does not prove untested behavior.

All five failures have this full message:

```text
System.InvalidOperationException : [CombatStageDetail] Combat stage requires Economy.
```

1. `ProjectL.Tests.Framework.ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
2. `ProjectL.Tests.Framework.DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
3. `ProjectL.Tests.Framework.DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
4. `ProjectL.Tests.Framework.DataBaseAssetTests.EveryStage_HasNodeVisual`
5. `ProjectL.Tests.Framework.DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

Asset inspection again found 11 Combat stage `.asset` files under `Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Combat/`, none with a serialized `economy:` reference, and no `.asset` under Assets referencing the `StageEconomyDataSO` script GUID. These are unresolved authored-data dependencies, not evidence that currency storage or spending rules are absent.

## Coverage matrix

Test names below omit the common `ProjectL.Tests.Framework.` namespace; files are `Assets/Tests/EditMode/Framework/<fixture>.cs`. All 54 distinct named witnesses passed in the fresh XML (69 cases including parameters), with zero missing or nonpassing matches. `Verified` is limited to the boundary described in that row; gaps remain even when the component tests pass.

| ID / next task | Exact existing witness | Real components and substitutions | Result and remaining gap |
| --- | --- | --- | --- |
| B01 / C2 registration + roster | `CharacterRosterServiceTests.InitialRoster_IssuesMainThenSubsInSpecOrder_AndDrawExcludesMain`; `CharacterRosterServiceTests.InvalidInitialDefinition_ConsumesNoIdAndInstallsNoRoster` | Real Core/Buffer factories, group InitData/PostBind and roster service; `CharacterRosterDatabase` supplies fixture queries | Verified registration/roster behavior. Production loadout and startup timing remain external. |
| B02 / C2 Artifact bootstrap | `ArtifactEditingServiceTests.FactoryRegistersServiceAndBindingModule`; `ArtifactEditingServiceTests.BindingDoesNotInitializeOrGrantInventory`; `ArtifactEditingServiceTests.InitializeTwiceAndRecreateService_PreservesCurrentInventory` | Real Core/Buffer factories and Artifact service; explicit Artifact definitions | Verified no bind-time grant and initialize-once retention. Production dimensions/caller are external. |
| B03 / C2 graph generation | `ChapterGraphPreparationTests.Entry_GeneratesFromRealDbQueries_AndReusesBufferGraph`; `ChapterGraphPreparationTests.Entry_MissingBossFailsBeforeGraphRegistrationAndCurrentSelection` | Real Framework runtime, Core/Buffer factories, generator/coordinator and InGame DB factory; in-memory SO tables, with recovery/Event stage fixtures | Verified real table/generator integration and failure before graph publication. These are not authored Combat stage assets. |
| B04 / C2 selection | `ChapterGraphPreparationTests.StagePreparation_ValidationAloneDoesNotRecordPlayableEntry`; `ChapterGraphPreparationTests.StageConfirmation_RechecksBufferAndRejectsAlreadyClearedTarget`; `ChapterGraphPreparationTests.StagePreparation_InvalidDataDoesNotCommitSelection` | Real registered coordinator and stage validation; in-memory definitions and explicit Buffer changes between calls | Verified validation/confirmation contracts. No integrated combat-preparation-failure -> selection-unchanged witness. |
| B05 / C2 battle preparation | `CharacterBattlePreparationTests.InitialDeal_StartJobCreatesMainPlusAtMostThreeLinkedSubs`; `CharacterBattlePreparationTests.InvalidPlacementOrCapacity_ChangesNoBufferOrAllocator`; `CharacterBattlePreparationTests.AlreadyLinkedCandidate_RejectsBeforeDrawCursorAdvance` | Real Core/Buffer and Job factories, prepare Job, real preparation service/calculator; injected mutable initialization source, roster seeded through Buffer | Verified preparation publication and duplicate retained-battle rejection. Does not use the default DB initialization source or complete four-Job execution. |
| B06 / C2 current facts | `CharacterBattlePreparationTests.StartPreparation_UsesBufferStateWithoutLegacyFactory` | Same fixture; wounded main and souls in Buffer, injected definitions | Verified service reads current facts and prepare Job completes once. Cleanup in this test uses the Buffer directly. |
| B07 / C2 DB adapter | `BattleDefinitionAdapterTests.Definitions_UseStageReferencesAndOwnedKeysWithoutLegacySession`; `BattleDefinitionAdapterTests.DefaultFactory_UsesBufferSelectionAndRejectsMissingMethodSchemaBeforeWrites` | First calls the actual adapter with a custom `DefinitionDatabase`; second uses the default factory/source, real roster initialization and Buffers with that query double | Verified conversion and fail-before-write behavior. No successful default-source battle preparation: attack profiles are deliberately empty until approved Method_Key schema/query data is supplied. |
| B08 / C2 Buffer validation | `CharacterBattlePreparationTests.BufferRejectsInvalidAllocatorOrDrawCursor_BeforeEitherStoreInstalls`; `CombatStateBufferTests.PrepareBattle_DefaultCharacterSnapshot_PreservesStoresAndAllowsRetry` | Real Buffer group with explicit malformed proposals | Verified multi-store preparation rejection. No need to recreate these cases as new service tests unless C2 finds a service-specific gap. |
| B09 / C3 rejection + no work | `CombatCommandPublicationTests.ProcessingService_NoInputPublishesNothing`; `CombatCommandPublicationTests.ProcessingService_HandlesRejectionOnceAndNeverAcknowledgesOrRemovesViewInput` | Factory-created/bound Core service, real Buffers, injected source; preseeded battle and fixture DB | Verified false for no command, handling/rejection once, retained View input and no Core acknowledgement. Preparation-to-command link is bypassed. |
| B10 / C3 accepted draw + payment | `CombatCommonPublicationTests.DrawPublication_InstallsCharacterCurrencyCursorAndResultOnce`; `CombatCommonPublicationTests.InsufficientGold_RejectionKeepsAllGameplay_AndCurrencyUpdateRefreshesQuery` | Directly constructed combat service, real Buffers and service preparation; fake roster draw planner, injected definitions and explicit balance | Verified spend/publication/rejection using live Buffer values. Real registered roster integration with this path is missing. |
| B11 / C3 automatic progression | `CombatCommonPublicationTests.CommandSequence_EntersCombat_AdvancesTurns_AndReturnsAtNightEnd`; `CombatCommonPublicationTests.RepeatedNights_PreserveMainHealthPositionAndLock_AndEnemyDeploymentOrigins` | Same directly constructed service; test drives automatic advancement and externally removes commands | Verified service phase/turn behavior. No real Flow/Plan/Runner path or terminal-to-next-battle cycle. |
| B12 / C3 preview | `CombatPreviewPublicationTests.Service_PublishesMatchingPreviewOnly_WithoutGameplayOrCommandResultWrites`; `CombatPreviewPublicationTests.SameAimReissuedAfterApChange_RecalculatesAndOldResponseDoesNotMatch`; `CombatCommonPublicationTests.PlacementAndMovePreview_PublishResponseOnly` | Direct service + real Buffers; injected definitions/roster doubles and prepared battle state | Verified preview-only publication and request identity within fixture scope. Factory-bound preview/advance smoke coverage remains a small C3 gap; production hover/popup freshness is external. |
| B13 / C3 failed publication | `CombatCommandPublicationTests.InvalidFinalSoulField_PublishesNeitherHealthNorResult`; `CombatCommandPublicationTests.InvalidCompletionSelection_PublishesNeitherHealthNorProgressNorResult`; `CombatCommandPublicationTests.ResultCounterOverflow_FaultsBeforeHealthPublication`; `CombatCommonPublicationTests.InvalidDrawTimeline_InstallsNeitherCharacterCurrencyCursorNorResult` | Real calculators/Buffer group, deliberately invalid or stale proposals | Verified rejection before partial install. A fake throwing sink is not the basis for this conclusion. |
| B14 / C3 noncombat spending regression | `CharacterRosterServiceTests.Purchase_SameKindCreatesNewInstanceWithoutCopyingSouls_AndRejectsSecondPayment`; `CharacterBattleSettlementTests.Recovery_IsOneUseAndPaysAndHealsTogether`; `CharacterBattleSettlementTests.Recovery_StaleCurrencyChangesNeitherHealthNorReceipt` | Shop uses registered services and fixture DB; recovery uses calculators and actual Buffer publication with explicit options | Verified relevant payment invariants. No claim of a complete shop/recovery stage execution flow. |
| B15 / C4 terminal command | `CombatCommandPublicationTests.ProcessingService_FinalAttackPublishesHpApStructuresCompletionAndProgressTogether` | Direct combat service and actual Buffers; preseeded battle, `RosterProbe`, `ProgressionProbe`, injected definitions | Verified combined terminal publication, but progression decision comes from a probe. |
| B16 / C4 Artifact-to-combat | `ArtifactEditingServiceTests.AcceptedPlacementFeedsExistingCombatEffectsOncePerInstance` | Real registered Artifact/combat/progression services and real Buffers; explicit definitions and injected battle source, direct battle seeding | Verified 0/1/2 placed healing effects, pool exclusion and no repeated command settlement. Does not include service PrepareBattle/EndBattle/next preparation. |
| B17 / C4 victory and defeat progress | `ChapterGraphPreparationTests.StageVictory_ClearsStageAndBossAlsoClearsMap`; `ChapterGraphPreparationTests.StageDefeat_RequestsAttemptEndWithoutClearingProgress` | Real coordinator and in-memory chapter fixtures, direct ResolveStage calls | Verified progression decisions/publication independently of combat command production. |
| B18 / C4 abandonment and cleanup guard | `ChapterGraphPreparationTests.CombatService_UsesBufferValuesAndRecordsAbandonmentBeforeCleanup` | Real registered combat/coordinator and Buffers, directly installed battle, empty explicit loadout | Verified unfinished cleanup rejection, abandonment once, retained completion and persistent HP after service cleanup. Next service preparation is absent. |
| B19 / C4 terminal priority | `ChapterGraphPreparationTests.CombatService_TurnDeathPublishesFinalHealthAndDefeatTogether`; `CharacterBattleSettlementTests.Outcome_MainDeathTakesPriorityOverDestroyedEnemyHeadquarters` | First uses registered service FinalizeTurn with explicit settlement facts; second directly calls the pure outcome calculator | Verified terminal defeat and pure simultaneous-death priority. Simultaneous-death agreement through the actual command/publication chain is not covered by the pure test. |
| B20 / C4 completion exactly once | `CharacterBattleSettlementTests.DuplicateCompletion_HealsOnceAndLeavesRosterUntouched`; `CharacterBattleSettlementTests.MultipleChalices_SettleOnceAndPersistHealedHealthAcrossCleanupAndNextPreparation` | Real completion/preparation calculators and Buffer group; direct publication/cleanup | Verified settlement idempotence and HP retention at the calculation/Buffer layer. Service lifetime composition is bypassed. |
| B21 / C4 result retention | `CombatCommandPublicationTests.ResultNumbersAndLastResult_SurviveEndBattleAndNextPreparation`; `CombatCommandPublicationTests.ChaliceCommand_PublishesOrderedCappedReceiptsOnce_AndPreservesReadonlyResultAfterCleanup` | Real calculators/Buffer, explicit result/completion proposals, direct Buffer cleanup/preparation; test performs acknowledgement | Verified result/counter retention and immutable read results at Buffer scope. Does not prove service cleanup sequencing or production acknowledgement ordering. |
| B22 / C4 roster and souls | `CharacterRosterBattleCycleTests.WoundedMain_KeepsCurrentHealth_MaxIsPersistentPlusSouls`; `CharacterRosterBattleCycleTests.RepeatedEntryWithSouls_AppliesCountOnce`; `CharacterRosterBattleCycleTests.SubEntry_FillsSoulAdjustedMaxAfterDamageOrDeath`; `CharacterRosterBattleCycleTests.SameKindCards_OnlyCollectorGrows_AcrossReassignedEntityIds` | Real preparation/soul calculations and Buffers, direct seeded roster and battle boundaries | Verified persistent-vs-temporary state and soul ownership. No registered service cycle witness. |
| B23 / C4 chapter retention | `ChapterGraphPreparationTests.BossCompletionAndChapterEntry_PreserveStageAndMapRecords`; `ChapterGraphPreparationTests.ChapterEntry_PreservesOtherChapterCompletionAndUnrelatedBuffers` | Real runtime/coordinator/service cleanup, first test uses direct battle/completion writes and manually registers next chapter; explicit roster/currency/Artifact fixtures | Verified no ordinary chapter reset and preserved records. Not a command-driven boss win followed by service preparation in a DB-backed next chapter. |
| B24 / C4 Artifact lifetime | `ArtifactEditingServiceTests.EveryMutationWhileHasBattle_PublishesLayoutLocked`; `ArtifactEditingServiceTests.ChapterAndBattleTransitions_PreserveArtifactAggregate`; `MapProgressBufferTests.ResetRunProgress_ClearsTemporaryDataAndPreservesArtifactsAndMapRegistrations` | Real Artifact service/Buffer; battle/transition/reset setup directly through existing Buffer permissions | Verified existing edit admission and retention. Explicit ResetRunProgress regression does not define new-Run policy or Artifact reset authority. |

Review-focus trace: invalid preparation -> B03-B08; rejection/preview/replay -> B09-B13; terminal agreement -> B15-B20; persistence/cleanup -> B18, B20-B24; fixture/production separation -> every boundary annotation above and the dependency table below.

## Concrete follow-up gaps for C2-C4

These are missing evidence, not newly proven gameplay bugs. Do not duplicate every passing component case.

1. **C2 — connect successful roster/bootstrap and preparation at the registered service boundary.** Use the real factories and an explicit supported `IBattleInitializationSource` fixture to connect real roster initialization and real preparation. Preserve adapter-negative tests. Default-source positive acceptance is externally blocked by missing attack-profile schema/query support; in-memory Economy values alone cannot resolve it. Do not invent Method_Key mappings or add a permissive production fallback.
2. **C2 — verify caller-side selection commit discipline.** Existing coordinator and preparation rejection tests are separate. Add or extend one fixture showing an unsuccessful preparation leaves selection unconfirmed under the agreed caller ordering. The test caller orchestrates this; Core does not gain a Flow or a transaction spanning independent APIs.
3. **C3 — verify registered processing ports together.** Accepted draw/automatic/preview tests use directly constructed services with a fake roster, whereas registered command tests cover no-input/rejection and the Artifact final attack. Reuse those behavioral tests; add only the missing factory-bound accepted-command/preview/automatic smoke assertions with current Buffer facts and unchanged once-only semantics.
4. **C4 — connect the full fixture-backed service lifetime.** A single registered Core group and canonical Buffer group should execute PrepareBattle -> accepted terminal command -> real progression publication -> EndBattle -> legal next stage selection -> PrepareBattle. Assert persistent main HP, owned IDs/souls, currency/Artifact preservation, temporary cleanup, completion/result/counter lifetime and no repeated grant/settlement at those boundaries. No existing inspected fixture witnesses this complete chain.
5. **C4 — extend the registered abandonment/priority boundary only where missing.** B18 already proves the cleanup guard and idempotent abandonment. Add its next-preparation/retention link rather than rewriting the test. B19's simultaneous-death case is pure; verify terminal agreement at the service/publication boundary if no stronger witness emerges during C4.
6. **C4 — state the input producer assumptions.** The cycle test must explicitly simulate the external owner's old-input cleanup. The current Core contract does not discard unhandled old input or enforce result presentation acknowledgement. Do not mark stale-input lifecycle safety or F2 complete from Core-only calls.

File decision: first extend the owning existing fixtures. If the full service cycle cannot remain clear in those fixtures, the plan's conditional `Assets/Tests/EditMode/Framework/CoreStageCombatCycleTests.cs` is justified for that integration witness only. No new shared test framework or production API is needed at C1.

## External dependencies and deferred decisions

| Status | Dependency and evidence | Owner / next evidence |
| --- | --- | --- |
| External dependency | Five actual catalog failures above; missing Economy asset references | DB/designer authors values and references; Core reruns actual adapter/catalog acceptance |
| External dependency | `BattleDefinitionAdapter.Read` deliberately returns no attack profiles; `DefaultFactory_UsesBufferSelectionAndRejectsMissingMethodSchemaBeforeWrites` passes by expecting T1 rejection | DB supplies approved Method_Key schema/queries/data; Core adapts and validates the real source afterward |
| External dependency | Production loadout, wallet seeding, Artifact dimensions and initialization timing are not proven by fixture startup | Existing execution owner + DB/designer; Core checks service contracts |
| External dependency | Command/Preview/Advance Jobs still throw placeholder exceptions; Prepare Job has its service call | View/Flow completes existing Jobs and proves the actual registration/Runner path |
| External dependency | JobRunner calls Execute and clears its current Job only after normal completion; no fault latch at that boundary | Framework F1 proves a fault cannot re-execute on a later tick |
| External dependency | CombatLifeCycle.OnExit still stops Flow and clears input without the complete outstanding-job/result/acknowledgement/cleanup sequence | View/Flow + Framework F2, with existing Core service support |
| External dependency | Artifact E3 production bootstrap, intents, preview/result correlation and popup lifetime are not covered by these service fixtures | Assigned execution/View owner |
| Deferred policy | New Run/death restart, persistence/crash recovery and final Artifact reset authority | Design/owner agreement; no new policy or reset API in this package |

## C1 completion and remaining work

C1's five checklist items are satisfied by the recorded checkout/hashes, fresh XML comparison, B01-B24 matrix, concrete missing service-chain links and external dependency table. C2-C4 should reuse these witnesses and target only the recorded gaps; C5 remains the final regression and scope audit after that work. No production defect was fixed, no new test was added, and no commit/push was performed in C1.

## C2: Registered preparation and stage-entry verification

Only `Assets/Tests/EditMode/Framework/BattleDefinitionAdapterTests.cs` changed outside documentation. It now uses a small local helper that exports the existing Core/Buffer factories into a real `FrameworkRuntime`, runs InitData/PostBindService, and resolves the Core group and canonical Buffer through their registered keys. No Job/Flow or production API was added. The existing `DefinitionDatabase` query double supplies typed in-memory definitions; the real adapter converts them. An explicit test initialization source supplies the converted definition plus fixture-only attack profiles.

| New witness (BattleDefinitionAdapterTests) | Evidence |
| --- | --- |
| `RegisteredPreparation_UsesInitializedRosterAndPreservesPersistentBuffers` | Real roster initialization produces the owned roster consumed by registered combat preparation. A wounded main remains at 7 HP, with soul-adjusted max HP 12 and attack 7; two souls and persistent max HP 10 remain stable across two preparations. Owned IDs and initial draw remain stable. Fixture balances 91 then 85 are retained instead of being reset to the definition's initial gold 27. Re-initializing the roster rejects; preparing while a battle is retained throws without replacing battle/character snapshots. |
| `StageEntry_PreparationFailureLeavesSelectionUnconfirmed(True)` | Missing initialization definition fails after entry validation but before confirmation. Selection, map/node progress, owned roster, persistent main, currency, soul field and result counters remain unchanged; no active characters or completion are installed. Replacing the missing definition permits successful preparation and confirmation with expected entity allocation. |
| `StageEntry_PreparationFailureLeavesSelectionUnconfirmed(False)` | The same preservation/retry assertions hold for an out-of-bounds main placement. This exercises actual preparation validation after the registered service reads the fixture definition. |

The stage-entry test's local caller deliberately performs validation -> preparation -> confirmation. This proves behavior under that caller discipline; it does not establish a cross-service transaction or prove a production Flow follows the same order. The default DB source still requires an existing selection and still rejects pending Method_Key support. Its production sequencing remains an execution integration dependency.

The repeated-preparation witness clears the battle directly through the existing Buffer permission solely to construct the next preparation input. It does not perform terminal settlement or service `EndBattle`, and does not close C4. Test profiles are explicit data, not a proposed production fallback. Actual DB tables remain covered by the existing chapter fixtures; adapter and default-source rejection tests use the typed definition-query double, not a complete authored DB catalog. The C2 checklist wording was clarified to describe those actual boundaries rather than requiring duplicate table-assembly scaffolding while default-source positive acceptance is externally blocked.

### Focused run and repair history

Evidence: `.superpowers/sdd/2026-10-03-core-stage-combat-run-integration-verification-plan/C2/`.

- First run: `focused.xml` / `focused.log`, **112/115 passed**, exit **2**. All three added cases stopped in `FrameworkRuntime.InitData -> BufferSystemGroup.InitData -> MapProgressBuffer.InitData` because the existing test double's `StageMapTableQuery` threw `NotSupportedException`.
- Repair: the test double now implements `IStageMapTableQuery` with a fixed typed map definition. Framework initialization registers that map; the stage-entry test verifies this instead of manually registering it. No production behavior was repaired or relaxed.
- Final run: `focused-final.xml` / `focused-final.log`, **115/115 passed**, exit **0**, zero skipped/inconclusive cases. The editor compiled the test addition and exercised all applicable C2 fixtures.

| Fixture class | Passed / total | Reused C2 evidence |
| --- | --- | --- |
| BattleDefinitionAdapterTests | 19 / 19 | Adapter conversion, default-source fail-before-write, three added registered preparation cases |
| CharacterBattlePreparationTests | 12 / 12 | Placement/capacity/allocator rejection, retained-battle guard, current facts |
| CharacterRosterServiceTests | 10 / 10 | Roster initialization, invalid definitions and retained identity |
| ChapterGraphPreparationTests | 40 / 40 | Real DB-table generation, graph reuse, selection validation/confirmation and progress preservation |
| ArtifactEditingServiceTests | 30 / 30 | No grant on binding, initialize-once, recreation/rebinding and lifetime admission |
| CombatStateBufferTests | 4 / 4 | Preparation rollback and retry at the Buffer boundary |

`ChapterGraphGenerationTests.cs` declares the partial `ChapterGraphPreparationTests` class. Its generation cases are included in that fixture's 40 cases, despite the initial filter also containing an unnecessary file-derived `ChapterGraphGenerationTests` name. XML witness checks confirm those generation methods ran; no test rerun or invented fixture is needed.

The complete three-assembly suite was not rerun in C2; its last complete evidence remains C1's 1300/1305 with five Economy failures. The C2 subset does not include those asset tests and must not be presented as resolving them. The incoming combat service, audio asset and ProjectSettings SHA-256 hashes were preserved; only verified FMOD importer timestamp changes were restored from the C2 incoming backup. No commit/push was performed.

### C2 completion boundary

All six C2 checklist items have fixture-backed evidence. C1 gap items 1 and 2 are covered within the explicit source/caller assumptions above. Default DB success, actual asset authoring, production bootstrap and entry ordering remain external. C3 registered accepted-command/preview/advance coverage and C4 terminal-to-next-battle service integration remain pending; the new helper is preparation test support, not a replacement execution system.

## C3: Registered commands, previews and publication

Only `Assets/Tests/EditMode/Framework/CombatCommonPublicationTests.cs` changed outside documentation in C3. Its existing setup now exports `InGameCoreSystemFactory`, propagates the group's InitData/PostBindService, resolves the Core group by its service key and consumes `ICombatProcessingService`. It acquires four subs through the registered roster service and uses its real initial-draw planner. The hand-written roster double and direct combat-service constructor were removed from this fixture.

The canonical Buffer group still comes from the existing factory-backed settlement fixture, which seeds the initial main, graph and selection. Battle definitions/profiles/economy are explicit fixtures, and commands are submitted by test code. This is registered Core service integration, not a default-DB, full-root bootstrap or production Job/Flow test. C2 already covers root InitData/PostBind with its separate preparation fixture. No production lifecycle or Framework/Buffer responsibilities changed.

### Strengthened witnesses and reused coverage

| Exact witness | C3 evidence |
| --- | --- |
| `CombatCommonPublicationTests.DrawPublication_InstallsCharacterCurrencyCursorAndResultOnce` | Registered accepted draw spends 2 from fixture balance 10, creates one character and advances cursor/allocator once. Reprocessing returns false, preserves the installed state and same result object, and advances the result sequence only once. Existing characters' HP/AP and owned soul counts remain unchanged. Input remains present and acknowledgement remains zero. |
| `CombatCommonPublicationTests.InsufficientGold_RejectionKeepsAllGameplay_AndCurrencyUpdateRefreshesQuery` | Registered rejection handles the command once and publishes one rejected result while preserving HP/AP, owned identity, runtime area/slot, anchor/rotation/occupied cells, soul field, currency and draw allocation. Reprocessing creates no second result. A subsequent explicit balance update permits the next command, proving the service reads current Buffer values. |
| `CombatCommonPublicationTests.PlacementAndMovePreview_PublishResponseOnly` | Registered command/preview/advance ports return false when no eligible work exists. A placement preview publishes its request-numbered response once, retaining battle/character/soul state, balance, completion, command/result numbering and View acknowledgement. The View preview request remains present. |
| `CombatCommonPublicationTests.CommandSequence_EntersCombat_AdvancesTurns_AndReturnsAtNightEnd` | Registered automatic processing publishes an Advance result with no command number, increments the result sequence once per advance and preserves the last handled command number. Returning to deployment leaves no eligible automatic work; another call preserves state and result number. Test code explicitly acts as the external command-removal owner. |
| Existing command publication rejection/fault tests | Real Buffer rejection of invalid field/timeline/soul/completion selection, changed identity and result-counter overflow still passes. No fake throwing sink is used to infer multi-store publication safety. |
| Existing preview, settlement, roster/shop and Artifact service fixtures | Attack preview/request identity, repeated healing settlement, shop/recovery payment and supported Artifact-to-command once-only behavior remain passing. Those fixtures retain their C1-documented substitution boundaries. |

C1 gaps B10-B12 / follow-up item 3 are closed at the registered nonterminal service boundary by reusing these tests; no duplicate `RegisteredCommand_ReprocessingDoesNotSpendOrPublishAgain` case was necessary. Existing terminal-effect witnesses remain separate from the full C4 lifetime chain.

### Focused verification and assertion correction

Evidence: `.superpowers/sdd/2026-10-03-core-stage-combat-run-integration-verification-plan/C3/` contains incoming hashes/copies, run arguments, first/final XML/logs/exit codes, witness validation, summary and preservation checks.

- First run: `focused.xml`, **87/88 passed**, exit **2**. The strengthened rejection test incorrectly required the pre-publication `CharacterData` object references to survive a normal rejection.
- Investigation: `CharacterBuffer.PrepareChanges` legitimately rebuilds immutable read views before publication, even when gameplay values are unchanged. Reference identity is not the rejection contract. The assertion was corrected to compare independent HP/AP, identity, area, slot, placement and occupancy values. No production code was changed to satisfy the test.
- Final run: `focused-final.xml` / `focused-final.log`, **88/88 passed**, exit **0**, zero skipped/inconclusive cases. The focused test-name set is unchanged from C1; C3 adds no cases and removes none.

| Fixture | Passed / total |
| --- | --- |
| CombatCommonPublicationTests | 9 / 9 |
| CombatCommandPublicationTests | 18 / 18 |
| CombatPreviewPublicationTests | 3 / 3 |
| CharacterBattleSettlementTests | 18 / 18 |
| CharacterRosterServiceTests | 10 / 10 |
| ArtifactEditingServiceTests | 30 / 30 |

The incoming combat-service changes, C2 test changes and ProjectSettings remained hash-identical to the C3 starting snapshot. Only the verified FMOD importer timestamp side effect was restored from exact incoming bytes. No commit/push was performed.

All six C3 checklist items are complete within fixture-backed scope. The full three-assembly suite was not rerun; C1 remains its last full baseline with five unresolved Economy failures. C4 terminal settlement/cleanup/next-battle integration, C5 final regression, authored DB readiness and View/Flow/Framework execution gates remain open.

## C4: Terminal settlement, cleanup and subsequent progression

Added `Assets/Tests/EditMode/Framework/CoreStageCombatCycleTests.cs` and its Unity `.meta`. C1 found no existing fixture that clearly expressed the entire cross-service lifetime; the plan's conditional file is used for that gap. Its setup remains local. No production service, Buffer, calculator, Factory, Job, asset or assembly definition was changed by C4.

The fixture exports the real Core and Buffer factories into `FrameworkRuntime`, runs InitData/PostBindService and resolves their registered public ports. Real roster initialization, Artifact initialization/placement, combat preparation/commands/settlement/cleanup and chapter progression all share the same canonical Buffers. The test does not directly install battle preparation or terminal completion, and does not call `ResolveStage` again after combat settlement.

### Inputs and responsibility boundaries

- A typed query double supplies two chapters, regular/boss/next-chapter Combat stage definitions, roster definitions and one healing Artifact definition. Legal graph edges are explicitly registered as inputs. Existing chapter-generation tests continue to cover real table/generator behavior.
- An injected `IBattleInitializationSource` supplies a deterministic 4x5 board, headquarters with 5 HP, supported attack profiles and explicit economy values. It deliberately reuses the same battle geometry across the three stage definitions. This verifies service lifetime and progression; C2's adapter tests cover data conversion. It does not prove successful default-source loading or distinct authored stage content.
- Initial persistent main HP 7, two main souls and wallet balance 91 are fixture values. Real Artifact placement provides a Chalice heal of 3 on stage clear. Bootstrap timing and authored balance remain external.
- The test submits commands and explicitly acknowledges/clears View input through its existing public port. One unhandled command is intentionally left through `EndBattle` and shown to remain present; the simulated external owner clears it before next preparation. Core alone does not prevent stale unhandled input from reaching a new battle.
- The priority case supplies a dead main through the health service and explicitly excludes its runtime placement through the existing Buffer permission. A real sub attack then destroys the headquarters and publishes defeat. This proves terminal-fact precedence through command publication; it does not claim that a production event kills both in one operation, or that bare `ApplyHealth(0)` is a complete death/placement/turn transaction.

### New witnesses and reused assertions

The new witness prefix is `ProjectL.Tests.Framework.CoreStageCombatCycleTests.`.

| Exact method/case | Real service evidence | Result / remaining boundary |
| --- | --- | --- |
| `RegisteredCycle_VictoryCleanupAndNextPreparationPreservePersistentState` | Validated entry -> service preparation -> confirmation -> enter combat/advance/move/attack -> terminal publication -> real stage/map progress -> service cleanup -> legal boss entry, then next chapter entry/preparation | Verified regular and boss victories, destroyed headquarters and final AP together. Only the boss clears map 1; its records survive entry into map 2. Chalice raises HP 7 -> 10 -> 12, capped at soul-adjusted max 12. Reprocessing/resolution/abandonment do not heal twice. Owned IDs, souls, currency and the Artifact aggregate remain stable; reinitialization rejects. |
| Same victory method, cleanup and re-entry boundaries | Captures completion, latest result object, sequence and handled number before `EndBattle`; checks them immediately after cleanup and after the next preparation | Verified cleanup removes active characters/links/field/preview and keeps completion/result/counters. Next preparation clears completion/preview while retaining result/counters. Main max HP 12 and attack 7 do not accumulate soul bonuses. Temporary bonuses start at zero. External acknowledgement and old-input cleanup are explicit fixture actions. |
| `RegisteredCycle_AbandonmentSettlesOnceBeforeCleanup` | Prepares through registered services; unfinished `EndBattle` throws; seeds a nonempty soul field and generates a preview; real abandonment records defeat once; service cleanup removes field/preview/runtime entities | Verified no stage/map clear or Chalice heal, HP remains 7, and no acknowledgement gate exists in Core. A direct subsequent service preparation reuses persistent state. This checks service reuse only; it does not define legal defeat retry, new Run or restart behavior. |
| `RegisteredResolution_MainDeathTakesPriorityOverDestroyedHeadquarters(False)` | Registered health change followed by real `ResolveBattle`, duplicate resolution and service cleanup | Verified main-death defeat, persistent HP 0 and no fabricated stage/map clear. |
| `RegisteredResolution_MainDeathTakesPriorityOverDestroyedHeadquarters(True)` | Real placement/turn commands select a sub; explicit dead/excluded main facts precede a real attack destroying the headquarters | Verified headquarters destruction and main death result in defeat, no Chalice resurrection or progress clear, and no repeated resolution. Expected completion is never seeded. |
| Existing `CharacterBattleSettlementTests.MainPersistentStatsSurvive_TemporaryBuffExpires`, `MultipleChalices_SettleOnceAndPersistHealedHealthAcrossCleanupAndNextPreparation`, and `CharacterRosterBattleCycleTests` | Existing calculator/Buffer cases retain their C1 setup boundaries | Verified nonzero temporary-buff expiry, multiple capped heals, soul ownership and entity reassignment without duplicating these cases in the service fixture. |

### Verification and fixture corrections

Evidence directory: `.superpowers/sdd/2026-10-03-core-stage-combat-run-integration-verification-plan/C4/`. It contains incoming hashes/copies, initial and final arguments, logs/XML/exit codes, `summary.json`, `witness-validation.json` and `preservation-check.json`.

Initial fixture iterations exposed missing test imports/constructor arguments, an invalid board whose two deployment zones consumed its entire height, and a dead-headquarters preparation rejected by existing validation. An attempted attack after only changing main HP also correctly failed the command placement invariant because the dead main still occupied the field. The final priority setup explicitly supplies excluded placement facts before the attack. These were test construction corrections, not production fixes; no validation was relaxed. `focused-5.xml` first passed 155/155. After adding immediate post-cleanup retention and nonempty-field/preview assertions, `focused-final.xml` again passed **155/155**, exit **0**, zero skipped/inconclusive cases.

| Fixture | Passed / total |
| --- | --- |
| CoreStageCombatCycleTests | 4 / 4 |
| BattleDefinitionAdapterTests | 19 / 19 |
| CombatCommonPublicationTests | 9 / 9 |
| CharacterBattleSettlementTests | 18 / 18 |
| CharacterRosterBattleCycleTests | 5 / 5 |
| CombatCommandPublicationTests | 18 / 18 |
| ChapterGraphPreparationTests | 40 / 40 |
| ArtifactEditingServiceTests | 30 / 30 |
| CombatStateBufferTests | 4 / 4 |
| MapProgressBufferTests | 8 / 8 |

All 151 existing focused case names match the union of C1 and C2 evidence; four cases are new. The final log contains no C# compilation failure. Its licensing token-refresh warning did not prevent the licensed test run from completing. All 30 protected incoming file hashes match, including the incoming combat refactor, C2/C3 test edits and ProjectSettings. Only a confirmed FMOD `lastScanTime` importer change was restored from exact incoming bytes. No commit/push was performed.

All eight C4 items are complete within the stated fixture/caller boundaries. C1 follow-up gaps 4-6 now have service-cycle witnesses and explicit external-input ownership evidence. C5's full regression and final responsibility audit remain pending. The last complete three-assembly baseline is still C1's 1300/1305 with five unresolved Economy authoring failures; C4's focused run neither includes nor resolves those failures. Default DB support, production Jobs/Flow/Runner timing, playable transitions and new-Run policy remain open.

## C5: Final regression, responsibility audit and handoff

### Fresh full-suite evidence

Unity `6000.0.73f1` ran all three requested EditMode assemblies once after the final C4 code changes. C5 changed only this report, the plan checklist and local evidence. No production or test repair was necessary after this run.

| Assembly | Passed / total | Failed |
| --- | --- | --- |
| ProjectL.Tests.Core | 918 / 918 | 0 |
| ProjectL.Tests.Framework | 383 / 388 | 5 |
| ProjectL.Tests.CombatViewBoundary | 6 / 6 | 0 |
| Total | 1307 / 1312 | 5 |

Exit code: **2**, reflecting the five failed tests. Skipped: **0**. Inconclusive: **0**. Exact test-name comparison retained all 1305 C1 cases and added only the seven documented C2/C4 cases, all passing. All 54 named C1 witnesses matched the final XML, totaling 69 passing cases. No case disappeared or was silently excluded.

The failure set is identical to C1 by fully qualified test name, complete message and stack trace:

- `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `DataBaseAssetTests.EveryStage_HasNodeVisual`
- `DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

All share the message `System.InvalidOperationException : [CombatStageDetail] Combat stage requires Economy.` They remain unresolved authored-data readiness failures, including the visual tests that stop while constructing the DB before visual assertions. No placeholder Economy assets, ignored tests or relaxed validators were introduced. The log has no C# compilation failure; its licensing token-refresh warning did not prevent the test run from completing.

Evidence directory: `.superpowers/sdd/2026-10-03-core-stage-combat-run-integration-verification-plan/C5/`.

- `final-args.txt` records the exact `-batchmode -nographics -runTests -testPlatform EditMode` invocation with `-assemblyNames ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary` and this checkout's project path.
- `final.xml`, `final.log`, `final.exit-code.txt`, `summary.json` and `failures.json` record the run and exact comparison with C1.
- `witness-validation.json`, `scope-audit.json`, `final-review.md`, `incoming-status.txt`, `incoming-hashes.json`, `verification-tests.patch` and `preservation-check.json` record coverage, attribution, review and preservation.

### Responsibility and diff audit

The package changes two existing test files (`BattleDefinitionAdapterTests.cs`, `CombatCommonPublicationTests.cs`), adds `CoreStageCombatCycleTests.cs` with its `.meta`, and maintains the plan/report. No Framework, Buffer, Core production, View/Flow/Job, assembly-definition, scene, prefab or authored gameplay asset change belongs to this package. The visible incoming `CombatProcessingService.cs` refactor remains hash-identical to C1; it is part of the tested checkout, not a change or independent refactor approval from this verification.

The existing production boundaries remain intact:

- Factory composition creates the concrete `ArtifactEditCalculator`; `ArtifactEditingService` consumes `IArtifactEditCalculation` from Contracts.
- Real services read the canonical Buffer facts, invoke calculations and publish proposals. Tests add no persistent service mirror or parallel state owner.
- `BufferSystemGroup.Apply` retains its prepare/validate/install publication role. Core owns gameplay calculation; this work adds no gameplay rule, reset authority or access port to Buffer.
- View/Flow retains input submission/removal, acknowledgement, scheduling, lifecycle and transition ordering. Test-driven service calls do not prove those production sequences.

All 32 protected incoming files retained their SHA-256 hashes after the run, including C2-C4 tests, the incoming combat refactor and ProjectSettings. The FMOD catalog differed only in importer `lastScanTime`; exact incoming bytes were restored after that comparison. No unrelated user changes were discarded. Evidence remains local and preserved; no commit, push, merge or checkout cleanup was performed.

### Independent review and final rulings

A fresh-context reviewer inspected the combined C1-C4 verification changes, source contracts, plan/report and focused XML. It found **no critical, important or minor actionable issue**. The parent separately verified the final full-suite XML, exit code and exact failure/name comparisons above. No deferred minor or repair pass remains.

The review set aside the following boundaries; C5 resolves each explicitly:

| Boundary | Final ruling and implication |
| --- | --- |
| Authored DB/default-source readiness | Keep external. Missing Economy references and approved Method_Key support remain open; fixture success cannot authorize production DB acceptance. |
| Production bootstrap and stage-entry ordering | Keep external. Fixtures supply definitions and caller ordering; preparing the intended unconfirmed target through the default source still needs an agreed real execution path. |
| Job/Plan/Runner, faults and transitions | Keep with View/Flow and Framework. Core must not duplicate their execution skeleton; F1/F2 remain open. |
| Stale input and result acknowledgement | Keep with the external owner. Unhandled input survives Core cleanup, so production lifecycle cleanup and presentation ordering still need execution evidence. |
| Simultaneous damage scheduling | Claim competing-fact precedence only. The priority fixture does not prove a production event generates both deaths atomically. |
| New Run/restart, save/crash recovery and Artifact reset | Keep deferred. Ordinary cleanup/re-entry tests do not define these policies. |
| Incoming combat refactor and FMOD changes | Preserve and attribute separately. Final tests measure the existing checkout; they do not imply a full review of unrelated incoming changes. |
| Final regression and C5 closure | Closed by the fresh parent-run evidence above. Known external failures are reported, not treated as passing cases. |

The decisions carried through C1-C5 are to verify the current checkout, reuse existing tests before extending them, inject explicit supported definitions while the default source is blocked, limit selection guarantees to caller ordering, use real registered services for the missing cycle, and keep external input/reset policy out of Core. Their limits are recorded with each witness. Local evidence is retained because it is an uncommitted verification deliverable.

All five C5 checklist items are complete. The handoff is **Core integration verified within fixture-backed scope**, with no new regression or actionable review finding. Actual authored DB readiness, production bootstrap, View/Flow/Framework execution and playable Stage/Run acceptance remain separate work; the external dependency table above assigns their owners.
