# Artifact A3 Registered Editing Service Handoff

Date: 2026-10-03

## Delivered scope and access

A3 registers one ArtifactEditingService through the existing InGameCoreSystemFactory, ExportServices, typed service key and Core group. Callers access IInGameCoreSystemGroupService.Artifacts (IArtifactEditingService). Existing parent InitData/PostBindService propagation binds the service; factory Create keeps its existing signature and upstream callers.

Only InGameCoreSystemFactory imports and constructs ArtifactEditCalculator, assigning it to IArtifactEditCalculation and injecting that required Contracts port. The internal sealed service has no concrete fallback, Application/Domain calculation calls, retained aggregate, pending command, lease, catalog or compiled loadout. No assembly reference or Framework infrastructure was changed.

```csharp
bool InitializeBoard(int width, int height);
void Place(int artifactId, CellCoord anchor, Rotation4 rotation);
void Move(int instanceId, CellCoord anchor, Rotation4 rotation);
void ReturnToPool(int instanceId);
void ExpandBoard(int width, int height);
void PreviewPlace(int artifactId, CellCoord anchor, Rotation4 rotation);
void PreviewMove(int instanceId, CellCoord anchor, Rotation4 rotation);
```

## Responsibility and publication

InitData retains only the Artifact definition query. PostBind resolves the Buffer query locally and retains current-read, initialization, edit-publication, preview-publication and combat-state-read permissions. It does not retain the broad query/context or receive SetSnapshot/Clear authority. ArtifactBuffer remains the canonical runtime state owner; each command reads fresh Current and uses E1's ToCalculationState conversion.

Binding alone grants nothing. InitializeBoard sorts definitions by ArtifactId and delegates inventory construction to the calculation port, then calls TryInitialize. It returns false for an already initialized Buffer before rereading definitions, including when a battle is retained. First initialization during HasBattle throws. Calls before DB/binding and edits/previews before initialization throw. Service recreation, rebinding and ordinary battle/chapter transitions preserve the Buffer aggregate; no shutdown or flow callback clears inventory.

One private HasBattle admission check serves first initialization and all mutations. Deployment, active combat and retained terminal presentation reject edits as LayoutLocked before definition lookup or state conversion; real EndBattle releases admission. Lobby/stage-selection eligibility follows the absence of a retained battle, while actual screen availability remains E3 work.

After admission, the service assembles definition facts, calls its Contracts port and publishes exactly one completed proposal. Expansion queries each distinct placed artifact kind once. The port owns inventory, geometry, expansion and identity decisions. Four edit methods return void; LastEditResult is the single outcome channel. Normal return follows completed publication, including a gameplay rejection. DB, calculation, overflow and publication faults propagate without being converted to rejection/success. Rejected edits preserve Current and clear stale preview through E1 publication policy.

Preview only publishes LastPreview using fresh facts. It does not alter inventory, LastEditResult, character, currency or combat state, and remains a spatial calculation during battle. E3 still owns preview freshness and popup output lifetime. ReturnToPool retains A2 identity behavior: the new pool item receives a fresh ID while the published result targets the removed placed ID.

No Artifact editing was added to CombatProcessingService. Real combat-command coverage places zero, one or two supported healing chalices through the new service: completion health is respectively 7, 10 or 13 from an initial 7 (maximum 20). Remaining pooled copies contribute nothing; retrying the command does not apply the effect twice. This fixture-based witness does not establish real Artifact asset completeness.

## Verification and review

Unity 6000.0.73f1 EditMode evidence is retained under `.superpowers/sdd/2026-10-02-artifact-core-migration-implementation-plan/A3/`.

- Pre-A3 affected baseline: 84/84 passed, exit 0.
- RED: compilation failed only for the missing planned service/interface; no assertions executed.
- Initial corrected GREEN: 86/86 passed, exit 0 (28 service, 31 ArtifactBuffer, 18 CombatCommandPublication, 9 CombatCommonPublication). Two further service cases cover definition deduplication and rebinding.
- Final expanded regression: 490/495 passed, 5 failed, 0 skipped, Unity exit 2. All 30 new service cases, 107 Artifact calculation/retained-service cases and 6 CombatViewBoundary cases pass. Framework totals are 377/382.

Final filter: `ProjectL.Tests.Framework;ProjectL.Tests.Core.ArtifactPlacementRulesTests;ProjectL.Tests.Core.ArtifactEditCalculatorTests;ProjectL.Tests.Core.ArtifactServiceTests;ProjectL.Tests.CombatViewBoundary`.

The five failures exactly match the existing names and messages in the [Task 8R6 baseline handoff](2026-10-02-task8r6-core-closure-and-view-handoff.md): ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews, and DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups, CombatStages_ReferenceExistingInGameData, EveryStage_HasNodeVisual and EveryUnitAndStructure_HasUnitVisual. Each reports `[CombatStageDetail] Combat stage requires Economy.` Authored DB/assets were preserved; the full filter is not claimed green.

CharacterRosterDatabase now supplies an empty Artifact query by default (or an explicit fixture query), FakeCoreService exposes the new interface property, and BattleDefinitionAdapterTests forwards its existing roster query. These are test-double contract updates required by complete Core-group binding; no production DB implementation changed.

A fresh read-only A3 reviewer found no Critical, Important or Minor issues and independently inspected final XML and GUID uniqueness. Normal git diff --check passes. All 23 captured incoming Assets files are preserved byte-for-byte, including restoring only the FMOD importer's scan timestamp from the verified incoming copy. Three new Unity GUIDs are unique. Review excludes A4 retirement and E3 activation.

## Remaining gates

A4 still owns legacy-owner/caller migration, mixed Contracts and serialized-reference audits, and restriction/retirement of legacy SetSnapshot/Clear authority. Existing legacy owner scripts remain present; A3 does not close migration retirement.

E3 uses the existing Job and LifeCycle systems. Authored board dimensions, the legitimate initialization caller, intent delivery, execution/completion and result correlation, preview freshness, popup output clearing, final reset authority and real UI/PlayMode acceptance remain pending. No default dimensions, new scheduler or popup reset was invented. G9/F1/F2 and incomplete effect/asset authoring remain separate dependencies.

No commit, push, merge, production input hookup or A4 retirement was performed.

## A4 closure update — 2026-10-03

A4 is delivered; see the [migration closure handoff](2026-10-02-artifact-core-migration-handoff.md). Legacy state owners/lease/catalog/old-map transfers and all Artifact SetSnapshot/Clear writer/export/query/key paths are retired after caller/GUID audit. Narrow E1 publication and A3 access remain. Useful assertions have current calculation/Buffer/combat coverage; obsolete owner mechanics are explicitly retired. Full approved assemblies pass 1300/1305 with exactly the same five pre-A4 Economy failures; source review is clean. Earlier counts and next-step notes above describe that delivery's historical state. E3 production startup/input/Job/UI activation and explicit final-reset lifetime decisions remain pending.