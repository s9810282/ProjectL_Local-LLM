# Core Combat Legacy Retirement Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` to implement this sequential continuation task by task. Checkboxes describe future execution, not work completed by writing this document. Preserve the completed Task 8P work and any incoming working-tree changes. Commit or push only when the user requests it.

**Goal:** Finish the independently executable Core portion of Task 8 legacy retirement: preserve approved combat behavior and regression coverage, remove unused persistent combat owners and their callers, and identify the exact remaining production dependencies.

**Architecture:** Keep the existing registered Systems processing services, Core fact-based calculations and canonical Buffers. Migrate legacy consumers and behavioral tests to those boundaries, then remove obsolete state and execution code. Preserve shared pure methods even when they currently share files with obsolete owners; no new runtime architecture is proposed.

**Tech Stack:** Unity 6000.0.73f1, C#, existing Core.Contracts/Domain/Application and Systems assemblies, NUnit Unity EditMode tests.

**Spec:** [CharacterBuffer and Effect migration design](../specs/2026-09-29-character-buffer-effect-migration-design.md), as amended by the [targeting design](../specs/2026-10-01-attack-targeting-methods-design.md), current user decisions, and the [implemented Task 7 command contract](2026-10-01-combat-buffer-command-task7-contract.md). The [master migration plan](2026-09-29-character-buffer-effect-migration-plan.md), Task 8 Steps 6–7, remains the completion ledger.

## Planning decision and baseline

A separate design document is unnecessary for this continuation. The state owner, assembly boundaries, service composition, execution ownership and current gameplay scope have already been specified and implemented. The difficult work is establishing behavioral coverage and removing dependencies safely. This plan makes those execution decisions concrete; it does not approve every historical technical proposal in the original design.

Revisit design only if execution finds a necessary change to public ownership, a missing gameplay decision, a new resource lifetime, or a state requirement that existing Buffers cannot express. Record the exact affected behavior and continue independent tasks; do not invent a new manager, scope, port or Buffer to keep the deletion task moving.

Planning inspection: 2026-10-02, branch `feature/Cheon/refactor/core-system`. Final planning inspection found HEAD `abb86ccb9af391a95d5560d464081c5f03d8c920` (`[천명] 구버전 구조 정리`), which includes Task 8P after the earlier Task 7/8 commit `6367026e211bf8769b5c0cef1707a07f5be7010a`. The working tree was clean apart from this new plan before its master-plan cross-reference was added. Capture HEAD and all working-tree changes again at execution time. No implementation or Unity test execution is performed by this planning change.

The previous Task 8P execution recorded Core 1282/1282, Framework 302/307 and CombatViewBoundary 13/13: 1597/1602 total, with five existing authored Economy failures. These are historical results, not a new run for this plan.

### What this continuation closes

| Status | Scope |
| --- | --- |
| Preserve completed work | Tasks 0–6, approved Task 7A–7E and common commands, current Task 8 service/definition/spawn work, and Task 8P. Do not restart these tasks because old checklist text lacks a later checkpoint. |
| Execute now | Caller/coverage inventory; current-contract test migration; removal of proven obsolete combat hosts and mutable state; separation of live pure members from obsolete members; final Core ownership audit. |
| Execute after an exact dependency arrives | Actual targeting DB conversion, authored Artifact compiler acceptance, agreed startup wiring and joint production verification. These are still existing Task 8 work. |
| Not included | New combat features, an independent Run system, custom movement routes, multicell-unit activation, new Artifact gameplay, production View/Flow implementation, common Framework redesign. |

Completion of this document's executable slice does not mean full Task 8, every historical Core feature, or playable production integration is complete.

## Global Constraints

- Buffer is the runtime source of truth. Character, CombatState, Current, Stage, Map, Currency and Artifact stores retain their existing distinct responsibilities.
- Core.Application/Domain do not reference Systems Buffers, DB implementations, Unity or Framework. Systems assembles current facts through existing InitData/PostBindService paths.
- View/Flow owns combat LifeCycle, CombatFlow, CombatPlan, combat Jobs, service-call wiring and JobTypes. Core must not introduce or extend a parallel Plan, Job, scheduler, dispatcher or runner. Noncombat Stage ownership is not changed by that combat decision.
- Keep existing processing/read API names and service registrations. A direct service call in an isolated test is not production Job integration evidence.
- Core reads CombatViewBuffer only. View owns intent writes, removal and presentation acknowledgement. Tests may exercise both sides explicitly without transferring production ownership.
- Preserve the existing joint command publication boundary. Validate all affected state and result metadata before installation. Do not add generic transactions, locks, Run IDs, revisions or stale-request systems.
- Ordinary chapter entry, victory, map return and EndBattle preserve the specified records. No resurrection of RunProgressionService, automatic roster reacquisition, wallet reset or NewRun dispatch.
- Preserve pure rules, definition compilation and immutable command-local projections. A class named `State`, `Service` or `Plan` is not automatically a retained owner or an execution layer.
- Do not create a fake StageSession, mutable test roster, reflection-based production adapter, duplicate processing service or new execution route to keep old tests compiling.
- Required shared Contracts and actual Artifact editing/resource semantics are not deletion targets merely because legacy battle tests consume them. Removing a lease/revision protocol requires evidence of the resource and every surviving consumer.
- Do not author DB schema/assets, infer Method_Key mappings from old enum numbers, invent free costs, or activate deferred profiles to reach a completion percentage.
- Implementation documentation is English; new or rewritten code comments are Korean. Preserve the spelling `CoreCaculator` and existing assembly names.

