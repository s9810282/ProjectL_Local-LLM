# Artifact Core Migration Closure Handoff

Date: 2026-10-03

View/Flow readers: see the [Korean Framework integration handoff](2026-10-03-artifact-framework-view-handoff-ko.md) for current APIs, output/identity semantics, existing execution scaffolds and the pending E3 checklist.

## Delivered boundary

A1/A2 pure calculations, E1 Buffer publication, A3 registered processing and A4 legacy-owner retirement are delivered. ArtifactBuffer is the sole retained inventory/layout owner. Production editing activation remains E3; this handoff does not claim a connected popup, real bootstrap or PlayMode acceptance.

The existing InGameCoreSystemFactory constructs one ArtifactEditCalculator, assigns IArtifactEditCalculation and injects that required Contracts port into ArtifactEditingService. Only this factory knows the concrete calculator. ExportServices registers the service and its binding module once; existing Core-group InitData/PostBind propagation binds it. Access is IInGameCoreSystemGroupService.Artifacts, keyed by InGameSystemServiceKey.Artifacts. The factory signature, upstream callers and assembly references are preserved.

```csharp
bool InitializeBoard(int width, int height);
void Place(int artifactId, CellCoord anchor, Rotation4 rotation);
void Move(int instanceId, CellCoord anchor, Rotation4 rotation);
void ReturnToPool(int instanceId);
void ExpandBoard(int width, int height);
void PreviewPlace(int artifactId, CellCoord anchor, Rotation4 rotation);
void PreviewMove(int instanceId, CellCoord anchor, Rotation4 rotation);
```

ArtifactEditingService holds the definition query, calculation port and narrow Buffer permissions only. Each command obtains fresh Current.ToCalculationState(). The service assembles facts, checks execution admission, invokes the port and publishes; calculations own gameplay rules, while Buffer owns structural validation, detached storage and output retention. No Framework, Job, View, production DB implementation or authored asset was changed in A4. CombatProcessingService and all incoming non-A4 work are preserved.

## Initialization, admission and outcomes

Binding does not seed inventory. A legitimate E3 bootstrap caller must supply trusted authored dimensions and call InitializeBoard once. The service sorts definitions by ArtifactId, calculates InitialCount inventory and calls TryInitialize. Repeated initialization returns false before querying definitions and preserves state/outputs. Calls before binding/DB or edit/preview calls before board initialization throw. First initialization while HasBattle throws rather than fabricating a bootstrap rejection result.

Lobby and stage-selection editing are admitted when no battle is retained. A single service guard rejects all mutations with LayoutLocked before definition lookup/calculation throughout deployment, combat and terminal presentation; real EndBattle releases admission. Screen visibility remains the execution/presentation owner's work.

The four mutation commands return void. Buffer.LastEditResult is the single outcome channel; normal return follows completed publication for acceptance or business rejection. Current remains unchanged on rejection; both accepted and rejected edits clear LastPreview. Definition/calculation/overflow/publication faults propagate and never become success or gameplay rejection. Initialization's bool is a separate bootstrap result.

Previews publish spatial facts only, including during battle; they do not consume inventory or apply effects. E3 must connect hover freshness, request/result correlation and popup output clearing. No automatic inventory reset occurs on popup close, battle cleanup, chapter/stage transitions, lobby return or ResetRunProgress.

ReturnToPool preserves the existing identity contract: a fresh pool ID is allocated across both current item lists, while the edit result targets the removed placed ID. Overflow occurs before publication. Zero IDs, ordering, bounds-before-collision, self-overlap moves and successful identical moves are preserved. The A2 identical-move compatibility exception and preview distinction remain documented in the [A2 handoff](2026-10-02-artifact-a2-calculation-handoff.md).

## Current Buffer permissions and bypass closure

All permissions resolve to the same physical ArtifactBuffer through the existing Buffer group.

