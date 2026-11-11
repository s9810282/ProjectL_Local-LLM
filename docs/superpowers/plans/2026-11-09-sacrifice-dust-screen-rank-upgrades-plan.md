# Sacrifice and Dust Screen Rank Upgrades Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` for sequential execution, or `superpowers:subagent-driven-development` only if that execution method is explicitly selected. Steps use checkboxes. Execute only the authorized Phase, then stop and report. Intended executor: GPT-6.1 sol; this document does not change the active model.

**Goal:** Add configurable rank-2 Sacrifice survival/shield and Dust Screen allied range exemption, with no further offers after each trait's configured terminal rank.

**Architecture:** Keep the existing rank slots and derive the cap from their presence. Compose effective specs through the existing resolver; use the existing damage/shield state and immutable trail facts. Preserve Framework/Buffer/Job ownership and keep View presentation as an explicit handoff.

**Tech Stack:** Unity 6000.0.73f1, C#, existing Core.Contracts/Domain/Application and ProjectL.Systems assemblies, NUnit EditMode tests.

**Spec:** [Approved design](../specs/2026-11-09-sacrifice-dust-screen-rank-upgrades-design.md), approved by the user on 2026-11-09. This turn authorizes this plan, not execution of all Phases.

## Global constraints

- No commits or pushes. Preserve existing FMOD deletions, font edits, and any newly discovered unrelated work. Do not reset/stash other work automatically.
- Implementation documents in English; code/XML comments in Korean. Existing namespaces, Core paths, LF/UTF-8, and GUIDs remain intact. Every new C# file needs its `.meta`.
- Start with current `AGENTS.md` and `Assets/AGENTS.md`. Do not expand Core work into FieldCamera, Architecture, or UI implementation.
- No View test code; no combat View MSBuild. Core tests in this plan verify Core contracts, not UI. If an existing test written by another contributor needs a behavior/expectation change, obtain approval before editing it; never weaken an assertion to obtain a pass.
- Use test-owned inputs. `RankedCombatTestData.Spec` currently reads `DataBaseAssetTests.ActiveGrowthCatalog()`; do not use it for new isolated gameplay tests. Reserve real asset reads for the explicit production-asset checks in Phase D.
- Sacrifice rank 2: `PreventsLethalSacrificeDamage=true`, `ShieldOnActivation=10`; rank 1 defaults false/0. Dust Screen rank 2: `ExemptsAlliedAttackRangePenalty=true`; rank 1 false. Both rank-3 slots absent.
- Shield is granted after damage settlement, does not absorb its triggering hit, persists by existing shield lifetime rules, and adds to the remaining budget for the same Sacrifice origin. Only actual redirected hits receive survival protection.
- Existing numeric trait tuning, training prices, visit limits, legacy `Enhanced` semantics, and acquisition pools remain unchanged. No skipped ranks, rank-4 support, silent history clamps, or automatic refunds.
- Do not activate production rank entries until both their gameplay path and contract tests exist. UI readiness must be reported separately from Core readiness.

## Review focus

1. A terminal-rank unit submits a stale/direct purchase request: no gold, receipt, or history mutation (A2, D1).
2. Protection leaks from a redirected hit to the next direct/DOT/reflection hit: only the redirected hit keeps 1 HP (B2).
3. A multi-hit attack uses the new shield on later hits, but preview/planning omit it: compare projected and executed HP/shield/activation state (B2).
4. An allied exempt trail overlaps another applicable trail: exclude only the exempt source, retain hostile/base penalty and undo evidence (C2).
5. Only a boolean rank field changes: no-op validation, definition identity, Buffer values, and View handoff must recognize the change (B1, C1, D1).

## Execution map and verified path aliases

All paths below are repository-relative, with these exact prefix aliases:

- `Core/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`
- `Bridge/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/`
- `UnitData/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Unit/Trait/`
- `GrowthData/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/TraitGrowth/Item/`
- `Test/` = `Assets/Tests/EditMode/`; `Integration/` = `Assets/Tests/EditMode/CoreIntegration/`
- `GrowthAssets/` = `Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth/Item/`

| Phase | Tasks | Deliverable / boundary |
| --- | --- | --- |
| A | A1–A2 | Rank-2 terminal definitions and consistent offer/purchase/history handling; no new trait gameplay yet. |
| B | B1–B2 | Sacrifice typed configuration and correct redirected-hit settlement, tested with owned fixtures. |
| C | C1–C2 | Dust Screen typed configuration and faction-aware range, AI, and undo behavior. |
| D | D1–D2 | Production SO settings, integrated Core verification, and actionable View handoff. |

Run A -> B -> C -> D sequentially. B and C both edit shared rank/adapter/identity files; do not assign concurrent writers. Each Phase ends at a user review point, even if its checks pass.

## Verification protocol used by every task