References: [Framework Development Guidelines](../framework/Framework%20Development%20Guidelines.md), [programmer guide](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), [architecture](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md), [Buffer correction](../handoffs/2026-09-28-buffer-contract-correction.md), [Core response to View](../handoffs/2026-09-30-combat-core-response-to-view.md). The programmer guide identifies itself as version 2.0, source baseline 2026-09-22. Its historical assembly listing must be checked against current asmdefs; the current Systems assembly already references Core.Application. The separately mentioned pipeline-v3 design-review document is not a prerequisite, per the user's instruction.

## Review Focus

1. Mixed files: deleting an old owner must not delete a live fact-based overload or shared interval/tie calculation. Owned by 8R4–8R5.
2. Legacy versus current rules: preserve useful behavior while replacing obsolete target selection, Run provenance and disposal assertions. Never restore old targeting merely to preserve an old golden test. Owned by 8R2–8R4.
3. Command integrity: repeated handling, invalid proposals and actor death must not duplicate costs, healing, souls or terminal publication. Owned by 8R2–8R4.
4. Lifetime: next battle preparation must use persistent Buffer state, clear transient state, and preserve result/counter and chapter-history contracts. Owned by 8R2/8R4/8R6.
5. Evidence boundaries: test definition sources cannot satisfy missing production DB, Artifact authoring, View or Framework acceptance. Owned by 8R1/8R6 and the dependency table.

## File and interface map

Path aliases below are relative to the repository root:

- `S/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/`
- `C/` = `S/CoreCaculator/`
- `B/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/`
- `F/` = `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/Core/`
- `T/` = `Assets/Tests/EditMode/`
- `D/` = `docs/superpowers/`

| Area | Existing files and treatment |
| --- | --- |
| Current production bridge — preserve | `F/InGameCoreSystemFactory.cs`, `S/InGameCoreSystemGroup.cs`, `S/CombatSystem/CombatProcessingService.cs`, `DatabaseBattleInitializationSource.cs`, `BattleDefinitionAdapter.cs`, `S/ChapterProgressionCoordinator.cs`. Change only a demonstrated Core defect or an actual newly available query connection. |
| Current calculations — preserve | `C/Application/Combat/CombatCommandCalculator.cs`, `CombatAttackQueries.cs`, `CombatMovementCalculator.cs`, `CombatPlacementCalculator.cs`, `CombatDrawCalculator.cs`, `CombatTurnCalculator.cs`, `CombatGimmickCalculator.cs`, `CombatCommandComposition.cs`, `BattlePreparationCalculator.cs`, `BattleCompletionCalculator.cs`, `CharacterSettlementCalculator.cs`; current `C/Application/Roster/` rules and `C/Application/Artifact/SoulEffectCalculator.cs`. |
| Legacy host candidates | `C/Application/StageRun/StageSession.cs`, `StageSessionFactory.cs`, `StageServices.cs`, `StageSnapshotBuilder.cs`, `StageQueryService.cs`, `StageSetupService.cs`, `StageDeploymentService.cs`, `StageFlowService.cs`, `StageDrawService.cs`, `StageGoldService.cs`, `StageOutcomeEvaluator.cs`, `StageCombatFacade.cs`. Delete only the audited dependency group after replacement coverage. |
| State-bound dependencies — audit, then remove | `C/Domain/Entities/UnitState.cs`, `CombatEntityState.cs`; old map/reserve/phase/night/outcome/draw/economy/timeline states; `C/Application/Combat/UnitAttackService.cs`, `UnitMovementService.cs`, `CombatTimelineService.cs`, object-bound effect owners/orchestrators; `C/Application/StageGimmicks/StageGimmickTimelineService.cs`; `C/Application/Run/RunCharacterDeckService.cs`, `C/Domain/Run/RunCharacterDeckState.cs`. Expand the concrete deletion manifest using reverse references, not folder deletion. |
| Artifact battle lifetime candidates | `C/Application/Artifact/StageArtifactLifecycleService.cs`, `StageArtifactLoadoutSession.cs`, `StageArtifactResolutionPlan.cs`, `StageSoulService.cs`, plus exclusively dependent prepared state/plans after resource and test classification. |
| Separate Artifact functionality — retain | `C/Application/Artifact/ArtifactService.cs`, `ArtifactServiceFactory.cs` and Artifact board/map/pool/layout operations unless a separate, explicit caller/ownership migration justifies change. This continuation is not a redesign of Artifact editing. |
| Rules and public data — retain | Core Contracts still used by current services/DB/View; Domain effect engine, rules, codecs, targeting geometry, damage and movement math; ArtifactEffectCompiler and pure compiled rule data. No whole CoreCaculator deletion. |

