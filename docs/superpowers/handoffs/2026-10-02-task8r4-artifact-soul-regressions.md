# Task 8R4 — Artifact, Soul and Settlement Regression Coverage

## Outcome and scope

Task 8R4 maps all 50 assigned method rows from the 8R1 inventory to approved current behavior, retained pure tests, or specifically withdrawn Stage-owner APIs. The original 606 matrix rows remain; the four `8r4_*` columns record assertion review, executed witnesses and remaining scope. Four existing test files add nine methods / eleven cases and one existing persistent-health assertion. No production code, public API, Buffer fields, Framework, execution Jobs, View/Flow, DB assets or source GUIDs change.

The current combat route needs no `UnitState`, `StageSession`, `SoulFieldState` instance or Stage-owned Artifact lease for soul growth, terminal healing or battle cleanup. Those old owners still have legacy callers and are **not deleted by this task**. Task 8R5 owns physical closure and member removal; passing witnesses do not authorize blanket deletion of mixed files or shared contracts.

## Behavioral coverage

| Concern | Current evidence and assertion |
| --- | --- |
| Growth and arrival | Existing `ArtifactSoulBufferCalculationTests`, `CharacterSoulPublicationTests`, `CharacterRosterBattleCycleTests` and `CombatCommandEffectTests` cover owned-count fixed +1 attack/max HP per soul, committed voluntary arrival, forced-arrival accounting, dead/rejected arrival preservation, actual death anchor, persistent main HP and no second growth on preparation. |
| Ordered collection | New unsorted IDs and same-cell souls prove deterministic capped healing: ID1 heals first, ID2 adds max HP without healing, yielding HP11/max12; other-cell ID3 remains. Snapshot construction copies its input and rejects duplicate or unissued IDs. Consumed IDs and final field entries reject writes. |
| Failure and publication | A second soul's max-HP overflow leaves all facts unchanged; existing real-Buffer overflow tests confirm no partial publication. A field changed after calculation rejects health, owned count and persistent main together through the existing public `BattleWriteSink.Apply(SoulActionChanges)` boundary. No lock, transaction, Run version or extra growth revision is introduced. |
| Spawn and lifetime | Two real enemy deaths and two altar instances produce IDs7–10 at actual captured anchors in entity-then-origin order. Repeated calculation does not consume source IDs. Duplicate death facts reject; a later ID overflow cannot publish earlier candidates. Duration5 starts before same turn-end countdown4 and expires after five boundaries. Mixed duration1/5 fields expire/decrement once, retaining origin/cell/heal/next ID. |
| Combined attack bonuses | Base20 + souls2 + temporary3 + Last Stand5 yields common potential30, splash21 and actual lethal loss1. Preview uses the same literal potential without consuming input AP. Next attack uses the surviving roster, removes Last Stand and yields splash17 while preserving base/count/temporary values and charging once. Living deployed facts are sorted; dead/excluded characters do not count; enemy attacks gain no player soul/Last Stand bonus. |
| Chalice results | New current calculation plus real Buffer publication tests use two explicit Chalice definitions at HP7/14/15 of max15. Requested amounts3/3 allocate applied3/3,1/0,0/0 in origin order. Repeating the command does not reapply costs/healing. Public `CombatResult.Events -> CombatArtifactEvent.Activations` is readonly, survives acknowledgement/EndBattle/next preparation, and needs no result-history Buffer. |
| Persistence and cleanup | Existing two-Chalice `7/15 -> 13/15` completion test preserves healed HP, stage clear and boss/map completion across cleanup/preparation. Existing temporary-attack cleanup and roster-cycle cases remain. One extra assertion proves an explicit persistent-max increase10->15 preserves HP7 before healing. Temporary max-HP authoring/design is not activated. |

M014's old projection accepted duplicate death facts; the current static death calculation rejects them. Valid command composition still spawns once per real death/origin. M039/M041/M047/M049's variable legacy soul amounts are replaced by the user's fixed owned-count +1 contract. M048's independent `SoulGrowthMath` arithmetic stays a pure test; it does not approve variable production growth. M021's old structures/neutral/multicell entities are not new CharacterCombatFacts profiles. Current field capture and explicit multicell-unit deferral are recorded separately.

Old `EntityHealthPlan` constructors, arbitrary max-HP delta/PlanHeal APIs, mutable revision evidence, Stage snapshots and delayed plan invalidation have no equivalent public operation requirement on the current route. The matrix distinguishes preserved persistent values/validation from those withdrawn surfaces. Preserve justified shared health-value/math assertions when splitting old test seams in 8R5.

## Artifact editing and resource consumers

