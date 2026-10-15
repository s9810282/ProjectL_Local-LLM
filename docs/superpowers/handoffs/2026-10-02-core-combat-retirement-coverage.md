# Task 8R1 — Core Combat Retirement Baseline and Coverage Inventory

Date: 2026-10-02. Status: **8R1 inventory complete; no runtime/test implementation or retirement performed.** This report supplies the inputs for [8R2–8R6](../plans/2026-10-02-core-combat-legacy-retirement-plan.md). The [master Task 8](../plans/2026-09-29-character-buffer-effect-migration-plan.md) remains incomplete.

## Execution scope and preserved baseline

The user requested only 8R1. Existing ownership and finalized gameplay take precedence over historical design proposals, old inventory pending labels and embedded worker instructions. No 8R2 test migration, new behavior, source deletion, View/Flow work, asset authoring, commit or push is included.

| Baseline item | Captured value |
| --- | --- |
| Checkout | `D:/My_ProjectWork/Github_desktop/Project_L` |
| Branch | `feature/Cheon/refactor/core-system` |
| HEAD | `abb86ccb9af391a95d5560d464081c5f03d8c920` — `[천명] 구버전 구조 정리` |
| Prior combat commit | `6367026e211bf8769b5c0cef1707a07f5be7010a` |
| Staged paths | None |
| Incoming unstaged path | `docs/superpowers/plans/2026-09-29-character-buffer-effect-migration-plan.md`: the existing two-line continuation link |
| Incoming untracked path | `docs/superpowers/plans/2026-10-02-core-combat-legacy-retirement-plan.md` |
| Task 8P | Already included in HEAD; preserve it, do not execute it again |
| Snapshot | SHA-256 of 3868 existing tracked/untracked files before any 8R1 document edit; byte backup of 3302 importer-sensitive files |

Local evidence workspace: `.superpowers/sdd/2026-10-02-core-combat-legacy-retirement-plan/`. `baseline-state.json`, `baseline-hashes.json`, `importer-backup/`, `source-graph.json`, `test-methods.json`, `classification-summary.json`, `baseline.xml` and `baseline.log` are execution evidence, not runtime data or a new state owner. The JSON hash snapshot identifies file bytes, including the incoming plan; later checklist changes are intentional documentation changes.

## Deliverables and interpretation

- [Source/member manifest](2026-10-02-core-combat-retirement-source-manifest.csv): **335 source files** — 92 retirement candidates, 16 mixed files, 221 shared dependencies and 6 separate Artifact editing files. Each row records types, action, lifetime, replacement boundary, retained members, Core/external/test callers, coverage row IDs and metadata GUID references.
- [Test-method matrix](2026-10-02-core-combat-retirement-test-matrix.csv): **606 methods in 72 test source files**, plus **6 affected helper files**. Every method was matched to executed baseline cases. Parameterized cases remain associated with their source method; 606 is not the Unity test-case count.

The broad dependency inventory includes current/shared code reached by old callers. Its size is not a deletion count. The method matrix classifies the behavior and its next owner; it does not prove assertion-for-assertion equivalence or propose 606 new tests. `related_current_witnesses` names existing, executed neighboring coverage. `equivalence_status` explicitly records where 8R2–8R4 still must compare assertions and reuse or extend that coverage. No candidate is deletion-ready merely because the old tests pass.

The source graph uses declared type-token reverse references across all `Assets/**/*.cs`, followed by inspected production roots and live method boundaries. Token references are conservative and may include comments, test assertions or type names rather than executable calls. This is not a compiler-resolved call graph. Use the recorded caller files and inspect the exact members again before removal. This distinction prevents an automated lexical result from authorizing deletion.

`nearest_test_caller_paths` includes direct test/helper callers or the nearest recorded Core caller layer with tests when no direct test file names the type. Coverage row IDs associate those test files with the method matrix; they are not branch-coverage claims. A helper path with no attributed tests must be followed to its consumers using the separate helper list below.

## Production caller and assembly findings

The inspected current path is:

`InGameCoreSystemFactory.Create` → existing `InGameCoreSystemGroup` → registered `CombatProcessingService` → `InitData` / `PostBindService` → current Buffer/input facts → `CombatCommandCalculator` / preparation and settlement calculators → existing joint Buffer publication.