### Confirmed mixed files: trim members, do not delete files wholesale

| File | Live member consumed by current calculations | Legacy portion to audit |
| --- | --- | --- |
| `C/Application/Combat/EnemyAi/EnemyAiTurnService.cs` | `internal static CombatCommandTransition Calculate(CombatCommandInput input)` from `CombatCommandCalculator.Advance` | Instance `Run(IEnemyAiCombatGateway, StageSnapshot)` and gateway execution. |
| `C/Application/Combat/EnemyAi/EnemyAiPolicy.cs` | Fact-based attack, pursuit and movement selection overloads | StageSnapshot overloads and exclusively old snapshot helpers. |
| `C/Application/Artifact/ArtifactActionProjection.cs` | `ProjectCollection(SoulCollectorInput, CharacterCombatFacts, int, SoulFieldChange, IReadOnlyList<long>)` returning `SoulActionChanges` | UnitState/health-plan/growth-plan instance projection. |
| `C/Application/Artifact/ArtifactCombatFacts.cs` | `Capture(IEnumerable<CharacterCombatFacts>)` | `Capture(IEnumerable<CombatEntityState>)`. |
| `C/Application/Artifact/ArtifactAttackPowerEvaluator.cs` | Static `Evaluate(CharacterCombatFacts, int, ArtifactCombatFacts, CompiledArtifactLoadout, ICombatEffectEngine, List<CombatEvent> = null)` | UnitState-based evaluation, retained projection and validation members. |
| `C/Domain/Artifact/SoulFieldState.cs` | Static `PlanCountdown(SoulFieldSnapshot)` returning `SoulFieldChange` | Mutable field entries, revision and instance plan/commit methods. |
| `C/Domain/Combat/CombatTimelineScheduler.cs` | `CalculateActionInterval(int delay, int priority)` and `CalculateTieKey(int seed, EntityId id, int completedTurnCount)` | Mutable CombatTimelineState selection/commit/forecast methods. |
| `C/Domain/Combat/AppointRangeCalculator.cs` | `CalculateRangeCells(TargetingMethodSpec, int, CellCoord)` | StageMapState/UnitState range and direction overloads. |
| `C/Domain/Combat/AttackGeometryCalculator.cs` | `CalculateCells` and `CalculateResolvedCells` | Old range-calculator constructor dependency and UnitState attack geometry. |
| `C/Domain/Combat/AttackTargetResolver.cs` | Fact-based `CollectCandidates` and `ResolveRole` | CombatEntityState candidate overload. |
| `C/Domain/Combat/CannonAppointExclusionCalculator.cs` | `IsForbidden(IReadOnlyList<CellCoord>, CellCoord)` | StageMapState ray-start helper. |
| `C/Domain/Combat/ImpactPatternCalculator.cs` | Static `GetForwardStep` and `GetRightStep` | StageMapState impact-cell calculations. |
| `C/Application/Artifact/StageSoulService.cs` | Static `PlanDeaths(SoulFieldSnapshot, IReadOnlyList<SoulDeathFacts>, bool, CompiledArtifactLoadout)` | Instance lifecycle/entity operations and PreparedStageSoulAction. |
| `C/Domain/Artifact/SoulGrowthState.cs` | Count-based `SoulGrowthMath.Collect` and `SoulGrowthRules` | Immutable old revision-based value/overload; retain justified pure tests separately from owner removal. |
| `C/Application/Combat/Traits/TraitEffectProvider.cs` | `CreateStatuses(UnitDefinition, UnitTraitStateSet)` used by BattleCharacterCalculator | UnitState bindings/evaluations/action conversion; retain required registry/validation helpers. |
| `C/Application/Combat/Categories/CategorySnapshotBuilder.cs` | `CreateSnapshot(CategoryDefinition, UnitCategoryState)` used by BattleCharacterCalculator | UnitState action conversion and exclusively old helpers/plans. |

Prefer preserving these pure member signatures and GUIDs in the existing files while removing unused members. Making an internal holder static is optional once its instance callers are gone. A new hierarchy or renamed compatibility layer is unnecessary. Extend this list when the full reverse-reference audit finds another mixed file.

Existing service boundary: `void PrepareBattle()`, `bool ProcessNextCommand()`, `bool ProcessLatestPreview()`, `bool AdvanceCombat()`, `void EndBattle()`, `CombatDrawAvailability GetDrawAvailability()`, `CombatTimelineEntrySnapshot[] GetTimeline(int maxEntries = 10)` on `ICombatProcessingService`. Existing settlement members include `ResolveBattle(ArtifactLoadoutSnapshot)`, `AbandonBattle(ArtifactLoadoutSnapshot)` and `Recover(RecoveryOptionId)`, all returning bool. New public members are not planned.

Pure entry points: `CombatCommandCalculator.Calculate(CombatCommandInput)` and `Advance(CombatCommandInput)` return `CombatCommandTransition`; `Preview(CombatCommandInput)` returns `AttackPreview`; `GetAttackRange(CombatCommandInput)` returns `AttackRangeResult`. Use existing Buffer publication APIs and the Task 7 contract for their semantics.

