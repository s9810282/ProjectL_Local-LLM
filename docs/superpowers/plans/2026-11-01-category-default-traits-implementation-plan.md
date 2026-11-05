# Default Traits by Combat Category Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` for inline execution or `superpowers:subagent-driven-development` if the user selects delegation. Track tasks with the checkboxes below. Stop at each numbered Phase exit for user review.

**Goal:** Give every Rider Leap and every Heavy Steadfast Will through one editable category configuration, including a Main that specializes into either category.

**Architecture:** Export optional trait keys from the existing category SO. Bind those keys to normal definitions in the immutable catalog used by Core. Resolve effective traits from the raw unit, combat category and growth history using one composition policy. Existing trait rules, roster publication, Buffer transactions and combat profiles remain the behavior and state owners.

**Tech stack:** Unity 6000.0.73f1, C#, ScriptableObject authoring, NUnit EditMode tests.

**Spec:** [Default traits by combat category](../specs/2026-11-01-category-default-traits-design.md)

**Status:** Both phases implemented and verified on 2026-11-01. Production Rider grants Leap and Heavy grants Steadfast Will. The integration suite retains the original camera test failure.

## Global constraints

- Each real category (Main, Rider, Heavy, Cannon, Boss) accepts zero or one reference. Production initially assigns Rider and Heavy only. Growth pool membership and the original Main progression remain independent of this setting.
- Preserve native trait parameters, owned growth history, category identity, stable public callers and captured battle definitions. Keep pure data free of Unity types. Use Korean code comments, and include Unity metadata for new source files.
- Use existing service and transaction boundaries. Do not implement an effect in a category branch, add a persistent slot, edit individual units to roll out the mapping, or change View, FieldCamera or Architecture. Production activation belongs at the end.
- For implementation, use the repository's approved AppData-access Unity route. Work only within the authorized phase. Report changes, evidence and remaining checks, then stop. Commit and push are user-owned.

## Review focus

1. Level-one units with empty growth currently take fast paths in stats and battle preparation. Both must receive their configured trait (Phases 1 and 2).
2. A legitimate pre-specialization Acquire can collide with a new default. Preserve the spent step while rejecting a second recorded Acquire for the same key (Phase 1).
3. A future parameterized replacement can collide with a native spec, or remove the only source for an Enhance. Verify the actual spec values and handle reload failure atomically (Phase 1).
4. A category edit can invalidate an offer or a prepared specialization before publication. Preserve existing rejection and transaction semantics (Phase 1).
5. Owned profiles, unowned profiles and definition discovery use different entry points. Exercise both factions, later entry and the definition captured for active combat (Phase 2).

## File map and verification conventions

Paths below use these repository-relative roots.

| Symbol | Root |
| --- | --- |
| S | `Assets/ProjectL/Script/Systems` |
| DB | `S/DataBaseSystem/Data/InGame/Table` |
| CS | `S/RuntimeSystem/InGameSystem/CoreSystem` |
| C | `CS/CoreCaculator` |
| T | `Assets/Tests/EditMode` |
| I | `T/CoreIntegration` |
| A | `Assets/ProjectL/DataBaseSO/InGameGroup` |

Use the existing local runner `.superpowers/position-swap-recon-undo/run-tests.ps1` with fresh run labels. Its evidence folder is local. If the runner is absent on a different checkout, use the same Unity version with `-batchmode -nographics -runTests -testPlatform EditMode -assemblyNames`, unique XML/log paths, and the approved launch route. Run one Editor at a time. Example commands for a targeted fixture and both assembly gates:

```powershell
$runner = '.superpowers/position-swap-recon-undo/run-tests.ps1'
pwsh -NoProfile -File $runner -Run CategoryDefaults-P1-growth-01 -Assembly ProjectL.Tests.Core -Filter ProjectL.Tests.Core.TraitGrowthRulesTests
pwsh -NoProfile -File $runner -Run CategoryDefaults-P1-core-01 -Assembly ProjectL.Tests.Core
pwsh -NoProfile -File $runner -Run CategoryDefaults-P1-integration-01 -Assembly ProjectL.Tests.CoreIntegration
```

Use the assembly namespace followed by the fixture or test name for each filter. Change run labels on every attempt. For each task, add the named regression cases first, observe the intended failure, then run them again after the change. Read the XML for selected count, exact failures and skips. A compilation failure or an empty test selection does not prove a behavioral regression. Record full-suite results against the baseline at each phase exit. If the prior StageKey 2101 failure recurs, record its current name and diagnostic separately. Treat a failing suite as non-green and investigate any new failure before advancing.