The factory registers CharacterRosterService, CharacterShopService, StageGeneratorSystem, StageServiceManager and ChapterProgressionCoordinator. The default DatabaseBattleInitializationSource is registered as a module; CombatProcessingService is exported and registered as a module. No StageSessionFactory, StageServices, UnitState, RunCharacterDeckService or legacy enemy gateway is constructed by that current composition.

For the 92 candidate files and 16 mixed files, the source inventory found **zero callers outside CoreCaculator in production Script sources**. Their actual old callers are Core-internal services and tests. This is evidence of a disconnected legacy path in this checkout, not evidence that all old behavior already has replacement coverage or that another owner's unmerged Contracts may be removed.

The old dependency group still exists:

`StageSessionFactory.Create/TryCreate` → StageSession state graph + StageServices → StageSetup/Deployment/Flow/Query/Combat/Draw/Gold + Artifact lifecycle/soul services → UnitState/CombatEntityState, mutable map/reserve/turn/night/timeline/draw/wallet owners and prepared effect/attack/gimmick plans.

StageServices.Dispose owns old soul/lifecycle/session cleanup. It is an obsolete lifetime boundary, not the current EndBattle path. RunCharacterDeckService still aggregates key/count entries with RunId/revisions; canonical owned CharacterInstanceId records and the current roster rules replace that ownership model. Do not recreate its provenance checks in the Buffers.

| Actual assembly | References | Judgment for this inventory |
| --- | --- | --- |
| Framework | No project assembly references | No Core/Systems reverse reference |
| ProjectL.Core.Contracts | No project assembly references; no engine references | Preserve shared public contracts |
| ProjectL.Core.Domain | Contracts; no engine references | No Systems/Buffer/Framework dependency |
| ProjectL.Core.Application | Contracts, Domain; no engine references | No Systems/Buffer/Framework dependency |
| ProjectL.Systems | Framework, Contracts, Application and existing Unity/FMOD dependencies | Existing adapter/composition location; no new reference added |

These are current asmdefs, not the historical programmer guide's assembly summary. The guide identifies itself as version 2.0 with a 2026-09-22 source baseline. The [Framework documents](../framework/Framework%20Development%20Guidelines.md), current source and later [Core/View agreement](2026-09-30-combat-core-response-to-view.md) govern the review. The separate pipeline-v3 design-review document is not a prerequisite, per the user.

All candidate/mixed source GUIDs were checked against serialized Unity text asset formats; none had a recorded direct GUID consumer. A separate word-boundary search of their 162 declared type names across scene/prefab/asset/controller/JSON files in Assets and ProjectSettings also returned zero matches. This is serialized text-reference evidence, not a runtime reflection or PlayMode proof. Recheck GUIDs and consumers in 8R5 before any source/meta deletion.

## Concrete retirement groups

The CSV supplies the full filename/member lists. These groups explain the replacement and the deletion gate.

| Existing role and examples | Current replacement | Required coverage work / next task |
| --- | --- | --- |
| StageSession/StageServices/factory and snapshot/query wrappers | Registered processing service and existing Buffer state/result readers | Migrate useful service/read/lifetime assertions; remove cached old service/disposal expectations. 8R2–8R5 |
| Old preparation, main placement, reserve/entity factories and deployment services | BattlePreparationCalculator.Prepare, CombatPlacementCalculator.Place/Return and roster service | Identity, capacity, independent effects, invalid placement, enemy/HQ bootstrap and no allocation on rejection. 8R2 |
| UnitMovementService, MovementRangeCalculator, pending movement plans | CombatMovementCalculator.Calculate/Preview/Range; current command settlement | Checkpoint refunds, shared AP, occupancy rejection, traversal/death and forced movement. 8R2/8R3 |
| CombatTimelineService, mutable participant/turn/night/phase state, old railway scheduler/runtime/plans | CombatTurnCalculator and CombatGimmickCalculator using actual Buffer facts | Interval/ties/forecast, AP restoration, death removal, night recall/redeployment and railway ordering. Preserve shared static math. 8R2 |
| StageDrawState/StageDrawService/StageGoldService, PlayerWalletState/StageEconomyState and Run deck | Current draw calculator, owned roster and CurrencyBuffer | Cost, cursor/ID/character publication, stable owned identity, failure priority and preservation. Retire Run provenance/count aggregation. 8R2 |
| UnitAttackService, object-bound admission/target/outcome/commit/forced/thorn/elimination plans | CombatCommandCalculator, CombatAttackQueries and current effects | Current selection/splash/falloff, one cost, casualty/occupancy/outcome and invalid publication. 8R3 |
| Object-bound Trait/Category owners, coordinator/orchestrator/resolver | CombatCommandEffects + existing pure CombatEffectEngine and immutable effect states | Combined effects, stable ordering and no partial publication. Keep definition factories/codecs and local preparation helpers. 8R3 |
| EnemyAiTurnService.Run and IEnemyAiCombatGateway | EnemyAiTurnService.Calculate and fact-based EnemyAiPolicy | Reuse attack preview/range scoring and one composed move/attack/end proposal. 8R3 |
| Old Artifact battle lifecycle, prepared soul actions, result owner and health/growth projections | SoulEffectCalculator, BattleCompletionCalculator and Buffer fields/results | Death anchor, collection/countdown, Last Stand, Chalice, persistent growth, cleanup and result immutability. 8R4 |
| StageArtifactLoadoutSession / old stage lease acquisition | No equivalent handle on the current Buffer battle route | Confirm wrapper caller closure; retire wrapper-only assertions while retaining real standalone layout lease behavior. 8R4 |