## Task 8R1 — Freeze the execution baseline and classify the complete dependency graph

**Files:** Read the files above, current asmdefs and all actual callers. Create `D/handoffs/2026-10-02-core-combat-retirement-coverage.md` during execution. Preserve this plan, completed Task 8P work and incoming modifications.

**Interfaces:** Consumes the current source tree, existing tests and approved rules. Produces a concrete source/member deletion manifest and a test-method coverage matrix; no runtime API.

- [x] Record HEAD, branch, staged/unstaged/untracked paths and hashes before editing. Include completed Task 8P and incoming files in the baseline; do not use HEAD-only diffs or reset the checkout.
- [x] Trace production factories, service callers, Core-internal callers, tests and Unity asset/GUID references for every candidate. The planning search found 30 test/helper files directly referencing StageSessionFactory/StageServices/StageCombatFacade/RunCharacterDeckState; this is a starting set, not the complete UnitState/effect dependency closure.
- [x] For each source/member record: existing role, current caller, replacement symbol, lifetime, shared pure members, action (`remove`, `trim`, `retain shared`, `retain separate feature`, `blocked by named requirement`) and the behavior tests proving it.
- [x] Classify each affected test method: approved behavior to migrate; shared pure regression to retain; obsolete constructor/owner/Run contract to retire with reason; superseded gameplay to replace with current expectation; deferred profile with its exact gate. Do not classify a test solely by its filename.
- [x] Run the three current EditMode assemblies once if no fresh result exists for the exact execution tree. Save XML and failure names. A new failure blocks deletion of its affected dependency group until understood; the five known Economy failures are a separate baseline.

**Acceptance:** Every deletion candidate has caller and coverage evidence. DB absence is not used as a blanket blocker for unused combat code; absence of external production callers is not used as proof that old behavior has a replacement.

**2026-10-02 completion checkpoint:** [8R1 baseline and coverage report](../handoffs/2026-10-02-core-combat-retirement-coverage.md) records 335 source rows (92 candidates, 16 mixed, 221 shared, 6 separate) and 606 test methods across 72 source files plus 6 helpers. All inventoried methods matched fresh baseline cases. Unity Core1282/1282, Framework302/307, boundary13/13:1597/1602 with the unchanged five authored Economy failures. Inventory and classification are complete; assertion equivalence remains 8R2–8R4 and source removal remains 8R5. No runtime/test/asset/assembly changes or production completion are asserted.

## Task 8R2 — Move preparation, movement, deployment, draw and turn regressions to current boundaries

**Files:** Migrate affected methods from `T/MainCharacterStageTests.cs`, `StageDrawTests.cs`, `StageGoldTests.cs`, `StageInitialEnemyBootstrapTests.cs`, `ReserveUnitCreationTransactionTests.cs`, `StageCombatMovementCheckpointTests.cs`, `StageOutcomeNightIntegrationTests.cs`, `StageServicesBoundaryTests.cs`, `CombatTimelineSchedulerTests.cs` and their helpers.

**Destination tests:** Extend `T/CombatCommonCommandTests.cs`, `T/BattlePreparationRuleTests.cs`, `T/CharacterRosterRuleTests.cs`, `T/Framework/CharacterBattlePreparationTests.cs`, `T/Framework/CharacterRosterServiceTests.cs`, `T/Framework/CharacterRosterBattleCycleTests.cs`, `T/Framework/CombatCommonPublicationTests.cs`, and `T/Framework/ChapterGraphPreparationTests.cs`. Existing helper inputs stay immutable; Systems tests instantiate real Buffers through the existing factories.

**Interfaces:** Uses current preparation and command calculation/processing APIs above, existing `InitializeRoster(loadoutKey)`, and Buffer read/publication APIs. Produces regression equivalence records and minimal fixes to current calculators only if approved behavior is missing. No startup caller or scheduling API is introduced.

- [x] Map approved preparation/roster cases: one main plus `min(3, owned subs)`, stable owned identity, duplicate card types as distinct instances, invalid capacity/placement before allocation, exact draw cost/cursor/currency publication, authored versus weighted enemy placements, headquarters-first order. Keep the current DB-missing preparation guard.
- [x] Map movement cases to current rules: the existing fixture moves `(1,3)` to `(3,3)`, AP `5 -> 3`, pending cost `2`, checkpoint `(1,3)`; preview leaves input unchanged. Preserve closer-retarget pending refunds, invalid/occupied destinations, committed movement, death and forced-displacement consequences. Do not add user-selected paths.
- [x] Map turn/night/gimmick cases: ending AP restores to maximum only on successful settlement; dead actors are not recreated; interval remains `ceil(100/Priority)` while Delay drives night progress; deterministic ties, recall/redeployment, railway casualties, main defeat and boss progress are preserved. Use current timeline query tests for default 10 and explicit limits.
- [x] Port the behavioral assertions before removing their old fixture. Existing passing current tests may satisfy a row; add only missing assertions. If a behavior needs a code fix, first capture the actual failure and fix the owning pure calculator or Systems bridge, not Buffer rules or a Job.
- [x] Run the affected destination fixtures and the existing relevant publication tests. Record exact replacement method names. Remove obsolete old fixture methods only when every assertion is classified; leave shared helpers until their last consumer moves.