## Phase 1 — Define who owns each trait and what a level-up may offer

**Reviewable outcome:** With fixture assignments, roster queries and growth offers agree as soon as a unit exists or a Main specializes. A designer can replace or clear a reference. Production assets still have their original values.

### 1.1 Author the mapping and bind it to the catalog

**Owners:** `DB/Category/Item/CategoryConfigDataSO.cs`, `CategoryConfigData.cs` in the same folder, `CS/CharacterSystem/TraitGrowthDefinitionAdapter.cs`, `C/Contracts/Enhancements/TraitGrowthCatalog.cs`, `S/Factory/DBFactory/Group/InGameDataBaseFactory.cs`. Tests in `I/TraitGrowthAuthoringTests.cs` and `T/TraitGrowthCatalogTests.cs`.

- [x] Capture the starting revision, scoped working-tree changes and fresh Core/CoreIntegration baselines before edits. Inspect the active category configuration, trait definition table, growth pools and ShotGun's explicit Steadfast Will.
- [x] Add authoring and catalog cases for empty legacy input, two assigned keys, clearing one entry, and the same trait in two categories. Assert that a fixture replacement reads its normal parameters from the registered definition. Reject unknown keys, missing tables for a configured default, retired, pending and timing-deferred defaults, even when no growth step uses the key.
- [x] Add five optional SO references to `TraitGrowthDefinitionDataSO`. Export immutable category-to-key data from `GetData()`. Extend `TraitGrowthDefinitionAdapter.Build` to accept that mapping and let `TraitGrowthCatalog` expose `TraitSpec GetDefaultTrait(CategoryKey category)` (null for unassigned). Retain the empty default for existing constructors and fixture callers. Reuse normal specs from the existing catalog.
- [x] Feed the mapping into DB registration before the existing optional-table early return. Keep legacy both-tables-absent content valid if the mapping is empty. Verify authoring and catalog tests against this boundary.

### 1.2 Compose native, category and earned traits once

**Owner:** `C/Application/Enhancements/OwnedUnitSpecResolver.cs`. Reuse `C/Application/Roster/MainSpecializationRules.cs` and the existing `UnitSpec` constructor contracts. Tests in `T/OwnedTraitGrowthTests.cs`, `T/MainSpecializationGrowthTests.cs` and `T/TraitGrowthRulesTests.cs`.

- [x] Add cases asserting one Leap for a native Rider with no growth, one Steadfast Will for a Heavy with a native copy, and one Leap for a Main with an earlier Acquire after Rider specialization. Assert the recorded step is still spent. Repeat resolution from raw input and compare keys and spec values.
- [x] Retain `Resolve(UnitSpec basis, OwnedCharacterRecord owned, TraitGrowthCatalog catalog)`. Add `ResolveBase(UnitSpec basis, TraitGrowthCatalog catalog)` for raw unowned input. Share the composition body. Resolve the current category before the empty-growth fast path. Preserve native same-key spec parameters. Append a missing normal default and replay recorded steps in order using source-aware duplicate checks. The result retains identity and specialization separately.
- [x] Pin reload behavior with a parameterized replacement, clear, historic Acquire and legitimate Enhance. Assert earned ownership survives clearing a default, and an Enhance whose sole normal source disappeared fails with unit, category and trait identified. Reject repeated Acquire/Enhance, native Acquire collision and invalid step/level/pool combinations. No operation mutates its input records.

### 1.3 Connect roster, growth selection and specialization

**Owners:** `CS/CharacterSystem/{CharacterStatsQueryService,CharacterRosterService,CharacterRosterService.MainSpecialization,MainSpecializationDefinitionSnapshot,TraitGrowthCommandProcessingService}.cs`, `C/Application/Roster/{OwnedCharacterStatsCalculator,TraitGrowthRules,TraitGrowthCatalogValidator}.cs` and the registration owner in 1.1. Tests in `I/{MainSpecializationPublicationTests,MainSpecializationPreparationTests,TraitGrowthChoiceServiceTests,TraitGrowthTransactionTests}.cs`, plus the growth fixtures from 1.2.