- [ ] Before implementation, capture `git status --short`, `git diff --stat`, branch, and a focused baseline. Inspect existing tests before choosing fixtures. Record pre-existing failures separately.
- [ ] Add each task's failing regression tests first; run the named new fixture and record the intended failure before implementing. A compilation error before adding a new API is distinct from a behavioral failure.
- [ ] After implementing, run the named new fixture plus the listed related existing fixtures. Do not rerun broad suites repeatedly when no code changed.
- [ ] Final combined check: Unity EditMode assemblies `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration`. Read NUnit XML counts and failing test names; exit code alone or a test list is not proof of a pass.
- [ ] Prefer the user's Unity Test Runner when an Editor already owns the project. Select EditMode and the exact fixture names below. If using a batch Editor for Core checks, first discover and verify the installed executable for 6000.0.73f1 and ensure the project is not open in another Editor. The usual `C:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe` was **not present during planning**; do not copy that path as a working command.
- [ ] Any Codex Unity launch must use the approved AppData-access execution path. After resolving `$unityEditor`, `$projectPath`, `$resultPath`, `$logPath`, and `$filter`, the argument contract is: `& $unityEditor -batchmode -nographics -projectPath $projectPath -runTests -testPlatform EditMode -testFilter $filter -testResults $resultPath -logFile $logPath`. Use a scratch output directory, not Assets. Do not add `-quit` that can interrupt the test runner; do not delete caches or change Unity versions for CurlRequestCache errors.
- [ ] Run `git diff --check` and inspect the final scoped diff. If execution is unavailable, report **컴파일 미확인 / tests not run** and hand over the filters; do not mark a test gate passed. Do not silently continue into the next Phase.

## Phase A — Terminal ranks and training rules

### A1. Accept a contiguous rank-2-only definition

**Modify:** `Core/Contracts/Enhancements/TraitGrowthDefinition.cs`, `GrowthData/TraitGrowthDefinitionData.cs`.
**Create tests:** `Test/TraitTerminalRankTests.cs`, `Integration/TraitTerminalRankAuthoringTests.cs` (+ `.meta`). Use Vanguard with explicit 1/4/9 values as the numeric control; this task does not need the new traits.

**Interfaces:** `public int MaxRank { get; }` derived from slots; `public bool CanAdvanceFrom(int rank)`; `public static int ValidateRankSlots(TraitKey key, TraitRankValues rank2, TraitRankValues rank3)` on `TraitGrowthDefinition` returns the derived cap and is reused by DB validation. Preserve `ValuesAt(int)` and `ResolveRank(TraitSpec,int)` signatures.

- [ ] Tests: `Rank2OnlyStopsAtTwo` expects cap 2, advance(1)=true, advance(2)=false; `ExistingThreeRanksRemainThree` expects cap 3 and values 1/4/9; `NoRanksRetainsLegacyEnhanced` keeps legacy selection behavior; `Rank3WithoutRank2IsRejected` and `MismatchedRankKeyIsRejected` fail at construction in Core and DB.
- [ ] Change slot validation: neither => 1; rank2 only => 2; both => 3; rank3 only or wrong keys => error. `CanAdvanceFrom` throws for rank outside 1–3, otherwise returns `IsTrainable && rank < MaxRank`. Do not store a serialized cap or create another service.
- [ ] `ValuesAt` rejects requests outside this definition's cap with key/rank context. Leave old non-trainable composition behavior intact; do not call numeric `FromNormal` for unsupported legacy-only traits.
- [ ] For each configured adjacent rank, resolve from the normal spec and compare with `TraitSpec.SameValues`. Reject an unchanged result. Tests: `DuplicateRank2IsRejected`, `DuplicateRank3IsRejected`, `UnavailableRankFailsWithTraitAndRank`. Perform semantic validation at Core definition construction; DB performs shape validation without referencing the runtime CharacterSystem adapter.
- [ ] Run new fixtures plus `TraitGrowthCatalogTests`, `OwnedTraitGrowthTests`, `NewTraitRankTests`, `TraitGrowthAuthoringTests`. If an older fixture deliberately supplied duplicate values, record its ownership and purpose before changing it.

### A2. Use the same cap for offers, purchases, and restored history

**Modify:** `Core/Application/Shop/TrainingRules.cs`, `Core/Application/Roster/TraitGrowthRules.cs`, `Core/Application/Enhancements/OwnedUnitSpecResolver.cs` only where explicit over-cap context is needed.
**Tests:** add to new `TraitTerminalRankTests.cs`; create `Integration/TerminalRankTrainingTests.cs` (+ `.meta`). Reuse test-owned integration setup; add an optional catalog input to `Integration/TrainingGroundTestRuntime.cs` only if needed, keeping all existing callers/defaults unchanged.

**Consumes:** A1 `MaxRank` / `CanAdvanceFrom`. **Produces:** unchanged public training commands, receipts, and offer DTOs with corrected eligibility.