| Group | Concrete consumer / responsibility | Retirement decision |
| --- | --- | --- |
| `ArtifactServiceFactory`, `ArtifactService`, ArtifactBoard/Map/Pool | Factory creates the separate editable layout service. Its public placement/move/return/expand/shape/state API is exercised by `ArtifactServiceTests` and `ArtifactLayoutLeaseTests`; compiler tests capture placed definitions. | Preserve this separate editing feature. No claim that its storage has migrated to combat Buffer, or that it is already production wired. |
| `ArtifactLayoutAccessState` / `IArtifactLoadoutLease` | `ArtifactService.TryAcquireLoadoutLease` protects layout writes; the concrete lease's owner-only Dispose releases the edit lock, service disposal invalidates it. Existing standalone lease tests retain stale/concurrent admission, locked writes, correct-owner release and disposed operations. | Preserve existing resource API/behavior. It is an editing lock, not an extra current battle state/lease. No external OS/native resource was found in this concrete implementation. |
| `StageArtifactLifecycleService` / `StageArtifactLoadoutSession` | `StageSessionFactory` captures the old service loadout; old lifecycle activation acquires the edit lease and Stage disposal releases it. Old release-failure tests inject throwing lease implementations and uncertain cleanup evidence. | Retire only with old Stage consumer closure in8R5. Those injected wrapper assertions do not authorize a new lease on the Buffer combat route and are not equivalent to nonthrowing standalone Dispose tests. |
| `StageSoulService`, `SoulFieldState`, `ArtifactActionProjection`, `ArtifactCombatFacts`, `ArtifactAttackPowerEvaluator` | Current `SoulEffectCalculator` calls static `PlanDeaths`, static `PlanCountdown`, `ProjectCollection`, fact-based `Capture` and static `Evaluate`. Old instances still serve Stage/tests. | Preserve exact pure signatures/GUIDs; trim only obsolete instances/overloads after8R5 caller closure. Compiled immutable rules are definitions, not canonical gameplay state. |
| `ArtifactActivationCalculator.FromHealing` | Current `BattleCompletionCalculator.PlanTerminalHealth` publishes immutable activation receipts through `CombatArtifactEvent`. | Preserve shared pure allocation when removing the old result owner/resolution plan. |

The real registered `CombatProcessingService.ReadInput` reads canonical characters/counts/field, then `ReadArtifactLoadout` reads `ArtifactReadSink.Current.PlacedItems` and `ArtifactDefinitionTableQuery.GetDefinition(...).Effects`. `Publish` attaches stage resolution to the already healed transition, then uses `BattleWriteSink.Apply(CombatCommandTransition)`. It does not heal a second time, instantiate the old editing service, acquire a Stage lease, write/ack View input, or add a Plan/Job/Runner. The new Chalice tests cover calculation plus real Buffer publication using explicit definitions; they **do not** prove this DB-loaded path or View-owned runtime wiring.

## Verification

Actual Unity6000.0.73f1 EditMode compilation and final regression execution:

| Assembly | Passed / total | Failed |
| --- | ---: | ---: |
| ProjectL.Tests.Core | 1372 / 1372 | 0 |
| ProjectL.Tests.Framework | 319 / 324 | 5 |
| ProjectL.Tests.CombatViewBoundary | 13 / 13 | 0 |
| Total | **1704 / 1709** | **5** |

Skipped/inconclusive: zero. Unity log exit code **2**. Exact failed names and full messages match both 8R1 and 8R3: all five report `System.InvalidOperationException : [CombatStageDetail] Combat stage requires Economy.` They are `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews` and `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`, `CombatStages_ReferenceExistingInGameData`, `EveryStage_HasNodeVisual`, `EveryUnitAndStructure_HasUnitVisual`. This is not a fully green suite; DB assets were not changed.

```powershell
& 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/My_ProjectWork/Github_desktop/Project_L' -runTests -testPlatform EditMode -assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary' -testResults 'D:/My_ProjectWork/Github_desktop/Project_L/.superpowers/sdd/2026-10-02-core-combat-legacy-retirement-plan/8r4-complete.xml' -logFile 'D:/My_ProjectWork/Github_desktop/Project_L/.superpowers/sdd/2026-10-02-core-combat-legacy-retirement-plan/8r4-complete.log'
```

The first check found two new **fixture** failures before death calculation: retained dead anchor2,1 disagreed with explicit actual anchor3,1/4,1. Corrected facts exclude the final dead character and pass the captured death anchor, as required by `SoulDeathFacts`. Focused tests then passed41/41. The first full run passed1704/1709; the final run additionally includes explicit duplicate/unissued soul-ID constructor assertions and the old Last Stand overflow/no-mutation assertion identified during matrix review. No production fix or fabricated RED replay is claimed.

Case identities compared to the actual 8R3 XML show exactly eleven additions and zero removals. All 44 distinct matrix witnesses are Passed in the final XML, including fourteen directly retained current/pure/shared methods. SHA-256 comparison before tracked documentation updates found only the four intentional test files changed; all incoming runtime files and previous work were preserved. All three importer/catalog/cache/settings files were restored byte-for-byte to their captured incoming state. Evidence and scripts remain in the plan workspace. No independent managed harness, PlayMode or actual gameplay verification was performed.

Independent read-only code review found no concrete correctness/ownership issues. Subsequent matrix/source review identified a missing old Last Stand overflow assertion and an incorrect direct-caller label for fact capture. Both were corrected: current facts now check maximum base attack plus positive Last Stand overflow without HP/attack/AP mutation, and the manifest names `SoulEffectCalculator.EvaluateAttackPower` as the direct capture caller. Final reviewer checked those corrections and the final XML, and reported no remaining Critical/Important/Minor findings. This review does not close production DB, View wiring or 8R5 deletion gates.

## Remaining boundaries

- **8R5:** Close old callers, remove audited battle owners/helpers and trim mixed files while preserving pure members, public/shared contracts and separate Artifact editing.
- **8R6:** Final whole-slice ownership/caller audit, fresh compilation/regression evidence and handoff completion.
- **External acceptance:** Actual targeting DB schema/query/unit mappings, actual DB-loaded Artifact compiler acceptance, startup roster/wallet timing, View-owned combat Flow/Job wiring, Framework F1/F2 and PlayMode/gameplay remain open.
- **Explicit deferrals:** ThornHeart attack settlement still throws until its integration order is confirmed; pure configured reflection tests remain. Rider character profile activation, temporary max-HP design, multicell units, empty-All policy and custom movement routes remain gated. Existing generic rules/storage tests do not close these gates.

No commit/push or8R5 execution is included.