**Acceptance:** Covered commands read current facts and publish through the same approved boundary. Preview and rejection preserve gameplay; invalid combined updates cannot install partial character, currency, draw or progress data.

**Execution checkpoint (2026-10-02):** [8R2 current-boundary regression report](../handoffs/2026-10-02-task8r2-current-boundary-regressions.md) records 163 reviewed legacy rows, 59 verified witness methods and 56 additional executed cases. Core 1334/1334, Framework 315/320 and CombatViewBoundary 13/13 passed in actual Unity EditMode; the same five authored Economy failures remain. No production source, public API, View/Flow code, old test or helper was changed or removed. Checkboxes close current-scope mapping and regression coverage, not physical retirement: mixed attack/AI/forced-movement assertions remain 8R3, surviving shared constructor assertions and old wrapper/helper deletion remain 8R5, and multicell-unit gameplay remains deferred. Full Task 8 and production DB/View acceptance remain open.

## Task 8R3 — Move attack, traits, categories and AI regressions without restoring old targeting

**Files:** Migrate affected methods from `T/UnitAttackServiceTests.cs`, `UnitAttackPreviewTests.cs`, `StageCombatAttackResultTests.cs`, `StageCombatTraitIntegrationTests.cs`, `CombatTraitAtomicityTests.cs`, `CharacterCategoryBootstrapTests.cs`, `UnitCombatStaticStatsTests.cs`, `AttackRangeResultTests.cs`, `BallistaCombatIntegrationTests.cs`, `ThornFieldAttackTests.cs`, `ThornFieldTurnEndTests.cs`, `KnockbackTestScenario.cs`, and `StageEnemyAiTurnTests.cs`.

**Destination tests:** `T/CombatCommandCalculationTests.cs`, `CombatCommandEffectTests.cs`, `TargetingPreviewParityTests.cs`, `TargetingMethodGeometryTests.cs`, `TargetingMethodSelectionTests.cs`, `TargetingSplashDamageTests.cs`, `CombatCommonCommandTests.cs`, `Framework/CombatCommandPublicationTests.cs`, `CombatPreviewPublicationTests.cs`. Preserve standalone engine/rule tests such as `CombatEffectEngineTests.cs` and `CategoryDamagePipelineTests.cs` where they have no retired-owner dependency.

**Interfaces:** Current Calculate/Preview/GetAttackRange/Advance plus fact-based EnemyAiPolicy methods. Preserve `EnemyAiTurnService.Calculate`; the legacy gateway `Run` is an eventual deletion candidate.

- [x] Reconcile each old attack expectation against the latest targeting design, owner inventory decisions and completed Task 7 tests. Do not revive target-centric old selection or treat historical inventory `pending` entries as current decisions.
- [x] Cover common original damage captured once, full damage for Primary/Designated, `trunc(D * splashPercent / 100)` for Additional, then existing matchup rounding, defense and incoming reduction. Preserve one attack cost, caster exclusion, friendly/own-structure eligibility and board clipping.
- [x] Preserve confirmed profiles: Main square/single range 1; Ballista eight-direction group selection with retained piercing falloff; Cactus Cannon range 4, configurable initial 70% splash and center-excluded adjacent eight-cell aim restriction; Gravekeeper fan/single range 1 and one-cell push. Empty single-cell aim may succeed; an empty direction group rejects. Multicell targeting remains deferred.
- [x] Migrate movement-before-attack death, thorn traversal, forced movement, trait/category transitions, Last Stand capture and terminal events to current facts/publication tests. Rejection before acceptance must leave gameplay unchanged; accepted lethal traversal keeps its specified movement consequences without attack cost/damage.
- [x] Verify player/preview/range/AI reuse on identical facts, including indirect splash candidates, insufficient AP, equal-score overlap and full-mask selection. Use immutable expected values; do not make one path's output the sole oracle for another.
- [x] Run affected current tests, record coverage, and retire only superseded/fully replaced old cases. Keep unconnected effect rule tests; no profile activation is authorized by test migration.

**Acceptance:** No legacy gateway is required to exercise approved AI or attack behavior. Required pure Effect engine/rule/codec code survives and no gameplay calculation moves into Buffer or Job.

**Execution checkpoint (2026-10-02):** [8R3 attack/effect/AI regression report](../handoffs/2026-10-02-task8r3-attack-effect-ai-regressions.md) records 330 reviewed rows (305 assigned to 8R3, 22 static-stat rows and three mixed 8R2 rows), 190 successfully executed witness methods and 31 additional Core cases. Actual Unity EditMode: Core 1365/1365, Framework 315/320, CombatViewBoundary 13/13; the same five Economy asset failures remain. Four existing test files changed; runtime/public APIs, View/Flow, DB and old fixtures/helpers did not. Completion means current-scope behavior coverage and classification; owner/adapter deletion and shared guard/geometry seam separation remain 8R5. Explicit test profiles do not close production DB mappings. Artifact lifetime remains 8R4, full Task8 and production acceptance remain open.