- [x] Add fixture cases for both specializations, Cannon as a control, and an unspecialized Main. Immediately after a successful specialization, assert baseline trait keys, existing weapon and Main identity through the public query. Assert Rider's Acquire candidates exclude Leap, Heavy's exclude Steadfast Will, and another eligible category can still acquire either. An unrelated earlier selection coexists. An Enhance offer still requires pool, step and enhanced definition.
- [x] Read the mapped catalog in roster services for level-one units as well as grown units. Use the resolver's effective specs for baseline publication, candidate ownership, hypothetical profile construction and authoring validation of reachable steps. Keep the raw basis for replay. Check all affected native targeting profiles including enemies and zero-step units, plus legal Main specializations. Preserve existing optional targeting-method behavior on unaffected units. Enforce the existing minimum of three Acquire choices and one Enhance choice at applicable steps.
- [x] Extend specialization's existing prepare/apply definition comparison with the assignment keys and relevant trait values. Test changing the mapping between prepare and install. For growth, exercise both an old displayed offer and a change after choice preparation. Recheck the affected choice against current content at the existing publication boundary before installation. Assert no invalid selection, spent step or partial roster write. Preserve existing failure receipt and acknowledgment semantics.
- [x] Run targeted RED/GREEN cases, both full assembly gates and scoped diff checks. Report the results for level-one ownership, immediate specialization and duplicate offers. **Stop for Phase 1 review.**

### Phase 1 execution record

Base revision `c3b38fa1a`. The category SO exports optional trait references as immutable keys. The roster and growth paths resolve the configured normal spec using current combat category. Unit-authored parameters win on overlap, prior acquisitions remain recorded, and a successful specialization immediately updates roster queries. Growth publication rejects stale choices and invalid histories before installing changes.

| Unity 6000.0.73f1 EditMode gate | Baseline | Final |
| --- | --- | --- |
| ProjectL.Tests.Core | 4,524 / 4,524 | 4,530 / 4,530 |
| ProjectL.Tests.CoreIntegration | 628 / 629 | 646 / 647 |

All 24 new cases passed. Both final suites had zero skips. The single integration failure matched the baseline test and error: `CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset",False)` raised `System.TypeLoadException` loading that type from Cinemachine. Full integration remains non-green. Local XML, log and exit files use `CD-P1-base-{core,integration}` and `CD-P1-verified-{core,integration}` under the runner's evidence directory.

Regression cases cover direct Core eligibility, native and specialized roster queries, same-key acquisitions and upgrades, clearing or replacing an assignment, ineligible content, attack-method compatibility, and configuration changes between selection and publication. A remaining historical Enhance without a normal source prevents another unit from publishing growth. The prior specialization failure contract for a native Acquire collision was checked in the full suite. The production category asset, unit assignments and View logic were untouched. Live combat wiring and production activation start in Phase 2.

## Phase 2 — Use the same traits in battle and activate the designer settings

**Reviewable outcome:** The configured benefits work through production data for both factions, from the first battle to later entries. The player sees the same traits in roster, level-up and combat.

### 2.1 Resolve battle definitions, profiles and dependencies

**Owners:** `CS/CombatSystem/{DatabaseBattleInitializationSource,BattleDefinitionAdapter,CombatInputAssembler}.cs`, `C/Application/Combat/BattlePreparationCalculator.cs`, `C/Application/Enhancements/OwnedUnitSpecResolver.cs` (`BuildProfiles`). Check existing consumers `C/Contracts/Combat/BattleInitialization.cs`, `S/Factory/Runtime/Development/TemporaryBattleInitializationSource.cs` and `C/Application/Combat/Summoning/CombatSummonCalculator.cs`. Tests in `T/{BattlePreparationRuleTests,EnemySpawnCalculationTests,CombatSummonTests}.cs` and `I/{MainSpecializationBattleTests,LeapPublicationTests}.cs`.

- [x] Write fixture regressions for a fresh owned Rider, an enemy-only Rider, an authored Heavy, a generated Heavy enemy and a specialized Main. Compare the trait key/count in initial facts and command profiles. Include reserve/hand entry and repeated commands. In a replacement fixture compare actual parameter values in the effective profile and combat result.
- [x] Build the mapped catalog before `BattleDefinitionAdapter.ReadUnits` traverses dependency keys. Walk the effective unowned unit and applicable owned variants, including the specialized Main. Keep key-based traversal cycle-safe while checking distinct owned variants for different dependencies. Return raw unit definitions for later composition. Carry the same catalog through `BattleInitialization` copies and keep no-mapping temporary fixtures working.
- [x] Resolve base profiles for unowned actors and per-instance profiles for owned actors via 1.2. `BuildProfiles` must receive raw base profiles. Build initial definitions and command/preview inputs from equivalent resolved specs, including the current empty-growth and no-specialization shortcuts. Do not re-read live DB assignments during a captured battle. Verify changing the fixture mapping affects the next battle and leaves the already-captured one consistent.
- [x] Verify dependency discovery with an effective-spec fixture and retain existing authored summon-target collection. The current Necromancy default is ineligible and summon execution returns `DeferredUnitTurnTiming`. Assert this rejection and the effective target definition separately. Do not enable the deferred mechanic or claim live summon playback as evidence.