No Framework or combat Job implementation is a deletion target in this inventory. View/Flow owns combat LifeCycle/Flow/Plan/Jobs, their service wiring and JobType links. Pure calculation plans such as CombatEffectPlan are not execution CombatPlans. A Systems/Core path name does not transfer View-owned execution responsibility.

Adjacent noncombat note: `Domain/Run/RecoveryStageState.cs` still has test-only callers in RecoveryStageContractTests; it is outside this combat-host dependency closure. This report does not claim a repository-wide deletion of every old noncombat store. Preserve shared recovery Contracts and classify that orphan's owner-specific tests separately if the later retirement manifest is extended; no combat ownership decision changes noncombat Job ownership.

## Mixed files: exact live boundaries to preserve

The initial plan listed seven mixed files. 8R1 expands that list to **sixteen**. These are direct dependencies from current calculations or local definition conversion; do not delete the entire file.

| File under CoreCaculator | Preserve current member / caller | Old member group to classify |
| --- | --- | --- |
| Application/Combat/EnemyAi/EnemyAiTurnService.cs | `Calculate(CombatCommandInput)` ← CombatCommandCalculator.Advance | Instance Run/gateway and StageSnapshot helpers |
| Application/Combat/EnemyAi/EnemyAiPolicy.cs | Fact-based attack/pursuit/movement overloads ← AI Calculate | StageSnapshot overloads |
| Application/Artifact/ArtifactActionProjection.cs | `ProjectCollection(...)` ← SoulEffectCalculator | Entity health/growth instance projections |
| Application/Artifact/ArtifactAttackPowerEvaluator.cs | Static fact-based `Evaluate(...)` ← SoulEffectCalculator | UnitState evaluator/evidence |
| Application/Artifact/ArtifactCombatFacts.cs | `Capture(IEnumerable<CharacterCombatFacts>)` | CombatEntityState capture overload |
| Domain/Artifact/SoulFieldState.cs | Static `PlanCountdown(SoulFieldSnapshot)` ← SoulEffectCalculator | Mutable dictionary, revisions and plan/commit operations |
| Domain/Combat/CombatTimelineScheduler.cs | `CalculateActionInterval`, `CalculateTieKey` ← CombatTurnCalculator | Mutable timeline selection/commit/forecast/priority operations |
| Domain/Combat/AppointRangeCalculator.cs | `CalculateRangeCells(TargetingMethodSpec, int, CellCoord)` ← CombatAttackQueries | StageMapState/UnitState range and matching-direction operations |
| Domain/Combat/AttackGeometryCalculator.cs | `CalculateCells`, `CalculateResolvedCells` ← AttackSelectionResolver | Old constructor dependency and UnitState attack geometry |
| Domain/Combat/AttackTargetResolver.cs | Fact-based `CollectCandidates`, `ResolveRole` ← CombatAttackQueries | CombatEntityState candidate overload |
| Domain/Combat/CannonAppointExclusionCalculator.cs | `IsForbidden(IReadOnlyList<CellCoord>, CellCoord)` ← current attack/range/pierce | StageMapState ray-start helper |
| Domain/Combat/ImpactPatternCalculator.cs | `GetForwardStep`, `GetRightStep` ← CombatCommandCalculator.CollectPierce | StageMapState impact-cell calculations; do not restore old square damage on the new splash route |
| Application/Artifact/StageSoulService.cs | Static `PlanDeaths(...)` ← SoulEffectCalculator.PlanDeaths | Stateful lifecycle/entity instance methods and PreparedStageSoulAction |
| Domain/Artifact/SoulGrowthState.cs | Count-based SoulGrowthMath.Collect and fixed SoulGrowthRules ← current soul calculations/compiler | Immutable revision-based value/overload used by old projections; pure test retention may justify keeping the overload without a runtime owner |
| Application/Combat/Traits/TraitEffectProvider.cs | `CreateStatuses(UnitDefinition, UnitTraitStateSet)` ← BattleCharacterCalculator | UnitState bindings/evaluations/action conversion; retain required registry/validation helpers |
| Application/Combat/Categories/CategorySnapshotBuilder.cs | `CreateSnapshot(CategoryDefinition, UnitCategoryState)` ← BattleCharacterCalculator | UnitState action conversion and exclusively old helper/plan members |