## Task 8R4 — Move Artifact battle regressions and isolate shared pure methods

**Files:** `C/Application/Artifact/` mixed files listed above, `C/Domain/Artifact/SoulFieldState.cs`, and legacy battle lifetime candidates. Inspect `T/ArtifactActionProjectionTests.cs`, `ArtifactOutcomeAtomicityTests.cs`, `ArtifactSoulLifecycleTests.cs`, `ArtifactEffectsIntegrationTests.cs`, `ArtifactHealingChaliceTests.cs`, `ArtifactLastStandCombatTests.cs`, `ArtifactStageLifetimeTests.cs`, `ArtifactRunHandoffTests.cs`, `ArtifactReleaseFailureTests.cs`, and direct transitive callers found by 8R1.

**Destination tests:** `T/ArtifactSoulBufferCalculationTests.cs`, `CombatCommandEffectTests.cs`, `Framework/CharacterBattleSettlementTests.cs`, `CharacterRosterBattleCycleTests.cs`, `CombatCommandPublicationTests.cs`. Keep Artifact compiler/rule/layout tests for surviving functionality, including `ArtifactEffectCompilerTests.cs`, `ArtifactLastStandRuleTests.cs`, `ArtifactThornHeartRuleTests.cs`, and `ArtifactLayoutLeaseTests.cs` as applicable.

**Interfaces:** `SoulEffectCalculator.Collect(SoulCollectorInput, SoulFieldSnapshot, bool) -> SoulActionChanges`, `PlanCountdown(SoulFieldSnapshot) -> SoulFieldChange`, current BattleCompletionCalculator and publication APIs. Preserve the pure members in the mixed-file table.

- [x] Map soul collection/death/countdown cases to owned count and immutable field facts: `+1` attack/max HP per soul, default field lifetime of five turn countdowns, correct actual death anchor, no consumption for rejected/dead arrivals, no second growth at next preparation, and current committed-movement ordering.
- [x] Map existing combined Last Stand/temporary bonus and Chalice cases. Preserve the current two-Chalice settlement example `7/15 -> 13/15` once, next-battle persistent HP, transient bonus cleanup and separate stage/map record retention. If event receipts are needed by an existing approved read contract, verify that exact current result path; do not fabricate a new result-history Buffer.
- [x] Inspect real Artifact acquisition/layout/lease/release consumers before retiring any lifecycle wrapper. Distinguish mutable battle ownership from immutable compiled definitions and a genuinely owned external resource. Keep independent Artifact editing functionality out of the deletion group.
- [x] Keep current unsupported reflection behavior explicit: SoulEffectCalculator rejects ThornHeart attack settlement while its integration order is unconfirmed. Retain pure ThornHeart rule tests and record production integration as gated; do not activate it or reinterpret a skipped old integration test as supported gameplay. Rider, temporary max-HP design and multicell rules remain scoped deferrals.
- [x] Migrate remaining approved behavioral tests to current inputs/publication. Then trim old instance projections, entity capture/evaluation overloads and mutable SoulFieldState members only when their last old caller can be retired. Preserve pure signatures and source GUIDs.
- [x] Run the migrated tests plus existing Artifact/compiler/rule suites. Keep actual DB-loaded Artifact acceptance separate from tests with explicit effect definitions.

**Acceptance:** Current soul, bonus, healing and battle cleanup no longer need an old UnitState/StageSession owner. Any surviving Artifact resource/editing API has a recorded consumer and separate responsibility, not an unexplained blanket exemption.

**Execution checkpoint (2026-10-02):** [8R4 Artifact/soul regression report](../handoffs/2026-10-02-task8r4-artifact-soul-regressions.md) records all50 assigned rows reviewed,606 original methods retained,44 Passed witnesses and17 explicit source responsibility reviews. Four existing test files add11 cases plus persistent-HP assertions. Actual final Unity EditMode: Core1372/1372,Framework319/324,CombatViewBoundary13/13; exact five existing Economy failures remain. Current calculations and canonical publication require no old mutable combat owner; runtime/API/DB/View/Flow are unchanged. The conditional trim gate was inspected and remains closed because old Stage/test callers still exist: physical member/owner/helper deletion stays8R5. Separate Artifact editing/layout leases and shared pure signatures/GUIDs are preserved. Test-defined loadouts do not close DB-loaded acceptance; fullTask8 remains open.

## Task 8R5 — Remove the audited combat host and obsolete dependency closure

**Files:** The exact manifest produced by 8R1 and updated by 8R2–8R4; source and matching `.meta` files. Trim the mixed AI/timeline files without deleting current functions. Migrate `T/CombatViewBoundary/StageCombatViewBoundaryTests.cs`, `StageGimmickViewBoundaryTests.cs`, `StageSessionViewTestFactory.cs` to current contract assertions or current Systems tests as appropriate. Remove `T/StageSessionTestFactory.cs` and `LegacyArtifactStageInput.cs` only after their last caller is removed.