| Permission / query | Current member |
| --- | --- |
| IArtifactBufferReadSink / ArtifactReadSink | Current |
| IArtifactInitializationWriteSink / ArtifactInitializationWriteSink | TryInitialize(ArtifactState) |
| IArtifactEditWriteSink / ArtifactEditWriteSink | PublishEdit(ArtifactEditProposal) |
| IArtifactPreviewWriteSink / ArtifactPreviewWriteSink | PublishPreview(ArtifactPlacementPreview) |
| IArtifactEditResultReadSink / ArtifactEditResultReadSink | LastEditResult (nullable) |
| IArtifactPreviewReadSink / ArtifactPreviewReadSink | LastPreview (nullable) |

ArtifactBufferSnapshot.ToCalculationState provides the detached representation conversion; services contain no alternate copier or retained aggregate. TryInitialize prepares structural storage before assigning Current and grants once for that Buffer lifetime.

A4 removed IArtifactBufferWriteSink, its ExportServices registration, IBufferQuery/BufferSystemGroup.ArtifactWriteSink, ArtifactBufferKey.Write and ArtifactBuffer.SetSnapshot/Clear. No private replacement/reset method or speculative reset permission remains. The typed-sink audit found no production caller. All live references were test setup in ArtifactBufferTests, ArtifactEditingServiceTests, MapProgressBufferTests and ChapterGraphPreparationTests, plus ChapterResetSpy forwarding. Setup now uses initialize-once permissions and captures the actual owned Current snapshot for flow-retention assertions; obsolete reset/replacement tests are retired. A real factory capability regression failed on the old castable writer and passes after retirement.

Final inventory reset authority is still an explicit future lifetime decision. Nobody receives implicit Clear access while that decision is pending. Existing narrow initialization/edit/preview publication is the approved Systems route; no new generic transaction, scheduler, lock or state manager was added.

## Retired scripts and shared-value audit

The following source files and paired metas were removed. Paths below are relative to `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`.

| Retired files | Audit outcome |
| --- | --- |
| Application/Artifact/ArtifactService.cs; ArtifactServiceFactory.cs | No production callers; obsolete test callers migrated/retired. No compatibility state-owner wrapper remains |
| Domain/Artifact/ArtifactBoard.cs; ArtifactMap.cs; ArtifactPool.cs; ArtifactLayoutAccessState.cs | Persistent dimensions, occupancy, inventory/allocator and lease/revision ownership replaced by Buffer and command-local calculations |
| Contracts/Artifact/IArtifactService.cs; IArtifactLoadoutLease.cs | Owner/disposal/lease APIs have no remaining callers |
| Domain/Artifact/ArtifactCatalog.cs; Contracts/Artifact/IArtifactCatalogQuery.cs | Orphaned after factory/service retirement; DB definition queries remain the live definition route |
| Domain/Artifact/ArtifactPlacementSnapshot.cs | Old-map-only placement DTO; live ArtifactItemState/ArtifactData/shape carriers preserved |
| Contracts/Results/ArtifactResult.cs | Both ArtifactResult and ArtifactPoolResult were old-map/pool-only transfers; no shared consumer remains |

The GUID audit found zero serialized references in Assets, Packages and ProjectSettings for all twelve removed production scripts. Removed fixture GUIDs also have no serialized references. All retained script GUIDs remain unchanged.

Contracts/Snapshots/ArtifactSnapshot.cs is retained: ArtifactCreateSnapshot and ArtifactDefinition.CreateSnapshot are live dependencies of ArtifactEditCalculator.CreatePoolItem. ArtifactState, ArtifactShapeSnapshot, definitions/effect data, ArtifactLoadoutSnapshot, ArtifactPlacedEffectSnapshot and the supported compiler/rules remain. The loadout revision field is a retained value, not a live revision/lease owner. ArtifactActionFailureReason numeric values are preserved; LayoutLocked now documents retained battle admission and obsolete RevisionOverflow value 12 is reserved rather than reused.

ArtifactEffectCompiler, SoulEffectCalculator, StageSoulService, ArtifactActionProjection, ArtifactAttackPowerEvaluator, ArtifactCombatFacts, ArtifactActivationCalculator, SoulFieldState/SoulGrowthState/CompiledArtifactLoadout and Domain rules were not removed. Existing real combat effect consumption is unchanged.

## Behavior coverage and explicit retirements

