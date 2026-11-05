# Mixed Shop — Integrated Verification

Date: 2026-10-18  
Authorization: Phase E, following separately completed A-D.  
Status: automated evidence and handoff prepared; human manual acceptance is not confirmed. Full Framework limitations are recorded below.  
Plan: Implementation plan (Git history: `2026-10-18-mixed-shop-single-purchase-plan.md`)\
Contract/ownership handoff: [Core/View handoff](2026-10-18-mixed-shop-core-view-handoff.md).

## Result and acceptance boundary

- Complete Core: **3343/3343 passed**, no skips.
- Completed affected Framework selections: **892 passed, 3 failed, 0 skipped** across **895 distinct cases**. The three failures are the same real combat/line asset identities observed in C/D; every selected shop acceptance, inventory/new-Run, recovery, and shared transport case passed.
- The first full Framework attempt ended in a Unity native render crash with no final XML. The graphics-enabled retry completed: **5122 passed, 225 failed, 0 skipped out of 5347**, exit **2**. A full Framework green state is not claimed; all failed case identities are preserved below.
- No production C# or serialized asset correction was needed in E. This Phase modifies only the two planned smoke test sources and one new partial smoke source with metadata.
- Final item icons remain unassigned. Actual default recruit candidates remain all owned at the normal start. The user confirmed manual Play/readability checks have not been performed yet.

## New and migrated acceptance

`ShopStagePlaySmokeTests.MixedPurchase.cs` adds nine automated cases using typed UI input, actual shop service/rules, Buffer transactions, command processing, and popup controller/display:

1. One item purchase grants exactly one, debits the frozen price, clears the node, closes, and refreshes. Immediate UI repeat input is blocked. A deliberately queued second command waits for acknowledgment, then rejects without a second grant.
2. An unowned recruit with owned Sub levels 2/3/6 costs 600G from 700G, acquires at level 3, leaves 100G, and closes. The fixture contains an unowned ShotGun and does not alter production candidates.
3. Empty/one/two-offer visits all permit no-buy completion without gold, roster, or inventory writes (three cases).
4. Insufficient funds preserves the visit/offer object, gold, inventory, and roster and releases UI submission for ordinary retry.
5. A prior visit's same numeric slot cannot target a new visit or a new Run (two cases).
6. A committed purchase plus recoverable completion rejection projects locked products and submits only Complete on retry; inventory and gold do not change again. This case injects a rejected completion receipt after a real purchase to isolate recoverable display handling; executor rejection/exception ordering is additionally exercised by `ShopCommandExecutorTests`.

These are automated EditMode integration cases with fixture definitions and a functional UI fixture, not a claim of manual Boot/scene play. D's real prefab/scene/reference and View catalog tests are included in the final selection.

The existing explicit natural-map Play smoke was migrated to typed items, nullable icons, frozen price/level/quantity text, a zero-gold rejection before purchase, and zero-or-one purchase followed by automatic close. If natural funds are insufficient it records a skip-purchase path; it never changes production funds to fabricate affordability. The obsolete post-purchase second buy and three-second success timer checks were removed. This Play test **passed in the graphics-enabled full Framework run**. Its evidence is `Logs/ShopS52Verification/20261018-144730-713/evidence.txt`: the observed natural visit had 20G, so it exercised zero-gold rejection, no-buy completion, UI-service closure, retained map appearance, and the next combat entry with 60 frames free of runtime faults. Actual item/recruit purchase is proven by the separate 700G fixtures, not this natural visit. Screenshots were intentionally omitted by the existing batch-mode capture policy, so this is automated runtime evidence rather than human visual acceptance. The recorded generic completion line mentions purchase; the more specific branch record explicitly says no purchase occurred. After the run, that final log wording alone was clarified to say zero-or-one purchase. Assertions and production behavior did not change, so the full suite was not repeated for this logging-only correction.

`TemporaryCoreShopRuntimeTests` now places Main before using field-only health/commands. Its all-owned Akimbo candidate produces a valid empty assortment; the battle -> empty shop -> next battle regression verifies preserved facts, expected visit context, no-buy completion, and no gold change from shopping. The obsolete repeated-owned-recruit path was removed; the new unowned-recruit smoke owns purchase coverage. `RecoveryStageMapViewTests` and `StageMapDepthCenteringBootSmokeTests` required no shop-cardinality/constructor changes.

## Executed evidence

All paths below are under ignored `Library/CodexShopValidation/`. Unity **6000.0.73f1** was run with approved AppData access, one process at a time. No Unity upgrade, cache deletion, or production render setting change was used.

| XML/log stem | Result | Interpretation |
|---|---|---|
| `e-runtime-before` | 1 passed, 2 failed; exit 1 | Existing temporary runtime cases failed before shop entry at InvalidMainCharacter because Main had not been placed. |
| `e-acceptance-initial2` | 2 passed, 9 failed; exit 1 | The migrated runtime cases passed; new harness cases failed on a missing required restore input Layer. This was test setup failure, not a product behavioral red. |
| `e-acceptance-green2` | 11 passed, 0 failed/skipped; exit 0 | New nine integration cases plus two migrated runtime cases. |
| `e-core-full` | 3343 passed, 0 failed/skipped; exit 0 | Entire Core assembly. |
| `e-framework-full` | Native crash, exit 1 from the command wrapper; no XML | Entire Framework attempt with `-nographics`, incomplete. |
| `e-framework-graphics` | 5122 passed, 225 failed, 0 skipped; exit 2 | Completed entire Framework assembly with graphics enabled. |
| `e-scoped-final` | 662 passed, 3 failed, 0 skipped; exit 1 | Completed affected selection, including the full 42-case DB asset fixture. |
| `e-transport-final` | 230 passed, 0 failed/skipped; exit 0 | Additional shared B/C regression boundaries. |

`e-acceptance-initial.log` contains a test compile error from partial-class Click/Submit name shadowing; `e-acceptance-green.log` contains stale local variable references while migrating the old explicit smoke. Neither produced XML or establishes behavioral red. A transient local migration script lookup failed before writing the Boot smoke file and was corrected. No product fix was inferred from these setup/migration errors. New feature behavior already existed in A-D; no artificial failing product implementation was introduced to manufacture red evidence.

### Full Framework rendering limitation

`e-framework-full.log` records URP `DrawDepthNormalPrepass: Unable to find surface for attachment 0`, followed by `Crash!!!` and native frames `GfxDevice::DrawSharedGeometryJobs` / `GfxDeviceClient::DrawSharedGeometryJobs`. The process exited before XML publication. This is separate from the AppData CurlRequestCache error, which was avoided by approved execution. It resembles the earlier native rendering signature recorded in A, but no definitive root cause or version fix is claimed.

The retry uses the same version/code with `-batchmode -force-d3d11` and omits `-nographics`, to test whether a graphics-capable run can finish. No project asset/settings change is involved.

`e-framework-graphics.xml` / `.log`: total **5347**, passed **5122**, failed **225**, skipped **0**. Unity exit **2**. This is the completed graphics-enabled full Framework result; ShopStagePlaySmokeTests passed all 10 cases (nine new cases and the natural Boot Play case), and TemporaryCoreShopRuntimeTests passed 2/2.

### Final selected fixture identities

