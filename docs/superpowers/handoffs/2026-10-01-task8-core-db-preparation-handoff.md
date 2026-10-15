# Task 8 Core services and retirement handoff

Original date: 2026-10-01. Updated: 2026-10-02 after Task 8R6. Scope: Core-owned Systems definition assembly, registration and independent legacy retirement. This is a partial checkpoint, not production activation or completion of [Task 8](../plans/2026-09-29-character-buffer-effect-migration-plan.md#task-8-core-production-binding-view-owned-execution-integration-and-old-path-cleanup).

## Core delivery status for View

**Ready to hand off the implemented Core services for View-owned execution wiring.** Tasks 0–6 have recorded completion, and Task 7A–7E plus the parent Task 7 are implemented for the currently approved scope. The independent Task 8 Core work is implemented: existing service/module binding, selected-stage DB fact conversion and weighted enemy placement conversion. View does not need to wait for additional implementation of the current command, preview or automatic processing logic before working on its call sites.

The registered `CoreGroup.Combat` supplies `PrepareBattle()`, `ProcessNextCommand()`, `ProcessLatestPreview()`, `AdvanceCombat()` and `EndBattle()`. The [Task 7 service/read/publication contract](../plans/2026-10-01-combat-buffer-command-task7-contract.md#processing-and-read-contracts) specifies return meanings, result identities, retention and read APIs. View/Flow owns LifeCycle, Flow, Plan, all combat Jobs, service-call wiring and input acknowledgment. Core retains rule calculation and existing Buffer publication. Existing definition doubles can support owner integration tests; they are not production authoring or an alternate runtime path.

**Production battle preparation is still gated.** The default source rejects the missing targeting DB contract before any gameplay installation. Actual authored unit/Economy/Artifact data and agreed startup/loadout/wallet caller timing also remain integration dependencies. They do not mean that the corresponding command calculation APIs are absent. Once those contracts/data arrive, Core must adapt the actual method query, verify the real preparation/effect route and assist with service-boundary integration failures. Tasks 8P and 8R1–8R6 retire the obsolete map/combat owners after current-boundary regression mapping. Full Task 8 remains incomplete because production dependencies and joint runtime acceptance remain open.

**Task 8P update:** The duplicate map/progression owners and their disconnected Run callers have now been removed in the working tree. Useful tests were migrated to current Systems/Buffer progression, shop and recovery paths, with obsolete Run contracts explicitly classified. See the [Task 8P retirement report](2026-10-01-task8p-map-ownership-retirement.md) for final verification and limits. This does not complete Task 8's DB targeting, View execution replacement, or production combat activation. Independent combat retirement is now recorded in the 8R6 report.

Suggested checkpoint commit title: `feat(core): implement combat processing and DB preparation adapters`. The commit represents the Core delivery described here, not complete production integration. Review and include the new source/test `.meta` files with their files; no commit or push has been performed by this handoff update.

## Implemented boundary

The existing `InGameCoreSystemFactory.Create()` now registers `DatabaseBattleInitializationSource` as a lifecycle module and passes it to the existing `CombatProcessingService`. The source binds only `ICurrentProgressBufferReadSink` and `ICharacterBufferReadSink` via the existing root Buffer query in `PostBindService`; it retains service references, not copied progression or combat state. The parent Core/InGame groups forward binding. The injectable `IBattleInitializationSource.Read(IDataBaseQueryContext)` contract is unchanged.

`BattleDefinitionAdapter.Read(database, mapKey, stageKey, owned)` reads current Stage/Field/Roster/Unit/Structure/Category/Economy table facts into the existing Contracts values. It validates chapter/stage correspondence and combat type, includes owned card definitions even when absent from the stage roster, preserves explicit enemy placements and the separate headquarters placement, and preserves field sizes, deployment depths, night/timeline values, railway settings and economy numbers. Unit conversion keeps legacy definition metadata, independent owner range/impact, trait parameters and timeline values; it does not translate legacy enum numbers into new targeting methods.

The returned definition carrier has no new attack profiles while DB method authoring is absent. It must not be treated as a complete preparation input. The default source validates available facts and then raises the explicit T1/G7 dependency before `CombatProcessingService` can install a battle. Weighted EnemyLoadout conversion is now implemented as described below; it no longer raises the earlier missing-conversion gate. Production initialization remains unsuccessful until actual targeting data/contracts are connected.

No public service method, Buffer field, shared Framework contract, JobType, execution path or View input writer was added. No DB schema/SO asset or production View/Flow/Plan/Job file was edited.

## Independent weighted-spawn continuation

The existing concrete allocation behavior was recovered from `a18d86d6^:Assets/Scripts/ProjectL/Unity/Setup/Stage/EnemySpawnPlanner.cs` and `StageEntryDataBuilder.cs`. These historical files establish migration behavior, not a new approved long-term allocation design. The current pure `Application.Combat.EnemySpawnCalculator.Calculate(stage, candidates, spawnCount, seed, structures)` uses only Core Contracts/Domain and returns placement definitions. It neither creates runtime entities nor installs state. The existing preparation calculation remains responsible for validating/creating the final combat proposal and the existing Systems service publishes it.

`BattleDefinitionAdapter` reads the existing EnemyLoadout query, checks candidate membership against the stage UnitRoster before adding owned definitions, assembles immutable `EnemySpawnCandidate` facts and calls the calculator. Candidates pair an existing UnitSpec with its DB weight; no second authored mapping or retained runtime state is introduced.

- Explicit enemy placements and a referenced loadout are mutually exclusive, including a zero-spawn loadout.
- Draw all unit types with the loadout seed and authored entry order, allowing repeated types, before shuffling available cells with the same Random instance.
- Available cells follow row-major enumeration of the enemy deployment zone, excluding the complete rotated footprint of every initial enemy structure, including the headquarters.
- Preserve R0 placement and fail when the requested count exceeds available cells; no clamping or silent spawn loss.
- Keep zero-spawn loadouts empty without requiring candidates. Invalid weights/roles/owner eligibility and overflowing weight totals fail explicitly.
- Gate multi-cell unit spawning pending design confirmation; do not infer a footprint allocation policy from anchor selection.
- Preserve the prior headquarters-first initial structure order, followed by authored structure placements, so entity allocation order is not changed by this adapter.

All lists, RNG and occupancy exclusions are command-local. There is no new Buffer, global random service, manager, startup loop, Job or Plan. The source continues to reject missing targeting schema after generated definitions are validated and before gameplay writes.

## Startup and remaining ownership

`CoreGroup.Characters.InitializeRoster(loadoutKey)` already performs explicit DB-backed initial ownership acquisition. Its caller must supply the agreed real loadout key after InitData/PostBind. This checkpoint does not introduce a lifecycle caller, select the first/only loadout by convention, reacquire cards on battle entry or reset currency from `Economy.InitialGold`. Initial wallet/startup timing needs its actual owner contract; carrying the DB value does not apply it.

| Dependency | Required next work / owner |
| --- | --- |
| T1 method schema | DB owner confirms Method_Key storage, table/query contract and unit reference; Core adapts that actual query into `CombatAttackProfile` and validates completeness before preparation |
| Authored unit data | DB owner supplies main and required confirmed character definitions; existing enum keys or fixtures are not authored definitions |
| Economy | DB owner connects actual combat Economy references; no default/free cost or validator relaxation |
| Weighted enemy loadout | Core conversion is implemented and verified against recovered behavior with existing DB query formats; actual production preparation still depends on the method/data gates above. Multi-cell allocation remains explicitly deferred |
| G9 Artifact data | DB owner aligns actual Soul/Chalice definitions with canonical compiler contracts; Core verifies the real table/compiler path without changing authoring semantics |
| Startup caller | Existing execution owner connects explicit roster/loadout and wallet initialization at the agreed lifetime boundary |
| View execution | View/Flow owns LifeCycle, Flow, Plan, all combat Jobs, their service wiring and input/presentation acknowledgment; Core provides synchronous processing and retained read results |
| F1/F2 | Framework/Flow owners complete real fault/exit ordering acceptance before activation |

The current production method query does not exist; the user confirmed no known approved schema or implementation. This is missing data/contract information, not authorization for Core to author a second static mapping.

## Current retirement and View execution handoff

The [Task 8R6 final Core report](2026-10-02-task8r6-core-closure-and-view-handoff.md) records the complete source/method audit and current limits. Ninety old source/meta pairs and 52 obsolete test/helper pairs are removed; 17 mixed sources retain justified pure/public-value members. StageSessionFactory/StageSession/StageServices/StageCombatFacade, mutable UnitState/CombatEntityState and duplicate Run/map owners no longer exist. AttackExecutionResult and MovementExecutionResult remain existing immutable public results; pure effect/geometry/timeline/calculation Contracts and separate Artifact editing features are preserved. No obsolete combat owner group is retained pending DB. This is independent Core closure, not a playable cutover.

Actual CombatPrepareJob already calls CoreGroup.Combat.PrepareBattle(). CombatCommandJob, CombatPreviewJob and CombatAdvanceJob still throw placeholder exceptions in the current checkout. View/Flow must wire their existing CoreGroup.Combat.ProcessNextCommand(), ProcessLatestPreview() and AdvanceCombat() calls respectively. Preserve completed guards and complete after normal synchronous return, including false; do not treat false as a processing fault or wait for true by repeatedly executing a Job. ProcessNextCommand true includes a handled rejection; acceptance is read from Buffer results. Exceptions must remain faults. Core has not implemented these View-owned call sites or created another Plan/Job/Runner.

Core reads CombatViewBuffer input and never clears/removes/acknowledges it. View owns old-input cleanup and presentation acknowledgement. Completion/latest result and counters survive EndBattle; next preparation clears completion/preview while preserving latest result/counters. EndBattle guards unfinished battle state, not presentation acknowledgement. F2 must order voluntary defeat, final result presentation/acknowledgement, cleanup and transition. The current LifeCycle exit is not proof of that ordering. Framework F1 must ensure a faulted Job cannot run again on a later tick; the current Runner does not latch faults. See the final report for exact owners and evidence.

## Verification and limits

**Fresh Task 8R6 verification (2026-10-02):** Actual Unity 6000.0.73f1 EditMode: Core 860/860, Framework 319/324, CombatViewBoundary 6/6; total 1185/1190, five failed, zero skipped/inconclusive, exit 2. All five failure names and full Economy-omission messages equal 8R1 and final 8R5. All 336 distinct witnesses for the historical 606 method rows pass. The final case set equals 8R5; lower historical totals are explained by the retirement case ledger, not skipped tests. No PlayMode, real default-source preparation success or production command/preview/advance Job acceptance is claimed. Full details and exact failure names are in the [8R6 report](2026-10-02-task8r6-core-closure-and-view-handoff.md).

The following Task 8 results are historical implementation evidence, not this final run.

Historical Task 8 weighted-spawn continuation: Unity 6000.0.73f1 focused EditMode tests passed 40/40, exit 0. They cover fixed-seed legacy draw/shuffle results, repeatability, rotated footprint exclusion, capacity rejection, authored/loadout exclusivity, empty/out-of-roster candidates, deferred multi-cell spawning, owner eligibility, weight overflow and pre-write missing-method rejection for both explicit and generated enemies. These are query-double/pure calculation tests; they do not demonstrate actual authored DB readiness or production Job execution.

Historical Task 8 weighted-spawn full EditMode run: Core 1388/1388, Framework 281/286, CombatViewBoundary 13/13; total 1687, passed 1682, failed 5, exit 2. The exact failed-test-name set matches the previous Task 8 run with zero differences; all five still report missing CombatStageDetail Economy. No PlayMode or playable cutover was tested. Fresh independent read-only review found no actionable defect in the complete scoped Task 8 code, including the continuation; the reviewer did not run tests.

Continuation evidence in the same workspace directory: `spawn-red.xml/log` (16 total, 7 expected failures before conversion), `spawn-focused.xml/log`, `spawn-final.xml/log`, and `continuation-start-sha256.json`. Prior dirty files were preserved except the intended adapter/tests and these two plan checkpoints/handoff. Unity-only FMOD/Sequences import effects were restored to the exact bytes captured before this continuation. No commit or push. The new candidate/calculator/test metadata is included; no asmdef change.

The following paragraphs preserve verification of the initial Task 8 checkpoint and are superseded by the fresh 8R6 counts below.

New adapter/source tests were written before implementation. Unity RED exited 1 with the missing `BattleDefinitionAdapter` declaration; GREEN ran the new definition assembly/gating tests and existing CharacterBattlePreparation/CharacterRosterService fixtures, 26/26 passed. These use real DB value formats and query doubles. They prove conversion, Factory/module binding, selected-stage lookup and preservation of roster/main/wallet/combat/result values on the missing-method gate. They do not prove authored DB mapping or production preparation success.

Final Unity 6000.0.73f1 EditMode verification: Core 1383/1383, Framework 272/277, CombatViewBoundary 13/13; total 1673, passed 1668, failed 5, exit 2. All five failures are the same pre-existing actual CombatStageDetail Economy omissions. The actual Artifact table/compiler regression continues to demonstrate the unresolved SoulDrop authoring rejection; no G9 readiness is claimed. An initial broad run also found a publication fixture using the default Factory without InitData; its helper was corrected to the existing explicit definition source and normal InitData/PostBind sequence, with all publication assertions preserved and passing in the final run. No production readiness check was relaxed.

Evidence directory: `.superpowers/sdd/2026-09-29-character-buffer-effect-migration-plan/task8/` (`red.log`, `focused.xml/log`, `full.xml/log`, `final.xml/log`). A fresh independent read-only review found no actionable defect in the scoped Core/source/factory/tests and the fixture correction. The reviewer did not run tests. Task 8 checkboxes remain open while production dependencies are unresolved.

Task-start hashes confirmed that prior dirty work was preserved except the intentional overlap in the two implementation plans, Task 7 lifetime contract wording and one publication test helper. Unity-only FMOD/Sequences import changes were restored to verified original bytes. `git diff --check` passed; no commit or push was made.

The Task 7 contract's lifetime wording was corrected: the latest result/counters survive next preparation; completion is cleared at next preparation, and preview is cleared at cleanup/preparation. The established code and retention tests were not changed.