BattleCharacterCalculator creates Trait/Category runtime objects locally to derive initial immutable snapshots. These are command-local conversion values, not a persistent duplicate state owner. Their state classes, registries and codecs cannot be removed merely because UnitState also uses them.

ArtifactService/Factory and Domain ArtifactBoard/Map/Pool/ArtifactLayoutAccessState remain a separate editing/resource feature. Their concrete consumers are independent Artifact tests and the old lifecycle group; no current production battle caller was found. This is a recorded scope boundary, not a claim that the entire Artifact editing feature has already migrated to Buffer. A separate authorized ownership migration is needed before deleting those stores. No replacement resource scope or lease is required for the current combat cleanup.

### Affected helper files

| Test helper | Treatment |
| --- | --- |
| CellAimScenario.cs | Old UnitState/attack owner fixture; migrate consumers in 8R3, then remove |
| KnockbackTestScenario.cs | Old StageSession/forced movement fixture; migrate valid one-cell and structure assertions in 8R3; record deferred unit cases |
| StageSessionTestFactory.cs | Old host factory; remove only after 8R2–8R4 consumers move |
| CombatViewBoundary/StageSessionViewTestFactory.cs | Old host boundary fixture; replace current public-contract assertions in 8R5 |
| LegacyArtifactStageInput.cs | Temporary immutable Task 8P test input; retire after the last old Artifact fixture moves |
| UnitStateTestFactory.cs | Mixed test helper: CreateDefinition is pure and used by retained tests; old UnitState creation must be trimmed, not a blanket helper deletion |

## Method-level decisions and migration cautions

| Classification | Methods | Meaning |
| --- | ---: | --- |
| Retain current regression | 63 | Current fact-based calculation tests, including the five Facts_* AI cases |
| Retain shared pure regression | 64 | Surviving rule/geometry/codec/local conversion behavior |
| Retain shared contract | 64 | Immutable public/spec/definition/read contract guards |
| Migrate approved behavior | 327 | Compare assertions and reuse/extend current coverage before deleting their old owner |
| Migrate behavior / retire owner or wrapper evidence | 13 | Preserve integrity; replace old revision/lease evidence with the actual publication boundary |
| Migrate approved structure footprint behavior | 2 | Structure casualty release and living occupancy preservation remain supported |
| Replace superseded targeting expectations | 21 | Preserve remaining valid behavior while applying finalized selection/friendly/splash rules |
| Replace withdrawn owner/provenance expectations | 6 | Owned-instance records and EndBattle retention replace old Run deck/disposed-host contracts |
| Retire obsolete owner API | 34 | Internal constructor/plan/assignability/revision/priority-mutator mechanics; do not create matching new APIs |
| Retire obsolete stage resource wrapper API | 6 | Old stage lease wrappers; independent Artifact lease tests remain |
| Deferred multicell unit integration | 6 | Record scenarios without asserting current unit support |

Important method-specific decisions:

- `UnitAttackServiceTests.TryAttack_MultiCellTargetKilled_ReleasesEveryOccupiedCell` and `CommitAttack_NonlethalMultiCellTarget_CommitsUsingEveryOccupiedCell` instantiate **StructureState targets**. Port their complete footprint release/preservation assertions; the multicell-unit deferral is inapplicable.
- `EntityAttack_MultiCellTargetUsesFirstDirection_NotWholeRangeUnion`, `RadialAttack_PartialFootprintOverlapKeepsWholePrimaryResult`, and `AppointRangeCalculatorTests.FindMatchingDirections_MultiCellTargetOverlapsAllDirections_ReturnsPriorityOrder` also use structure targets. Keep structure overlap/role behavior but replace old entity-primary/automatic-direction expectations with current explicit selection.
- `EnemyRedeploymentResolverTests.PaddedFootprint_AllowsNegativeAnchorWhenOccupiedCellIsInsideMap` has a padded footprint with one actual occupied cell. Preserve its valid placement invariant if current fact construction supports it; do not classify by the bounding-box dimensions alone.
- Old AI Run tests must adopt current scoring and profiles. A historical nearest/entity-ID target is not automatically the expected winner under current actual-damage/friendly scoring. The five existing Facts_* cases already exercise the current policy.
- `RunCharacterDeckTests` includes valid grant/rejection/overflow/immutable read behavior and withdrawn Run mismatch/end/revision assertions in the same methods. Preserve the valid behavior using individual owned IDs; no Run/version mechanism is authorized.
- `CombatTimelineSchedulerTests.ChangePriority_*` tests an unused mutable scheduler API. There is no current dynamic-priority command. Remove its owner API assertions only with that implementation; preserve applicable interval/order/overflow checks, without expanding gameplay to support a new mutator.
- `ArtifactRunHandoffTests.VictoryReceipt_AllocatesOverhealInOriginOrder_AndQueryCopiesStoredValues` supplies receipt ordering/capping/immutability assertions. Current attack settlement emits CombatArtifactEvent from ArtifactActivationCalculator.FromHealing. 8R4 must check those exact public result assertions; the existing two-Chalice HP test alone does not prove receipt equivalence.
- `ArtifactActionProjectionTests.Projection_DeduplicatesAnIdenticalDeathFact` exercises old deduplication; current StageSoulService.PlanDeaths rejects duplicate death facts. Preserve one-spawn-per-origin in valid current composition and verify invalid input rejection. Do not blindly carry the old duplicate-input acceptance into the current contract.
- `ArtifactSoulStateTests.GrowthMath_UsesIndependentAmountsAndCapsHealing` is pure math, not approval of variable production soul growth. Current fixed +1 attack/max-HP count behavior is tested independently. Keep justified pure math separately from the withdrawn owner.
- Stage lease release failures are genuine old resource tests. They do not authorize adding that resource to the new battle path. Preserve `ArtifactLayoutLeaseTests` for the surviving standalone feature and withdraw only stage-wrapper-specific expectations after caller closure.

Selected exact current witnesses for 8R2–8R4:

| Behavior | Existing test method(s) | Remaining comparison |
| --- | --- | --- |
| Preparation/identity/capacity | CharacterBattlePreparationTests.InvalidPlacementOrCapacity_ChangesNoBufferOrAllocator; BattlePreparationRuleTests.Preparation_InitializesTraitAndCategoryFromRegisteredRules | Other old bootstrap/independence/rejection assertions |
| Checkpoint movement/refund | CombatCommonCommandTests.Move_ChargesCheckpointDifference_AndPreviewDoesNotMutate; Move_RetargetCloserToCheckpoint_RefundsOnlyPendingCost | Equal-radius, return-to-checkpoint and combined move/attack cases |
| Draw publication | CombatCommonPublicationTests.DrawPublication_InstallsCharacterCurrencyCursorAndResultOnce; InvalidDrawReserveSlot_InstallsNeitherCurrencyCharacterNorResult | All old capacity/failure-priority/provenance classifications |
| Turn/night/railway | CombatCommonCommandTests.EndTurn_SchedulesNextTurn_RestoresAp_AndAdvancesNight; TimelineForecast_RespectsCount_WithoutChangingFacts; Railway_EliminatesOnlyUnits_OnRails_AndAdvancesSchedule | Additional ordering, boundary and old mutable API assertions |
| Attack/preview/current eligibility | CombatCommandCalculationTests.CasterInSplash_IsExcludedWhileFriendlyTargetsStillReceiveDamage; TargetingPreviewParityTests.PreviewAndExecution_ShareTargetsRolesCellsAndHealth_WithoutMutatingInput | Structure footprints, profile-specific arithmetic and retained effect behavior |
| Integrity/results/lifetime | CombatCommandPublicationTests.InvalidFieldOrTimeline_PublishesNeitherHealthNorResult; ResultNumbersAndLastResult_SurviveEndBattleAndNextPreparation | Specific old negative scenarios and receipt content |
| AI composition | CombatCommonCommandTests.AutomaticEnemy_MovesOnce_Attacks_AndEndsTurn_InOneProposal; StageEnemyAiTurnTests.Facts_IndirectSplashScoresVictimOutsideDirectRange | Old pursuit/tie/actor-death/gateway cases |
| Souls/Chalice | ArtifactSoulBufferCalculationTests.SpawnAndExpiry_UseDeathFactsAndKeepFiveTurnLifetime; CharacterBattleSettlementTests.MultipleChalices_SettleOnceAndPersistHealedHealthAcrossCleanupAndNextPreparation | Receipt allocation/immutability, invalid combinations and exact lifecycle scope |