| Actual NUnit fixture | Passed | Failed | Selection |
|---|---:|---:|---|
| CombatItemIntegrationTests | 7 | 0 | Affected shop/assets |
| CombatItemInventoryTests | 12 | 0 | Affected shop/assets |
| DataBaseAssetTests | 39 | 3 | Affected shop/assets |
| MixedShopServiceTests | 19 | 0 | Affected shop/assets |
| OwnedLevelShopDataTests | 11 | 0 | Affected shop/assets |
| RecoveryCommandProcessingTests | 37 | 0 | Affected shop/assets |
| RecoveryNodeCommandTests | 20 | 0 | Affected shop/assets |
| RecoveryPopupControllerTests | 13 | 0 | Affected shop/assets |
| RecoveryStageMapViewTests | 11 | 0 | Affected shop/assets |
| RecoveryStageServiceTests | 80 | 0 | Affected shop/assets |
| RecoveryUITests | 21 | 0 | Affected shop/assets |
| RunGoldLifecycleTests | 2 | 0 | Affected shop/assets |
| RunTerminationAdmissionTests | 50 | 0 | Affected shop/assets |
| ShopCommandExecutorTests | 15 | 0 | Affected shop/assets |
| ShopConsumableVisualTests | 9 | 0 | Affected shop/assets |
| ShopEntryPublicationTests | 26 | 0 | Affected shop/assets |
| ShopPopupModelBuilderTests | 7 | 0 | Affected shop/assets |
| ShopPurchaseTransactionTests | 17 | 0 | Affected shop/assets |
| ShopStagePlaySmokeTests | 9 | 0 | Affected shop/assets |
| ShopStageServiceTests | 207 | 0 | Affected shop/assets |
| ShopUITests | 25 | 0 | Affected shop/assets |
| ShopVisitIdentityTests | 9 | 0 | Affected shop/assets |
| StageShopUIAssetTests | 4 | 0 | Affected shop/assets |
| TemporaryCoreShopRuntimeTests | 2 | 0 | Affected shop/assets |
| ViewPopupCompositionTests | 10 | 0 | Affected shop/assets |
| ArtifactEditingServiceTests | 53 | 0 | Shared transport |
| CharacterRosterServiceTests | 14 | 0 | Shared transport |
| InGameViewFaultCleanupTests | 14 | 0 | Shared transport |
| JobSystemTests | 11 | 0 | Shared transport |
| RecoveryEntryPublicationTests | 28 | 0 | Shared transport |
| RecoveryRequestLifetimeIntegrationTests | 20 | 0 | Shared transport |
| RecoveryTypedOperationTests | 48 | 0 | Shared transport |
| StageFlowTests | 30 | 0 | Shared transport |
| StageSelectInputBufferTests | 12 | 0 | Shared transport |

The shop controller and command tests in partial source files are counted in `ShopStageServiceTests`, not as separate filename-based fixtures. Selection details and full assertions are preserved in the XML. No additional Tool.Editor or CombatViewBoundary run was needed: E changes no editor tool, production assembly, or combat View boundary, and does not claim those suites from Core/Framework results.

### Known asset failures

1. `DataBaseAssetTests.EveryCombatStage_EnvironmentAndViewRotationsReachOperationalDefinition`: Stage 1101 expects 12 entries, gets 13 (extra 213).
2. `DataBaseAssetTests.EveryCombatStage_UsesRequestedNineByNineCenterRail_AndBoundTrainPrefab`: Stage 2101 expects 8, gets 0.
3. `DataBaseAssetTests.LineMaterial_IsRegisteredInCatalog_AndSupportsColorOverride`: `Assets/ProjectL/Script/Render/Shader/Line/LineShader.mat` is absent.

The remaining 39 DataBaseAsset cases passed, including real four-item prices/visual keys/catalog and the default all-owned recruit fallback. These three failures were already observed in C/D. There was no clean pre-feature full Framework baseline; they are not retrospectively declared pre-feature failures.

## Baseline comparison

Phase A recorded complete Core **3323/3323** and focused Framework **243/243**. Its initial pre-change baseline was incomplete because of concurrent compile changes, and its broader Framework attempt had a native rendering crash with no final XML. E's complete Core has 3343 cases and no failures; the increased test count includes concurrent work and is not attributed solely to shopping. E's focused failures match the three C/D observed asset identities. The completed graphics-enabled Framework run has 225 current failures. Without a clean full pre-feature Framework baseline, their regression count and feature attribution cannot be established. They span combat, camera/scene, legacy DB fixtures, and UI; the three known DB asset failures are included within the 225. No global green claim is made.

Historical evidence remains in the [A report](2026-10-18-mixed-shop-phase-a-verification.md), [B report](2026-10-18-mixed-shop-phase-b-verification.md), [C report](2026-10-18-mixed-shop-phase-c-verification.md), and [D report](2026-10-18-mixed-shop-phase-d-verification.md). Their XML/log paths are listed there and are not represented as new E executions.

## Manual and graphical acceptance ledger

The user explicitly confirmed in this chat that they have not manually checked these items yet. Automated behavior and source/asset inspection are separate evidence:

| Scenario | Automated/source evidence | Human manual confirmation |
|---|---|---|
| Default item-only stock | Real DB/loadout/sampler case and passing natural Boot visit | Pending |
| Deliberate unit/item content | Fixture projection and separate item/recruit transactions | Pending |
| Purchase closes and refreshes | Item/recruit smoke; popup controller tests; natural Boot no-buy close | Pending |
| Second input while close is delayed | Immediate input guard, held receipt, queued second command | Pending |
| Completion-only retry | Real acquisition + rejected receipt; executor failure tests | Pending |
| No-buy exit; 0/1/2 offers | Three smoke cases | Pending |
| Hidden/restored purchased/completed visit | Shop controller regressions | Pending |
| Readable missing-icon layout | Null-icon identity/text and prefab reference tests; no human visual check | Pending |

No screenshots or manual session are claimed. Final icons remain null for Whiskey/Roket/Rope/Lantern. The existing prefab heading still reads `캐릭터 상점`. Production mixed recruitment is not demonstrated because the authored recruit pool is owned at the normal start. These content/visual limits are not resolved by passing fixture tests.

## Scope and contract review

E changed only:

- `Assets/Tests/EditMode/Framework/ShopStagePlaySmokeTests.cs`.
- `Assets/Tests/EditMode/Framework/ShopStagePlaySmokeTests.MixedPurchase.cs` and `.meta` (new).
- `Assets/Tests/EditMode/Framework/TemporaryCoreShopRuntimeTests.cs`.
- The plan status and the two requested handoff/verification documents.

`phase-e/status-before.txt`, `head.txt`, `before-hashes.json`, and `sources.json` preserve preflight and final comparison. All production `.cs` and `.asset` hashes, including the already modified FMOD catalog and TMP fallback asset, match E preflight. Source and serialized asset churn were reviewed separately from pre-existing dirty A-D/concurrent changes. Changed C# files use UTF-8/LF and new metadata is present. Git status retained a modified indication on `Render/RendererFeatures/Systems/Stencil/TargetRenderTexture.renderTexture` after graphics execution, but its HEAD, index, and unfiltered working-file blob hashes are identical (`976df1a0951c6bb4eecf6e2d410fd625f2e385ad`), and its diff is empty. No content restoration or renderer edit was performed. `git -c core.autocrlf=false diff --check` passed.

Active purchase calls still require visit context; item text/prices are frozen Buffer facts, View icons remain View-only, permission derives from HasPurchase/progression/input lifecycle, and no View inventory/currency writes or copied prices were added. The L1-L4 ownership/locality review is in the accompanying handoff. No Core/gameplay contract change, scene/prefab rewrite, assembly reference expansion, commit, push, or external review/delivery occurred in E.

## Stop point

The automated E work and requested handoff are delivered with their limitations. Human manual acceptance remains open. The completed full Framework result remains red at 225 failures; the earlier incomplete attempt remains blocked evidence. Broad camera/combat/UI fixes are outside this shop Phase and were not implemented. No next-feature work or automatic integration is authorized by this report.

## Full graphics-enabled Framework failures

These are current-run observations, not a proven pre-feature failure baseline. No unrelated failure was silently fixed.