### 2.2 Activate production content and verify gameplay

**Production write:** `A/Category/Item/CategoryConfig_Default.asset`. **Read-only content references:** `A/TraitGrowth/Item/{TraitGrowth_Leap,TraitGrowth_SteadfastWill}.asset`, category growth pools, and `A/Unit/Item/Playerable/Unit_ShotGun.asset`. Tests in `I/{DataBaseAssetTests,TraitGrowthAuthoringTests,MainSpecializationBattleTests,MainSpecializationGrowthPublicationTests,LeapPublicationTests}.cs` and `T/{LeapTraitTests,SteadfastWillTraitTests,KnockbackImmunityPolicyTests}.cs`.

- [x] Add production assertions for Rider → Leap, Heavy → Steadfast Will and the three remaining empty assignments. Assert ShotGun retains its native entry and has exactly one effective Steadfast Will. Verify the other eligible categories still offer these keys and the actual pools pass exhaustive candidate-count validation.
- [x] Set only the two production references. Use a fixture to replace Rider's reference with a supported parameterized trait, then clear it. Verify a single data edit drives native Rider, specialized Main, enemy profiles and growth eligibility. An incompatible assignment fails before state publication. Leave the production file on the approved pair.
- [x] Exercise movement preview and execution across an intermediate wall/cliff, blocked landing and intermediate/destination mines. For Heavy, verify knockback preview and actual displacement agree, and pulling still works. Run the existing trait fixtures together with the DB-backed publication checks so the evidence covers configuration through behavior.
- [x] Run `DataBaseAssetTests` explicitly and run full Core and CoreIntegration sequentially on the final tree. Record assembly totals, skips, failures, XML/log locations and the scoped diff. Verify only the intended production configuration changed, pure types have no Unity dependencies and no View-side rule was added. **Report and stop for final review.**

### Phase 2 execution record

The production category asset assigns Leap to Rider and Steadfast Will to Heavy. One catalog captured at battle entry resolves the initial units and each later command or preview. The database adapter retains raw definitions and walks summon dependencies for native and owned variants, including a specialized Main with an older enhanced definition. Unowned command profiles carry the category grant. Owned profiles compose from the raw basis so a unit-specific copy keeps its original parameters.

- The real loadout 1 Main (`Unit_Akimbo`) specialized through the roster service. Production stage 1101 preparation contained the correct combat category and exactly one corresponding status for each specialization. Its native Rider enemies also received Leap.
- A controlled database-backed encounter using production category and trait tables exercised the command services and Buffer publication. The Rider Main crossed the intermediate wall and landed at (4, 1). Against the same enemy knockback, the Heavy Main took damage and remained at (2, 1), while the Cannon Main moved to (2, 0). Clearing the live DB mapping after entry did not change the captured battle.
- Replacing Rider's default with Knockback in a fixture gave the specialized Main, native Rider and Rider enemy the configured distance. Preview and attack publication agreed on one cell. Clearing the mapping removed the category movement from the next fixture battle. Separate tests covered weighted Heavy enemies, the native ShotGun overlap and definition dependency discovery. The existing suite verified cliff crossing, blocked landings, mines, and pulling a Steadfast Will holder. Deferred summon execution remained outside live gameplay coverage.

| Gate | Result | Local evidence under the runner directory |
| --- | --- | --- |
| Production `DataBaseAssetTests` | 12 / 12 passed | `CD-P2-verified-assets` |
| `ProjectL.Tests.Core` | 4,531 / 4,531 passed | `CD-P2-core2` |
| `ProjectL.Tests.CoreIntegration` | 655 / 656 passed | `CD-P2-verified-integration` |

Zero skips. The final integration failure matched the Phase 1 baseline in test name and error message (the Cinemachine camera type load failure recorded above). The final two added tests belong only to CoreIntegration. Verification used Unity EditMode. No manual PlayMode or presentation acceptance was performed. Production data edits were confined to the existing category configuration. View code was untouched.

## Change locality at handoff

After Phase 2 a compatible balance edit touches one category asset. Validate its affected units, specialization, growth choices and the replacement's combat behavior. A new effect still belongs to the trait registry and its own implementation. The cross-cutting work in this plan is needed once because the current roster, DB validator and combat entry points resolve unit definitions separately. Report automated Core/DB evidence separately from any manual UI or PlayMode observation.