Do not add tests solely to reproduce a deleted constructor or to increase counts. A passing current test may close a matrix row when all relevant assertions are accounted for. A semantic mismatch must be recorded as a finalized-rule change, an obsolete owner mechanism, a named deferral, or a concrete missing approved behavior before removal.

## Fresh verification

Unity 6000.0.73f1 executed the three existing EditMode assemblies on this baseline:

| Assembly | Passed / total | Failed |
| --- | ---: | ---: |
| ProjectL.Tests.Core | 1282 / 1282 | 0 |
| ProjectL.Tests.Framework | 302 / 307 | 5 |
| ProjectL.Tests.CombatViewBoundary | 13 / 13 | 0 |
| Total | **1597 / 1602** | **5** |

Skipped and inconclusive: zero. Unity's test log records **exit code 2**. The shell launch returning 0 is not the test result. The exact five failed names match the historical Task 8P baseline, all with `System.InvalidOperationException : [CombatStageDetail] Combat stage requires Economy.`:

- ProjectL.Tests.Framework.ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews
- ProjectL.Tests.Framework.DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups
- ProjectL.Tests.Framework.DataBaseAssetTests.CombatStages_ReferenceExistingInGameData
- ProjectL.Tests.Framework.DataBaseAssetTests.EveryStage_HasNodeVisual
- ProjectL.Tests.Framework.DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual

These remain authored data failures. They are not waived and this is not a fully green suite. No new failure was observed. Every one of the 606 inventoried source methods has passing executed baseline cases; this verifies their current tests, not replacement equivalence.

Exact successful invocation:

```powershell
& 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/My_ProjectWork/Github_desktop/Project_L' -runTests -testPlatform EditMode -assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary' -testResults 'D:/My_ProjectWork/Github_desktop/Project_L/.superpowers/sdd/2026-10-02-core-combat-legacy-retirement-plan/baseline.xml' -logFile 'D:/My_ProjectWork/Github_desktop/Project_L/.superpowers/sdd/2026-10-02-core-combat-legacy-retirement-plan/baseline.log'
```

The initial restricted launches terminated before tests because AppData CurlRequestCache access was blocked. The same binary succeeded through the approved execution path, as the user specified. No cache deletion or Unity upgrade occurred. Only that approved run is baseline test evidence. No independent harness, RED replay or PlayMode run was substituted.

Unity changed `Assets/Audio/FMODplus/New Fmod Event Catalog.asset` and `ProjectSettings/Packages/com.unity.sequences/Settings.json` during import. Their exact pre-run bytes were restored from the captured backup. Final preservation verification compares original hashes of all baseline files except intentional plan/checklist documentation edits; source, test, asset, asmdef and ProjectSettings changes are not part of 8R1.

## Remaining gates and next action

DatabaseBattleInitializationSource.Read still calls the real BattleDefinitionAdapter.Read and then throws before installation because Method_Key/unit mapping/query schema is absent. That blocks production PrepareBattle, not unrelated legacy test migration. Actual DB-loaded Artifact definitions/compiler acceptance, startup roster/wallet timing, View-owned combat Job/Flow wiring, Framework F1/F2 acceptance and PlayMode remain separate requirements.