- `CameraEffectSystemTests.RenderFactoryExposesCameraGroupAndForwardsLifecycle`: System.InvalidOperationException : Loding service is required for CameraCatalogSystem.
- `ProjectL.Tests.Framework.AreaOutlineHandleTests.Update_WritesGridOutlineAndDisposeReturnsBothCoreHandles`: System.MissingMethodException : Default constructor not found for type ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem.AreaOutlineSystem
- `ProjectL.Tests.Framework.ArtifactDataTableTests.CurrentSoulDropAuthoring_CollectionAddsOneGrowthWithoutHealingOrMutatingInput`: System.ArgumentOutOfRangeException : Specified argument was out of the range of valid values. Parameter name: maxActionCost
- `ProjectL.Tests.Framework.ArtifactDataTableTests.CurrentSoulDropAuthoring_ExpiresAfterItsAuthoredUnitTurns(False,5)`: System.ArgumentOutOfRangeException : Specified argument was out of the range of valid values. Parameter name: maxActionCost
- `ProjectL.Tests.Framework.ArtifactDataTableTests.CurrentSoulDropAuthoring_ExpiresAfterItsAuthoredUnitTurns(True,4)`: System.ArgumentOutOfRangeException : Specified argument was out of the range of valid values. Parameter name: maxActionCost
- `ProjectL.Tests.Framework.ArtifactDataTableTests.CurrentSoulDropAuthoring_SpawnsOneSoulOnlyForNightEnemyDeath(True,Enemy,1)`: System.ArgumentOutOfRangeException : Specified argument was out of the range of valid values. Parameter name: maxActionCost
- `ProjectL.Tests.Framework.ArtifactDataTableTests.CurrentSoulDropAuthoring_SpawnsOneSoulOnlyForNightEnemyDeath(False,Enemy,0)`: System.ArgumentOutOfRangeException : Specified argument was out of the range of valid values. Parameter name: maxActionCost
- `ProjectL.Tests.Framework.ArtifactDataTableTests.CurrentSoulDropAuthoring_SpawnsOneSoulOnlyForNightEnemyDeath(True,Player,0)`: System.ArgumentOutOfRangeException : Specified argument was out of the range of valid values. Parameter name: maxActionCost
- `ProjectL.Tests.Framework.ArtifactDataTableTests.CurrentSoulDropAuthoring_SpawnsOneSoulOnlyForNightEnemyDeath(False,Player,0)`: System.ArgumentOutOfRangeException : Specified argument was out of the range of valid values. Parameter name: maxActionCost
- `ProjectL.Tests.Framework.ArtifactHealingChaliceIntegrationTests.CatalogChalice_ActualVictoryPublishesHealingOnceAndRetainsItInNextBattle(True,5,3)`: InvalidPrimaryTarget   Expected: True   But was:  False
- `ProjectL.Tests.Framework.ArtifactHealingChaliceIntegrationTests.CatalogChalice_ActualVictoryPublishesHealingOnceAndRetainsItInNextBattle(True,1,1)`: InvalidPrimaryTarget   Expected: True   But was:  False
- `ProjectL.Tests.Framework.ArtifactHealingChaliceIntegrationTests.CatalogChalice_ActualVictoryPublishesHealingOnceAndRetainsItInNextBattle(False,5,0)`: InvalidPrimaryTarget   Expected: True   But was:  False
- `ProjectL.Tests.Framework.AudioSystemTests.Global_ExportsAudioOnlyThroughGlobalContext`: System.Collections.Generic.KeyNotFoundException : [SystemServiceStore] Service was not found. Requested: IGlobalSystemGroupService
- `ProjectL.Tests.Framework.BattleDefinitionAdapterTests.StageEntry_PreparationFailureLeavesSelectionUnconfirmed(False)`: Expected: <System.InvalidOperationException>   But was:  null
- `ProjectL.Tests.Framework.BoomEffectPlaybackTests.OriginalBoom_NaturallyReturnsAndSupportsConcurrentReuse`: 루트와 네 자식이 실제로 방출돼야 합니다.   Expected: True   But was:  False
- `ProjectL.Tests.Framework.BoomEffectPlaybackTests.OriginalBoom_RepeatedPlaybackPreservesSizeUnderScaledParent`: 루트와 네 자식이 실제로 방출돼야 합니다.   Expected: True   But was:  False
- `ProjectL.Tests.Framework.BossCombatIntegrationTests.BossBattle_SummonActDayReturnAndWin`: InvalidMainCharacter   Expected: True   But was:  False
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.BossCleanup_ReentryRejected_ContinuationStillSucceeds(False)`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.BossCleanup_ReentryRejected_ContinuationStillSucceeds(True)`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.Composition_InitAndBindOrdersUseSameCoordinator(False)`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.Composition_InitAndBindOrdersUseSameCoordinator(True)`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.ConfiguredTarget_IsTheOnlyAllowedSuccessor(False)`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.ConfiguredTarget_IsTheOnlyAllowedSuccessor(True)`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.ContinuationFault_AfterSettlement_RetryOnlyCompleteChapter("route")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.ContinuationFault_AfterSettlement_RetryOnlyCompleteChapter("map:2")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.ContinuationFault_AfterSettlement_RetryOnlyCompleteChapter("stages:2")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.ContinuationFault_AfterSettlement_RetryOnlyCompleteChapter("root:202")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.FinalContinuationFault_RetriesWithoutSettlementReplay(False)`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.FinalContinuationFault_RetriesWithoutSettlementReplay(True)`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.InvalidDefinitionIdentity_RejectsBeforePublication("source-key")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.InvalidDefinitionIdentity_RejectsBeforePublication("source-map")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.InvalidDefinitionIdentity_RejectsBeforePublication("source-role")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.InvalidDefinitionIdentity_RejectsBeforePublication("target-key")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.InvalidDefinitionIdentity_RejectsBeforePublication("target-map")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.InvalidDefinitionIdentity_RejectsBeforePublication("target-role")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.InvalidDefinitionIdentity_RejectsBeforePublication("map-key")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.InvalidDefinitionIdentity_RejectsBeforePublication("route-key")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.LateLookupMutation_AddsNoContinuationWrites("route","selection")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.LateLookupMutation_AddsNoContinuationWrites("root:102","source")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.LateLookupMutation_AddsNoContinuationWrites("root:202","source")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.LateLookupMutation_AddsNoContinuationWrites("root:202","target")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.LateLookupMutation_AddsNoContinuationWrites("stages:2","terminal")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.LateLookupMutation_AddsNoContinuationWrites("root:202","battle")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.LateLookupMutation_AddsNoContinuationWrites("route","target")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.LateLookupMutation_AddsNoContinuationWrites("root:202","map")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.LateLookupMutation_AddsNoContinuationWrites("root:102","terminal")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.LateLookupMutation_AddsNoContinuationWrites("root:202","selection")`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.MissingDefinition_DoesNotMeanFinal(False)`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.MissingDefinition_DoesNotMeanFinal(True)`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.RegisteredTarget_IsValidatedAndReused(False)`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.RegisteredTarget_IsValidatedAndReused(True)`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.ChapterContinuationServiceTests.RetainedBattle_BlocksContinuationUntilCallerEndBattle`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CharacterActionCostPublicationTests.ActualBossAndSummon_PublishSixThroughAutomaticTurn`: System.ArgumentException : 중복된 키입니다 : Unit_Stage1Boss Parameter name: specs
- `ProjectL.Tests.Framework.CharacterActionCostPublicationTests.RegisteredAssets_ReachRealBattlePreparationAndBufferWithSix`: Expected: 7   But was:  9
- `ProjectL.Tests.Framework.CharacterStatsQueryServiceTests.InvalidOwnedDefinition_IsReportedNotHidden("MissingDefinition")`: Expected: instance of <System.Exception>   But was:  null
- `ProjectL.Tests.Framework.CombatAttackFeedbackTests.BlockerAndHoverPriority_RestoresAffectedRangeAndActorColors(False)`: Expected: RGBA(1.000, 0.550, 0.150, 1.000)   But was:  RGBA(1.000, 0.550, 0.150, 1.000)
- `ProjectL.Tests.Framework.CombatAttackFeedbackTests.BlockerContacts_UnionBoardCells_AndReplaceOrEmptyPreviousWalls(False)`: Expected: RGBA(1.000, 0.300, 0.300, 1.000)   But was:  RGBA(1.000, 0.300, 0.300, 1.000)
- `ProjectL.Tests.Framework.CombatAttackFeedbackTests.ClearAffected_ClearsOnlyCost_WhileClearHealthPreservesCostAndOtherLayers(False)`: Expected: RGBA(0.350, 1.000, 0.450, 1.000)   But was:  RGBA(0.350, 1.000, 0.450, 1.000)
- `ProjectL.Tests.Framework.CombatCodeMotionHitTests.Linear_FollowUpStartsAtBodyContact_BeforeProjectileArrives`: Expected: False   But was:  True
- `ProjectL.Tests.Framework.CombatCodeMotionHitTests.Linear_StructurePrimaryTarget_UsesStructureBody`: System.ArgumentOutOfRangeException : Specified argument was out of the range of valid values. Parameter name: 공격 가능한 건물임에도 공격력이 없습니다.
- `ProjectL.Tests.Framework.CombatCodeMotionHitTests.Melee_FollowUpShowsAtImpact_WhileAttackerReturns_ResultWaitsForFullEnd`: 전체 종료 전에는 결과를 확인하지 않습니다.   Expected: False   But was:  True
- `ProjectL.Tests.Framework.CombatCodeMotionHitTests.Melee_KnockbackStepFollowsHit_WhileAttackerStillReturns`: Expected: False   But was:  True
- `ProjectL.Tests.Framework.CombatCommonPublicationTests.RejectedReturn_KeepsAllGameplayAndRunGold`: Expected: 4   But was:  3
- `ProjectL.Tests.Framework.CombatDisplayReuseTests.ActionPanel_ButtonsReachEntriesThroughEventSystem_NotOnClick`: System.NullReferenceException : Object reference not set to an instance of an object
- `ProjectL.Tests.Framework.CombatDisplayReuseTests.ActionPanelReturn_NewVisibleLifetime_InvalidatesCachedQuote`: System.NullReferenceException : Object reference not set to an instance of an object
- `ProjectL.Tests.Framework.CombatDisplayReuseTests.ActionPanelReturn_QuoteAndVisibilityTogetherDecideButton_AndRebindReplacesEntry`: System.NullReferenceException : Object reference not set to an instance of an object
- `ProjectL.Tests.Framework.CombatDisplayReuseTests.AuthoredCombatScene_DragPlaybackAndReturn_UseActualPresenters`: UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatDisplayReuseTests.HandCards_OrderBySlot_CenterInRoot_ReuseCards_AndHideWithRoot`: System.InvalidCastException : Specified cast is not valid.
- `ProjectL.Tests.Framework.CombatDisplayReuseTests.HandCards_PickByScreenPoint_FrontCardWins_HiddenCardIsSkipped_DropZoneIsRoot`: System.InvalidCastException : Specified cast is not valid.
- `ProjectL.Tests.Framework.CombatDisplayReuseTests.HudFeedback_WarmRepeatedValues_DoNotAllocateStrings`: System.NullReferenceException : Object reference not set to an instance of an object
- `ProjectL.Tests.Framework.CombatEntryIntegrationTests.Abandon_IsIgnored_AfterARecordedOutcome`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CombatEntryIntegrationTests.Abandon_WhenQueuedAttackWinsFirst_PresentsVictoryAndKeepsRun`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CombatEntryIntegrationTests.ChapterVictory_WithoutContinuationDefinition_StartsNewRunThenReturnsToStage`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CombatEntryIntegrationTests.CompletionDisplayException_StopsWithoutEndBattleOrRetry`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CombatEntryIntegrationTests.Defeat_EndsRun_StartsNewRunThenReturnsToLobby`: 실제 처리로 패배 결과가 게시되지 않았습니다.   Expected: not null   But was:  null
- `ProjectL.Tests.Framework.CombatEntryIntegrationTests.DeferredCombatReturn_SharedFaultDoesNotRegisterStagePreparation`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CombatEntryIntegrationTests.DeferredCombatReturn_ShutdownClearsPendingPreparationWithoutRegisteringJobs`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CombatEntryIntegrationTests.DetachedCompletion_CannotAcknowledgeOrTransition`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CombatEntryIntegrationTests.FinalChapterVictory_RunCompleted_StartsNewRunThenReturnsToStage`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CombatEntryIntegrationTests.NextEntry_UsesNextTargetAndRetainsOldResultWithoutTreatingItAsAcceptance`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CombatEntryIntegrationTests.NormalCombatReturn_FastLoadAfterRunnerBudget_StillDrainsExitCheckAndEntersStage`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CombatEntryIntegrationTests.NormalExit_WaitsForActualRunnerToDrain_WithoutClearingRegisteredWork`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CombatEntryIntegrationTests.NormalExit_WaitsForFinalResultBeforeCompletionDisplay_WithoutClearingInput`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CombatEntryIntegrationTests.Shutdown_DuringPendingCombatReturn_DetachesManagerAndDropsExitCheck`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CombatEntryIntegrationTests.TransitionFailure_AfterEndBattle_DoesNotRetryOrDropCompletion`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CombatMortarAssetTests.ActualSceneAttackAndThornReuse_DoNotMutatePersistentProfileOrPrefabTransforms`: Expected: (0.75, 0.75, 0.75)   But was:  (0.50, 0.50, 0.50)
- `ProjectL.Tests.Framework.CombatMortarAssetTests.CatalogProfileAndNativeClones_KeepPersistentReferencesAndOriginalModels`: Expected: (0.75, 0.75, 0.75)   But was:  (0.50, 0.50, 0.50)
- `ProjectL.Tests.Framework.CombatMortarPresentationTests.LandingHitStartsDirectFollowUp_BeforeLongWeaponReturn`: 무기 복귀가 끝나기 전에는 결과를 확인하지 않습니다.   Expected: False   But was:  True
- `ProjectL.Tests.Framework.CombatMovementDraftDisplayTests.DraftHighlight_ChangesPathAndEndpoint_AndClearPreservesUnderlyingRange`: Expected: RGBA(0.050, 1.000, 0.750, 1.000)   But was:  RGBA(0.050, 1.000, 0.750, 1.000)
- `ProjectL.Tests.Framework.CombatMovementPlanningDisplayTests.DefaultAssetUsesOwnedTransparentGhostAndFiniteSettings`: System.NullReferenceException : Object reference not set to an instance of an object.
- `ProjectL.Tests.Framework.CombatMovementPlanningDisplayTests.EverySlotPreservesShaderTextureAndColor_HudColliderAndOriginalPoseStayUnchanged(False)`: Expected: RGBA(0.200, 0.400, 0.600, 0.520)   But was:  RGBA(0.200, 0.400, 0.600, 0.520)
- `ProjectL.Tests.Framework.CombatMovementPlanningDisplayTests.EverySlotPreservesShaderTextureAndColor_HudColliderAndOriginalPoseStayUnchanged(True)`: Expected: RGBA(0.200, 0.400, 0.600, 0.520)   But was:  RGBA(0.200, 0.400, 0.600, 0.520)
- `ProjectL.Tests.Framework.CombatMovementPlanningDisplayTests.GhostMovesContinuouslyAndKeepsOneRental_HidesUiAndCollider_AndResetsForPlacement(False)`: Expected: RGBA(0.450, 1.000, 0.550, 0.500)   But was:  RGBA(0.450, 1.000, 0.550, 0.500)
- `ProjectL.Tests.Framework.CombatMovementPlanningDisplayTests.GhostMovesContinuouslyAndKeepsOneRental_HidesUiAndCollider_AndResetsForPlacement(True)`: Expected: RGBA(0.450, 1.000, 0.550, 0.500)   But was:  RGBA(0.450, 1.000, 0.550, 0.500)
- `ProjectL.Tests.Framework.CombatPlacementFeedbackTests.GhostAndPlacementGrid_ChangeTogether_WithoutClearingCostOrHealth(False)`: Expected: RGBA(1.000, 0.400, 0.400, 0.500)   But was:  RGBA(1.000, 0.400, 0.400, 0.500)
- `ProjectL.Tests.Framework.CombatPlacementFeedbackTests.GhostAndPlacementGrid_ChangeTogether_WithoutClearingCostOrHealth(True)`: Expected: RGBA(1.000, 0.400, 0.400, 0.500)   But was:  RGBA(1.000, 0.400, 0.400, 0.500)
- `ProjectL.Tests.Framework.CombatPointerMappingTests.AfterRelease_LateAttackFeedback_DoesNotChangeReattachedScene`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatPointerMappingTests.AfterRelease_LateDraftAndReturnCalls_AreIgnored_AndUnbindStillAllowed`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatPointerMappingTests.AfterRelease_LatePlacementFeedback_DoesNotChangeReattachedScene`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatPointerMappingTests.AttackCostThenOtherActorDraft_ReplacesTheSharedHudCostPreview`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatPointerMappingTests.BuildFailure_UnbindsMapping_EvenWhenOldFieldStillExists`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatPointerMappingTests.Clear_UnbindsMapping_DropsUiCache_AndReinstallMapsAgain`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatPointerMappingTests.ClearAffectedCells_ClearsCostAndAffectedLayer_PreservesHealthPreview`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatPointerMappingTests.ClearDraft_ClearsCostAndDraftLayer_PreservesHealthPreview`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatPointerMappingTests.EventSystemReplacement_RecreatesDataBoundToNewSystem`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatPointerMappingTests.MissingOrDestroyedEventSystem_DropsCache_AndKeepsFieldMapping`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatPointerMappingTests.RaycastFailure_ClearsHits_AndNextCallCanReuseData`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatPointerMappingTests.Release_ClearsReferences_AndLateCallDoesNotRaycastOrRecreateData`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatPointerMappingTests.SameEventSystem_ReusesData_AndStillRaycastsEveryPosition`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatPointerMappingTests.SamePosition_UsesCurrentCamera_AndKeepsHandAndOutsideBoardMeaning`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatPointerMappingTests.UiAndNonUiHits_OnlyGraphicsCancel_AndSamePositionIsRechecked`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatPointerMappingTests.WarmMapping_DoesNotCreateMorePointerEventData`: SetUp : UnityEngine.UnassignedReferenceException : The variable worldCamera of CombatSceneRoot has not been assigned. You probably need to assign the worldCamera variable of the CombatSceneRoot script in the inspector.
- `ProjectL.Tests.Framework.CombatProgressIntegrationTests.Automatic_InvalidReadAfterRealPublication_ThrowsInTheJob(Missing)`: 연출이 끝났으므로 자동 진행이 등록됐다.   Expected: False   But was:  True
- `ProjectL.Tests.Framework.CombatProgressIntegrationTests.Automatic_InvalidReadAfterRealPublication_ThrowsInTheJob(Rejected)`: 연출이 끝났으므로 자동 진행이 등록됐다.   Expected: False   But was:  True
- `ProjectL.Tests.Framework.CombatProgressIntegrationTests.Automatic_InvalidReadAfterRealPublication_ThrowsInTheJob(WrongKind)`: 연출이 끝났으므로 자동 진행이 등록됐다.   Expected: False   But was:  True
- `ProjectL.Tests.Framework.CombatProgressIntegrationTests.Automatic_InvalidReadAfterRealPublication_ThrowsInTheJob(WrongSequence)`: 연출이 끝났으므로 자동 진행이 등록됐다.   Expected: False   But was:  True
- `ProjectL.Tests.Framework.CombatProgressIntegrationTests.Automatic_WaitsForEnterCombatPresentation_ThenConfirmsResultInTheSameExecution`: Expected: True   But was:  False
- `ProjectL.Tests.Framework.CombatProgressIntegrationTests.Command_AcceptanceAndRejection_BothWaitForPresentationBeforeNextCommand(False)`: Expected: X : 0, Y : 1   But was:  null
- `ProjectL.Tests.Framework.CombatProgressIntegrationTests.Completion_EndBattleAndNextPreparation_PreserveAcknowledgedResultAndNumbers`: System.InvalidOperationException : Nullable object must have a value.
- `ProjectL.Tests.Framework.CombatProgressIntegrationTests.Preview_ReplacedOrClearedBeforeExecution_UsesLatestRequestWithoutGameplayPublication(False)`: Expected: X : 0, Y : 1   But was:  null
- `ProjectL.Tests.Framework.CombatProgressIntegrationTests.Preview_ReplacedOrClearedBeforeExecution_UsesLatestRequestWithoutGameplayPublication(True)`: Expected: X : 0, Y : 1   But was:  null
- `ProjectL.Tests.Framework.CombatRailwayContactPlaybackTests.ContactOrderUsesExpectedCenter_AndCompletionWaitsForTrainAndEveryDeath(False)`: Expected: <empty>   But was:  < <ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.CombatCharacterEvent>, <ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.CombatCharacterEvent> >
- `ProjectL.Tests.Framework.CombatRailwayContactPlaybackTests.ContactOrderUsesExpectedCenter_AndCompletionWaitsForTrainAndEveryDeath(True)`: Expected: <empty>   But was:  < <ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.CombatCharacterEvent>, <ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.CombatCharacterEvent> >
- `ProjectL.Tests.Framework.CombatRailwayContactPlaybackTests.InvalidDirectDeathBlock_FailsBeforeAnyTrainOrDeathStarts(0)`: Expected: null   But was:  <System.Action`2[System.Single,System.Single]>
- `ProjectL.Tests.Framework.CombatRailwayContactPlaybackTests.InvalidDirectDeathBlock_FailsBeforeAnyTrainOrDeathStarts(1)`: Expected: null   But was:  <System.Action`2[System.Single,System.Single]>
- `ProjectL.Tests.Framework.CombatRailwayContactPlaybackTests.InvalidDirectDeathBlock_FailsBeforeAnyTrainOrDeathStarts(2)`: Expected: null   But was:  <System.Action`2[System.Single,System.Single]>
- `ProjectL.Tests.Framework.CombatRailwayContactPlaybackTests.InvalidDirectDeathBlock_FailsBeforeAnyTrainOrDeathStarts(3)`: Expected: null   But was:  <System.Action`2[System.Single,System.Single]>
- `ProjectL.Tests.Framework.CombatRailwayContactPlaybackTests.InvalidDirectDeathBlock_FailsBeforeAnyTrainOrDeathStarts(4)`: Expected: null   But was:  <System.Action`2[System.Single,System.Single]>
- `ProjectL.Tests.Framework.CombatRailwayContactPlaybackTests.InvalidDirectDeathBlock_FailsBeforeAnyTrainOrDeathStarts(5)`: Expected: null   But was:  <System.Action`2[System.Single,System.Single]>
- `ProjectL.Tests.Framework.CombatRailwayContactPlaybackTests.InvalidDirectDeathBlock_FailsBeforeAnyTrainOrDeathStarts(6)`: Expected: null   But was:  <System.Action`2[System.Single,System.Single]>
- `ProjectL.Tests.Framework.CombatRailwayContactPlaybackTests.LaterMalformedGroup_IsValidatedBeforeFirstTrainStarts`: Expected: null   But was:  <System.Action`2[System.Single,System.Single]>
- `ProjectL.Tests.Framework.CombatRailwayContactPlaybackTests.TwoPreparedGroups_KeepSeparateDeathConsumptionAndCompletion`: System.InvalidOperationException : 사망 시작 실패
- `ProjectL.Tests.Framework.CombatRailwayContactPlaybackTests.ZeroTargetsInitialContactAndImmediateFallback_StillWaitForTrain(0)`: Expected: 0   But was:  4
- `ProjectL.Tests.Framework.CombatRailwayContactPlaybackTests.ZeroTargetsInitialContactAndImmediateFallback_StillWaitForTrain(1)`: System.InvalidOperationException : 사망 시작 실패
- `ProjectL.Tests.Framework.CombatRailwayContactPlaybackTests.ZeroTargetsInitialContactAndImmediateFallback_StillWaitForTrain(2)`: System.InvalidOperationException : 사망 시작 실패
- `ProjectL.Tests.Framework.CombatRailwayContactSceneTests.ContactUsesExpectedPositionInsteadOfIdOrder_AndSettlementWaits(False,False,False)`: Expected: 2   But was:  12
- `ProjectL.Tests.Framework.CombatRailwayContactSceneTests.ContactUsesExpectedPositionInsteadOfIdOrder_AndSettlementWaits(False,True,False)`: Expected: 2   But was:  14
- `ProjectL.Tests.Framework.CombatRailwayContactSceneTests.ContactUsesExpectedPositionInsteadOfIdOrder_AndSettlementWaits(True,False,False)`: Expected: 2   But was:  16
- `ProjectL.Tests.Framework.CombatRailwayContactSceneTests.ContactUsesExpectedPositionInsteadOfIdOrder_AndSettlementWaits(True,True,False)`: Expected: 2   But was:  18
- `ProjectL.Tests.Framework.CombatRailwayContactSceneTests.ContactUsesExpectedPositionInsteadOfIdOrder_AndSettlementWaits(False,False,True)`: Expected: 2   But was:  20
- `ProjectL.Tests.Framework.CombatRailwayContactSceneTests.ContactUsesExpectedPositionInsteadOfIdOrder_AndSettlementWaits(False,True,True)`: Expected: 2   But was:  22
- `ProjectL.Tests.Framework.CombatRailwayContactSceneTests.ContactUsesExpectedPositionInsteadOfIdOrder_AndSettlementWaits(True,False,True)`: Expected: 2   But was:  24
- `ProjectL.Tests.Framework.CombatRailwayContactSceneTests.ContactUsesExpectedPositionInsteadOfIdOrder_AndSettlementWaits(True,True,True)`: Expected: 2   But was:  26
- `ProjectL.Tests.Framework.CombatRailwayContactSceneTests.InvalidResultFailsBeforeTrainStartsOrAudioPlays`: Expected: <empty>   But was:  < (UnitDead, 5), (UnitDead, 0), (UnitDead, 5), (UnitDead, 0), (UnitDead, 5), (UnitDead, 0), (UnitDead, 5), (UnitDead, 0), (UnitDead, 5), (UnitDead, 0)... >
- `ProjectL.Tests.Framework.CombatRailwayContactSceneTests.NoVictimsOrMissingProfile_WaitsForTrainWithoutDeadlock(False)`: Expected: 0   But was:  38
- `ProjectL.Tests.Framework.CombatRailwayContactSceneTests.NoVictimsOrMissingProfile_WaitsForTrainWithoutDeadlock(True)`: Expected: 2   But was:  40
- `ProjectL.Tests.Framework.CombatSceneEventPresenterTests.CancelDuringConcurrentSteps_EndsEveryLaneAndPlacement`: System.ArgumentException : 주 셀은 유일한 지정 셀이어야 합니다. Parameter name: designatedCells
- `ProjectL.Tests.Framework.CombatSceneEventPresenterTests.CancelDuringUnityVisibilityChange_DoesNotRestoreMovementOrCompleteEvent(False)`: Expected: 0   But was:  1
- `ProjectL.Tests.Framework.CombatSceneEventPresenterTests.CancelDuringUnityVisibilityChange_DoesNotRestoreMovementOrCompleteEvent(True)`: 표시 중 취소한 이동을 다시 진행 상태로 쓰지 않는다.   Expected: True   But was:  False
- `ProjectL.Tests.Framework.CombatSceneEventPresenterTests.FallAndRoar_CompleteOnceWithoutChangingUnits(Roared)`: Expected: 1   But was:  0
- `ProjectL.Tests.Framework.CombatSceneEventPresenterTests.KnockbackOfTwoTargets_FromOneHit_StepTogether`: System.ArgumentException : 주 셀은 유일한 지정 셀이어야 합니다. Parameter name: designatedCells
- `ProjectL.Tests.Framework.CombatSceneEventPresenterTests.NeutralObstacle_ShowsOnlyHealthHud_UpdatesDamage_AndReusesAfterDeath(Structure_PowderBarrel)`: Expected: True   But was:  False
- `ProjectL.Tests.Framework.CombatSceneEventPresenterTests.NeutralObstacle_ShowsOnlyHealthHud_UpdatesDamage_AndReusesAfterDeath(Structure_Supplies)`: Expected: True   But was:  False
- `ProjectL.Tests.Framework.CombatSceneEventPresenterTests.SharedHudLayer_ClearReusesHud_AndReleaseReattachRebindsPooledViews`: Expected: False   But was:  True
- `ProjectL.Tests.Framework.CombatSceneEventPresenterTests.StaggeredSteps_KeepEachOthersInterpolatedPlacement`: System.ArgumentException : 주 셀은 유일한 지정 셀이어야 합니다. Parameter name: designatedCells
- `ProjectL.Tests.Framework.CombatSceneEventPresenterTests.Summon_ShowsCreatedUnitAtEventWithoutAnimation`: Expected: 1   But was:  0
- `ProjectL.Tests.Framework.CombatSceneEventPresenterTests.WarmFactRefresh_AllocatesLessThanLegacyProjection_AndReusesViews`: 동일 RenderFacts 비용 외의 중복 투영/배열 생성을 비교한다.   Expected: less than 0   But was:  0
- `ProjectL.Tests.Framework.CombatServiceCompositionTests.InitAndBind_OrderDoesNotChangeBehavior`: InvalidMainCharacter   Expected: True   But was:  False
- `ProjectL.Tests.Framework.CombatServiceCompositionTests.Register_ExportsSixContractsAndSingleSharedLifetime`: InvalidMainCharacter   Expected: True   But was:  False
- `ProjectL.Tests.Framework.CombatStandardAttackAssetTests.OperationalVisualProfile_UsesOriginalKindAndNativeRepeatedAttack("ShotGun",True)`: Expected: RGBA(1.000, 0.720, 0.160, 1.000)   But was:  RGBA(1.000, 0.720, 0.160, 1.000)
- `ProjectL.Tests.Framework.CombatStandardAttackAssetTests.OperationalVisualProfile_UsesOriginalKindAndNativeRepeatedAttack("Sniper",True)`: Expected: RGBA(1.000, 0.720, 0.160, 1.000)   But was:  RGBA(1.000, 0.720, 0.160, 1.000)
- `ProjectL.Tests.Framework.CombatStandardAttackAssetTests.OperationalVisualProfile_UsesOriginalKindAndNativeRepeatedAttack("MainCharacter",False)`: Expected: same as <CombatAttack_Melee (ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation.CombatAttackPresentationSO)>   But was:  <CombatAttack_Linear (ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation.CombatAttackPresentationSO)>
- `ProjectL.Tests.Framework.CombatTimelineRetirementBootSmokeTests.Boot_NightCombatWithoutTimelineUI_PresentsAcceptedCommandsWithoutUiErrors`: Expected: greater than or equal to 2   But was:  1
- `ProjectL.Tests.Framework.ConnectedWallBootSmokeTests.DatabaseLiveBoot_DisplaysTwelveWallsAndFiveEnvironmentItems`: Expected: greater than or equal to 2   But was:  1
- `ProjectL.Tests.Framework.CoreTableQueryTests.DataBaseFactory_ExportsAllTenCoreQueries_AlongsideRender`: System.InvalidOperationException : Database of type StageMapDataTableSO not found.
- `ProjectL.Tests.Framework.CoreTableQueryTests.ImportedGroup_ExportsEveryRegisteredRow`: System.Reflection.TargetInvocationException : Exception has been thrown by the target of an invocation.   ----> System.InvalidOperationException : Database of type ChapterMasterDataTableSO not found.
- `ProjectL.Tests.Framework.CoreTableQueryTests.InGameFactory_RejectsMissingTable`: Expected: String containing "ChapterMasterDataTableSO"   But was:  "Database of type StageMapDataTableSO not found."
- `ProjectL.Tests.Framework.DataBaseAssetTests.EveryCombatStage_EnvironmentAndViewRotationsReachOperationalDefinition`: Stage 1101   Expected is <System.Int64[12]>, actual is <System.Collections.ObjectModel.ReadOnlyCollection`1[System.Int64]> with 13 elements   Values differ at index [12]   Extra:    < 213 >
- `ProjectL.Tests.Framework.DataBaseAssetTests.EveryCombatStage_UsesRequestedNineByNineCenterRail_AndBoundTrainPrefab`: Stage 2101   Expected: 8   But was:  0
- `ProjectL.Tests.Framework.DataBaseAssetTests.LineMaterial_IsRegisteredInCatalog_AndSupportsColorOverride`: 에셋을 찾을 수 없습니다: Assets/ProjectL/Script/Render/Shader/Line/LineShader.mat   Expected: not null   But was:  null
- `ProjectL.Tests.Framework.DrawPointLineSystemTests.Endpoint_MovesExistingEffect_EmptyPathStopsIt`: Expected: 1   But was:  0
- `ProjectL.Tests.Framework.DrawPointLineSystemTests.Shutdown_InvalidatesHandlesAndDestroysGameObjects`: Expected: 1   But was:  0
- `ProjectL.Tests.Framework.DynamiteHammerCombatTests.LethalHit_DoesNotPush`: System.InvalidOperationException : 처치된 적 3의 보상 정의가 없습니다.
- `ProjectL.Tests.Framework.EnvironmentAuthoringAssetTests.Catalog_ReferencesPlacedVisualsWhileKeepingUnplacedTerrainDefinitionsSeparate`: Expected: not collection containing "Assets/ProjectL/DataBaseSO/InGameGroup/Terrain/Item/Terrain_Tornado.asset"   But was:  < "Assets/ProjectL/Script/Render/Effect/Toneido/Shader/Dust.mat", "Assets/ProjectL/DataBaseSO/InGameGroup/Field/Item/Field_9x8.asset", "Assets/ProjectL/Prefabs/Default/Environment/Materials/Cube_Supplies.mat", "Assets/ProjectL/DataBaseSO/InGameGroup/Economy/StageEconomyDataTable.asset", "Assets/
- `ProjectL.Tests.Framework.EnvironmentEntryPublicationTests.AuthoredTornadoTiming_DefaultSourceCapturesStageSettingsInPreparedBuffer`: System.InvalidOperationException : G7: 클리어 보상 정의가 없습니다.
- `ProjectL.Tests.Framework.EnvironmentEntryPublicationTests.DefaultSource_PreparesBattleFromDatabase_WithEnvironmentAndProfiles`: System.InvalidOperationException : G7: 클리어 보상 정의가 없습니다.
- `ProjectL.Tests.Framework.EnvironmentEntryPublicationTests.InitialRandomTornado_SoAuthoringPublishesDrawnSourceThroughDatabaseSource`: System.InvalidOperationException : G7: 클리어 보상 정의가 없습니다.
- `ProjectL.Tests.Framework.EnvironmentStageAuthoringTests.StageSO_AuthorsAllFourBundlesAndPreservesObjectOrder`: System.ArgumentException : 벽은 활성 상태이며 양측에 공개되어야 합니다. 원본: 3@(2,0) Parameter name: environment
- `ProjectL.Tests.Framework.GlobalUITests.GlobalCanvasPrefab_RetainsFourOwnedCardsAndRegisteredGroupAcrossRebinds`: Expected: (1.00, 1.00, 1.00)   But was:  (0.33, 0.33, 0.33)
- `ProjectL.Tests.Framework.HojinCombatAttackDragIntegrationTests.Boot_AttackClickAndDrag_PreservesPresentationContract`: 결과 표시 확인   Expected: True   But was:  False
- `ProjectL.Tests.Framework.HojinCombatTornadoTerrainAssetTests.CombatMaterials_AreIndependentCopiesWithPaddingScaledFromWrapper`: _FlowSpeed   Expected: 2.0f   But was:  -5.0f
- `ProjectL.Tests.Framework.LobbyBootSmokeTests.Boot_AllStartModesInitializeCatalog_CombatFixture`: System.ArgumentException: 유닛의 키가 레지스터에 없습니다 : Unit_CactusCannon Parameter name: key   at ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels.UnitCatalog.GetRequired (ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.EntityKey key) [0x0002b] in D:\My_ProjectWork\Github_desktop\Project_L\Assets\ProjectL\Script\Systems\RuntimeSystem\InGameSyst
- `ProjectL.Tests.Framework.LobbyBootSmokeTests.Boot_AllStartModesInitializeCatalog_Lobby`: Expected: same as <Main Camera (UnityEngine.Camera)>   But was:  <LobbyOutputCamera (UnityEngine.Camera)>
- `ProjectL.Tests.Framework.LobbyBootSmokeTests.BootToLobby_ShowsSpaceWithoutStartingRun`: Expected: same as <Main Camera (UnityEngine.Camera)>   But was:  <LobbyOutputCamera (UnityEngine.Camera)>
- `ProjectL.Tests.Framework.LobbyBootSmokeTests.LobbyStart_IgnoresCombatDevProgress`: Expected: same as <Main Camera (UnityEngine.Camera)>   But was:  <LobbyOutputCamera (UnityEngine.Camera)>
- `ProjectL.Tests.Framework.LobbySceneAssetTests.Boot_DefaultStartsLobby_AndRetainsSerializedCombatAndStageValues`: 시작 enum의 Lobby 값 2와 씬 번호 1을 혼동하면 안 됩니다.   Expected: 2   But was:  0
- `ProjectL.Tests.Framework.RecoverySceneConnectionTests.StageSelect_RecoveryOverlayKeepsExistingMapAndOutputCameraConfiguration`: Expected: property Length equal to 1   But was:  0
- `ProjectL.Tests.Framework.StageEnvironmentAdapterTests.Adapter_UsesRequestedStagesLayoutOnSharedField`: System.InvalidOperationException : G7: 클리어 보상 정의가 없습니다.
- `ProjectL.Tests.Framework.StageEnvironmentAdapterTests.LegacyStage_ProducesEmptyEnvironment`: System.InvalidOperationException : G7: 클리어 보상 정의가 없습니다.
- `ProjectL.Tests.Framework.StageEnvironmentAdapterTests.MinePlacement_PreservesExplicitDamageVisibilityAndAuthoredOrder`: System.InvalidOperationException : G7: 클리어 보상 정의가 없습니다.
- `ProjectL.Tests.Framework.StageEnvironmentAdapterTests.ObjectOrder_UsesAuthoredListAndPassesThroughBattleDefinitionAdapter`: System.InvalidOperationException : G7: 클리어 보상 정의가 없습니다.
- `ProjectL.Tests.Framework.StageEnvironmentAdapterTests.ObstaclePlacement_PassesThroughActualBattleDefinitionAdapter`: System.InvalidOperationException : G7: 클리어 보상 정의가 없습니다.
- `ProjectL.Tests.Framework.StageEnvironmentAdapterTests.ProductionAdapter_PreservesIndependentTornadoTimingAndIdentity`: System.InvalidOperationException : G7: 클리어 보상 정의가 없습니다.
- `ProjectL.Tests.Framework.StageEnvironmentAdapterTests.StaticTerrain_StageAdapterPreservesKindIdentityAndVisibility`: System.InvalidOperationException : G7: 클리어 보상 정의가 없습니다.
- `ProjectL.Tests.Framework.StageEnvironmentAdapterTests.WeakGroundAndCliff_PassThroughActualBattleDefinitionAdapterWithFreshCounts`: System.InvalidOperationException : G7: 클리어 보상 정의가 없습니다.
- `ProjectL.Tests.Framework.StageLegendBootSmokeTests.Boot_OpensLegendBlocksItsAreaAndRestoresItAfterSceneRoundTrip`: System.ObjectDisposedException : The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.
- `ProjectL.Tests.Framework.StageLegendUITests.AuthoredLegend_UsesHeaderBadgeAndFiveIconTextRowsWithSharedSprites`: System.NullReferenceException : Object reference not set to an instance of an object
- `ProjectL.Tests.Framework.StageMapCameraBootSmokeTests.EmptyBackgroundDrag_MovesOutputCameraAlongMapAxis`: System.ObjectDisposedException : The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.
- `ProjectL.Tests.Framework.StageMapCameraBootSmokeTests.ExtremeBackgroundDrags_StopAtBossAndStart`: System.ObjectDisposedException : The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.
- `ProjectL.Tests.Framework.StageMapCameraBootSmokeTests.PressOnUiOrNode_DragToEmptyBackground_DoesNotPanOrRequestEntry`: System.NullReferenceException : Object reference not set to an instance of an object
- `ProjectL.Tests.Framework.StageMapCameraBootSmokeTests.ShopPopup_CancelsHeldMouseDrag_AndPreservesPannedMapPose`: Unhandled log message: '[Exception] ObjectDisposedException: The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.'. Use UnityEngine.TestTools.LogAssert.Expect
- `ProjectL.Tests.Framework.StageMapDepthCenteringBootSmokeTests.BootGraph_BackgroundDragPreservesCenteredMapAndVisibility`: System.ObjectDisposedException : The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.
- `ProjectL.Tests.Framework.StageMapDepthCenteringBootSmokeTests.BootGraph_CentersEveryDepthAndKeepsLinksOnActualNodes`: System.ObjectDisposedException : The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.
- `ProjectL.Tests.Framework.StageMapDepthCenteringBootSmokeTests.BootGraph_EventNodeClickAcceptsRecoveryAndRejectsUnclearedRepeat`: System.ObjectDisposedException : The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.
- `ProjectL.Tests.Framework.StageMapDepthCenteringBootSmokeTests.BootGraph_NodeClickTargetsDisplayedNode`: System.ObjectDisposedException : The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.
- `ProjectL.Tests.Framework.StageMapDepthCenteringBootSmokeTests.BootGraph_ShopNodeClickAcceptsAndKeepsMap`: System.ObjectDisposedException : The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.
- `ProjectL.Tests.Framework.StageMapNodeMovementBootSmokeTests.OperationalBoot_NormalVictoryRestoresSameMapNode_AndAcceptsNextNodeClick`: System.ObjectDisposedException : The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.
- `ProjectL.Tests.Framework.StageMapNodeMovementBootSmokeTests.OperationalBoot_RejectShopRecoveryCombatAndAbandonReturn_RespectLanding`: System.ObjectDisposedException : The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.
- `ProjectL.Tests.Framework.StageMapRecoveryEntryBootSmokeTests.ActualEventClick_OpensRecoveryWithMatchingContext`: System.ObjectDisposedException : The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.
- `ProjectL.Tests.Framework.StageMapRecoveryEntryBootSmokeTests.AfterCompletion_NextNodeRestoresMapInputAndPole`: System.ObjectDisposedException : The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.
- `ProjectL.Tests.Framework.StageMapRecoveryEntryBootSmokeTests.CompletedCurrentEntry_ReattachDoesNotOpen`: System.ObjectDisposedException : The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.
- `ProjectL.Tests.Framework.StageMapRecoveryEntryBootSmokeTests.CompleteWithoutSelection_ClosesAndClearsOnlyCurrentNode`: System.ObjectDisposedException : The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.
- `ProjectL.Tests.Framework.StageMapRecoveryEntryBootSmokeTests.ModalCancelHeldDrag_RestoresCameraPose`: System.ObjectDisposedException : The handle was returned, its camera was destroyed, or the scene was rebound. Object name: 'CameraCatalogHandle'.
- `ProjectL.Tests.Framework.TemporaryCoreChapterContinuationTests.DefeatInSecondChapter_TerminatesWithoutClearingChapter(False)`: InvalidMainCharacter   Expected: True   But was:  False
- `ProjectL.Tests.Framework.TemporaryCoreChapterContinuationTests.DefeatInSecondChapter_TerminatesWithoutClearingChapter(True)`: InvalidMainCharacter   Expected: True   But was:  False
- `ProjectL.Tests.Framework.TemporaryCoreChapterContinuationTests.NewScenario_DoesNotMutateAnotherRuntime(False)`: InvalidMainCharacter   Expected: True   But was:  False
- `ProjectL.Tests.Framework.TemporaryCoreChapterContinuationTests.NewScenario_DoesNotMutateAnotherRuntime(True)`: InvalidMainCharacter   Expected: True   But was:  False
- `ProjectL.Tests.Framework.TemporaryCoreChapterContinuationTests.TwoChapters_KeepOwnedFacts_ThenFinishRun`: InvalidMainCharacter   Expected: True   But was:  False
- `ProjectL.Tests.Framework.TemporaryCoreRuntimeTests.TemporaryDefinitions_RunTwoBattlesThroughRecoveryAndChapterClear(False)`: InvalidMainCharacter   Expected: True   But was:  False
- `ProjectL.Tests.Framework.TemporaryCoreRuntimeTests.TemporaryDefinitions_RunTwoBattlesThroughRecoveryAndChapterClear(True)`: InvalidMainCharacter   Expected: True   But was:  False
- `ProjectL.Tests.Framework.TerrainOperationalDisplayTests.EveryProductionStage_PreparesTwelveWallsAndDisplaysOriginalCells(False)`: Stage 1101, GridOutline=False   Expected is <ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.CellCoord[12]>, actual is <System.Linq.Enumerable+SelectArrayIterator`2[ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.StaticTerrainFact,ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.CellCoord]>   Values differ
- `ProjectL.Tests.Framework.TerrainOperationalDisplayTests.EveryProductionStage_PreparesTwelveWallsAndDisplaysOriginalCells(True)`: Stage 1101, GridOutline=True   Expected is <ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.CellCoord[12]>, actual is <System.Linq.Enumerable+SelectArrayIterator`2[ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.StaticTerrainFact,ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.CellCoord]>   Values differ 
- `ProjectL.Tests.Framework.TextNoticeUITests.HideVersion_TracksExternalDisableAndRepeatedUnHover`: Expected: not equal to 1   But was:  1
- `ProjectL.Tests.Framework.TextNoticeUITests.Prefab_BindsBothTextsAndHoverTarget_WithoutBlockingPointer`: Expected: False   But was:  True
- `ProjectL.Tests.Framework.UnityCameraCatalogPersistenceTests.DuplicateNamesFailBeforeMovingAnyCameraRoot`: System.NullReferenceException : Object reference not set to an instance of an object