- [ ] Replace the two hard-coded training limits with the captured visit catalog's definition rule. Reject terminal rank with `RankLimitReached` before mutation. Preserve ownership/availability/stale-rank/price validation ordering except where the cap check requires the definition.
- [ ] Replace only the rank-aware `rank < 3` candidate filter in `TraitGrowthRules`. Preserve legacy non-trainable `Enhanced` eligibility and the existing once-per-selection history rules. Do not enable level-up enhancement for Sacrifice/Dust Screen by filling `Enhanced`.
- [ ] Validate effective restored rank through the resolver/definition before exposing or mutating state. Tests: `OverCapPurchaseHistoryIsRejected`, `LegacyEnhancementPlusPurchaseRespectsCap`, `UnsupportedLegacyEnhancementStillResolves`. Counts/history remain the SSOT, not a new current-rank field.
- [ ] Integration tests: `Rank2PurchaseRemovesNextVisitOffer` (one purchase, final rank 2, next visit no eligible trait), `TerminalRequestDoesNotMutateBuffers` (gold/RunSpent/receipt/history unchanged), `ActiveVisitRetainsCapturedRankDefinition` (change the external catalog after entry; published trade follows captured catalog), `ExistingRank3OfferAndPricesUnchanged` (1->2 and 2->3 still cost the fixture's 10/20).
- [ ] Run new fixtures plus `TrainingGroundPurchaseTests`, `TrainingGroundVisitTests`, `TrainingGroundCommandTests`, `TraitGrowthRulesTests`, and `TraitGrowthTransactionTests`. Report A and stop.

## Phase B — Sacrifice upgrades

### B1. Carry survival and shield settings through the existing definition chain

**Modify:** `UnitData/SacrificeTraitAuthoring.cs`, `UnitData/SacrificeTraitData.cs`, `Core/Contracts/Commands/Traits/SacrificeTraitSpec.cs`, `Core/Domain/Traits/Definitions/SacrificeTraitDefinition.cs`, `Core/Application/Combat/Traits/SacrificeTraitFactories.cs`, `Bridge/TraitSpecAdapter.cs`, `GrowthData/TraitRankAuthoring.cs`, `Core/Contracts/Enhancements/TraitRankValues.cs`, `Bridge/MainSpecializationDefinitionSnapshot.cs`.
**Create tests:** `Test/SacrificeUpgradeTests.cs`, `Integration/SacrificeUpgradeContractTests.cs` (+ `.meta`). Keep production assets unchanged until D.

**Interfaces:** extend the existing `(int radiusCells, int maxTriggersPerEnemyTurn)` spec/data/definition constructors with optional `bool preventsLethalSacrificeDamage=false, int shieldOnActivation=0`; expose corresponding get-only properties `PreventsLethalSacrificeDamage` and `ShieldOnActivation`. Add `SacrificeRankAuthoring` in the existing authoring file.

- [ ] Add named booleans to `TraitRankValues` as optional constructor arguments/properties: `PreventsLethalSacrificeDamage` and `ExemptsAlliedAttackRangePenalty`, default false. Preserve the existing `key, amount, secondAmount` arguments. Reject a true flag for an unrelated key. Sacrifice `Amount` is the non-negative shield; its `SecondAmount` is absent. C implements the Dust Screen case.
- [ ] Add the Sacrifice `FromNormal` / `Apply` cases: preserve radius and trigger limit, set shield from `Amount` and survival from the named flag. Normal defaults remain false/0. Validate authoring via existing spec validation; do not duplicate HP rules in SO code.
- [ ] Pass fields through adapter/factories and include them in `SameValues` and definition identity. Snapshot identity must include rank-slot presence and both boolean flags, not only amounts. The existing `TraitSettingsSet` publishes the effective spec; no extra settings registry is needed.
- [ ] Tests: `OldConstructorKeepsRank1Behavior`; `Rank2ResolvesSurvivalAndTenShield`; `ShieldCanBeRetunedToTwenty`; `BooleanOnlyUpgradeIsNotNoOp`; `BooleanOnlyDefinitionChangeIsDetected`; `SplitUnlocksAtRanksTwoAndThree` (survival at 2, shield at 3); `NegativeShieldAndWrongFlagKeyAreRejected`. Exercise SO -> data -> spec -> rank resolution with temporary SOs, not asset files.
- [ ] Run these new contract tests plus `TraitDefinitionContractTests`, `NewTraitRankTests`, `TraitGrowthAuthoringTests`, `MainSpecializationStoreTests`, and `MainSpecializationPreparationTests`. B is not complete until B2.

### B2. Settle actual redirected hits once, consistently in simulation and execution

**Modify:** `Core/Application/Combat/{SacrificeDamagePlanner,AttackDirectDamageCalculator,KillChainAttackCalculator,CombatDamageApplication}.cs`, `Core/Domain/Traits/Effects/SacrificeEffectRule.cs` as needed to expose resolved settings.
**Create:** `Core/Application/Combat/SacrificeDamageSettlement.cs` (+ `.meta`) for the small per-hit policy/context, not a new damage engine. Extend the B1 test files.

**Interfaces:** in that file, `internal readonly struct SacrificeHitUpgrade` exposes `EntityId RecipientId`, `bool PreventsLethalSacrificeDamage`, and `int ShieldOnActivation`. The static `SacrificeDamageSettlement` helper provides `Capture(CombatCommandInput input, CharacterCombatFacts protector, CombatProfileReadSet profileReads = null) -> SacrificeHitUpgrade`, `LimitHealthLoss(int currentHealth, int damageAfterShields, in SacrificeHitUpgrade upgrade) -> int`, and `GrantShield(CharacterCombatFacts recipient, in SacrificeHitUpgrade upgrade) -> CharacterCombatFacts`. Capture reads the effective definition; GrantShield returns the new facts without maintaining another state store. Add optional `SacrificeHitUpgrade? sacrificeUpgrade = null` to existing `CombatDamageApplication.Apply` and `ApplySteps`, forwarding it unchanged. The existing application owns mutation/event/settlement order.

- [ ] Read the complete four application files before editing. Map ordinary pending-hit flush, planner simulation/probe, and extra-attack calls. Record which hits are redirects. Do not infer a redirect from trait possession or set a persistent immunity flag.
- [ ] Create context only after real redirect selection/activation succeeds, and on equivalent hypothetical redirects in planner simulation. Ordinary lethal probes of the original victim receive null context. Carry context in `pendingHealth` tuples to `FlushHealthSteps`; pass it through `ResolveSingle` consumers for extra attacks without consuming activation twice.
- [ ] At `CombatDamageApplication.ApplySteps`, after existing item defense and shield absorption but before HP mutation/statistics, apply `actualLoss = min(currentHP - 1, remainingDamage)` only for a live recipient matching survival-enabled redirect context; otherwise preserve the current calculation. Reject mismatched recipient context. Do not change `Defeat` or unrelated damage kinds to support survival.
- [ ] After existing after-damage/death/trait/presence settlement and before completing the outcome, grant the configured shield only if the current recipient is alive. Use the existing Sacrifice origin, `ShieldStatus`, and `ShieldStatusRules`; sum same-source remaining budget with checked arithmetic. Emit existing state/ShieldGranted events once. `CombatDamageOutcome.After` includes the shield. Actual HP loss—not raw attempted damage—continues to drive HP-loss stats/kill decisions; preserve the distinct existing raw-damage accounting semantics.
- [ ] The next pending hit must see the preceding hit's new shield at actual settlement. Mirror this in planner simulation and extra attacks. Keep the current reflection/pending-flush boundaries and attack-selection order; do not globally change all attacks to immediate settlement. If parity cannot be achieved without changing those contracts, stop with the concrete counterexample instead of broadening this Phase.
- [ ] Exact fixture assertions: `LethalRedirectLeavesOneThenTenShield` uses bearer HP=5, no defenses, damage=20 => HP=1, actual loss=4, shield=10, no bearer death/kill reward. `NonlethalRedirectDoesNotForceOneHP` uses HP=30/damage=10 => HP=20/shield=10. `ExistingShieldIsConsumedBeforeFloor` uses HP=5/shield=7/damage=10 => HP=2/new shield=10. These fixtures must independently make the original ally's hit lethal.
- [ ] Additional cases: bearer already HP=1 => no HP loss and one shield grant; disabled survival with lethal redirect => death and no grant; item-after-damage heal survives and raises HP normally; later direct/DOT/reflection hit has no floor; a later 6-damage hit consumes the new shield leaving 4; two legal activations across enemy turns add remaining same-source budget; zero post-defense damage still grants once after a valid activation. Instant defeat remains outside redirect handling.
- [ ] `PreviewAndExecutionAgreeWithUpgrade` and `ExtraAttackUsesSameUpgradeSettlement` compare final HP, shield, activation count, and protected ally outcome. Add an AOE/multi-hit case with competing protectors and a later direct hit; include damage-ledger and shield publication assertions through Core Buffer integration.
- [ ] Run `SacrificeUpgradeTests`, `SacrificeUpgradeContractTests`, `SacrificeTraitTests`, `SacrificePublicationTests`, `TraitShieldTests`, `TraitShieldPublicationTests`, `CombatItemDirectDamageTests`, `CombatItemDamageRuleTests`, `KillChainAttackTraitTests`, `ThornHeartReflectionDirectTests`, and `ThornHeartReflectionPreviewAiTests`. Report B and stop.

## Phase C — Dust Screen allied exemption

### C1. Author the flag and preserve it on immutable trails

**Modify:** `UnitData/DustScreenTraitAuthoring.cs`, `UnitData/DustScreenTraitData.cs`, `Core/Contracts/Commands/Traits/DustScreenTraitSpec.cs`, `Core/Domain/Traits/Definitions/DustScreenTraitDefinition.cs`, `Core/Application/Combat/Traits/DustScreenTraitFactories.cs`, `Bridge/TraitSpecAdapter.cs`, `GrowthData/TraitRankAuthoring.cs`, `Core/Contracts/Enhancements/TraitRankValues.cs`, `Bridge/MainSpecializationDefinitionSnapshot.cs`, `Core/Contracts/Combat/TraitTrailFact.cs`, `Core/Domain/Traits/Rules/DustScreenTraitRule.cs`.
**Create tests:** `Test/DustScreenUpgradeTests.cs`, `Integration/DustScreenUpgradeContractTests.cs` (+ `.meta`).

**Interfaces:** append optional `bool exemptsAlliedAttackRangePenalty=false` to Dust Screen spec/data/definition and trail constructors, with get-only `ExemptsAlliedAttackRangePenalty`. Add `DustScreenRankAuthoring`; its numeric `Amount` must be 0 and `SecondAmount` absent, while its named boolean is the upgrade value.

- [ ] Implement authoring -> data -> adapter -> spec -> definition -> rank resolution. Preserve range penalty 1, damage reduction 20%, lifetime, and all source fields. Add flags to value equality/identity, using B1's rank payload rather than another flag encoding.
- [ ] Capture the resolved flag in `DustScreenTraitRule.Create`. In `TraitTrailFact`, preserve it through `WithKnownTo`, both remaining-turn copy paths, and every constructor delegation. Include it in `SameConfiguration` / successor validation. Reject an enabled Dust Screen-only flag on GroundCompaction facts.
- [ ] Tests: `Rank2OnlyChangesAlliedExemption`; `TrailCapturesCreationSettings`; `TrailCopiesRetainExemption`; `ChangingExemptionIsNotSameConfiguration`; `CreatorDeathDoesNotChangeTrailSettings`; `GroundCompactionCannotCarryDustFlag`. Defaults on existing constructor calls must remain false.
- [ ] Run new fixtures and `TraitTrailTests`, `TraitTrailPublicationTests`, `TraitDefinitionContractTests`. C is not complete until C2.

### C2. Thread querying faction through range, AI, and undo

**Modify:** `Core/Domain/Traits/Rules/DustScreenTraitRule.cs`, `Core/Domain/Movement/EnvironmentTerrainRules.cs`, `Core/Application/Combat/TraitTrailCalculator.cs`, `Core/Application/Combat/EnemyAi/EnemyActionCandidates.cs`, `Core/Application/Combat/MovementUndoPolicy.cs`; update any additional authoritative callers found by the exact searches below. Extend C1 tests.

**Interfaces:** `RangeSourcesAt(CellCoord cell, MovementEnvironmentFacts environment, Owner queryingSide)` and `RangeReductionAt(CellCoord cell, MovementEnvironmentFacts environment, Owner queryingSide)`; matching public overload on `TraitTrailCalculator`. `EnvironmentTerrainRules.AppointRangeAt(CombatCommandInput input, int range, CellCoord anchor, MovementEnvironmentFacts environment, Owner queryingSide)` supplies explicit side; `AppointRange` passes `actor.Owner`.

- [ ] Before editing, run `rg -n 'RangeSourcesAt|RangeReductionAt|AppointRangeAt' Assets/ProjectL/Script/Systems -g '*.cs'`. Update every authoritative range caller, including AI endpoint `_actor.Owner` and movement undo's captured attacker owner. Never default to Player. Preserve an existing public faction-less overload only as explicitly unfiltered compatibility behavior; combat callers cannot use it.
- [ ] Filter eligible range sources by `!(trail.ExemptsAlliedAttackRangePenalty && trail.CreatingSide == queryingSide)` before computing maximum penalty and source ordering. Leave defense selection/20% reduction untouched. Do not let one friendly exempt source erase another applicable source.
- [ ] For movement undo, capture only actually applicable range sources. Retain a trail's defense-consumption evidence even if it was range-exempt. Keep creation cells, expiry, leap handling, and existing movement-owned state unchanged.
- [ ] Parameterized tests for Player and Enemy creators: base trail => both lose 1 range; upgraded => creator faction loses 0, opponent loses 1. Mixed friendly-upgraded + hostile/base trail => applicable penalty remains 1; equal maximum contributors retain deterministic IDs. Both factions still receive the same 20% defense effect.
- [ ] `LegalAttackPreviewAndAiAgreeOnExemption` checks an attack at the original range boundary and an AI endpoint on upgraded terrain. `ExemptRangeDoesNotCreateUndoConsumption` checks range-only usage; `DustDefenseStillBlocksUnsafeUndo` checks subsequent damage. Include expiry/copy roundtrip and creator-removed cases.
- [ ] Run new fixtures plus `TraitTrailTests`, `TraitTrailPublicationTests`, `MovementEffectUsageTests`, `MovementUndoBarrierTests`, `MovementEffectCombinationTests`, `MovementEffectCombinationPublicationTests`, `EnemyAiBaselineTests`, and `EnemyAiBaselineServiceTests`. Repeat the caller search and confirm no authoritative faction-less call remains. Report C and stop.

## Phase D — Asset activation, integrated proof, and View handoff

### D1. Configure the two SOs and verify end-to-end Core publication

**Modify:** `GrowthAssets/TraitGrowth_Sacrifice.asset`, `GrowthAssets/TraitGrowth_DustScreen.asset`; production description text only as needed for correct values. Keep GUIDs, category pools, catalogs, and other traits unchanged.
**Tests:** extend the new Integration fixtures for training -> effective settings -> battle publication; inspect `Integration/DataBaseAssetTests.cs` before any ownership-approved update. Do not change existing tests merely because their method name contains an obsolete count.

- [ ] Add rank2 managed references using the new authoring types, with Sacrifice true/10 and Dust Screen exemption true. Keep normal defaults, `enhanced` null, and rank3 null. Confirm Inspector serialization roundtrips; do not manufacture a duplicate rank3.
- [ ] Verify real assets resolve caps 2 and exact flags/values, and the previous 19 ranked traits retain their effective settings. `DataBaseAssetTests.GrowthProduction_OnlyTheSeventeenRankedKeysCanTrain` currently asserts an exact key set; inspect its data source, then seek approval if another author's test needs expectation changes. Add separate boolean/cap assertions rather than forcing the new traits through a numeric-only rank-1/2/3 test table.
- [ ] With test-owned inputs, buy each upgrade through the Core service, assert one paid history entry and exact current/next flags in `TrainingChoiceSnapshot`, final rank/settings in Character/Combat Buffer, and omission from next-visit eligible offers. Receipt history can remain visible. Verify wrong-rank/repeat requests leave the complete Buffer snapshot unchanged.
- [ ] Verify definition identity notices changed boolean settings and rank-slot presence; valid existing saves resolve unchanged, over-cap restored histories fail descriptively, and active visits retain pinned definitions/prices. Keep other legacy enhancement purchase restrictions unchanged.
- [ ] Run the two full Core EditMode assemblies once after integration; separately list `DataBaseAssetTests` outcomes. Compare failures to baseline, inspect any new dependency-caused regressions, and stop on unauthorized test changes or design deviations. Do not label Core complete while relevant new failures remain unexplained.

### D2. Deliver presentation requirements and a precise verification report

**Create:** `docs/superpowers/handoffs/2026-11-09-sacrifice-dust-screen-upgrades-core-view-handoff.md` (English). **Update:** this plan's execution record only. No UI implementation, new service, or UI test is included.

- [ ] Document the exact final names/types of rank boolean fields and effective spec fields; existing Buffer read paths; the two caps; command/error behavior; sample rank1/rank2 values; shield event ordering; and trail creating-side semantics.
- [ ] Identify the current formatter `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/TrainingCardContentBuilder.cs`: `ValueLine` only formats Amount/SecondAmount and requires `RankValueFormat`. Hand off named boolean presentation requirements and exact affected TraitVisual assets found in the repository. Never show Dust Screen as only `0 -> 0`, and do not infer effects from rank numbers in View.
- [ ] Inspect those two TraitVisual rows for missing required text. Record the concrete gap before claiming production readiness; a Core SO activation can expose the current formatter's missing-text exception. View work must be scheduled explicitly, and UI acceptance remains pending until that owner integrates and the user verifies it.
- [ ] User Unity checklist: rank2 offer text covers both Sacrifice effects / allied exemption; purchase changes stats; next visit does not offer rank3; HP/shield playback matches Core; attack range highlighting matches the unit's faction; no missing-text exception. Do not author View tests or run combat View MSBuild.
- [ ] Final report: changed files and why; what is confirmed by code/tests; exact test counts/log paths; pre-existing failures; **컴파일 미확인** wherever compile evidence is absent; Unity/UI items still pending. Report D and stop. User performs any commit/push.

## Completion records and resume instructions

| Phase | Code / assets | Verification evidence | User review | Next action |
| --- | --- | --- | --- | --- |
| A | A1–A2 implemented | Unity compiled. Final focused run passed 120/120 cases. The earlier full run and baseline comparison are recorded below. | Two existing assertion changes approved and verified | Phase A review boundary. |
| B | B1–B2 implemented | Final Unity EditMode run had 6,246 passes, 64 baseline failures and 10 skips. All 25 new cases and all five affected existing cases passed. | Bool fixture and one frozen row update approved and verified | Phase B review boundary. |
| C | C1–C2 implemented | Unity compiled. All 252 focused cases passed, including 26 new cases. The full run had 64 matching baseline failures and one timeout reproduced against Phase B in the same environment. | Pending | Phase C review boundary. |
| D | D1–D2 implemented | Unity compiled. All four new cases passed. The full run had 6,275 passes, 65 failures matching Phase C, and 10 skips. | Exact trainable key expectation update approved and verified | Phase D review boundary. View integration and manual Unity checks are pending. |

When resuming, read the approved design, this table, and current scoped diff. Do not repeat completed work merely to rebuild context. If a required change exceeds the listed boundary, report the exact source contract and proposed adjustment before implementing it.

### Phase A execution record (2026-11-09)

- At baseline `eac06712f`, 90/90 focused cases passed. The A1 red run reproduced five expected failures (rank-2-only definitions and no-op validation). The A2 red run reproduced four expected failures (offer generation and legacy enhancement eligibility).
- `TraitGrowthDefinition` owns the cap, slot shape and semantic change checks. The DB reuses the shape check. Training offers, purchases and ranked enhancement candidates share the cap. The existing resolver rejects over-cap history through `ResolveRank`, which now reports the trait key, requested rank and cap. Progression history and the visit definition retain their existing owners.
- Before Phase A, a one-upgrade definition required changes to DB and Core validation and to multiple rank consumers. With Phase A, the author configures the existing rank-2 slot and leaves rank 3 empty. The relevant contracts are rank validation, restored history and the visit snapshot. The focused cases cover these contracts and the existing three-rank prices. Independent review found no additional product-code blocker.
- In the full EditMode run, Core had 5,236 passes, 25 failures and 8 skips. CoreIntegration had 983 passes, 41 failures and 2 skips. All 24 new cases passed. The baseline comparison re-ran the 36 fixtures with failures and reproduced 64 of the 66 failures with identical messages, including all seven `DataBaseAssetTests` failures. The production ranked-key and effective-tuning assertions passed.
- With user approval, `TraitGrowthAuthoringTests.RankedAuthoring_ValidatesBothHalvesAndPublishesIndependentFinalAmounts` now verifies the rank-2 amount and absent rank-3 slot. `NewTraitAuthoringTests.InvalidValuesFailBeforeActivation` verifies rejection of a rank-3-only input. Both passed in the final focused run. Across the 13 selected fixtures, 120 cases passed with zero failures or skips.
- Unity 6000.0.73f1 results and logs are under `Library/PhaseA-TerminalRank/` (`baseline`, `a1-red`, `a1-green`, `a2-red`, `phase-a-core`, `baseline-failures` and `phase-a-focused-final`). The full run predates the two test-only updates. Phase A is at its review boundary.

### Phase B execution record (2026-11-09)

- The pre-change focused run passed 287/287 cases. B1 red stopped at compilation on the missing named parameters and authoring type. B2 red reproduced the missing survival and shield behavior. The two-argument `SacrificeTraitSpec` constructor remains available for callers that instantiate it by reflection.
- The existing trait authoring, adapter, factory and rank resolver carry survival and shield settings. Definition identity compares both flags and rank-slot presence. The existing damage boundary settles the per-hit upgrade, and existing status effects own the shield budget. A later tuning change uses rank authoring. Changes to damage settlement require understanding the redirect, ordered damage and shield contracts and verifying direct hits, extra attacks, reflection and Buffer publication together.
- Direct attacks attach the upgrade only after consuming a redirect. The planner passes the upgrade for hypothetical redirects and leaves the original-target lethality probe unchanged. Kill-chain attacks use the same settlement boundary. The existing ordered flush lets a later hit use the granted shield. The shield remains available across enemy turns under the existing status lifecycle. Independent review found no product-code defect requiring a change to the ordered flush or reflection boundary.
- The final focused run passed 311 of 312 cases, including all 25 new cases. It covers item healing, the real HP-loss ledger, the next hit consuming a granted shield, two enemy turns, ordinary and extra attacks, and a duplicate delivery stopped by lethal reflection after the first redirect. The sole failure is the existing reflection-based argument generator in `MainSpecializationPreparationTests`, which cannot construct a bool argument.
- The full run before the final reflection test had 6,240 passes, 69 failures and 10 skips. Of the failures, 64 match the pre-change baseline and one is the bool argument generator. Four more failures share the `sacrifice-ordered` row in `phase-2-preview-reference.json`. A diagnostic comparison proved exact equality after removing only the neutral `PreventsLethalSacrificeDamage=false` and `ShieldOnActivation=0` fields from the current serialization. The type list also matched exactly. The candidate row is under `Library/PhaseB-SacrificeUpgrade/sacrifice-ordered-candidate-row.json`.
- With user approval, the reflection-based argument generator now supplies both bool values and the frozen reference now contains the verified `sacrifice-ordered` row. The type list and all other rows were preserved. The final full run passed all five affected cases and all 25 new cases. Core had 5,255 passes, 25 failures and 8 skips. CoreIntegration had 991 passes, 39 failures and 2 skips. All 64 failures matched the pre-change test names and messages.
- Unity 6000.0.73f1 result XML and logs are under `Library/PhaseB-SacrificeUpgrade/`. The final full result is `phase-b-approved-core`, the prior full result is `phase-b-core`, the diagnostic comparison is in `b2-boundary-probes`, and the targeted result before approval is `phase-b-focused-final`. Phase B is at its review boundary.

### Phase C execution record (2026-11-09)

- The final Phase B result had 151 passes across the nine pre-existing Phase C regression fixtures. C1 red detected missing authoring and contract APIs at compile time. C1 green passed 115 cases. C2 red reproduced six failures in allied range, overlapping sources, AI endpoint selection, creator-independent queries and undo consumption. The final focused run passed all 252 cases, including 26 new cases.
- The existing authoring and rank resolver carry the exemption to the Dust Screen spec and definition. Trail creation copies it into the existing immutable fact, which retains it across visibility, expiry and undo copies. Existing callers using the original spec or trail constructors receive false. The faction-less public range overload retains its unfiltered behavior.
- The range rule filters exempt friendly sources before choosing the greatest applicable penalty and ordering ties. Attack, preview, AI endpoint and movement undo paths pass the querying unit's owner. The damage reduction path still selects the same sources for both factions. The caller search and independent review confirmed the authoritative routes use faction-aware queries.
- A tuning change can now use the existing rank slot. The affected behavior is determined by the rank definition, trail creation facts and querying owner. Verification covers faction pairs, overlap and ties, removed creators, attack legality, AI endpoints and range versus defense undo evidence. Overlap selection and undo evidence were exercised in separate cases. Existing Framework and Buffer ownership was preserved.
- Full EditMode passed 6,271 cases, with 65 failures and 10 skips. Of the failures, 64 matched the Phase B full result by name and message. `AttackPreviewResumptionTests.AllPartitionsMatchFrozenPreview(1)` hit the existing 180-second timeout in the full run (197.7 s) and on isolated retry (195.3 s). The same isolated test on the Phase B sources timed out with the same message in the current environment (194.0 s). The other partition cases and the frozen preview assertions passed in the full run. The Phase C files were restored byte for byte after the comparison.
- Unity 6000.0.73f1 result XML and logs are under `Library/PhaseC-DustScreenUpgrade/` (`c1-red`, `c1-green`, `c2-red-behavior`, `c2-focused`, `phase-c-core`, `preview-partition-1-retry` and `preview-partition-1-phase-b`). Phase C is at its review boundary.

### Phase D execution record (2026-11-09)

- Added the rank-2 managed references to the Sacrifice and Dust Screen production growth SOs. Sacrifice enables redirected-hit survival and grants 10 shield. Dust Screen exempts allies from the trail's attack range penalty. Both definitions cap at 2. Normal settings, absent enhanced and rank-3 slots, asset GUIDs, category pools, and catalog registrations were preserved.
- With user approval, the exact trainable key assertion now includes both traits. The existing 17 live traits passed all 51 numeric tuning cases. The dormant ranked Rampage and Bleeding rows passed their existing settings and exclusion checks. The production catalog now has 19 trainable traits.
- The pre-activation Unity run passed both isolated purchase cases and failed only the two production cases, which reported a cap of 1 instead of 2. After activation, both production cases verified exact base and rank-2 settings and passed a save, unload, and reimport round trip of copied SOs, including their managed-reference types. Both purchase cases verified current and next values, one paid upgrade, stale and repeated request atomicity, the absence of a later offer, and owned and battle Buffer settings. The existing definition identity, restored history, pinned definition and price, and legacy purchase tests passed.
- Core had 5,279 passes, 26 failures and 8 skips. CoreIntegration had 996 passes, 39 failures and 2 skips. All 65 failures matched Phase C by test name and message, including the preview partition timeout already reproduced against Phase B. `DataBaseAssetTests` had 69 passes and 7 existing failures involving production category and pool assignments, missing stage 1101, Akimbo and Cannon trait setup, and Rest/ordinary shop references. The updated exact key assertion and all four new cases passed.
- Only the existing rank slots need authoring changes to enable these upgrades. Their effective values follow the existing roster and battle resolver paths. Verification covered catalog composition, shop transaction and publication boundaries, and managed-reference persistence without adding a rule or state owner.
- Unity 6000.0.73f1 results and logs are under `Library/PhaseD-RankActivation/` (`d1-before-assets` and `phase-d-core`). The [Core-to-View handoff](../handoffs/2026-11-09-sacrifice-dust-screen-upgrades-core-view-handoff.md) records the training formatter exception, presentation requirements, and manual Unity checklist. Independent review found no Core blocker. Visual Inspector and UI playback acceptance are pending. Phase D is at its review boundary.

Planning self-review: design sections 4/5 map to A and B1/C1; section 6 to B2; section 7 to C1/C2; section 8 to D1/D2; section 9 boundaries are carried in the file map/interfaces; section 10 verification is distributed across all tasks. No product code, tests, SOs, or Unity execution were changed/performed while writing this plan.
