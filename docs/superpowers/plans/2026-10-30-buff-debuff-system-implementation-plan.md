# Reusable Buff and Debuff System Implementation Plan

> **For agentic workers:** Phases 0-F are complete within the approved Core scope; final acceptance and residual risks are in section 5.8. View implementation/visual acceptance and external delivery remain pending. Do not start new work or commit/push without user authorization.

**Goal:** Make existing trait effects reusable through a shared applied-status contract and lifecycle, migrate HuntersMark to configurable Weakness Exposure, restore configurable Regeneration, and retire the agreed obsolete components without changing other gameplay.

**Architecture:** Keep canonical applied status in immutable character facts published by the existing Buffer transaction. Traits retain their triggers, targets, counters, and magnitude/period configuration; common typed policies own status aggregation, replacement, consumption, expiry, and removal. Reuse the existing combat engine, damage/recovery settlement, result stream, and View ownership.

**Tech Stack:** Unity 6000.0.73f1, C#, existing Core Contracts/Domain/Application and Systems assemblies, NUnit EditMode tests, existing SO/SerializeReference authoring. No new package or Framework subsystem.

**Spec:** [Approved design](../specs/2026-10-30-buff-debuff-system-design.md). Read it with this plan; its D01-D17 and sections 4-9 are binding, including the user's subsequent Core-only/View-handoff ownership clarification.

**Status:** Phase F (F1-F2) is complete. Final independent whole-system review found one movement-preview admission regression, reproduced and fixed. Fresh final Core: 4,269/4,269; CoreIntegration: 404/405, only unchanged StageKey 2101; zero skips. All production asset and retired-authoring checks pass. Existing native growth serialization diagnostics remain unresolved. Core implementation/evidence complete; handoff prepared, not externally delivered; View/integrated-game/visual acceptance pending. See section 5.8.

## Global Constraints

- Preserve existing gameplay unless the approved design explicitly changes it: HuntersMark/Weakness Exposure, restored Regeneration, Intimidation priority-component retirement, and MoveAttackDiscount retirement.
- User performs commits and pushes. No task in this plan authorizes either action; do not follow skill/template commit steps.
- Execute only the Phase explicitly authorized by the user. At every Phase exit, stop and report; do not automatically enter the next Phase.
- Respect Core/View/Buffer/Job ownership. This is the user's Core plan: C3/F2 prepare and finalize the View handoff, not View implementation. Do not edit ViewSystem, UI, or presentation DB types/assets/registration. The user explicitly authorized Task 0.2 to remove misplaced View tests/assertions from CoreIntegration while preserving Core coverage; that does not authorize editing external View tests or runtime presentation. Required View implementation remains an external integrated-release gate; it does not block independently verified Core completion.
- No changes to FieldCamera, Architecture, unrelated item/artifact/category systems, or existing Missing Script assets. Preserve unrelated worktree changes.
- Keep Necromancy, ThornField, and old Focus disabled; new Focus and StationaryFire redesign remain deferred. Keep the six previously retired traits retired. General Priority and flag redesign are outside scope.
- Preserve one authoritative status value, the existing transaction, and immutable preview/result snapshots. No parallel status manager, event bus, per-effect Framework services, or duplicate HP/recovery implementation.
- Core/InGame runtime data remain Unity-free. Implementation documents are English; new C# comments/XML documentation are Korean. Preserve existing spelling and namespace conventions, Allman braces, four spaces, UTF-8/LF, and `.meta` GUIDs on moves.
- Positive turn counts are per-trait SO settings. Keep each effect's clock meaning; do not replace all clocks with one global timer or infer permanent duration from zero.
- Weakness Exposure initially uses duration 1. Preserve the current ShotGun magnitude 30%; explicitly migrate missing duration fields. Do not populate missing enhanced definitions or unrelated growth pools.
- Regeneration initially heals every 1 own-side turn end; configurable intervals use the approved field-only cadence/reset rules.
- Unity verification requires the approved AppData-access execution path. Do not upgrade Unity or delete caches to work around the known sandbox limitation.

## Review Focus

Each risk below is assigned an explicit test task; these are the five high-risk cases that happy-path tests would otherwise miss.

1. A weak or short same-source reapplication replaces a stronger/longer entry without erasing another source's fallback: C1.
2. Sacrifice, pending multi-hit flushing, and reflected damage must evaluate the actual recipient once, with preview and commit agreeing: C4.
3. A legacy partial `With(debuffs: ...)` or playback event must not erase canonical statuses it cannot represent: A1/C4 Core tests, C3 handoff reproduction for the View owner.
4. Death, terminal environment damage, full HP, and the last night turn must not cause duplicate/banked Regeneration or heal after combat ended: D2.
5. A stale status/cadence expectation or a downstream prepare failure must install neither partial state nor a result: A2, C4, D2, F1.

## 1. Phase order and stop boundaries

| Phase | Tasks | Deliverable | Exit and stop |
| --- | --- | --- | --- |
| 0 | 0.1-0.2 | Current Core-only baseline, ownership map, runnable verification route | Report exact current failures and test cleanup; no gameplay implementation in this Phase. |
| A | A1-A2 | Shared typed state plus a complete attack-grant migration | Existing attack-grant behavior and atomic publication preserved; completed, stopped (5.3). |
| B | B1-B3 | Remaining active applied states and reusable contribution/read contracts | Preserved restrictions, attack reduction, armor stacks, shields, and conditional calculations; completed, stopped (5.4). |
| C | C1-C4 | Weakness Exposure policy/configuration, prepared View contract handoff, and Core HuntersMark cutover | Core verified; completed, stopped (5.5). View behavior acceptance remains external and pending. |
| D | D1-D2 | Configurable Regeneration restored | Own-side/end-effect/cadence/health publication gates pass; completed, stopped (5.6). |
| E | E1 | Obsolete priority component and attack discount retired | Active attack reduction preserved; retired/deferred boundaries verified; completed, stopped (5.7). |
| F | F1-F2 | Final Core regression evidence and completed View handoff document | Report Core completion and remaining external View integration/visual acceptance separately; stop. |

Order is `0 -> A -> B -> C -> D -> E -> F`. These tasks touch shared facts, turn hooks, and damage boundaries; do not parallel-edit those owners. C3 records the B3/C2 contract and required View migration. C4 depends on that prepared contract, not on completion of the other worker's View code. The Core worker may complete later authorized Phases while View remains pending. Whole-game release still waits for downstream consumer acceptance.

This is one integration plan, not independent refactors: each stage changes how the same effects travel through the same state and publication pipeline. No intermediate Phase may leave two writable representations of one effect or a broken project build.

## 2. Path map and planned decomposition

Paths use these exact repository-relative prefixes to avoid repeating long names. For example, `C/Contracts/Combat/CharacterCombatFacts.cs` resolves to the full prefix below plus `/Contracts/Combat/CharacterCombatFacts.cs`. Within a file list, an unprefixed filename inherits the directory of the preceding prefixed file; names already defined in section 2.1 use that exact path. A `*.meta` accompanies every new/moved Unity asset or C# file at execution time.