**Interfaces:** No new production API. Required current signatures are the preservation boundary. Public/shared Contracts are retained when DB/View or a legitimate standalone consumer still needs them.

- [x] Close every manifest row: migrated, shared/preserved, obsolete, or blocked by a specific unsupported requirement. For a real missing approved behavior, return to its task instead of deleting its tests. A test name containing an old type is not sufficient deletion evidence.
- [x] Remove legacy host factories/services and the exclusively dependent state graph as a coherent change. Remove object-bound prepared plans/owners only after all consumers in that group and direct tests are accounted for. Verify absolute paths inside the checkout before filesystem removal; no recursive folder purge.
- [x] In mixed files remove only the old members. Preserve AI Calculate, fact-based selection, Artifact pure adapters and static timeline math/tie functions. Simplify unused constructors/fields only when this follows directly from caller removal.
- [x] Remove or migrate legacy-only test constructors, reflected state mutation and retained snapshots. Do not expand production visibility or add test compatibility adapters. Maintain assembly boundaries; boundary tests need not construct an obsolete StageSession to prove they cannot use Domain types.
- [x] Search all production sources, tests and Unity asset/GUID references again. Compile and run affected suites before declaring any dependency group removed. Do not remove common Contracts solely because the current checkout lacks another owner's unmerged caller.

**Acceptance:** Proven obsolete battle hosts and persistent state owners have no callers. Retained names such as SoulFieldState/CombatTimelineScheduler contain only justified pure members or a precisely recorded surviving responsibility. No replacement mutable host, Core combat execution path, or whole-buffer snapshot/restore adapter has appeared.

**8R5 completion checkpoint (2026-10-02):** [Retirement report](../handoffs/2026-10-02-task8r5-legacy-retirement.md). All335 source/606 method rows have scoped closure;90source/meta pairs removed,17mixedsources retain pure/public-value responsibilities,52test/helper pairs retired. Independent review identified one missing pure provider guard, restored and verified. Final actual Unity Core860/860,Framework319/324,Boundary6/6;1185/1190 with exactly five unchanged Economy failures. Production acceptance is not claimed. 8R6 remains unstarted; no commit/push.

## Task 8R6 — Verify the independent Core slice and update handoff status

**Files:** Coverage report from 8R1, this plan's checkboxes, the master Task 8 checklist, the existing Core-to-View handoff and current assembly/source/test files for audit. Do not change implementation merely to make unrelated production data tests green.

**Interfaces:** Consumes completed coverage and fresh compilation/test evidence. Produces explicit Core completion versus external/deferred dependencies; no runtime interface.

- [x] Verify the registered processing path uses Buffer facts and existing publication APIs; no Core input write/ack, long-lived state copy, direct View calculation, or duplicate Plan/Job. Preserve service/module registration, parent lifecycle propagation and existing Runner completion semantics.
- [x] Re-run the final three assemblies after the final code change. Compare failure names and reasons to the 8R1 baseline, not only totals. Explain test count changes using the method-level coverage matrix. Keep mock/pure, actual Buffer/service, existing Job and PlayMode evidence distinct.
- [x] Audit stage clear/boss clear, acknowledgement, EndBattle and subsequent preparation retention with existing ChapterGraphPreparation/CharacterBattleSettlement tests. No ordinary path may dispatch ResetRunProgress. Confirm new command numbers/results are not replayed across cleanup.
- [x] Update the master Step 6 and Step 7 only to the extent their criteria are met. If production/shared consumers still require a legacy type, identify the exact type and blocker and keep overall retirement incomplete. Completing this independent slice never closes missing production DB or joint runtime gates.
- [x] Have one final read-only review of the whole execution diff and coverage mapping, following the execution workflow. Fix concrete correctness or ownership findings; do not expand into stylistic refactoring.
- [x] Publish the updated handoff with: removed groups, retained pure/shared/separate features, any blocked groups, exact next owner actions, current test results and production limits. Preserve existing user work and importer-only file bytes. No automatic commit/push.

**8R6 completion checkpoint (2026-10-02):** [Final independent Core audit and View handoff](../handoffs/2026-10-02-task8r6-core-closure-and-view-handoff.md). Fresh actual Unity Core 860/860, Framework 319/324, boundary 6/6: 1185/1190, with five failure names/full messages exactly equal 8R1 and final 8R5. All 336 distinct witnesses for the 606 historical method rows pass; all 335 source classifications retain scoped closure. One independent read-only whole-execution review found no actionable correctness/ownership issue and confirmed authorized merge attribution. Master Task 8 Steps 6–7 close; full Task 8, real DB/Artifact authoring, three View-owned Job call sites, startup timing, F1/F2 and PlayMode remain open. No obsolete owner group is blocked; existing immutable public results, pure/shared rules and separate Artifact editing responsibilities remain. This step changes documentation only, preserves incoming work/importer bytes and makes no commit/push.