| Legacy assertion group | Current coverage / disposition |
| --- | --- |
| InitialCount, deterministic IDs and definition facts | ArtifactEditCalculatorTests initialization cases; ArtifactEditingServiceTests sorted real-factory initialization, bind-without-grant and recreation/rebinding |
| Definition and transfer independence | Migrated DefinitionCells_RemainIndependentOfSourceAndTransferSnapshots, proposal copy cases and ArtifactBufferTests nested-array/historical-snapshot tests |
| Rotation, spatial preview, empty inventory, bounds/collision precedence | ArtifactPlacementRulesTests and ArtifactEditCalculatorTests previews/precedence; service preview-only publication |
| Unknown targets, first matching pool instance, self-overlap, blocked/invalid moves and rejection preservation | Calculator selection/missing-instance/move/invalid-rotation tests; Buffer rejection and service failure cases |
| Expansion, successful no-op, return identity/overflow and zero IDs | Calculator expansion/no-op/identity cases and service publication/overflow/deduplication cases |
| Placed effects, multiplicity and captured-input preservation | Real service-to-combat cases for 0/1/2 placed healing chalices, pooled exclusion and no retry reapplication; retained compiler/rule suites; migrated Compile_PreservesCapturedPlacedEffectsAcrossLaterLayoutProposals |
| Locking and snapshot retention | Service HasBattle tests through deployment/combat/terminal/cleanup, Buffer historical snapshots, chapter/battle/run retention and repeated initialization |
| Old GetPlacedEffects/catalog owner access, disposal, stale/concurrent leases, revision advancement and revision overflow | Retired with the obsolete APIs. These ownership mechanics are not recreated; useful state/fact/effect guarantees above remain |

ArtifactServiceTests (21 cases), ArtifactMapTests (4) and ArtifactLayoutLeaseTests (4) and their metas were retired. Two old Buffer reset/replacement cases were retired. One public capability regression and one migrated definition-copy case were added, and one compiler test migrated in place. The total decreases by 29 for this stated reason, not because a failure was suppressed. Existing active-battle read, combat publication, settlement and DB validation tests remain.

## Verification and pending production gates

Unity 6000.0.73f1 ran all `ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary` EditMode assemblies using `-assemblyNames`.

- Pre-A4: 1329/1334 passed; 5 failed; 0 skipped; Unity exit 2.
- RED capability regression: 0/1 passed, failing because the registered read Buffer still implemented IArtifactBufferWriteSink.
- Post-A4: 1300/1305 passed; 5 failed; 0 skipped; Unity exit 2. Core 918/918, Framework 376/381, CombatViewBoundary 6/6. All 30 service cases pass.

The exact five names and messages equal the captured pre-A4 XML: ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews; DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups, CombatStages_ReferenceExistingInGameData, EveryStage_HasNodeVisual and EveryUnitAndStructure_HasUnitVisual. Every failure reports `[CombatStageDetail] Combat stage requires Economy.` They also match the [Task 8R6 baseline](2026-10-02-task8r6-core-closure-and-view-handoff.md). No new A4 failure remains; the full suite is not claimed green.

Local evidence: `.superpowers/sdd/2026-10-02-artifact-core-migration-implementation-plan/A4/` contains incoming hashes/copies, pre-change sources, baseline/RED/GREEN XML/logs/exit codes, GUID audit, source-change manifest and review package. The progress ledger records caller, retirement and test-count decisions. Incoming non-A4 files and retained GUIDs are preserved; normal git diff --check passes.

A fresh read-only final reviewer approved all 11 modified A4 source/test files and all 15 deleted script/meta pairs, plus the surrounding migration boundaries, with no Critical, Important or Minor findings. The reviewer independently confirmed GUID/caller closure and exact baseline/final XML failure equality. Final document checks were performed by the implementer; the independent review covered source retirement rather than production activation.

E3 still owns trusted board dimensions, the real bootstrap caller, intent delivery/acknowledgement, Job execution/completion, result correlation, preview freshness, popup-output lifetime and UI/PlayMode acceptance through the existing Job/LifeCycle systems. Current supported combat effects are regression-tested using explicit fixtures; incomplete Artifact authoring and G9 effect-profile work remain separate. F1 execution fault handling and F2 combat exit/acknowledgement remain their existing owners' work.

No commit, push, merge or production activation was performed.