| Prefix | Exact directory |
| --- | --- |
| C | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator` |
| CS | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem` |
| B | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems` |
| DB | `Assets/ProjectL/Script/Systems/DataBaseSystem` |
| TA | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Unit/Trait` |
| V | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem` |
| UI | `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global` |
| T | `Assets/Tests/EditMode` |
| TI | `Assets/Tests/EditMode/CoreIntegration` |

### 2.1 New production files and boundaries

| File | Responsibility | First task |
| --- | --- | --- |
| `C/Contracts/CombatEffects/StatusEffectKey.cs` | Stable effect keys and Buff/Debuff classification; independent of TraitKey | A1 |
| `C/Contracts/CombatEffects/AppliedStatus.cs` | Small typed immutable applied-status values; add each needed family during its migration | A1/B1/B2/C1 |
| `C/Contracts/CombatEffects/StatusEffectSet.cs` | Immutable ordered collection, defensive copy, equality, empty value | A1 |
| `C/Contracts/CombatEffects/CharacterStatusCompatibility.cs` | Single normalization/projection boundary for legacy DTO input/read surfaces | A1 |
| `C/Domain/CombatEffects/Rules/AppliedStatusRules.cs` | Shared typed grant/reapply, clock expiry, attack consumption, and cleanup dispatch | A2/B1 |
| `C/Domain/CombatEffects/Rules/ShieldStatusRules.cs` | Existing source-budget reconciliation and absorption moved to a producer-independent boundary | B2 |
| `C/Contracts/Snapshots/StatusEffectSnapshot.cs` | Immutable effect-level display/read facts and per-source lifetime details | B3 |
| `C/Application/Combat/StatusEffectReadModel.cs` | Core-owned projection of applied status and context-valid derived contributions | B3 |
| `C/Domain/CombatEffects/Rules/WeaknessExposureRules.cs` | Max-source, replace, expiry/removal, and numeric amplification policy | C1 |
| `C/Application/Combat/WeaknessExposureApplication.cs` | Thin command-projection integration; no duplicated numerical policy | C4 |
| `C/Application/Combat/RegenerationTurnApplication.cs` | Holder cadence, shared recovery call, and ordered result publication | D2 |

Do not create a hierarchy for hypothetical effects. Keep closely related typed payloads together while they are small. Only introduce another policy file if that family has independently changing behavior; file count is not an improvement metric.

### 2.2 Existing owners and integration files

- Facts/copy/equality: `C/Contracts/Combat/CharacterCombatFacts.cs`, `CharacterDebuffState.cs`, `CharacterTraitEffects.cs`, `CharacterChanges.cs`.
- Effects/turns: `C/Application/Combat/CombatCommandEffects.cs`, `CombatCommandEvents.cs`, `TraitTurnLifecycle.cs`, `TraitPresenceReconciler.cs`, `CombatEffectiveStatsCalculator.cs`, `CombatTemporaryStateCleanup.cs`, `CombatTurnCalculator.cs`, `CombatPlacementCalculator.cs`.
- Damage: `C/Application/Combat/CombatDamageApplication.cs`, `CombatDamageCalculator.cs`, `AttackDirectDamageCalculator.cs`, `SacrificeDamagePlanner.cs`, `KillChainAttackCalculator.cs`, `HuntersMarkApplication.cs`; `C/Domain/Combat/TargetDamagePipeline.cs` only if required to expose shared evidence without changing its arithmetic.
- Evidence/publication: `C/Contracts/Combat/CombatEvent.cs`, `CombatChangeEvidenceReader.cs`, `CombatChangeEvidence.cs`, `CombatHuntersMarkEvent.cs`; `CS/CombatSystem/CombatTransitionPublisher.cs`; `B/Core/BattleTransactions.cs`; `B/Character/CharacterData.cs` and `CharacterBuffer.cs`.
- Authoring: existing `TA` family files, `CS/CharacterSystem/TraitSpecAdapter.cs`, `DB/Data/InGame/Table/TraitGrowth/Item/TraitGrowthDefinitionDataSO.cs`, `DB/Editor/UnitTraitAuthoringDrawer.cs` where retirement requires it.
- Read-only handoff references (not Core edit targets): `V/Combat/Playback/Handlers/CombatFactEventHandler.cs`, `V/Combat/Playback/Ordering/CombatEventTypeTable.cs`, `V/Combat/Display/Unit/FieldUnitPresenter.cs`, `V/Combat/Display/Unit/CombatObjectiveDisplay.cs`, `V/StageMap/Deck/DeckEditDisplayModel.cs`, `UI/UnitCardTraitContent.cs`.

Use task-specific edits below, not a cleanup sweep across this entire list. Existing transaction code usually needs only propagated immutable data/equality checks; do not move gameplay logic into it.

## 3. Interface decisions shared by tasks

These are the agreed implementation seams for this plan. The final implemented consumer contract is recorded in the handoff; phased task instructions below remain historical execution requirements. Keep Contracts free of Domain/Application references. Namespaces follow actual folders. Existing internal-access conventions govern Domain helpers.

### 3.1 Canonical collection and compatibility

`StatusEffectSet` exposes `static Empty`, `IReadOnlyList<AppliedStatus> Entries`, a defensive-copy constructor from that list, and `bool SameValues(StatusEffectSet other)`. Preserve ordered identical entries where a policy permits repeated grants; do not make source globally unique.

`AppliedStatus` exposes `StatusEffectKey Key` and `EffectOriginKey? Origin`. Provenance is nullable only to round-trip existing legacy synthetic facts; live grants must provide a valid source. Concrete immutable values carry only family-specific state:

- `AttackIncreaseStatus(origin, amount, remainingControllerTurnEnds)`.
- `AttackDecreaseStatus(origin, reductionPercent)` with the existing first-application/attack-consumption policy. Nullable provenance supports legacy synthetic facts; new grants require valid provenance. Phase E removed the obsolete priority payload.
- `ActionRestrictionStatus(key, origin, remainingAffectedSideTurnEnds)`, restricted to Disarm and Bind keys.
- `DefenseDecreaseStatus(origin, reductionPercent)` for each existing ArmorBreaker stack.
- `ShieldStatus(origin, remainingBudget, eligible, lastResetControllerTurnCount)`.
- `WeaknessExposureStatus(origin, increasePercent, remainingAffectedSideTurnEnds)`.

Use named typed properties rather than an arbitrary payload dictionary. Existing legacy-only synthetic facts with optional provenance must still round-trip unchanged through the compatibility path; they must not be converted into fabricated live trait sources. New grants require valid provenance.

Add `StatusEffectSet CharacterCombatFacts.StatusEffects` and `CharacterCombatFacts WithStatusEffects(StatusEffectSet statuses)`. Existing `With`, `WithPlacement`, and constructors preserve/normalize the canonical collection. Legacy getters for migrated families become projections. A legacy partial update replaces only the families that DTO owns and preserves new families such as Weakness Exposure. Conflicting explicit canonical and legacy inputs throw before mutation.

`CharacterStatusCompatibility` provides `StatusEffectSet Import(CharacterDebuffState debuffs, CharacterTraitEffects effects)`, `CharacterDebuffState ProjectDebuffs(StatusEffectSet statuses)`, and `CharacterTraitEffects ProjectTraitEffects(StatusEffectSet statuses, int cohesionDefenseBonus)`. During staged migration it handles only the families already moved, leaving other families in their existing owner until their task. Each family switches ownership in one task; no mirrored independently writable lists. Cohesion's derived defense remains a projection owned by its existing reconciliation, not another applied status. Deferred thorn `DefenseReduction` facts remain legacy-only and are preserved, not activated or duplicated. After C4, old single HuntersMark types remain inert read/source-compatibility contracts, never projections of canonical Weakness Exposure.

### 3.2 Common operations

`AppliedStatusRules` supplies:

```csharp
StatusEffectSet Apply(StatusEffectSet current, AppliedStatus incoming);
StatusEffectSet EndControllerTurn(StatusEffectSet current, Owner affectedSide, Owner endedSide);
StatusEffectSet CompleteAttack(StatusEffectSet current);
StatusEffectSet OnAreaChanged(StatusEffectSet current, CharacterRuntimeArea before, CharacterRuntimeArea after);
StatusEffectSet ClearForDeathOrBattleEnd(StatusEffectSet current);
int AttackIncrease(StatusEffectSet current);
int ApplyAttackDecrease(int attackPower, StatusEffectSet current);
int ApplyDefenseDecrease(int defense, StatusEffectSet current);
bool IsRestricted(StatusEffectSet current, StatusEffectKey key);
```

Dispatch by typed effect family, never by the granting trait. Add implementation only as a task migrates that family. Policy differences are mandatory: attack grants append, ArmorBreaker stacks append/multiply, restrictions refresh as today, Intimidation retains its first percentage, Weakness Exposure replaces per logical source and evaluates max. `OnAreaChanged` removes Weakness Exposure for Hand/EnemyWaiting only; it must not clear Intimidation or other legacy statuses indiscriminately. Timing selection and immunity conditions stay outside a generic numeric bag.

`ShieldStatusRules` preserves the existing `Reconcile` inputs `(prior, origin, capacity, eligible, completedControllerTurns, resetInterval, resetDue)` with `ShieldStatus` result; `Absorb(StatusEffectSet current, int incoming, out int remaining)` returns a replacement set. No timer reset is implied by range reentry.

`WeaknessExposureRules.Apply(StatusEffectSet, WeaknessExposureStatus)`, `MaximumPercent(StatusEffectSet)`, and `Amplify(int damage, StatusEffectSet)` implement section 6 of the design. Logical equality ignores Origin.RuleKey for reapplication and uses applying character + applying TraitKey; a different trait is a different source. `Amplify` is `checked((int)((long)damage * (100L + percent) / 100))` after nonnegative validation. Do not expose an API accepting an already-amplified request as raw damage.

### 3.3 Read/evidence contract

`StatusEffectReadModel.Capture(CharacterCombatFacts character)` returns `IReadOnlyList<StatusEffectSnapshot>`. A snapshot exposes effect key/classification, named effective values, and typed source contributions including their own remaining clock/count. Publish this through `CharacterData` alongside the canonical facts. Do not convert different lifetime kinds into a misleading single `RemainingTurns` integer.

Reuse `CombatTraitStateEvent(expected, final, origin, expectedStats, finalStats)` for full before/after facts unless a test proves information cannot be represented there. Do not invent another event stream. Update Core evidence validation and document required consumer ordering-table changes for the View worker. Retire the old single-mark event producer at C4 after C3 documents the new facts; preserve event number 39 and legacy public types still needed to compile current consumers. Do not emit fake old mark state/events to simulate the changed behavior.

Derived conditions retain their existing authoritative sources. Catalog their effect family and reuse existing capability calculations. Only publish a stable badge if Core has context-valid facts; target-specific TrackingShot/FocusedBombardment conditions are not unconditional persistent actor buffs. Do not create a cached recipient timer for a terrain trail.

### 3.4 Effect identifiers

Allocate stable effect keys once in A1 and record a uniqueness test. Use a separate enum from TraitKey; reserve `None = 0`. Plan the active catalog as `AttackIncrease`, `AttackDecrease`, `DefenseIncrease`, `DefenseDecrease`, `OutgoingDamageIncrease`, `IncomingDamageDecrease`, `Shield`, `MovementRangeIncrease`, `AttackRangeDecrease`, `Disarm`, `Bind`, `KnockbackImmunity`, `WeaknessExposure`. Do not allocate fake runtime statuses for deferred traits, Hunter's target relation, instant defeat, or direct healing. Exact nonzero numbers are an A1 implementation choice recorded in the catalog test, not a reason to ask for new gameplay decisions.

## 4. Verification commands and evidence

Task 0.1 prepares a local runner only when execution is authorized. The existing `.superpowers/trait-system/run-tests.ps1` supports Assembly/Filter/Run, hidden launch, unique evidence names, and XML checks. Reuse it as `.superpowers/buff-debuff-system/run-tests.ps1` with the new evidence directory and keep its Core/CoreIntegration-only assembly allowlist. Do not change the unrelated original runner. The intentionally deleted `ProjectL.Tests.Framework` assembly is not a gate and must not be restored or invoked for this Core plan.

Verified editor path at planning: `D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe`. Verify again before execution. The installed Test Framework documentation supports `-assemblyNames` and `-testFilter`. Use `-batchmode -nographics -projectPath -runTests -testPlatform EditMode -assemblyNames -testResults -logFile`; do not add `-quit` or `-runSynchronously`. Do not launch a second editor against a project already open; coordinate existing-editor verification instead of closing the user's editor automatically.

Example commands, executed later from the repository root with approved AppData access:

```powershell
pwsh -NoProfile -File .superpowers/buff-debuff-system/run-tests.ps1 -Run A1-red-01 -Assembly ProjectL.Tests.Core -Filter 'ProjectL.Tests.Core.StatusEffectContractTests'
pwsh -NoProfile -File .superpowers/buff-debuff-system/run-tests.ps1 -Run A1-green-01 -Assembly ProjectL.Tests.Core -Filter 'ProjectL.Tests.Core.StatusEffectContractTests'
pwsh -NoProfile -File .superpowers/buff-debuff-system/run-tests.ps1 -Run A-core-01 -Assembly ProjectL.Tests.Core
pwsh -NoProfile -File .superpowers/buff-debuff-system/run-tests.ps1 -Run A-integration-01 -Assembly ProjectL.Tests.CoreIntegration
```

For every task below, substitute its exact fixture filter and a unique `Task-red/green-NN` run name into the first two commands. Expected red: the named behavioral assertion fails (a missing planned type may fail compilation initially). Expected green: positive discovered test count, zero failures/skips, successful exit, no compilation errors, and matching XML/log/exit evidence. A missing XML, zero tests, compilation failure, or unrelated infrastructure failure is not a valid passing gate. Do not overwrite old evidence.

Run targeted tests after meaningful task changes. Run full Core and CoreIntegration at each Phase's final coherent state because canonical state/publication are shared, plus relevant InGame asset checks when assets change. Reuse results from exactly unchanged source/assets when the next checkpoint adds only documentation. Never repeat broad runs without a code/asset change, failure, or unresolved concern. View/UI playback and presentation acceptance belong to the receiving owner and are recorded separately from these Core gates.

CoreIntegration is for Core-to-Buffer/Framework/InGame DB integration, authoring/adapters, immutable output contracts and transactional publication. It must not instantiate View presenters/HUDs, assert card text/icons/visual registration, drive playback, or depend on a View-owned development fixture. Task 0.2 removes that historical scope mixing and retains mixed tests' Core assertions in Core fixtures. Unity/UnityEditor references used for InGame SO authoring remain valid. Mandatory `IDataBaseQueryContext.ViewQuery` interface placeholders and the Core input `CombatView` Buffer contract are not View behavior tests. All Core behavioral/publication tests remain required; do not suppress their failures or fabricate compatibility behavior. The receiving View worker owns downstream implementation/tests and integrated acceptance.

Record pre-existing failures by full test name and cause in 0.1. Do not repair unrelated production configuration or hide failures with skips. Distinguish `passed`, `unchanged pre-existing failure`, and `blocked`; a reported accepted exception is not an all-green claim. No historical test counts are a baseline.

The repository instructions require `DataBaseAssetTests` for asset changes, but no class/file with that name was found in the inspected checkout. Recheck in 0.1. If still absent, C2 creates `TI/DataBaseAssetTests.cs` in `ProjectL.Tests.CoreIntegration`, covering the actual touched InGame trait assets and current catalog validity. New View table/assets and their tests are the receiving worker's responsibility. If restored before execution, extend the existing suite instead of creating a duplicate. The executable filter is `ProjectL.Tests.CoreIntegration.DataBaseAssetTests` for the planned new suite. Do not report an empty filtered run as asset validation.

## Phase 0 — Current baseline and migration inventory

### Task 0.1: Establish the execution baseline

**Files:** read `AGENTS.md`, `Assets/AGENTS.md`, the approved spec and all mapped owners; prepare `.superpowers/buff-debuff-system/run-tests.ps1` and `baseline.md` only at execution. Update this plan's execution ledger. Do not change product sources.

**Interfaces:** consumes current branch/dirty manifest, Unity test assemblies and the existing runner; produces exact baseline evidence and the asset/consumer ownership list for A-C/E.

- [x] Record `git status --short`, branch and HEAD; inspect current scoped diffs before editing. Record code/asset fingerprints so later evidence corresponds to an exact state.
- [x] Prepare the runner described in section 4, including hidden `Start-Process`, unique output paths, and approved AppData access. Recheck Unity path and editor ownership.
- [x] Run full `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` as `P0-core-01` and `P0-integration-01`; retain those original mixed-suite results as history, superseded by Task 0.2's Core-only baseline. Record exact totals/failures; do not repair unrelated failures. The historical empty Framework invocation is not a required gate.
- [x] Locate all authoring instances by managed-reference type: HuntersMark, Regeneration, MoveAttackDiscount and Intimidation in unit, normal/enhanced growth, and legacy enhancement assets. Record exact matching asset paths and configured values; do not create absent enhanced content.
- [x] List existing writes/reads of Debuffs, TraitEffects, CombatHuntersMarkEvent and Regeneration snapshots, including hand/night recall, preview, undo, equality, evidence validation, cards and HUD. Assign Core/Buffer/DB/View ownership; record required View coordination for C3.

### Task 0.2: Keep CoreIntegration limited to Core verification

**Authorization:** the user's follow-up explicitly requests removal of View tests from CoreIntegration and correction of this plan/Phase 0 record. This is test/document cleanup, not Phase A gameplay implementation.

**Files:** remove `TI/PullAndMarkPresentationTests.cs`, `TI/HuntersMarkCardContentTests.cs` and their `.meta`; update `TI/PullAuthoringTests.cs`, `TI/HuntersMarkAuthoringTests.cs`, `TI/CategoryDefinitionMigrationTests.cs`, the CoreIntegration asmdef, this plan, and local baseline/runner evidence. Do not create a replacement Framework/View test assembly or edit View runtime/assets.

- [x] Classify direct View dependencies. Remove the two presentation/card fixtures; preserve their owned-character Pull/HuntersMark configuration assertions in existing Core authoring fixtures, without HUD/card setup.
- [x] Remove Pull visual-table/name assertions. Replace Category migration's View development source with an existing Core fixture and retain Core authoring/preparation coverage. Remove the now-unused direct FMODPlus assembly dependency; keep required Framework/Systems/Core and authoring dependencies.
- [x] Verify the three affected fixtures in full CoreIntegration and run full Core, with the shared source snapshot and all failures recorded. Initial targeted attempts 01/02 were compile-blocked; completed full runs and the 22 affected cases are in section 5.2. No concurrent Phase A file or failing assertion was modified by this cleanup.
- [x] Correct the plan and Phase 0 report so only Core/CoreIntegration are Core gates. Preserve original XML/logs as historical mixed-suite evidence and record removed/migrated coverage, actual results or execution blocks, remaining Core failures, and separate external View acceptance.

**Phase exit:** Core-only baseline and ownership report. This cleanup does not advance Phase A; the user confirmed another chat is implementing it, so preserve that chat's changes and record the exact verification state.

## Phase A — Canonical contract and first real consumer migration

### Task A1: Add the typed immutable collection and compatibility boundary

**Files:** create the four A1 contract files in section 2.1 and `T/StatusEffectContractTests.cs`; modify `C/Contracts/Combat/CharacterCombatFacts.cs` and `T/CharacterCombatFactsSameValuesTests.cs`. Keep behavior unchanged.

**Interfaces:** produces section 3.1 collection/facts/compatibility APIs and the stable effect catalog. Old constructor and `With` call sites remain source-compatible.

- [x] Write `StatusEffectContractTests`: defensive-copy input arrays; equality distinguishes source/payload/order/count; two equal same-source attack grants remain two entries. Assert `Entries.Count == 2` and mutation of the supplied array does not change the snapshot.
- [x] Add copy/compatibility assertions: `facts.With(health: changedHealth).StatusEffects.SameValues(original.StatusEffects)` is true; `WithPlacement` preserves the set; unrelated legacy debuff replacement retains an existing new-family test status; inconsistent dual input throws `ArgumentException` without modifying either input. Assert key values are unique and distinct from classification.
- [x] Run the exact fixture filter `ProjectL.Tests.Core.StatusEffectContractTests;ProjectL.Tests.Core.CharacterCombatFactsSameValuesTests` for red evidence.
- [x] Implement section 3.1 normalization/projection. Do not duplicate migrated state behind old getters. Keep unported families at their current owner until their task; add no gameplay clock hook yet.
- [x] Run the same filter for green evidence and compile all affected assemblies. Audit `With`, `WithPlacement`, and direct fact construction; no family may disappear in unrelated health/location copies.

### Task A2: Migrate additive attack grants through turn expiry and publication

**Files:** create `C/Domain/CombatEffects/Rules/AppliedStatusRules.cs`, `T/AppliedAttackStatusTests.cs`, `TI/StatusEffectPublicationTests.cs`; modify `C/Contracts/Combat/CharacterTraitEffects.cs`, `TraitAttackGrant.cs`, `C/Application/Combat/CombatEffectiveStatsCalculator.cs`, `TraitAttackNotifications.cs`, `TraitKillSettlement.cs`, `TraitTurnLifecycle.cs`, `CombatTemporaryStateCleanup.cs`, `C/Contracts/Combat/CombatChangeEvidenceReader.cs`, `B/Character/CharacterData.cs`. Inspect `B/Core/BattleTransactions.cs` and `B/Character/CharacterBuffer.cs`; change only propagation/validation when necessary.

**Interfaces:** consumes A1; implements `Apply`, `EndControllerTurn`, `AttackIncrease`, and cleanup for additive attack grants. Producers supply existing source, amount and controller-end duration.

- [x] Write tests with grants `(3, duration 1)` and `(4, duration 2)` from the same source: assert total `7`, after either side's first end `4`, after the second end `0`. Assert other sources/trait held values and Hunter's permanent bonus are preserved.
- [x] In `StatusEffectPublicationTests`, calculate a transition, change only its expected status through the existing fixture publication path, then assert stale apply throws and `CoreIntegrationSnapshot.AssertUnchanged` includes HP, statuses, rewards and result sequence. Verify original snapshots stay unchanged after a successful publication.
- [x] Run Core filter `ProjectL.Tests.Core.AppliedAttackStatusTests;ProjectL.Tests.Core.TraitAttackNotificationTests;ProjectL.Tests.Core.TraitKillRewardTests` and Integration filter `ProjectL.Tests.CoreIntegration.StatusEffectPublicationTests;ProjectL.Tests.CoreIntegration.TraitAttackGrantPublicationTests` for red evidence where newly required behavior is missing.
- [x] Move canonical grant storage and common lifecycle handling in one coherent change. Retain existing producers' triggers and values; project old TraitEffects/traitAttackBonus reads from that source. Remove the previous independent grant-decrement loop so a boundary is consumed once.
- [x] Run both filters green, then Phase A full Core/CoreIntegration gates. Document actual before/after modification, understanding, and verification scope for adding a second producer.

**Phase exit:** canonical additive grants exercised by real producers; no weak-exposure gameplay change. Stop for review.

## Phase B — Remaining active status families and read contracts

### Task B1: Migrate attack reduction, restrictions, and armor stacks

**Files:** extend `AppliedStatus.cs`, `AppliedStatusRules.cs`, `CharacterStatusCompatibility.cs`; modify `C/Contracts/Combat/CharacterDebuffState.cs`, `ArmorBreakState.cs`, `C/Application/Combat/CombatDebuffApplication.cs`, `TraitRestrictionApplication.cs`, `ArmorBreakerApplication.cs`, `CombatActionAdmission.cs`, `CombatEffectiveStatsCalculator.cs`, `TraitTurnLifecycle.cs`, `CombatTemporaryStateCleanup.cs`; adjust `C/Domain/Traits/Rules/DisarmTraitRule.cs`, `AnkleBreakerTraitRule.cs`, `ArmorBreakerTraitRule.cs` only to delegate shared effect policy. Create `T/AppliedRestrictionStatusTests.cs`; extend `TI/StatusEffectPublicationTests.cs`.

**Interfaces:** consumes A's canonical collection; implements typed first-application attack reduction, affected-side restriction refresh, ordered defense stacks, `CompleteAttack`, and `IsRestricted` from section 3.2.

- [x] Assert a 20% attack reduction followed by 30% retains 20% under the legacy policy: attack `100 -> 80`, then returns to `100` only after the established actual-attacker completion. Preview/rejected attacks do not consume it.
- [x] Assert Disarm/Bind duration `2 -> 1 -> removed` on affected-side endings only; reapply replaces duration rather than adds. Voluntary movement is blocked, forced movement still works, and disarmed Roar retains its pending counter.
- [x] Assert two 20% defense stacks reduce defense `100 -> 64` with existing rounding; same-source repeated stacks remain separate. A restriction refresh never removes armor/attack-reduction/grant state. Hand/night recall still preserves Intimidation as before.
- [x] Run red/green Core filter `ProjectL.Tests.Core.AppliedRestrictionStatusTests;ProjectL.Tests.Core.TraitRestrictionTests;ProjectL.Tests.Core.ArmorBreakerTraitTests;ProjectL.Tests.Core.IntimidationAttackTests;ProjectL.Tests.Core.IntimidationStatePreservationTests`; implement the canonical switch and delegated policies between runs.
- [x] Run Integration filter `ProjectL.Tests.CoreIntegration.StatusEffectPublicationTests;ProjectL.Tests.CoreIntegration.ArmorBreakerPublicationTests;ProjectL.Tests.CoreIntegration.TraitTurnLifecyclePublicationTests`. Preserve old inactive priority payload compatibility until E1; do not activate priority calculation.

### Task B2: Migrate shields without changing budget or eligibility rules

**Files:** create `C/Domain/CombatEffects/Rules/ShieldStatusRules.cs` and `T/ShieldStatusPolicyTests.cs`; extend `AppliedStatus.cs`/compatibility; modify `C/Domain/Traits/Rules/ShieldGrantRules.cs`, `C/Application/Combat/TraitPresenceReconciler.cs`, `CombatDamageApplication.cs`, `CombatTemporaryStateCleanup.cs`, `C/Contracts/Combat/CharacterTraitEffects.cs`; extend `TI/TraitShieldPublicationTests.cs`.

**Interfaces:** `ShieldStatusRules.Reconcile` and `Absorb` from section 3.2. Trait presence still determines source eligibility/capacity/reset settings; the policy knows no SoloAction/Cohesion internals.

- [x] Assert two eligible budgets `30 + 20` absorb `40`, leaving `10` shield and `0` HP damage in existing source order; absorb `60`, leaving `0` shield and `10` HP damage.
- [x] Assert leaving/reentering eligibility does not refill a spent budget; one source leaving does not remove another; reset intervals use the existing completed-controller boundary; neighboring defense is recomputed without rewriting shield budget.
- [x] Run red/green Core filter `ProjectL.Tests.Core.ShieldStatusPolicyTests;ProjectL.Tests.Core.TraitShieldTests;ProjectL.Tests.Core.SacrificeTraitTests` while moving the shared budget implementation and canonical storage. Keep the old helper as a stateless adapter only if a current caller still needs it.
- [x] Run `ProjectL.Tests.CoreIntegration.TraitShieldPublicationTests;ProjectL.Tests.CoreIntegration.SacrificePublicationTests` green. Check ordered damage and preview scopes; no doubled absorption or cloned writable shield budget.

### Task B3: Publish effect identities and preserve conditional contributions

**Files:** create `C/Contracts/Snapshots/StatusEffectSnapshot.cs`, `C/Application/Combat/StatusEffectReadModel.cs`, `T/StatusEffectReadModelTests.cs`; modify `B/Character/CharacterData.cs`, `C/Contracts/Combat/CombatChangeEvidenceReader.cs`, `C/Contracts/Combat/CombatEvent.cs` only if required; extend `TI/TraitPresentationContractTests.cs`. Inspect `C/Application/Combat/CombatCommandEffects.cs`, `TraitPresenceReconciler.cs`, `TraitTrailCalculator.cs` and existing contribution types before deciding whether adding an effect-key projection is sufficient.

**Interfaces:** produces section 3.3 read contract. Include per-source lifetime kind/count and effective value. A formal family identity does not introduce another canonical condition/timer.

- [x] Assert two traits granting the same effect yield the same effect identity with distinct sources; legacy trait names do not select calculation/HUD semantics. Assert the snapshot is immutable and preserves suppressed-source details.
- [x] Assert movement of a Cohesion neighbor updates its existing derived defense without adding a stored DefenseIncrease timer; DustScreen/GroundCompaction retain terrain-owned expiry. Assert target-dependent damage bonuses are not published as unconditional always-on statuses.
- [x] Assert buff-before-debuff/key ordering in the effect read section, without changing unrelated objective/equipment UI ordering. Preserve per-source accepted application order and avoid one fictitious aggregate expiration.
- [x] Run red/green filters `ProjectL.Tests.Core.StatusEffectReadModelTests;ProjectL.Tests.Core.TraitDamageReductionTests;ProjectL.Tests.Core.TraitTrailTests;ProjectL.Tests.Core.TraitEffectiveStatsProjectionTests` and `ProjectL.Tests.CoreIntegration.TraitPresentationContractTests;ProjectL.Tests.CoreIntegration.TraitDamageReductionPublicationTests;ProjectL.Tests.CoreIntegration.TraitTrailPublicationTests` while implementing the minimal read projection.
- [x] Freeze the C3 consumer contract in the execution ledger. Run Phase B full Core/CoreIntegration gates; confirm actual existing gameplay equality rather than only type compilation.

**Phase exit:** old active effects preserved, common contracts ready. Stop before changing HuntersMark semantics.

## Phase C — Weakness Exposure Core implementation and View contract handoff

### Task C1: Implement the reusable policy in isolation

**Files:** create `C/Domain/CombatEffects/Rules/WeaknessExposureRules.cs` and `T/WeaknessExposurePolicyTests.cs`; add `WeaknessExposureStatus` to `AppliedStatus.cs`, and typed delegation to `AppliedStatusRules.cs`/read model. No live producer switches yet.

**Interfaces:** section 3.2 WeaknessExposure APIs; initial duration is producer-supplied, never a runtime default.

- [x] Add named tests with exact cases: `DifferentSources_MaxThenFallback` grants `20%/2` and `30%/1`, asserts `MaximumPercent == 30`, one affected end gives `20`, the next gives `0`; `SameSource_ReplacesWeakerAndShorter` replaces `30%/3` with `10%/1`, asserts `10` and `1`.
- [x] `SourceIdentity_IsCharacterAndTrait` asserts same character/different trait remain separate, different character/same trait remain separate, and same character/trait with a different RuleKey replaces. Caller TraitKey is provenance, not a whitelist of allowed producers.
- [x] Assert own-side application with duration 1 expires at that current side's end; opposite-side end does not tick. Source death/return leaves the target unchanged; target Hand and EnemyWaiting transitions clear all exposure sources; Field-to-Field relocation and persistent Main do not.
- [x] Assert `Amplify(100, 30%) == 130`, `Amplify(1, 30%) == 1`, `Amplify(0, 30%) == 0`; negative values fail and overflow throws without partial state. Lifetime zero/negative is rejected on application; reaching zero by expiry removes the entry.
- [x] Run red/green filter `ProjectL.Tests.Core.WeaknessExposurePolicyTests`, implementing only the policy between runs. No real trait is added to demonstrate reuse; use distinct existing TraitKey identities in test grants.

### Task C2: Carry configurable duration through every existing authoring path

**Files:** modify `TA/HuntersMarkTraitAuthoring.cs`, `HuntersMarkTraitData.cs`, `CS/CharacterSystem/TraitSpecAdapter.cs`, `C/Contracts/Commands/Traits/HuntersMarkTraitSpec.cs`, `C/Domain/Traits/Definitions/HuntersMarkTraitDefinition.cs`, `C/Application/Combat/Traits/HuntersMarkTraitFactories.cs`, `C/Contracts/Snapshots/Traits/HuntersMarkTraitStatusSnapshot.cs`, `C/Application/Combat/Traits/TraitEffectProvider.cs`, `C/Application/Roster/OwnedCharacterStatsCalculator.cs`; extend `TI/HuntersMarkAuthoringTests.cs` and create `TI/TraitTurnConfigurationTests.cs` and `TI/DataBaseAssetTests.cs` under section 4's missing-suite rule.

**Interfaces:** add `DurationAffectedSideTurnEnds` to the existing data/spec/definition/status description chain. Runtime grant/spec constructors require `(int bonusDamagePercent, int durationAffectedSideTurnEnds)`; no omitted runtime duration. Update Core-owned callers explicitly. Preserve read-only legacy DTO signatures temporarily where current View code/tests require them for compilation; they must not create executable grants or claim to encode a new multi-source status. Document their eventual removal for the View owner. SO authoring supplies initial `1` and Inspector minimum `1`.

- [x] On instantiated copies of current UnitDataSO and growth definition assets, set duration `3` and percentage `25`; assert data -> spec -> definition -> snapshot retains both. Normal duration `1` and enhanced duration `3` remain independent when an enhanced entry exists.
- [x] Assert invalid duration `0/-1` fails pure validation, with no live asset mutation. Add round-trip cases for existing Vanguard/CleanSweep/MoraleBoost durations, Disarm/AnkleBreaker, SoloAction/Cohesion reset periods, and GroundCompaction/DustScreen expiry using nondefault `3` values.
- [x] Run red/green filters `ProjectL.Tests.CoreIntegration.HuntersMarkAuthoringTests;ProjectL.Tests.CoreIntegration.TraitTurnConfigurationTests;ProjectL.Tests.CoreIntegration.TraitDefinitionAuthoringTests` while threading configuration. Existing HuntersMark runtime remains old until C4; carrying an unused duration during preparation must not change active behavior.
- [x] Migrate exact existing assets recorded in 0.1: always include `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/Playerable/Unit_ShotGun.asset`, retaining `bonusDamagePercent: 30` and explicitly adding duration `1`. Set existing normal/enhanced producer entries to initial `1` without adding missing definitions/pools.
- [x] In `DataBaseAssetTests`, load the actual touched assets through AssetDatabase/catalog and assert duration 1, unchanged per-asset percentages (ShotGun 30%), positive configured intervals, normal/enhanced key agreement, valid references and unchanged GUIDs. Run the suite under section 4 plus the authoring filters. Record exact changed asset paths and before/after values. No hand-edited managed-reference type rename.

### Task C3: Prepare the Core-to-View contract and migration handoff

**Owner:** Core programmer. This task writes a handoff; it performs no View/UI/presentation DB implementation.

**Files:** create `docs/superpowers/handoffs/2026-10-30-buff-debuff-system-core-view-handoff.md` during execution and update this plan's ledger. Read the consumer references in section 2.2, `DB/Queries/ViewDataBase/ViewDataBase.cs`, `Assets/ProjectL/Script/Systems/Factory/DBFactory/Group/ViewDataBaseFactory.cs`, `Assets/ProjectL/Script/Systems/ServiceKeys/DBFactory/Group/ViewTableKey.cs`, and `TI/HuntersMarkCardContentTests.cs` for accurate migration guidance; do not edit them.

**Interfaces:** documents B3's `StatusEffectSnapshot`, canonical before/after facts, C2's duration-bearing trait descriptions, source identity, expiry/removal, and changed result production. Clearly label any not-yet-implemented C4 behavior as planned until its evidence is available.

- [x] Document exact exported types/members and data flow, including effective maximum versus per-source remaining duration, clock units, original grant target versus actual damage recipient, and no View-side timer/calculation authority.
- [x] Provide deterministic input/expected-output examples from C1/B3 tests: `20%/2 + 30%/1`, fallback after one affected-side end, same-source replacement, both reserve returns, source death persistence, and a `100 -> 130 -> shield 50 -> HP loss 80` receiving example.
- [x] List the precise required View changes: full-fact playback application and ordering, effect-keyed HUD readout, broken-shield mapping, normal/enhanced card description updates, and preservation of Hunter's distinct Mark. Include the partial CharacterDebuffState reconstruction risk and expected preservation of unrelated shields/restrictions.
- [x] Recommend an effect-keyed visual lookup in the existing View DB ownership path, separate from possessed-trait visuals. The View worker chooses its concrete file/layout and registration implementation; Core supplies no new View DB types/assets or service registration.
- [x] List consumer test cases for the View worker: two sources display one effect with accurate detail, playback reaches the published state without losing unrelated statuses, configured values appear in cards, old one-hit-consumption text is gone, required registrations validate, and no duplicate lifetime ticking. Record current legacy types retained solely for source compatibility.
- [x] Verify every documented Core member against implemented A/B/C2 code, and every planned example against its applicable policy tests. Mark the document `Prepared; View implementation pending; not externally delivered`. C3 completion does not require editing or waiting for View code.

### Task C4: Switch the producer and all numeric damage paths together

**Files:** create `C/Application/Combat/WeaknessExposureApplication.cs`, `T/WeaknessExposureDamageTests.cs`, `T/WeaknessExposureLifecycleTests.cs`, `TI/WeaknessExposurePublicationTests.cs`; modify the damage/evidence owners in section 2.2, `C/Application/Combat/CombatPlacementCalculator.cs`, `TraitTurnLifecycle.cs`, `CombatTemporaryStateCleanup.cs`, and `C/Domain/Traits/Rules/HuntersMarkTraitRule.cs`. Replace old-semantics tests in `T/HuntersMarkAttackTests.cs`, `HuntersMarkRuleTests.cs`, `HuntersMarkStateTests.cs` and `TI/HuntersMarkPublicationTests.cs`; maintain reserved old identifiers.

**Interfaces:** `WeaknessExposureApplication.CompleteHit(CharacterCombatFacts attacker, CombatAttackProfile profile, CombatDamageKind kind, IDictionary<EntityId, CharacterCombatFacts> characters, List<CombatEvent> events, EntityId targetId, CombatDamageOrigin origin = null)` replaces old mark granting. All numeric receiving paths use C1's `Amplify` at the shared application boundary; the status is granted after the hit, to the existing eligible contact target.

- [x] Before Core cutover, verify C2 authoring and the prepared C3 contract. Add integration fixtures using duration 1 and a second independent source. Keep source compilation compatible with unmodified View through inert/read-only DTO compatibility where necessary; log runtime display limitations explicitly. This Core change is not a whole-game release with accepted View behavior.
- [x] Add damage tests: raw final `100`, exposure `30%`, shield `50` -> request `130`, shield loss `50`, HP loss `80`; each ordinary/extra/reaction/mine/field/reflection hit applies once. Source attacker also benefits. BeforeDamage item input sees `130`; existing item order is unchanged.
- [x] Add Sacrifice cases: exposed original target/unexposed protector does not amplify protector damage; unexposed original/exposed protector does. The original contact still receives the post-hit grant under existing grant targeting. Test simulation/preview and live results match, including shield/item/HP outcome.
- [x] Add ordered-hit tests: first granting hit uses pre-grant exposure, later hits read the new projection; same-source reapplication refreshes each eligible hit; batching cannot hide state changes. Preserve FinishingShot's strict threshold and instant-defeat/fall bypasses. Reflection termination and damage basis remain unchanged apart from the receiving multiplier.
- [x] Run red/green Core filter `ProjectL.Tests.Core.WeaknessExposureDamageTests;ProjectL.Tests.Core.WeaknessExposureLifecycleTests;ProjectL.Tests.Core.HuntersMarkAttackTests;ProjectL.Tests.Core.SacrificeTraitTests;ProjectL.Tests.Core.CannonThresholdTraitTests`. Implement shared receiving amplification, post-hit application, affected-side expiry, Hand/EnemyWaiting cleanup, and complete before/after evidence. Remove old consumption/first-source-retention producer logic in this same task.
- [x] Assert no mutation in preview/rejection; stale refresh/expiry or late prepare failure installs nothing. Reconstruct state from the Core event stream and compare with published canonical facts; leave actual View playback tests to its owner. Run `ProjectL.Tests.CoreIntegration.WeaknessExposurePublicationTests;ProjectL.Tests.CoreIntegration.StatusEffectPublicationTests` green.
- [x] Remove old single-mark producer behavior, but retain public event/DTO/read signatures still referenced by current View until coordinated cleanup. Do not fabricate a legacy single mark from multiple sources or add a second writable store. Preserve old event numeric ID as reserved. Run full Phase C Core/CoreIntegration and InGame asset validation under section 4's separated ownership rules; record downstream View acceptance independently.
- [x] Refresh the C3 handoff with implemented signatures, actual result samples, successful Core evidence, exact consumer failures/limitations, and the remaining View acceptance cases. Do not modify their tests or claim integrated release readiness.

**Phase exit:** Core ShotGun behavior, configurable duration, all-damage coverage, publication, and handoff verified. View implementation/integration is pending externally; it does not prevent this Core Phase completing. Stop before D.

## Phase D — Configurable Regeneration restoration

### Task D1: Add cadence configuration and one holder-owned state

**Files:** modify `TA/RegenerationTraitAuthoring.cs`, `RegenerationTraitData.cs`, `CS/CharacterSystem/TraitSpecAdapter.cs`, `C/Contracts/Commands/Traits/RegenerationTraitSpec.cs`, `C/Domain/Traits/Definitions/RegenerationTraitDefinition.cs`, `C/Application/Combat/Traits/RegenerationTraitFactories.cs`, `TraitEffectProvider.cs`, `C/Domain/Traits/States/RegenerationRuntimeState.cs`, `C/Domain/Traits/Effects/RegenerationEffectStateSnapshot.cs`, `TraitEffectStateCodec.cs`, `C/Contracts/Snapshots/Traits/RegenerationTraitStatusSnapshot.cs`, `C/Application/Combat/CombatCommandEffects.cs`; create `T/RegenerationCadenceTests.cs`, extend `TI/TraitTurnConfigurationTests.cs`.

**Interfaces:** add `HealEveryOwnSideTurnEnds` (positive, authoring initial 1) to configuration; holder state/snapshot exposes `CompletedEligibleTurnEnds` initialized to 0. Planned config constructors take `(int healAmount, int healEveryOwnSideTurnEnds)`. Canonical progress lives in the established trait state/snapshot pipeline, never the recipient status collection.

- [x] Assert authoring/data/spec/definition preserve `(healAmount: 10, interval: 2)` and reject `0/-1` intervals. Existing normal/enhanced definitions remain independent; migrate only existing Regeneration authoring instances to interval 1.
- [x] Assert state/codec/facts round trips preserve progress 1 of interval 2; old snapshots do not change when the next transition resets it. An unrelated status application does not reset cadence.
- [x] Run red/green `ProjectL.Tests.Core.RegenerationCadenceTests;ProjectL.Tests.Core.RegenerationTraitTests` and `ProjectL.Tests.CoreIntegration.TraitTurnConfigurationTests` while implementing configuration and snapshot propagation. Regeneration remains disabled until D2's turn/health integration is ready.

### Task D2: Integrate side-end recovery, resets, and publication

**Files:** create `C/Application/Combat/RegenerationTurnApplication.cs` and `TI/RegenerationPublicationTests.cs`; modify `C/Domain/Traits/Rules/RegenerationTraitRule.cs`, `C/Application/Combat/CombatTurnCalculator.cs`, `CombatPlacementCalculator.cs`, `CombatTemporaryStateCleanup.cs`, `DeferredCombatFeatures.cs`, and initial trait state construction in `C/Application/Combat/BattleCharacterCalculator.cs`/D1 factories if needed; inspect `B/Character/CharacterBuffer.cs` and `B/Core/TraitGrowthTransactions.cs` without weakening their no-live-battle growth guard. Update `T/RegenerationTurnTests.cs`, extend `RegenerationCadenceTests.cs`.

**Interfaces:** `RegenerationTurnApplication.EndOwnSide(CombatCommandInput input, CombatDamageApplication settlement, Owner endedSide)` operates on the current command projection after environment/outcome settlement. `Reset(CharacterCombatFacts character)` returns a replacement with cadence zero and every other fact unchanged. Reuse `CharacterSettlementCalculator.Recover` and existing recovery recording; callers publish one coherent turn transition, including persistent Main HP.

- [x] Replace tests asserting disabled healing with approved behavior: interval 1 heals HP `40 -> 50` for amount 10 after an idle own-side end; opposite-side end does nothing; HP `95/100 -> 100` records actual recovery 5; dead/Hand/EnemyWaiting holders do not heal.
- [x] Interval 2 heals on eligible ends 2 and 4; full HP at a due end resets progress without banking; source definition replacement/removal and Field exit reset cadence; persistent Field Main retains it across a night. Preview/rejected commands cannot advance it. Current growth is prohibited while battle records exist, so normal post-battle upgrades obtain fresh zero progress from new battle construction; test this path instead of introducing a live trait replacement feature.
- [x] End-environment lethal damage prevents healing; terminal outcome stops the rest of the turn; the final enemy turn heals eligible survivors after environment effects and before recall. Verify nonterminal post-damage recovery in the existing event order and no repeated recovery when a processed command is replayed/rejected.
- [x] Run red/green filters `ProjectL.Tests.Core.RegenerationTurnTests;ProjectL.Tests.Core.RegenerationCadenceTests` while connecting the approved boundary and reset hooks. Do not start another turn runner or heal from View callbacks. Enable only Regeneration after this path is complete.
- [x] Run `ProjectL.Tests.CoreIntegration.RegenerationPublicationTests;ProjectL.Tests.CoreIntegration.TraitTurnLifecyclePublicationTests;ProjectL.Tests.CoreIntegration.StatusEffectPublicationTests` with stale cadence and late prepare failure: assert all stores/results unchanged. Check Main persistent HP and actual recovery evidence.
- [x] Run full Phase D Core/CoreIntegration and relevant asset tests. Compare CallOfBlood recovery and unrelated end-effect ordering against baseline.

**Phase exit:** Regeneration active at default period 1 and tested at longer configured periods. Necromancy, ThornField and Focus remain disabled. Stop before E.

## Phase E — Retire only the agreed obsolete components

### Task E1: Remove priority reduction and retire the discount safely

**Files:** modify `TA/IntimidationTraitAuthoring.cs`, `IntimidationTraitData.cs`, `C/Contracts/Commands/Traits/IntimidationTraitSpec.cs`, `C/Domain/Traits/Definitions/IntimidationTraitDefinition.cs`, `C/Contracts/Snapshots/Traits/IntimidationTraitStatusSnapshot.cs`, `C/Contracts/Combat/IntimidationDebuffState.cs`, `C/Domain/Combat/CombatDebuffCalculator.cs`, `CS/CharacterSystem/TraitSpecAdapter.cs`, `C/Application/Combat/Traits/IntimidationTraitFactories.cs`, `TraitEffectProvider.cs`, `TraitRegistry.cs`, `TraitFactories.cs`, `C/Application/Combat/DeferredCombatFeatures.cs`, `MovementContextCalculator.cs`, `CombatCommandEffects.cs`, `C/Domain/Traits/Effects/TraitEffectStateCodec.cs`, `DB/Editor/UnitTraitAuthoringDrawer.cs`; update only exact asset references identified in 0.1. Extend `TI/RetiredTraitAuthoringTests.cs`; create `T/StatusEffectRetirementTests.cs`; update `T/IntimidationTraitTests.cs` and `IntimidationScheduleTests.cs`.

**Interfaces:** Intimidation configuration now exposes attack reduction only; its active source/lifetime contract stays B1's. `DeferredCombatFeatures.IsTraitRetired(MoveAttackDiscount)` is true; TraitKey 20 remains reserved. Six prior retired keys stay reserved and unexecutable. No replacement action economy.

- [x] Assert active attack reduction still maps `100 -> 80` for 20%, completes/cleans up as before, and no runtime configuration/snapshot/presentation advertises priority reduction. General Priority fields and unrelated calculations still compile and retain baseline behavior.
- [x] Assert direct spec/registry and SO authoring cannot execute/acquire MoveAttackDiscount; existing six retired keys stay rejected. Assert Regeneration available and Necromancy/ThornField/Focus unavailable, with no accidental availability broadening.
- [x] Run red/green filters `ProjectL.Tests.Core.StatusEffectRetirementTests;ProjectL.Tests.Core.IntimidationTraitTests;ProjectL.Tests.Core.IntimidationScheduleTests;ProjectL.Tests.Core.IntimidationAttackTests` and `ProjectL.Tests.CoreIntegration.RetiredTraitAuthoringTests` while removing active paths.
- [x] Before removing managed-reference types or source files, migrate any known live references and prove no required serialized references remain. If a compatibility shell must survive, make it explicitly retired and unable to produce executable data. Do not create Missing Script/managed-reference losses, delete unrelated assets, or reuse numeric keys. Remove only obsolete files and their `.meta` when references are demonstrably absent.
- [x] Update Core-owned description/availability data and document required View display changes in the handoff. Preserve minimal inert/read-only compatibility members referenced by current View; schedule their eventual removal with that owner. Run InGame DataBaseAssetTests under section 4. Record retained serialized/read-only shells and exact consumer limitations. The retired Framework test assembly and its deleted CombatCostRetirementViewTests are not a Core gate.
- [x] Run Phase E full Core/CoreIntegration and relevant InGame asset checks under section 4's separated ownership rules. Final grep for `PriorityReduction`, `PriorityPenalty`, and `MoveAttackDiscount` must distinguish allowed reserved/migration/read-only-consumer/test references from executable leftovers; it is not a requirement for zero textual matches.

**Phase exit:** active attack reduction unchanged, agreed inactive mechanics retired, unrelated priority/flag/state behavior untouched. Stop before final acceptance.

## Phase F — Final verification and owner handoff

### Task F1: Verify integrated contracts and actual change locality

**Files:** extend existing Core-owned targeted tests only for an identified uncovered failure; update this plan's execution ledger and the handoff created in C3. Do not make unrelated refactors or edit View-owned tests during acceptance.

**Interfaces:** consumes exact final code/asset manifest, published statuses and tests from A-E; produces evidence and owner-facing contracts, not another runtime API.

- [x] Map every design requirement to the coverage table below and actual tests. Close any gap with a meaningful test before claiming completion; do not add mirror tests of getters solely to inflate counts.
- [x] Run final full Core/CoreIntegration and relevant InGame asset checks on the Core manifest, or reuse the exact E exit runs if no product/asset change occurred. Inspect discovered count, failed/skipped tests, XML/logs, compilation, and namespaces. Separate Core regressions and pre-existing Core failures from downstream View acceptance; no blanket green claim or suppressed failures.
- [x] Audit new/changed pure InGame/Core files for Unity references, single canonical state, full snapshot preservation, default/missing-data handling, source identity, and Framework registration ownership. Preserve original transaction install ordering and no partial publication.
- [x] Demonstrate locality with a test producer grant from a different trait identity using existing policy, and a nondefault SO duration/period. Confirm no new effect-engine/HUD trait-name branch or duplicated timer was needed. Record actual changed files and required contracts, not a count-only claim.
- [x] Record Core/Buffer/DB and consumer integration results separately from PlayMode/manual/production artwork. No test count implies visual acceptance or external handoff delivery.

### Task F2: Finalize the Core handoff and external acceptance checklist

**Owner:** Core programmer. The receiving View worker implements and verifies the downstream checklist later. No View asset/code changes, new artwork, or external messaging are authorized by this task.

**Files:** the C3 handoff document and this plan's execution ledger only.

- [x] Finalize actual Core signatures, configuration fields, source identity, clock units, actual-recipient behavior, before/after result examples, sample timelines, evidence paths, compatibility shims, and deferred features. Replace planned claims with implementation evidence or explicitly unfinished items.
- [x] Record production ShotGun duration 1/unchanged percentage, Regeneration default period 1, normal/enhanced separation, and immutable installed grants from Core/authoring tests. Supply deterministic fixtures that the View worker can reuse.
- [x] Give the View worker its required acceptance list: HUD effect ordering/broken-shield icon, accurate source duration detail, removal of old one-hit text, Hunter Mark separation, full-fact replay preservation, cancel/refresh without timer changes, both reserve returns, and recovery timing display. Every item starts `Pending View owner` unless actual evidence is received.
- [x] List consumer migration requirements, any externally supplied View test results, temporarily retained public compatibility types, and the coordinated cleanup needed after View updates. View tests are outside CoreIntegration and are not a Core gate. Do not promise that current presentation already works.
- [x] Mark `Core implementation/evidence complete` and `handoff prepared` only if their requirements are met. Mark View implementation/integration/manual visual acceptance separately as pending. External delivery stays `not delivered` unless the user separately authorizes it.

**Final exit:** Core work and the prepared handoff can complete independently of View. Whole-game integration/visual acceptance remains pending until the receiving worker provides evidence. Stop; the user handles commits and pushes.

## 5. Coverage and phase review ledger

| Design requirement | Owning tasks |
| --- | --- |
| D01-D05: preservation, common applied state, effect-specific policy, catalog, non-status trait behavior | 0.1, A1-A2, B1-B3, F1 |
| D06, D09-D10, D12: deferred/new/retired scope | Every Phase constraint; E1, F1 |
| D07, D14-D15: Weakness Exposure, duration 1, reserve cleanup | C1-C4 |
| D08, configurable cadence | D1-D2 |
| D11: retirement with active attack reduction preserved | B1, E1 |
| D13: no commit/push, no code until authorized | Global constraints and every exit |
| D16: all in-scope numerical clocks owned by trait authoring | C2, D1, B2-B3 preservation, F2 |
| One canonical owner and compatibility migration | A1-A2, B1-B2, C3-C4 |
| Damage ordering, reflection, Sacrifice, preview and transactional evidence | C4, F1 |
| Normal/enhanced authoring, missing fields, stable identifiers | C2, D1, E1 |
| D17: Core-only execution, View handoff, separate completion gates | C3, C4, E1, F1-F2 |
| Framework/Buffer/View boundaries and locality | A2, B3, C3 documentation, F1-F2 |

| Phase | Authorization | Implementation | Tests/evidence | Review outcome |
| --- | --- | --- | --- | --- |
| 0 | User authorized baseline and Core-only cleanup, 2026-10-30 | Inventory/test cleanup complete; gameplay unchanged by this cleanup | Affected 22/22; shared Core 4,212/4,214; CoreIntegration 365/366; 0 skips | Cleanup verified; 2 concurrent Phase A Core failures and existing StageKey failure remain explicit |
| A | User authorized 2026-10-30 | A1-A2 complete | Core 4,214/4,214; CoreIntegration 365/366; only existing StageKey 2101; 0 skips | Independent review finding fixed; stopped before B (5.3) |
| B | User authorized 2026-10-30; completed in another session | B1-B3 complete | Core 4,239/4,239; CoreIntegration 371/372; only existing StageKey 2101; 0 skips | See 5.4; baseline hashes verified before C |
| C | User authorized 2026-10-30 | C1-C4 complete | Core 4,264/4,264; CoreIntegration 390/391; only existing StageKey 2101; 0 skips | One P2 fixed with reproduced tests and full gates; stopped before D (5.5) |
| D | User authorized 2026-10-30 | D1-D2 complete | Core 4,279/4,279; integration 402/403; existing StageKey failure | See 5.6; fresh-cadence review fix verified |
| E | User authorized 2026-10-30 | E1 complete | Core 4,267/4,267; integration 404/405; existing StageKey failure | See 5.7; retirement review complete |
| F | User authorized 2026-10-30 | F1-F2 complete | Fresh Core 4,269/4,269; integration 404/405; existing StageKey failure | Whole-system P2 reproduced/fixed; external View acceptance remains pending (5.8) |

For each executed Phase record date, exact baseline/final manifest, task checkboxes completed, actual command/XML/log paths, failures and their scope, ownership/deviation decisions, remaining acceptance, and the explicit stop. Do not pre-fill pass counts or mark work complete from this document alone.

### 5.1 Phase 0 initial execution — historical mixed-suite evidence, 2026-10-30

**Authorization and outcome:** the user requested Phase 0 only. Task 0.1 ran inline using `executing-plans`; all five baseline/inventory steps are complete. No production source, test, InGame/View asset, public contract, or state ownership was changed. No commit/push/external delivery. Stop before A.

**Exact state:** branch `feature/Cheon/Core/NewCombatSytem`, HEAD `bef74f0d0f8eb2b0a2c2821b2a3e595bc0f7dbd9`. Initial dirty entries were the unrelated legacy enhancement design (modified), this implementation plan (untracked), and the approved buff/debuff design (untracked). The two design documents retain their initial SHA256. Before/after product manifests contain 11,181 files under `Assets`, `Packages`, and `ProjectSettings`, including `.meta`; zero differences. Both manifest SHA256 values are `92E133B6E50D56D8853B0884B50165FC1A80D032D88714A3FA4C37BB36C6656B`. Unity-generated FMOD local-folder metadata was restored to its original bytes before the final audit.

**Local artifacts:** [baseline report and ownership map](../../../.superpowers/buff-debuff-system/baseline.md), [runner](../../../.superpowers/buff-debuff-system/run-tests.ps1), `P0-source-assets-before.csv`, `P0-source-assets-after.csv`, `P0-source-assets-delta.csv`, `P0-documents-before.csv`, `P0-authoring-inventory.csv`, `P0-ownership-references.csv`, `P0-integration-failures.csv`, and `P0-verification-summary.csv`, all in `.superpowers/buff-debuff-system/`. Local evidence follows the existing ignored scratch-directory convention. The unrelated original trait-system runner is unchanged.

**Executed commands and evidence:** from repository root, through approved AppData access with Unity `D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe`:

```powershell
pwsh -NoProfile -File .superpowers/buff-debuff-system/run-tests.ps1 -Run P0-core-01 -Assembly ProjectL.Tests.Core
pwsh -NoProfile -File .superpowers/buff-debuff-system/run-tests.ps1 -Run P0-integration-01 -Assembly ProjectL.Tests.CoreIntegration
pwsh -NoProfile -File .superpowers/buff-debuff-system/run-tests.ps1 -Run P0-framework-01 -Assembly ProjectL.Tests.Framework
```

| Run | Total | Passed | Failed | Skipped | Unity exit | Evidence files in the local directory |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| `P0-core-01` | 4,197 | 4,197 | 0 | 0 | 0 | `P0-core-01.xml`, `.log`, `.exit.txt` |
| `P0-integration-01` | 384 | 363 | 21 | 0 | 2 | `P0-integration-01.xml`, `.log`, `.exit.txt` |
| `P0-framework-01` | 0 | 0 | 0 | 0 | 0 | `P0-framework-01.xml`, `.log`, `.exit.txt`; runner rejected zero tests |

The initial logs contain no C# compiler errors. The initial CoreIntegration run was not green. The empty Framework run was an unnecessary planning-time check of an intentionally retired assembly, not a missing Core gate. The 21 initial failures, all recorded by full name/message/stack in the CSV/XML, were:

- `ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss`: missing StageKey 2101 in the active Stage table. `Stage_Boss.asset` exists, but its GUID is not registered in the catalog's `StageRootDataTable.asset`. Existing production configuration; unchanged.
- Twenty `ProjectL.Tests.CoreIntegration.PullAndMarkPresentationTests` cases: common `SetUp` line 63 throws `NullReferenceException` because line 62 reflects the removed private `UnitHUD.actionedImage` field. View-owned fixture/API mismatch; test bodies did not run. No View test/code changes were made.

**User correction after this run:** deletion of `ProjectL.Tests.Framework` and `CombatCostRetirementViewTests` is intentional; CoreIntegration was created for Core-side integration extracted from the mixed Framework tests. Their absence requires no restoration, replacement, repeated invocation, or missing-Core-coverage warning. Task 0.2 removes View behavior checks mistakenly present in CoreIntegration and supersedes the old instruction to retain them. `DataBaseAssetTests` is still absent; section 4's C2 creation/reuse rule applies. No asset-change acceptance is claimed from an empty run.

**Exact authoring inventory:** the whole-Assets managed-reference scan found only `Unit_ShotGun.asset` (`bonusDamagePercent: 30`, no duration), `Unit_UndeadHeavy.asset` (`healAmount: 10`, no cadence), and `Unit_Shielder.asset` (`attackReductionPercent: 20`, `priorityReduction: 3`) under `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/`. GUIDs and hashes are in the inventory CSV. No MoveAttackDiscount instance or matching normal/enhanced instance exists among the 23 growth definition assets; the legacy Enhancement table is empty. Do not create missing enhanced content. Presentation TraitVisual assets are View-owned and excluded from Core asset migration.

**Ownership/locality findings:** the report traces immutable facts/copy/equality, applied-state producers, existing effect policies, turn/return/death cleanup, numeric damage/recovery, preview, movement undo/evidence, atomic Buffer preparation/publication, authoring, HUD/cards and playback. `CharacterData` projects one facts owner. A representative new producer currently crosses bespoke mark state/policy/event/damage/lifecycle assumptions; the approved future seam is a typed grant plus existing producer/configuration/registration, with shared policy and publication regression still required. This is an expected boundary, not a measured implementation improvement.

**C3 coordination recorded:** the initial View review found mark playback rebuilding only three debuff fields and no full `CombatTraitStateEvent` handler/table row. HUD/card consumers encoded the old single-hit mark semantics. The future handoff must cover lossless facts, event ordering, effect identity/source lifetimes, Hunter separation, new icon/text, both reserve returns and cadence/recovery timing. The removed presentation fixture's original setup failure is historical; View acceptance remains with its owner, outside the Core gates. No View implementation or external delivery occurred. Core results establish no PlayMode/manual/visual acceptance.

**Historical stop:** the initial baseline stopped before Phase A. Its mixed counts and View setup failures must not be reused as the current Core-only baseline. The subsequent cleanup and concurrent Phase A implementation are recorded in section 5.2.

### 5.2 Phase 0 Core-only cleanup — user-authorized follow-up, 2026-10-30

The user clarified that CoreIntegration is Core-side Framework/Buffer/InGame integration and explicitly requested removing View tests and correcting this plan/Phase 0 evidence. The old Framework test assembly is intentionally retired. No new View/Framework suite is introduced.

**Implemented test scope:**

- Removed `CoreIntegration/PullAndMarkPresentationTests.cs` (20 parameterized cases) and `HuntersMarkCardContentTests.cs` (6 cases), together with their unreferenced `.meta` files. Original sources and XML are retained locally as historical evidence, not active tests.
- Preserved the mixed fixtures' Core checks as three Pull owned/combat snapshot cases (0/2/7) in PullAuthoringTests and two real-asset-copy HuntersMark owned-snapshot cases (10/25) in HuntersMarkAuthoringTests. No card/deck/HUD/Presenter dependency remains in these tests.
- Kept Pull's production authoring/adapter/profile assertions and removed only its TraitVisual registration/name assertions. CategoryDefinitionMigrationTests now has one Core authoring/preparation test using the existing Core EncounterSource, replacing two production/development variants that depended on View's DevBattleInitializationSource.
- Removed the unused direct `FMODPlus` reference from `ProjectL.Tests.CoreIntegration.asmdef`; required Framework/Systems/Core references and Unity authoring support remain. Static scan for ViewSystem/UISystem/Data.View/View factories and actual ViewQuery consumption has zero matches. Two required IDataBaseQueryContext ViewQuery interface placeholders and Core CombatView Buffer requests remain valid contract references.

**Change locality:** eight test/asmdef/meta paths are changed by this cleanup; no production Framework, Buffer, Core, View, DB source or authored asset is changed by this cleanup. Core tests now require producer/adapter/snapshot/publication contracts without unrelated Presenter/HUD internals. Shared Core and CoreIntegration full regression remains required. Removed visual coverage belongs to the receiving View owner's acceptance list, not a prerequisite to finishing this Core cleanup.

**Concurrent work:** the user confirmed another chat is implementing Phase A in this same checkout. Targeted attempt `P0-cleanup-targeted-01` encountered new StatusEffectContractTests and missing planned StatusEffectSet/AppliedStatus types (CS0246). Attempt `P0-cleanup-targeted-02`, after those types appeared, encountered the next concurrent tests AppliedAttackStatusTests and StatusEffectPublicationTests (CS0103/CS1929 and CS0103/CS1061 respectively). Both exited Unity with code 1 and produced no XML. The `.log`/`.exit.txt` files and `P0-cleanup-compilation-blocks.csv` preserve both blocks; neither is a test verdict or passing gate. Those external files are retained. Another chat's active Unity process is not closed or duplicated. Final verification must identify the shared manifest rather than claim the original 11,181-file state is current.

**Completed verification:** after the concurrent compiler blocks cleared, full CoreIntegration and full Core ran through the approved AppData route. No Framework invocation is required. The full integration run includes all three affected fixtures: CategoryDefinitionMigrationTests 1/1, HuntersMarkAuthoringTests 10/10, PullAuthoringTests 11/11, all passed. Those results supersede the blocked targeted attempts; a redundant third filtered run is unnecessary.

| Run | Total | Passed | Failed | Skipped | Unity exit | Evidence |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| `P0-integration-02` | 366 | 365 | 1 | 0 | 2 | `.xml`, `.log`, `.exit.txt` in the local directory |
| `P0-core-02` | 4,214 | 4,212 | 2 | 0 | 2 | `.xml`, `.log`, `.exit.txt` in the local directory |

These are completed non-green suites, not suppressed failures. Integration's only failure remains `ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss` (StageKey 2101). Core's two failures are `ProjectL.Tests.Core.IntimidationAttackTests.CompleteAttack_ChangesOnlyDebuffs_RejectsFriendlyAndNonCharacterIds` and `ProjectL.Tests.Core.IntimidationStatePreservationTests.WithDebuffs_ReplacesOnlyDebuffs_AndRejectsNull`. Both fail at generic reflected `TraitEffects` equality assertions while concurrent Phase A changed that property into a projection. The initial pre-Phase-A Core run passed; these observations belong to the Phase A owner's follow-up and are not waived or repaired by test ownership cleanup. Full names/messages/stacks are in `P0-cleanup-core-failures.csv` and the XML.

**Manifest/evidence:** `.superpowers/buff-debuff-system/` contains before/after cleanup hashes, `P0-cleanup-coverage-map.csv` (28 original cases classified), `P0-cleanup-retained-test-results.csv` (22 passed cases), original removed fixtures, pre-cleanup plan/report copies, and reference scans. Retained `.meta` GUID files are unchanged. `P0-cleanup-verification-final.csv` captures 7,313 files under `Assets/ProjectL/Script`, `Assets/Tests`, `Assets/ProjectL/DataBaseSO`, `Packages`, and `ProjectSettings` after the full runs; SHA256 `B7BA5D6A94C10161667B02DFED55D8E206BA6EF597D8C005A8D57936421EECEC`. This is a shared Phase A snapshot, not the initial unchanged 11,181-file manifest or a claim that source was frozen across both runs. Concurrent writes are retained in `P0-cleanup-writes-after-integration-start.csv`. This cleanup's eight-file hashes are checked separately. A read-only independent review found no actionable correctness/Core-coverage issue.

**Stop:** requested test/document cleanup is complete with the full-run limitations above. No production implementation, authored asset change, View runtime change, commit/push, or external delivery was performed by this cleanup. The other chat owns Phase A completion and its remaining Core failures; the existing StageKey configuration failure remains outside this cleanup. Do not repeat broad tests solely for later documentation edits.

### 5.3 Phase A execution — 2026-10-30

**Authorization and boundary:** the user requested Phase A only and clarified that View tests belong to another worker. A1-A2 ran inline; all Phase A checkboxes above are complete. Phase B and later gameplay/authoring migrations were not started. No commit, push, or external handoff delivery.

**Checkout and concurrent work:** branch `feature/Cheon/Core/NewCombatSytem`, HEAD `bef74f0d0f8eb2b0a2c2821b2a3e595bc0f7dbd9`. The starting product tree was clean; the untracked approved design/plan and unrelated legacy-retirement design were preserved. During A, the separate session completed section 5.2's eight-path CoreIntegration cleanup. Those edits and its historical evidence remain intact. The final integration run uses that cleaned Core-only assembly. No Framework or View tests were run for A; Core input `CombatView` Buffer contracts and Core-produced presentation facts remain valid Core integration coverage.

**Implemented contracts:**

- Allocated stable `StatusEffectKey` values `None=0`, active keys 1-13, with separate Buff/Debuff classification. Added immutable ordered `StatusEffectSet` and typed `AttackIncreaseStatus`; duplicate same-source grants remain separate.
- `CharacterCombatFacts.StatusEffects` is the canonical applied-status value. Constructor normalization, `WithStatusEffects`, legacy partial `With`, placement/health copies, and `SameValues` preserve it. Conflicting explicit canonical/legacy attack inputs reject before publication. Unported debuffs, shields, cohesion, and inactive legacy payloads retain their existing owners.
- `CharacterTraitEffects` is a stable read-only compatibility projection. Its `TraitAttackGrant` wrappers reference the exact canonical `AttackIncreaseStatus` objects; unrelated copies reuse that projection. There is no independently writable second grant state. `CharacterData.StatusEffects` exposes the same immutable facts value.
- `AppliedStatusRules` owns additive grant application, checked totals, every-controller-end expiry, and death/battle-end removal. Vanguard/CleanSweep attack notifications and MoraleBoost kill settlement delegate their existing grants to this policy. Their triggers, target selection, amounts, and configured clocks are unchanged. The former separate decrement loop is removed. Acceleration held values and Hunter permanent growth retain their owners.
- Existing full trait-state events and evidence validation carry canonical changes. Buffer preparation rejects fresh characters containing canonical statuses and rejects stale expected statuses atomically. `BattleTransactions`, install order, Framework registration, and Job ownership are unchanged. No Weakness Exposure gameplay or Regeneration restoration is included yet.

**Verification:** all runs use `.superpowers/buff-debuff-system/run-tests.ps1` through the approved AppData-access route and Unity 6000.0.73f1, without cache deletion, upgrades, or a second Editor on the same project. XML totals agree with discovered test-case counts.

| Evidence | Result | Meaning |
| --- | --- | --- |
| `A1-red-01` | Exit 1; missing planned types, no XML | Planned contract-absence red evidence |
| `A1-green-01` | 28/28 passed, exit 0 | Contract, copy/equality and shape guard |
| `A2-red-01`, `A2-integration-red-01` | Compile-blocked; no XML | Initial missing policy/read API; test import/enumeration mistakes corrected before behavioral runs |
| `A2-red-02` | 24/25; exit 2 | Canonical-only state survived legacy-only cleanup |
| `A2-integration-red-02` | 5/6; exit 2 | Fresh preparation incorrectly accepted canonical-only state |
| `A2-green-01`, `A2-integration-green-01` | 25/25 and 6/6; exit 0 | Policies, real producers, immutable publication, stale rejection and preparation checks |
| `P0-core-02` (shared checkout) | 4,212/4,214 | Intermediate regression evidence from section 5.2; superseded below |
| `A-review-fix-green-01` | 40/40; exit 0 | Both existing Intimidation regressions and nonempty projection-reference preservation pass |
| `A-core-01` | **4,214/4,214; exit 0** | Final full Core gate |
| `A-integration-01` | **365/366; exit 2** | Final full CoreIntegration; only retained StageKey 2101 failure |

Every successful targeted run and both final full runs have zero skips. Final logs contain no C# compiler errors. Existing project warnings are not described as warning-free. The full commands were:

```powershell
pwsh -NoProfile -File .superpowers/buff-debuff-system/run-tests.ps1 -Run A-core-01 -Assembly ProjectL.Tests.Core
pwsh -NoProfile -File .superpowers/buff-debuff-system/run-tests.ps1 -Run A-integration-01 -Assembly ProjectL.Tests.CoreIntegration
```

The sole final failure is `ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss`: active Stage data lacks StageKey 2101. It was present before A and is not repaired or suppressed. CoreIntegration is therefore **not all green**. The retired View fixtures are absent from the executed test-case list; their removal is the separate section 5.2 cleanup, not a Phase A test relaxation.

**Review and correction:** one fresh read-only Phase A reviewer identified the same compatibility issue exposed by the shared full run: returning a new `TraitEffects` object on every getter access broke unchanged-property preservation. Both existing Intimidation assertions were kept intact. A stable immutable projection backed by canonical objects fixes the issue; the 40-case correction gate and final full Core run verify it. No other actionable correctness finding or deferred minor remained. Later status families, View/PlayMode, and unmeasured performance claims were outside this review, not accepted as implemented.

**Actual change locality:**

| Dimension | Before | After / verified scope |
| --- | --- | --- |
| Add another attack-increase producer | Produce a legacy grant; understand legacy grant storage, stat projection, the trait-specific decrement loop and cleanup integration | Produce the typed grant at its trigger/target boundary and use shared application; no producer-name case or new timer in common policy. Existing registration/trigger wiring remains legitimately producer-owned. |
| Understand a balance/lifetime change | Trait configuration plus attack-grant DTO, projection, turn and cleanup implementation | Trait configuration, provenance, and `Apply`/every-controller-end policy contract; stored grants retain their captured values. Unrelated shield/debuff internals are not part of granting attack increase. |
| Verify a producer change | Producer behavior plus shared grant/turn/publication regressions | Producer trigger/targets plus shared policy contracts. The migration still ran full Core and CoreIntegration because facts/equality/publication are shared; no mandatory gate was dropped. |

A synthetic different trait identity (`SteadfastWill`) grants the same attack effect in a policy test without adding a gameplay producer or modifying that trait. Same-source `(3,1)` and `(4,2)` grants total `7 -> 4 -> 0` on either side's boundaries. Tests also preserve another source, held Acceleration, permanent Hunter growth, and old snapshots. These are observed tests, not a claim that future traits need no integration work.

**Manifest and artifacts:** Phase A owns 29 source/test/meta paths (16 production C# files, 5 test C# files, 8 new `.meta` files), plus this plan update. The exact list is [A-scope.txt](../../../.superpowers/buff-debuff-system/A-scope.txt); owned-file SHA256 manifest is [A-final-owned-manifest.csv](../../../.superpowers/buff-debuff-system/A-final-owned-manifest.csv), hash `2C570AEBD9F27EBA0F4DF359789B3A9C7F762F7854634D6FA6692B8943462734`. The final gate's UTF-8 path manifest covers 11,193 Assets/Packages/ProjectSettings files, hash `A827D97BA54FFA46794695A7B53CEBBA99E494B71C60EDA969537DF9105D2C24`. The delivery manifest differs only by restoring Unity-generated FMOD local-folder metadata; no authored game asset was intentionally changed. Core sources remain Unity-free; eight new `.meta` GUIDs are unique; existing GUIDs and both design-document hashes are unchanged. Logs/XML/exit files, `A-verification-summary.csv`, `A-integration-failures.csv`, manifests, and the review ledger are local evidence. Earlier non-UTF8 path inventory attempts are superseded by the explicitly named UTF-8 manifest.

**Stop:** Phase A is complete with the explicit existing StageKey exception. Do not begin Phase B automatically. No View implementation, visual acceptance, production-content acceptance, or entire buff/debuff-plan completion is claimed.

### 5.4 Phase B execution — 2026-10-30

**Authorization and boundary:** the user requested Phase B only. B1-B3 ran inline in the existing `feature/Cheon/Core/NewCombatSytem` checkout at `bef74f0d0f8eb2b0a2c2821b2a3e595bc0f7dbd9`, preserving the uncommitted Phase A and Core-only test cleanup. No commit, push, View implementation, authored asset change, HuntersMark cutover, Weakness Exposure, Regeneration, or retirement is included. B1-B3 are complete with the existing StageKey exception below.

**Implemented state/policy boundaries:**

- `AttackDecreaseStatus`, `ActionRestrictionStatus`, `DefenseDecreaseStatus` and `ShieldStatus` join A's canonical `StatusEffectSet`. Existing public legacy constructors retain their validation; their immutable wrappers reference canonical objects. Partial legacy updates replace only their migrated families and preserve unknown statuses, HuntersMark, inactive DefenseReduction and unrelated facts. Explicit conflicting canonical/legacy inputs reject before publication. Unchanged projections retain stable references.
- Shared policies retain the first accepted attack reduction until actual attacker completion, restriction replacement with affected-side expiry, and ordered armor factors with one final truncation. Two 20% defense stacks still yield `100 -> 64` and `2 -> 1`. Preview/rejection, Hand/night Intimidation retention, forced movement and pending disarmed Roar remain covered by existing regression tests. The old priority payload remains inactive and is not retired early.
- `ShieldStatusRules` owns immutable source budgets, reset history and source-sorted absorption. Existing traits still own eligibility/capacity/reset configuration. Departing/reentering eligibility does not refill a spent budget. Presence reconciliation replaces only its exact SoloAction/Cohesion origins, preserving another producer's budget; ordered damage recognizes canonical shields without requiring a specific producer trait. The old helper is a stateless adapter. Damage/item/HP order is unchanged.
- Full `CombatTraitStateEvent` facts and the existing Buffer transaction remain authoritative. No new event stream, mutable repository, service registration, Framework installation order or Job responsibility was introduced. Trait activation events retain actual trait provenance; non-Trait origins still publish full state changes without fabricated Trait activations.

**Frozen C3 read contract:** `StatusEffectReadModel.Capture(CharacterCombatFacts)` returns immutable `IReadOnlyList<StatusEffectSnapshot>`, cached as `CharacterData.StatusEffectSnapshots`. The canonical `CharacterData.StatusEffects` remains available. Effect rows sort Buff before Debuff, then by `StatusEffectKey`; per-family source details retain accepted application order, independently of damage absorption ordering.

| Read family | Effective value and lifetime |
| --- | --- |
| AttackIncrease | `AppliedAmount`, `HeldForAttackAmount`, `TotalAmount`; applied sources retain `RemainingControllerTurnEnds`. Acceleration held attack reads its existing owner and names `AttackCompletion` separately. |
| AttackDecrease | First `ReductionPercent`, source and `AttackCompletion` lifetime; no invented turn counter. |
| DefenseIncrease | Existing Cohesion `Amount`, `CurrentEligibility`; empty source list because the existing derived amount has no lossless origin list. No stored duplicate or timer. |
| DefenseDecrease | `EffectiveDefense` and ordered per-source stacks, `BattleEnd` lifetime; no fictitious aggregate percentage/expiry. |
| Shield | `RemainingEligibleBudget`; all source budgets including suppressed/spent entries, `Eligible`, historical `LastResetControllerTurnCount`, `ReconciledBudget` lifetime. Reset history is not remaining duration. |
| Disarm / Bind | Typed sources with `RemainingAffectedSideTurnEnds` and `AffectedSideTurnEnds` lifetime. |

Existing evaluated outgoing/incoming damage contributions expose a nullable effect identity when active. TrackingShot depends on target HP, FocusedBombardment on captured target count, Entrenchment on another living source/profile/radius, and DustScreen/GroundCompaction on terrain state. Their calculations and expiry owners are unchanged. StationaryFire percentages and knockback capabilities also need context/profile data unavailable to an isolated facts read. None becomes an unconditional actor badge or a second recipient timer. The detailed consumer contract is frozen in [B-progress.md](../../../.superpowers/buff-debuff-system/B-progress.md); C3 must carry it into the later handoff. No View acceptance is implied.

**Verification and corrections:** all Unity runs use the approved AppData route, Unity 6000.0.73f1 and the existing runner, one process at a time. XML totals match discovered test-case counts.

| Evidence | Result / significance |
| --- | --- |
| `B1-red-01` -> `B1-green-03`, `B1-integration-green-01` | Missing planned contracts -> 62/62 Core and 14/14 integration. Intermediate own-test namespace error corrected. |
| `B2-red-01` -> `B2-green-02`, `B2-integration-green-02` | Missing shield contract -> 42/42 Core and 9/9 integration. A mapped-file partial write was completed atomically; an invalid test-only Domain dependency was removed without expanding the integration asmdef. |
| `B3-red-01`, `B3-green-01/02`, `B3-integration-green-01` | Read API absence; two test-fixture assumptions corrected to the existing origin factory/high-health predicate. Integration 8/8. Later full Core verifies all corrected B3 cases. |
| `B-core-01` | 4,228/4,234: two real completion provenance failures and four Phase A tests that still assumed debuffs/shields were unported. Updated exact canonical expectations, retaining unrelated facts/source/snapshot assertions. |
| `B-boundary-red-01/02`, `B-origin-red-01` -> `B-boundary-green-01` | Witnessed another producer's shield deletion, producer-name damage ordering, and non-Trait event misattribution; corrected gate 41/41. |
| `B-core-02`, `B-integration-01` | Pre-review-correction full gates 4,237/4,237 and 371/372; integration's sole failure is StageKey 2101. |
| `B-review-red-01` -> `B-review-green-01` | Real Item-origin shield absorption threw `ArgumentOutOfRangeException(traitKey)` (1/2); source-kind guard and unchanged Trait behavior verified, 57/57. |
| `B-core-03` | **4,239/4,239; exit 0**, post-review full Core, zero skips. |
| `B-integration-02` | **371/372; exit 2**, post-review full CoreIntegration, zero skips; only `BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss` (missing StageKey 2101). |

One independent read-only review found the absorption provenance issue above and no other actionable B finding or deferred minor. The fix retained full state events and passed RED-to-GREEN before the fresh full gates. Review exclusions confirm the approved scope: later phases, context-dependent badges, View rendering/playback and production Stage repair remain outside B.

**Actual change locality:** another producer can grant a migrated effect using its typed value and shared policy, while retaining its own trigger, target, configuration and registration. It does not need unrelated trait calculations or a new decrement/cleanup loop. Source-specific shield reconciliation still legitimately owns eligibility and settings; it no longer deletes budgets owned elsewhere. This improvement is verified by synthetic distinct-trait grants and a non-Trait shield through actual damage, without adding gameplay to those producers. Shared facts, turn, damage and publication changes still require full Core/CoreIntegration regression; those gates were not replaced by isolated policy tests.

**Scope/evidence:** 45 source/test/meta paths differ from the B starting manifest: 29 production C#, 10 test C#, six new `.meta` files, plus this plan update. Exact paths: [B-scope.txt](../../../.superpowers/buff-debuff-system/B-scope.txt). The owned manifest SHA256 is `5D89E2C58CC9819DCA6C5707CFA414B9D01964DA2EBF3B0DD671FCD842B2849A`; the 7,325-file final-gate manifest is `44FA25979F2CF1E898879DD24AB1B0D88E80DCD9DEB95724BDC3739C87192874`. Existing metadata is unchanged, six new GUIDs are unique, Core remains Unity-free, and B diff whitespace checks pass. Logs/XML/exit files, summaries, rulings and manifests remain in `.superpowers/buff-debuff-system/`. Post-run hashes match every final-gate source exactly (zero drift). Unity-generated FMOD local-folder metadata was restored to the recorded pre-B bytes (SHA256 `71F9498C60897ED084AF4419CD2011EC54BA8B707BF3CC5C679AD2125A76957E`); no authored content was changed.

**Stop:** Phase B is complete with the unchanged StageKey 2101 exception. Stop before Phase C. Core gates do not establish PlayMode, visual, scene or whole-game release acceptance.

### 5.5 Phase C execution — 2026-10-30

**Authorization and boundary:** the user explicitly requested Phase C after Phase B completed in another session. C1-C4 ran inline in the existing `feature/Cheon/Core/NewCombatSytem` checkout, HEAD `bef74f0d0f8eb2b0a2c2821b2a3e595bc0f7dbd9`. All B-owned hashes matched before C; its final XML results were verified without repeating its baseline. Earlier A/B work and the Core-only test cleanup are preserved. No commit/push, View/UI/presentation DB edit or test, FieldCamera/Architecture change, unrelated production Stage repair, Regeneration, retirement, or external delivery occurred.

**Implemented behavior:**

- `WeaknessExposureStatus` joins the existing immutable canonical collection. `WeaknessExposureRules` owns strongest-source selection, replacement by character + trait identity (independent of callback RuleKey), checked integer amplification, and shared typed application. Lower sources keep their own clocks; weaker/shorter same-source requests replace both values. Existing shared lifecycle handles affected-side expiry and death/battle cleanup; Hand and EnemyWaiting returns remove exposure, while source departure, ordinary field relocation and persistent Main retain the approved behavior.
- Duration is required through `HuntersMarkTraitData`, spec, definition, factories, runtime description and owned description. Authoring has positive initial 1. Actual asset copies verify independent normal/enhanced durations and existing other-trait clocks. The only authored asset change is `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/Playerable/Unit_ShotGun.asset`: existing 30% retained; explicit `durationAffectedSideTurnEnds: 1` added; GUID/managed-reference type unchanged. No growth pool or missing enhanced definition was populated.
- `WeaknessExposureApplication` grants after an eligible ordinary/extra hit to its original living hostile contact. `CombatDamageApplication` amplifies the actual recipient once after existing non-item/held-defense evaluation and before items/shields/HP. Sacrifice selection/simulation, sequential pending hits, extra/reaction/environment/mine/reflection damage, item context, breakdowns and actual-loss evidence use this shared result. Instant defeat and falling remain outside numeric amplification.
- Direct and secondary damage ledgers consume the evaluated outcome. The independent review exposed two callers that still added raw barrel/forced-field damage; both now use `RemainingDamage`, including shield remainder. An active barrel test proves direct20 + explosion10 with exposure30% produces expected39, secondary13 and HP100 -> 61. With shield30, expected/secondary9 and HP91 agree. The retained forced-field path passes the same cases without enabling its deferred producer.
- Old single-mark granting/consumption/first-source retention is removed. The internal `HuntersMarkApplication` file/meta was replaced by the new integration. Public legacy `HuntersMarkState`, `CharacterDebuffState.HuntersMark`, `CombatHuntersMarkEvent` and reserved event ID39 remain for source/old-fact compatibility; new grants never synthesize a misleading old mark. Full `CombatTraitStateEvent` facts and existing Buffer transactions remain authoritative.

**Prepared View handoff:** [Core-to-View handoff](../handoffs/2026-10-30-buff-debuff-system-core-view-handoff.md) was prepared before C4 cutover, then updated against the implemented members and final evidence. It includes B3 typed rows/clocks, maximum versus source detail, C2 descriptions, original contact versus actual recipient, complete-fact ordering, real numeric/event examples, exact current consumer gaps, broken-shield lookup ownership, compatibility cleanup and pending acceptance. Current View still lacks the new full-state ordering/HUD/card migration. No View implementation or whole-game release readiness is claimed; the document is not externally delivered.

**Verification:** all Unity runs used 6000.0.73f1, one process at a time, through approved AppData access. The same runner permits only Core and CoreIntegration. Full gates compiled the project; XML totals match discovered test cases. No skipped/ignored test or log suppression was added.

| Evidence | Result / significance |
| --- | --- |
| `C1-red-01` -> `C1-green-02` | Planned policy/status/read contract absent -> 14/14. An intermediate own-code namespace error was corrected before GREEN. |
| `C2-red-02` -> `C2-green-01` | Required duration contract absent -> 56/57. All configuration/copy/ShotGun checks pass; first cold growth load emits existing native Refid diagnostics, detailed below. |
| `C4-red-02`, `C4-integration-red-01` | 51/77 and 12/18: expected pre-cutover failures in receiving/grant/lifecycle/publication behavior. |
| `C4-green-01` -> `C4-green-02` | 84/85 exposed a real reflection activation request-bound failure; after correction, 114/114, including existing reflection/preview/AI behavior. Expanded reflection RED preserved no-shield/shield coverage. |
| `C4-integration-green-01` | 18/18: immutable source snapshots, full-fact reconstruction, fallback/expiry, stale refresh and late preparation atomic rejection. |
| `C-core-01`, `C-integration-01` | 4,257/4,260 and 387/391. Six obsolete single-mark assertions were migrated to canonical persistence, provenance, immutable preview, no old consumption event and exactly-once publication; StageKey 2101 retained. |
| `C-review-red-02` | 76/80: all four new barrel/forced-field cases reproduce incorrect preview totals while projected/live HP is already correct. Both discarded-outcome callers corrected. |
| `C-core-02` | **4,264/4,264; exit 0; zero skips.** Consolidated review-fix GREEN and final full Core gate. |
| `C-integration-02` | **390/391; exit 2; zero skips.** Only `BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss` fails for missing StageKey 2101. Both `DataBaseAssetTests` cases pass. |

**Existing asset diagnostics:** `C2-green-01`'s cold growth-load case failed on 22 native SerializeReference `Refid -2` errors. The same 22 diagnostics occur in the pre-C A/B logs and in final C integration; all growth assets remain byte-identical. In the full final run, the production ShotGun and growth-definition assertions both pass, but that does not resolve the existing serialization diagnostics. No `LogAssert` suppression, fixture order manipulation, cache deletion, asset repair or Unity upgrade was used. StageKey 2101 is likewise unchanged and not counted as a passing test.

**Review and implementation decisions:** one independent read-only review found the secondary-damage ledger issue above; no deferred minor. Its reproducer failed before the two-line product correction and passes in the final full gate. Review exclusions remain the authorized boundary: A/B internals beyond C integration, D-F, View runtime/visual acceptance and external delivery. Final documentation and full gates were completed by the coordinator.

- Reflection retains its configured formula and existing positive-request/actual-loss snapshot validation. The recorded request is `max(original configured request, shared evaluated pre-item request)` so amplification can exceed the original basis without an invalid snapshot; actual HP loss is separate. This preserves original zero/reduced-damage recording. A future consumer needing the pre-modifier basis separately would require an explicit additional evidence field.
- Existing public preview/AI deliberately omits ThornHeart reflection; that contract is preserved. Internal receiving simulation/live execution still use the same multiplier. Making reflection visible to public preview is a separate behavior change, not silently included in C.

**Actual change locality:** a new producer supplies its trigger, target, magnitude, duration and provenance to the shared typed policy; it no longer needs a new strongest-source, replacement, decrement, cleanup or single-mark event implementation. Normal registration remains legitimate producer work. Changing one configured duration touches its authoring entry and the existing immutable description chain, not unrelated traits. Shared facts/damage/publication still required full Core/CoreIntegration regression; those gates were retained. Buffer/Job/Framework ownership and service registration are unchanged. These are verified seams and examples, not a claim that future producers need no integration or that View is migrated.

**Scope and evidence:** 55 source/test/meta/asset paths differ from the exact pre-C manifest: 28 current production C# files, 16 test C# files, eight new `.meta` files, one authored asset, and deletion of the old internal producer `.cs`/`.meta`. Two English documents are the plan and handoff. Exact list: [C-scope.txt](../../../.superpowers/buff-debuff-system/C-scope.txt). Owned manifest SHA256: `81C442A59825E53E74515FB29936996D4F383B4B09A31B66B6E21598B7BF71B4`. Final-gate manifest covers 11,219 Assets/Packages/ProjectSettings files, SHA256 `FCAFDC716882A8E4FE5515FF5A1D959A1FDC066D5BF287912056429A89A3E576`. Eight new GUIDs are unique; remaining existing metadata and the approved design are unchanged; changed pure Core/InGame runtime data has no Unity import. Final source bytes match the tested gate. Unity's generated FMOD local-folder metadata was restored byte-for-byte to pre-C SHA256 `71F9498C60897ED084AF4419CD2011EC54BA8B707BF3CC5C679AD2125A76957E`.

Local logs/XML/exit files, `C-verification-summary.csv`, `C-final-integration-failures.csv`, per-file originals, review package/result, progress, and source/delivery manifests are retained under `.superpowers/buff-debuff-system/`. They are local evidence, not new committed artifacts.

**Stop:** Phase C is complete with the explicit existing integration failure and asset diagnostics. Phase D is not started. Core completion does not establish View, PlayMode, scene, visual or whole-game acceptance.

### 5.6 Phase D execution — 2026-10-30

**Authorization and scope:** the user requested D1-D2 and clarified that Phase C, rather than E, was already complete. D continues the existing feature checkout at `bef74f0d0f8eb2b0a2c2821b2a3e595bc0f7dbd9`, preserving the dirty A-C implementation and Core-only cleanup. No commit/push, View work, missing enhanced content, unrelated Stage repair, or E retirement is included. D1-D2 and the final review correction are complete with the existing StageKey exception reported below.

**Implemented behavior and ownership:**

- Positive `HealEveryOwnSideTurnEnds` flows through authoring, pure data, spec, definition, factories and immutable status snapshots. Default 1 preserves existing amount-only callers. The sole existing Regeneration managed-reference instance, `Unit_UndeadHeavy.asset`, keeps heal amount 10 and its GUID and explicitly stores interval 1. Temporary unit/growth copies verify independent intervals 2/3 and frozen earlier values; no normal/enhanced production definition was invented.
- `CompletedEligibleTurnEnds` is holder-owned state in the existing runtime/effect/status snapshot path. Clone/copy/codec/provider/facts equality preserve it, and unrelated applied statuses do not reset it. It is not another recipient buff, HP owner or Framework service.
- The existing controller-end cleanup calls `RegenerationTurnApplication.EndOwnSide` after environment/gimmick damage and terminal outcome handling, retaining existing trait/trail ordering. Each living Field holder of the ending side advances once, even when idle. At the configured interval, existing `CharacterSettlementCalculator.Recover` caps HP and existing settlement records actual recovery. Trait `HealthRecovered` and `CombatCharacterEvent` evidence carry the actual amount; Main persistent HP is published in the same transition.
- Full HP consumes a due opportunity. Other-side endings, preview, rejected/replayed commands, dead holders and non-field time do not advance/heal. Field exit/reentry, death and battle cleanup reset progress; field relocation and a persistent Field Main across night retain it. The final enemy ending heals eligible survivors before recall; terminal environment outcomes stop further recovery/turn work. Only Regeneration is enabled; the other deferred traits remain disabled.
- Existing growth guards remain unchanged. A real integration path advances native `(10,2)` to progress 1, observes growth blocked during battle, ends battle, enhances to `(20,3)`, and constructs the next battle with zero progress while the old snapshot remains 1. No live replacement feature was added.

**Review correction:** one fresh read-only review found a missing fresh-state invariant. `CharacterBuffer.RequireFreshBattleProgress`, already shared by initial preparation and newly created command records, accepted nonzero Regeneration progress. `D-review-red-01` proves the invalid interval-2/progress-1 preparation was accepted. The existing predicate now rejects it before installation, alongside all previous guards. This is the only additional Buffer implementation change; there is no new transaction or weakened growth guard. The corrected integration gate passes 18/18, including unchanged-store assertions. No other actionable finding or deferred minor remains.

The review also confirmed that the marker effect emits no generic effect-plan state mutation, so no unused `CombatCommandEffects.Apply` branch is needed; dedicated turn projection carries the trait snapshot. Actual recovery is visible through the existing health/activation stream. Default-one constructor compatibility and the existing external View/configuration boundaries remain intentional. Detailed decisions are in [D-progress.md](../../../.superpowers/buff-debuff-system/D-progress.md).

**Verification evidence:** every Unity invocation uses 6000.0.73f1 through approved AppData access, with one process and only Core/CoreIntegration assemblies. No cache deletion, upgrade, log suppression or skipped test was introduced.

| Evidence | Result / significance |
| --- | --- |
| `D1-red-01/02` -> `D1-green-01` | Planned cadence contracts absent; corrected two own-test imports before the second RED. Configuration/state/codec gate 22/22. |
| `D1-integration-green-01` | 19/20. All configuration cases and the new real UndeadHeavy validation pass; the existing growth asset case fails with C's documented native `SerializeReference Refid -2` diagnostic. It is retained and not suppressed. |
| `D2-red-01`, `D2-integration-red-02` | 13/23 and 1/5 before integration: missing healing, progress/reset and published changes. An earlier integration attempt had one missing test namespace, corrected before behavioral RED. |
| `D2-green-01` -> `D2-green-02` | 102/105 -> 105/105. Corrected fixtures to account for all three Demon holders, real night participants/reserve facts, and isolated mine damage; product behavior was not weakened. |
| `D2-integration-green-01` | 16/16: capped recovery, persistent Main, immutable snapshots, duplicate publication, stale cadence, late prepare failure, preview and post-battle enhancement. |
| `D-core-01` | 4,277/4,279. Two old deferred-Regeneration expectations updated precisely: active/deferred registration counts 31/5 -> 32/4 and owned/battle Regeneration availability false -> true. Other retirement assertions remain. |
| `D-integration-01` | 400/402. StageKey 2101 retained; the new night fixture reached normal Main death before end 8. Diagnostics confirmed `MainHP=0/Finished`; the recall-specific fixture now uses the existing HQ-only encounter. |
| `D-review-red-01` -> `D-review-green-01` | Fresh-state rejection absent and night fixture diagnostic -> 18/18 after predicate/fixture correction. |
| `D-core-02` | 4,279/4,279, exit 0, zero skips; before refining the existing idle fixture to the plan's exact amount-10, HP40 -> 50 example. |
| `D-core-03` | **4,279/4,279; exit 0; zero skips.** Final full Core on the exact idle40 -> 50 fixture and reviewed implementation. |
| `D-integration-02` | **402/403; exit 2; zero skips.** Only retained StageKey 2101 failure; all seven Regeneration and all three production asset cases pass. |

**Change locality and verification scope:** configuring Regeneration uses its existing authoring-to-definition chain and named cadence contract. Its trigger/reset behavior is isolated in one turn application plus the existing end/placement/cleanup hooks; no Weakness Exposure, shield, CallOfBlood formula or unrelated trait internals were changed. Shared facts, health, turn and publication boundaries still require the full Core/CoreIntegration gates. Existing CallOfBlood/recovery and end-effect tests remain in those suites; View tests are not substituted for Core coverage.

**Artifacts and scope:** 33 source/test/meta/asset paths differ from the starting manifest: 19 production C#, 10 test C#, three new `.meta` files, and one authored unit asset, plus this plan update. Exact paths: [D-scope.txt](../../../.superpowers/buff-debuff-system/D-scope.txt). The 7,345-file gate manifest SHA256 is `34508DAEB69E0F717298106F809F4BB9EEBCB15468C311003985717F96F97035`; owned-file SHA256 is `FD15B8DEAB756CB2CB3636A317A3C5F278C34A95F28A69083CF0C753B5B96471`. There are no scoped deletions or existing metadata changes. Core remains Unity-free, new GUIDs are unique, and source uses LF. Logs/XML/exit files, originals, review diff, summaries and hashes remain under `.superpowers/buff-debuff-system/`. Final delivery hashes match the tested source manifest exactly (zero drift). Unity-generated FMOD local-folder metadata was restored to the recorded pre-D bytes from the C delivery manifest, SHA256 `71F9498C60897ED084AF4419CD2011EC54BA8B707BF3CC5C679AD2125A76957E`.

**Stop:** Phase D is complete with the unchanged StageKey 2101 exception. Stop before E. View/PlayMode/scene acceptance and the known external configuration/serialization issues remain separate from Phase D Core completion. F2 will incorporate D's implemented cadence and recovery contract into the final View handoff.

### 5.7 Phase E execution — 2026-10-30

**Authorization and boundary:** the user explicitly requested Phase E after D completed. E1 continues the existing dirty feature checkout at `bef74f0d0f8eb2b0a2c2821b2a3e595bc0f7dbd9`, preserving Phase 0/Core-only cleanup and A-D. No commit/push, View runtime/UI/presentation DB edits, Framework/View tests, unrelated Priority/flag redesign, Stage/growth content repair or Phase F work is included. Required retirement display instructions update the handoff under E1; this is not F2 finalization or external delivery.

**Implemented behavior:**

- Intimidation authoring, pure data, spec, definition, factory and immutable trait status expose attack reduction only. `PriorityReduction`, `IntimidationDebuffState.PriorityPenalty`, canonical `AttackDecreaseStatus.LegacyPriorityPenalty` and the obsolete debuff-priority calculator are removed. The first attack reduction remains authoritative across repeated hits (`100 -> 80` at 20%); attack completion and existing death/battle cleanup are preserved. Canonical state and source provenance remain the same owners, with no second representation. Core availability no longer advertises `DeferredPriorityComponent`.
- `MoveAttackDiscount = 20` is reserved and explicitly `Retired`, rejected by registry/definition/growth admission. Its default registration, factories, state/rules, effect capture/restore/provider/composition branches, authoring and pure data/spec/status are removed. Prior six retired keys remain rejected. Regeneration stays available; Focus, ThornField and Necromancy remain deferred. General Priority, Rope, flag and unrelated active behavior are unchanged.
- Whole-Assets serialized inspection found no live discount authoring/data instance and no GUID/type reference to the seven removed pure C# types. Their `.meta` files were removed with them only after that proof. The sole live `priorityReduction` field was removed from `Unit_Shielder.asset`; attack reduction remains 20%, its managed-reference type and asset GUID are unchanged. No Missing Script asset was deleted. The existing TypeCache-driven authoring drawer excludes the absent type without another whitelist or branch. Current View code has no dependency on the removed members/types, so no Core compatibility shell is retained.
- The existing handoff section 7 identifies the actual stale Intimidation sentences, inert key-20 visual/table entry and dead `DeferredPriorityComponent` text mapping for the View owner. View assets/code remain untouched. Reserved historical activation values remain without an active producer; they are not executable discount support.

**Verification:** Unity 6000.0.73f1 used the approved AppData execution route; every run had zero skips. The runner was `pwsh -NoProfile -File .superpowers/buff-debuff-system/run-tests.ps1` with a unique `-Run` and Core-only `-Assembly`. The focused Core `-Filter` selected `StatusEffectRetirementTests;IntimidationTraitTests;IntimidationScheduleTests;IntimidationAttackTests` in `ProjectL.Tests.Core`; the integration filter selected `RetiredTraitAuthoringTests`, adding `DataBaseAssetTests` for GREEN. Full gates omitted the filter.

| Evidence | Result / significance |
| --- | --- |
| `E1-red-01` | 32/36: the four new retirement/configuration/availability cases fail before production changes. |
| `E1-integration-red-01` | 2/5: two new authoring/Shielder cases fail as intended; existing Cowboy catalog loading hits the documented native growth serialization error. |
| `E1-green-01` | **34/34; exit 0.** Attack-only configuration, first-application retention, completion, scheduling, retired registry/growth admission and deferred boundaries pass. |
| `E1-integration-green-01` | 7/8; exit 2. Both new authoring/Shielder cases pass. The first cold growth load (`ExistingGrowthDefinitions_KeepValidPayloadKeysAndPositiveClockSettings`) reports the existing native `SerializeReference Refid -2` error. No suppression, asset repair, cache deletion or version change. |
| `E-core-01` | **4,267/4,267; exit 0; zero skips.** Full E Core gate, including preserved A-D behavior and shared clone/factory/preview/atomic-publication regressions. |
| `E-integration-01` | **404/405; exit 2; zero skips.** Only `BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss` fails for missing StageKey 2101. All three `DataBaseAssetTests` and five `RetiredTraitAuthoringTests` cases pass. Passing data assertions does not repair the previously recorded native diagnostics. |

Test count is accounted for: seven obsolete discount/priority methods (13 cases), two invalid-priority cases and one timing-deferred discount case were removed; four retirement cases were added (`4,279 - 13 - 2 - 1 + 4 = 4,267`). Generic collection/clone/copy/factory-mismatch coverage now uses active Roar; invalid/blocked attack admission preserves a pending Acceleration bonus instead of a removed discount. Focus and unrelated regression assertions remain. Integration adds two authoring cases (`403 + 2 = 405`). No tests were skipped or moved into View. The replacement map and exact pre-E sources are retained locally.

**Final review:** one fresh read-only reviewer compared the exact E delta, verified all 68 source/asset/test hashes, independently rechecked removed GUID/type references and found no Critical, Important or Minor issue. No fix pass or deferred minor is required. The reviewer also confirmed that ordinary registration/authoring removal preserves SSOT and Framework/Buffer ownership; no new service, state owner or abstraction is added.

The coordinator explicitly resolved the review's scope exclusions:

1. **Ruling:** View card/HUD text and registration acceptance remain with the receiving owner; section 7 identifies the known stale content and required changes. Cost if treated as finished: the player could still see retired Priority/discount descriptions. Core completion does not waive this gate.
2. **Ruling:** A-D algorithms are not independently reapproved wholesale by the E reviewer; E changes to their tests remove obsolete arguments/assertions, while the full Core/CoreIntegration gates verify preserved integration. Cost of an uncovered prior defect remains outside E's independent-review assurance; no expanded acceptance claim is made.
3. **Ruling:** StageKey 2101 and native growth serialization repair remain outside E. The failing Stage case and cold-load error are reported explicitly; no product/data workaround or log suppression is introduced. Cost remains unavailable boss preparation and unresolved native serialization diagnostics.
4. **Ruling:** the read-only review does not establish whole-game, View, PlayMode or scene acceptance. The coordinator inspected the full XML/logs for the Core results above; external consumer/visual checks remain pending. Cost if conflated: presenting untested gameplay/UI as accepted.

**Actual change locality:** retiring one trait touches its authoring/data/registration contract and the existing trait state/provider/codec/composition seams, plus fixtures that formerly instantiated it. Intimidation's configuration change follows its immutable description and canonical payload chain. General Priority/item implementations, the common transaction, Buffer state ownership, other effect clocks and unrelated trait formulas were not modified. Shared contract removal still requires broad Core/CoreIntegration regression; those gates were run. The improvement is removal of obsolete contracts and narrower configuration, not a claim based on fewer files or no assembly edits.

**Scope and artifacts:** 68 source/test/meta/asset paths differ from the exact pre-E manifest: 19 modified production C# files, seven deleted production C# plus seven deleted `.meta` files, 32 modified test C# files, one new test C# plus its `.meta`, and one authored unit asset. This plan and the existing handoff are the two English documentation changes. `E-review-changes.csv`, `E-review-normalized.patch`, `E-before-sources`, `E-removal-proof.csv`, `E-retired-test-map.csv`, XML/logs/exit files and the review result remain under `.superpowers/buff-debuff-system/` as local evidence. The final tested 7,333-file source/asset/settings manifest SHA256 is `2EE273F943A9A0E02350453A3C6A52064D3C0718FEB5E345D93A49DF22EB53B3`. Existing metadata GUIDs are unchanged; the new test GUID is unique; changed C# uses LF; pure Core/InGame data remains Unity-free. Final delivery source hashes match the tested manifest with zero drift. Unity-generated FMOD local-folder metadata was restored to exact pre-E bytes, SHA256 `71F9498C60897ED084AF4419CD2011EC54BA8B707BF3CC5C679AD2125A76957E`.

**Stop:** Phase E is complete with the existing integration/configuration/serialization limitations explicitly retained. Phase F is not started. The View handoff remains prepared, not externally delivered; whole-game and visual acceptance remain pending.

### 5.8 Phase F final acceptance — 2026-10-30

**Authorization and scope:** the user requested F1-F2 with explicit final judgment on design omissions, contract/ownership boundaries and regression risks. D/E completion was verified against current source. The initial 7,333-file E delivery manifest and an independent path enumeration had zero changes; E XML/exit counts matched discovered cases. F1 permitted reuse, but the final whole-system review uncovered a product gap, so fresh gates supersede that initial reuse decision. No commit/push, View code/assets/tests, production Stage/growth repair or external delivery.

**Final review and correction:** one independent read-only review traced A-E as an integrated system, not merely the E delta. It found one P2 admission defect: changing Weakness Exposure after movement preview capture did not invalidate its expected damage context. `F-review-red-01` reproduced this (1/2): a visible damage10 mine was previewed without exposure, then a30% exposure changed actual loss to13 while the context still matched. `MovementContextCalculator.SameTarget` now compares the shared `WeaknessExposureRules.MaximumPercent` result for movers and affected targets. It does not broaden equality to unrelated trait internals or duplicate amplification. The second case preserves validity when only duration changes at the same effective percentage. Complete expected/final status facts still belong to Buffer publication. Both cases pass in the final full Core gate. No other actionable finding or deferred minor was identified.

**Coverage against the approved design:** fixture names below identify tests actually present and passing in the final XML. `F-coverage.csv` records the focused fixture inventory; full suites cover existing interaction regressions too.

| Decisions / risk | Actual evidence and judgment |
| --- | --- |
| D01-D03: preserve behavior, one applied-state owner, distinct policies | `StatusEffectContractTests`, `CharacterCombatFactsSameValuesTests`, `ContractWithPreservationTests`, `AppliedAttackStatusTests`, `AppliedRestrictionStatusTests`, `ShieldStatusPolicyTests`: canonical normalization/partial writes/conflicts, additive grants, first reduction, restriction clocks, armor ordering, shield eligibility/source preservation. No parallel writable store. |
| D04-D05: expanded catalog; contextual effects and producer responsibilities | `StatusEffectReadModelTests`: stable family/classification, separate clocks, held/derived contributions and conditional eligibility; no unconditional target-dependent badges or new generic trait state. |
| D06, D09-D12: deferred/retired boundaries | `StatusEffectRetirementTests`, `RetiredTraitAuthoringTests`, `LegacyEnhancementRetirementTests`: discount and six old keys rejected, Intimidation attack-only, Regeneration enabled, Focus/ThornField/Necromancy still deferred. Final symbol search leaves reserved key20/retired classification and unrelated Rope PriorityPenalty only. |
| D07, D14-D15: Weakness Exposure | `WeaknessExposurePolicyTests`, `HuntersMarkAttackTests`, `WeaknessExposureDamageTests`, `WeaknessExposureLifecycleTests`, `WeaknessExposurePublicationTests`: max/fallback, weaker/shorter replacement, actual-recipient/ordered damage, items/shields, reflection/secondary evidence, source departure and both reserves. Final F adds the missing preview-to-submission dependency test. |
| D08: Regeneration | `RegenerationCadenceTests`, `RegenerationTurnTests`, `RegenerationPublicationTests`: idle healing, intervals, full HP, terminal ordering, last-night recall, Main persistence, reset and fresh-state admission. No live replacement feature is implied by the guarded post-battle enhancement test. |
| D16: configurable producer clocks | `TraitTurnConfigurationTests`, `HuntersMarkAuthoringTests`, `DataBaseAssetTests`: nondefault exposure/regen values, independent normal/enhanced copies, frozen definitions, positive validation and existing other clocks. ShotGun30%/1; UndeadHeavy heal10/interval1; Shielder attack reduction20%. |
| Publication risk | `StatusEffectPublicationTests`, `WeaknessExposurePublicationTests`, `RegenerationPublicationTests`: immutable old snapshots, full-fact reconstruction, stale state/cadence and late prepare failure install nothing. Existing prepare-before-install transaction source is unchanged. |
| D13, D17: ownership and completion gates | No commit/push/external delivery. Pure Core/InGame code remains Unity-free; no Framework/factory/Job registration or transaction ownership change. Full-fact playback/HUD/card acceptance remains pending under the View owner, not replaced by Core tests. |

**Final architectural judgment:** no unresolved in-scope design omission or ownership violation was found after the reproduced correction. SRP keeps producer trigger/target/configuration separate from common status policy, existing health settlement and Buffer publication. OCP/LSP reuse typed grants without a producer-name whitelist; ISP avoids per-effect universal interfaces; DIP retains current assembly/composition boundaries. SSOT is one immutable status collection and one holder-owned regeneration cadence, with explicit read projections. These are source/contract findings, not a claim that every possible combination or performance behavior was proven.

**Actual locality:** `AppliedAttackStatusTests.AnotherProducer_UsesTheSameAggregationAndIndependentLifetime` uses CleanSweep/MoraleBoost/SteadfastWill identities through one policy without adding gameplay. A producer change needs its trigger/target/provenance/configuration and normal registration, not other trait algorithms. Nondefault SO clocks use the existing immutable chain. A common policy or damage/admission change still requires broad regression: F's two changed C# files required fresh full gates. No mandatory integration coverage was dropped for locality.

**Verification:** `F-core-01`: **4,269/4,269, exit0**; `F-integration-01`: **404/405, exit2**. Zero skips; XML test-case totals match root totals. All three production asset cases and five retired-authoring cases pass. The unchanged StageKey2101 production test remains the sole integration failure. Existing native growth `Refid -2` diagnostics are recorded and neither repaired nor suppressed. No new tests mirror getters; only the two meaningful final-review cases were added.

**Residual risks and release judgment:** Core implementation/evidence is complete with the named existing integration exception; this is not whole-game release acceptance. Current View `CombatEventTypeTable.RouteOf` skips unregistered `CombatTraitStateEvent`, its fact handler lacks full-state application, and HUD/cards still use old mark/retirement meanings. Those are concrete intermediate playback/state/display risks until the receiving worker completes handoff sections4-5/7-9. StageKey2101 still blocks that boss preparation; native growth serialization still needs its owning content investigation. PlayMode, scenes, artwork and profiling were not performed. The final reviewer excluded those domains; the coordinator retains them explicitly as external gates rather than waiving them.

**Deliverables and scope:** finalized English plan and Core-to-View handoff (including regeneration cadence/recovery, retirement, exact compatibility and per-item Pending View owner checklist), plus the reviewed `MovementContextCalculator.cs` correction and `WeaknessExposureDamageTests.cs` cases. No new runtime API, asset, service, dependency or metadata. Source/asset/settings hashes and path set are recorded in `F-final-gate-manifest.csv`; final delivery audit confirms no post-test drift. Local review/evidence/progress artifacts remain under `.superpowers/buff-debuff-system/`. Stop after F; user owns commits/pushes and any external handoff delivery.

## 6. Planning self-review

- Design coverage: mapped in section 5, including all confirmed Q1/Q2 answers and the later trait-owned interval requirement.
- Step granularity: each task has named files/interfaces, explicit failing assertions, implementation boundary, and runnable verification filters. Production bodies are left to the implementer.
- Type consistency: `StatusEffectSet`, `AppliedStatus`, `StatusEffectSnapshot`, shared policies, and Regeneration cadence are defined once in section 3/tasks and reused. No alternate status repository or invented global lifecycle service.
- Review risks: all five risks have named Core tests or an explicit downstream consumer acceptance case. Phase C prepares a contract before the Core producer changes; integrated-release gating and legacy partial-update preservation remain explicit.
- Proportion/scope: implementation-only decisions are added to the approved design; no new trait gameplay is specified. C3/F2 are handoff-document work. View code/UI/presentation DB and consumer-test edits are outside Core execution; pending downstream acceptance is not conflated with Core completion or silently waived.

Phase 0/Core-only cleanup and Phases A-F are complete within their explicitly authorized Core scope; evidence is in sections 5.1-5.8. External View implementation/acceptance and delivery remain pending. Stop here; no commit/push.