## Dependencies that do not block unrelated retirement tasks

| Dependency | Exact trigger and next action | Owner / completion evidence |
| --- | --- | --- |
| Attack method DB | Actual Method_Key table/query/unit reference becomes available. Adapt it in the existing DatabaseBattleInitializationSource/BattleDefinitionAdapter path, validate all required profiles and return BattleInitialization. The current source deliberately throws before installation; do not replace that guard with fixtures/defaults. | DB owns schema/data; Core owns adaptation and real-query validation. Actual assets and successful PrepareBattle are required for production readiness. |
| Unit/Economy definitions | Authored main/unit data and combat Economy references are supplied. Validate through existing query/definition conversion. | DB fixes assets; Core verifies. Existing five Economy failures cannot be waived as new success. |
| Artifact definitions | Actual Soul/Chalice data conform to the compiler contract; any remaining effect ordering is explicitly confirmed. | DB/designer supply facts/decisions; Core tests actual DB -> compiler -> command -> Buffer behavior. Pure fixtures do not close G9. |
| Initial roster/wallet and stage entry caller | The owner supplies the loadout key, startup timing, wallet initialization policy and the existing execution entry point. Preserve InitializeRoster and chapter prepare/confirm semantics. | Existing execution owner wires calls; Core validates service preconditions/side effects. Do not build another Run coordinator or let View assemble Core facts. |
| Combat execution/exit | View/Flow wiring and Framework F1/F2 behavior are available for joint validation. | View owns combat Jobs/Flow/Plan and acknowledgement; Framework owns Runner fault behavior; Core diagnoses its service/publication failures. A direct-call test does not prove this gate. |
| Deferred gameplay | A specific multicell, custom movement, ThornHeart/reflection or other expressly deferred decision changes the approved scope. | Write only the necessary design amendment after confirmation. It does not block unrelated owner removal or authorize silent feature activation. |

Do not create another implementation plan merely because a dependency arrives: execute the corresponding remaining master Task 8 step and update its evidence. A genuinely new ownership/gameplay design requires its own scoped design decision.

## Verification commands and result interpretation

Run searches from the actual checkout. Expand the symbol set from the manifest rather than assuming this initial list is exhaustive:

```powershell
git status --short
git rev-parse HEAD
rg -n 'StageSessionFactory|StageServices|StageSession|StageCombatFacade|UnitState|CombatEntityState|RunCharacterDeckState|IEnemyAiCombatGateway' Assets/ProjectL/Script Assets/Tests --glob '*.cs'
rg -n 'CalculateActionInterval|CalculateTieKey|ProjectCollection|PlanCountdown|ArtifactCombatFacts|ArtifactAttackPowerEvaluator' Assets/ProjectL/Script --glob '*.cs'
rg -n 'ResetRunProgress|JobType.NewRun' Assets/ProjectL/Script --glob '*.cs'
git diff --check
```

Use the existing Unity test route; never force-close an Editor owning the checkout. Example final run, with logs under the preserved local execution workspace:

```powershell
$retirementProject = (Get-Location).Path
$retirementOutput = Join-Path $retirementProject '.superpowers/sdd/core-combat-retirement'
New-Item -ItemType Directory -Force -Path $retirementOutput | Out-Null
$retirementArgs = @('-batchmode', '-nographics', '-projectPath', $retirementProject,
    '-runTests', '-testPlatform', 'EditMode', '-assemblyNames',
    'ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary',
    '-testResults', (Join-Path $retirementOutput 'final.xml'),
    '-logFile', (Join-Path $retirementOutput 'final.log'))
$retirementProcess = Start-Process -FilePath 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -ArgumentList $retirementArgs -PassThru -WindowStyle Hidden
$retirementProcess.WaitForExit()
[xml]$retirementXml = Get-Content (Join-Path $retirementOutput 'final.xml')
$retirementXml.'test-run' | Select-Object total, passed, failed
$retirementXml.SelectNodes('//test-case[@result="Failed"]') | ForEach-Object {
    $_.fullname
    $_.failure.message.InnerText
}
```

For a task-focused run, use `-testFilter` with the fully qualified destination fixtures listed in that task and a distinct XML/log pair. Core tests use `ProjectL.Tests.Core`, Systems/Buffer tests `ProjectL.Tests.Framework`, boundary tests `ProjectL.Tests.CombatViewBoundary`. Read the XML: nonzero intended case count, no new failures, no compilation errors. Known baseline failures do not constitute a fully green run. No need to replay RED for already passing migrated behavior; new fixes require a meaningful failing case, then a passing case and affected regression checks.

Restore only importer changes established by pre-run byte backups; never blanket-revert the incoming Task 8P work. For new/moved/deleted C# files include their `.meta` files and check GUID references. Historical logs are not current execution evidence.

## Planning handoff

This is a documentation-only plan ready for review. Execution starts at **8R1**, followed by the current-behavior migration tasks, then deletion and final audit. Do not begin by deleting UnitState/StageSession or by inventing production DB bindings. Task 8P is already complete and is an input to this continuation, not work to repeat.