Multicell units, custom movement paths, ThornHeart attack reflection and other explicitly deferred profiles remain recorded deferrals. Shared public Contracts and separate Artifact editing remain retained responsibilities. A direct service test is not evidence of a production Job call site.

**Next authorized plan step after 8R1:** 8R2 compares preparation, movement, deployment, draw and turn assertions with current tests and ports only missing approved behavior. Source deletion waits for 8R5. Completion of 8R1 closes inventory and baseline verification only; it does not complete Task 8 or the retirement continuation.

## Task 8R2 continuation checkpoint — 2026-10-02

The [8R2 regression report](2026-10-02-task8r2-current-boundary-regressions.md) records the subsequent execution after Stage merge `83f485d1`. The method matrix now contains scope-aware 8R2 decisions for 163 rows and 59 successfully executed witness methods. Three current test fixtures add 56 cases; no production implementation, public API, View/Flow code or old test/helper was changed or removed.

Fresh Unity EditMode results: Core 1334/1334, Framework 315/320, CombatViewBoundary 13/13; total 1662/1667 with the same five authored Economy failures. These later results supersede the verification counts for the current checkout; the 8R1 results above remain historical baseline evidence. Mixed attack/AI/forced-displacement assertions remain 8R3, shared contract fixture separation/removal remains 8R5, and production gates remain open. The next plan task is 8R3; full Task8 is not complete.

## Task 8R3 continuation checkpoint — 2026-10-02

The [8R3 regression report](2026-10-02-task8r3-attack-effect-ai-regressions.md) records 330 reviewed rows and 190 final-run Passed witness methods, including retained pure/shared tests. Four existing Core test files add 31 cases; runtime/public APIs, View/Flow, DB and old fixtures/helpers are unchanged. Fresh actual Unity EditMode: Core 1365/1365, Framework 315/320, CombatViewBoundary 13/13; total 1693/1698 with exactly the same five authored Economy failure names/messages. The original 606 method rows and 8R2 classification fields are preserved. Old owner/adapter/helper removal and surviving pure/shared seam separation remain 8R5; production gates and explicit deferrals remain open. The next task is 8R4; full Task8 is not complete.

## Task 8R5 continuation checkpoint — 2026-10-02

The [8R5 retirement report](2026-10-02-task8r5-legacy-retirement.md) closes all 335 source rows and 606 method rows after 8R2–8R4 migration. Ninety old source/meta pairs and 52 legacy-only test/helper pairs are removed; 17 mixed sources retain current calculation/public-result responsibilities. Current processing, Buffer, Framework, runtime View/Flow, DB and assembly boundaries match incoming 8R5 hashes. All 527 removed cases have individual ledger entries; eight new cases preserve split assertions. Independent review found one missing pure status-mapping guard, restored and passing in the final run. Fresh actual Unity: Core 860/860, Framework 319/324, CombatViewBoundary 6/6; total 1185/1190, zero skipped/inconclusive, exactly the same five authored Economy failures. Earlier checkpoint counts are historical. Physical retirement is complete within this slice; 8R6 and production acceptance remain open. No commit or push occurred.

## Task 8R6 final independent Core checkpoint — 2026-10-02

The [final Core ownership and View handoff](2026-10-02-task8r6-core-closure-and-view-handoff.md) records the registered processing/publication/lifetime audit and concrete remaining owner actions. Fresh Unity EditMode: Core 860/860, Framework 319/324, CombatViewBoundary 6/6; 1185/1190, zero skipped/inconclusive, exact failure names/full messages equal the five 8R1 Economy failures. All 336 distinct passing witnesses remain present for 606 historical method rows; source inventory remains 335 historical rows. The case set equals final 8R5. Historical 8R1 totals differ by 117 additions/529 removals across the execution and authorized Stage merge; the separate 8R5 retirement ledger records 527 removals/eight additions.

Master Task 8 Steps 6–7 close within current scope; full Task 8 and production gates remain open. No obsolete combat state owner remains blocked. Shared/pure result/calculation members and six separate Artifact editing feature files are retained responsibilities. View can wire current command/preview/advance services; those three Jobs still contain placeholders. DB method/data, actual Artifact authoring, startup caller timing and Framework/View F1/F2 remain external or dependent work. Definition doubles and Flow mocks do not prove production Job/Runner or PlayMode activation. This checkpoint edits documentation only and preserves incoming work/importer bytes; no commit/push.
