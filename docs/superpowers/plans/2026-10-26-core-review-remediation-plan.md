# Core Review Remediation — Task Plan

Date: 2026-10-26
Design: [Core Review Remediation](../specs/2026-10-26-core-review-remediation-design.md) (all decisions recorded in its section 9).
Source baseline: `feature/Cheon/Core/NewCombatSytem`, HEAD `e413008c`. Recheck the checkout before starting; preserve any concurrent changes.
Status: Plan only. No code, asset or test has been changed. Each Phase needs the user's go-ahead and stops for review.

## 0. Global rules

- Scope: Core (`C`), Core-facing adapters (`CS`), InGame DB data (`D`) and the named InGame assets only. Do not edit ViewSystem, UI, LifeCycle, Framework, scenes, prefabs or ViewGroup assets.
- Tests: change and run only `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration`. The Framework test assembly is abandoned and is neither migrated nor run.
- Verification: no MSBuild. Each Phase ends with a list of Unity Test Runner targets for the user to run, and the report says "not run" until results come back.
- No commit or push. The user commits.
- New or moved `.cs` files get a `.meta`; deleted `.cs` files lose their `.meta`; GUIDs of kept files are unchanged.
- Asset edits are limited to the ones named in a Task. Preserve every other YAML value, GUID and reference. Do not delete any other asset, including Missing Script or unreferenced assets; report them instead.
- New or edited code comments are Korean. Namespaces follow folders, Allman braces, UTF-8, LF.
- If a Task needs a decision the design did not make, stop and ask instead of implementing.

Path aliases follow design section 2 (`C`, `CS`, `D`, `A`, `T`, `TI`).

## 1. Phase overview

| Phase | Tasks | Content | Stop condition |
|---|---|---|---|
| P1 | T1–T10 | Fixes and decided changes: R12, R4, R1, R5, R2, R3, R6, R7, R8, cleanup C1–C6 | Test Runner targets listed; report delivered. |
| P2 | T11 | Comments only: R1, R9, R10, R14 pointers and the R11 comment | Report delivered. |
| P3 | T12–T13 | R15 trait availability outside combat, and the paste-ready View/planner message | Report and message delivered. |

Order inside P1 goes from the most isolated change to the broadest. T3 and T4 both edit `BattlePreparationCalculator.cs`; do them in that order.

## 2. Phase P1

### T1. R12 — restore `MovementFailureReason` values

**Files:** `C/Contracts/Results/MovementFailureReason.cs`; test in `T/PlayerTurnMovementTests.cs`.

- [ ] Give explicit values: `UnsupportedMoveMethod = 7`, `SameAnchor = 8`, `OutOfBounds = 9`, `DestinationOccupied = 10`, `MoveRangeExceeded = 11`, `ActionCostInsufficient = 12`, `StateInconsistent = 13`, `StageEnded = 14`, `PathOriginMismatch = 15`, `InvalidPath = 16`, `RepeatedCell = 17`, `StaleContext = 19`, `VoluntaryMoveDisabled = 20`. Keep `None`–`Dead` (0–5) and `WrongController = 30`… as they are.
- [ ] Add a Korean comment that 6 (`NotActiveUnit`) and 18 (`NoPendingMovement`) are retired and not reused, matching `AttackFailureReason`.
- [ ] Add `MovementFailureReason_ValuesAreStable` pinning every value above.

### T2. R4 — validate Pull targeting at profile construction

**Files:** `C/Contracts/Combat/CombatAttackProfile.cs`; a Contracts-level home for the Pull targeting condition (a public static method in an existing or new Contracts file, + `.meta` if new); `C/Domain/Traits/Rules/PullTraitRule.cs`; tests in `T/PullRangeTests.cs`.

Layering fact: `ProjectL.Core.Contracts` references no other assembly, and `CombatAttackProfile` lives there, so it cannot call `PullTraitRule` (Domain). The single definition of the condition must therefore live in Contracts.

- [ ] Move the method condition of `PullTraitRule.ValidateMethod` (`CardinalLine` / `Single` / no splash / `Unrestricted` angle) into one public static Contracts method. `PullTraitRule.ValidateMethod` calls it, so there is still one definition.
- [ ] In the `CombatAttackProfile` constructor, when the definition has `TraitKey.Pull`: reject an unsupported method through that method, and reject `definition.AttackRange < 1`, with `ArgumentException` like the other combination checks.
- [ ] Keep the runtime call in `PullAttackCalculator` (line 37) as an invariant check.
- [ ] Change `PullRangeTests.UnsupportedTargetingConfiguration_IsAnError` to expect `ArgumentException` when the profile is built (`PullTestData.Input(method: ...)`), keeping its three cases.
- [ ] Add `PullProfile_RejectsZeroAttackRange`.
- [ ] Confirm `BattleDefinitionAdapter` (`CS/CombatSystem/BattleDefinitionAdapter.cs:140`) and `OwnedUnitSpecResolver` (`C/Application/Enhancements/OwnedUnitSpecResolver.cs:58`) need no change: they already construct profiles and now fail at battle entry.

### T3. R1 — narrow the boss preparation guard

**Files:** `C/Application/Combat/BattlePreparationCalculator.cs:120-122`; tests in `T/BattlePreparationRuleTests.cs` and a CoreIntegration test file.

- [ ] Replace the Necromancy-specific guard with a "no available behavior" guard for initial enemies: throw only when the authored unit has `!Behavior.CanBasicAttack`, `!Behavior.CanMoveVoluntarily`, and no Roar trait for which `DeferredCombatFeatures.IsTraitAvailable(TraitKey.Roar)` is true. Keep it an `InvalidOperationException` with a Korean message naming the unit key.
- [ ] Core tests: an enemy with Roar + Necromancy and neither attack nor movement prepares; an enemy with neither attack, movement nor Roar is rejected; an ordinary attacking enemy is unaffected.
- [ ] CoreIntegration test: the real `Stage_Boss` (stage key 2101, `Combat_Boss`) read through the production battle-definition source used by `TI/CoreIntegrationTestData.cs` (`DatabaseBattleInitializationSource`) prepares without an exception and places the boss on the field.
- [ ] Do not add Necromancy behavior. Existing `T/BossTurnTests.cs` cases for pending Roar keep passing.

### T4. R5 — place deferred flags inactive

**Files:** `C/Application/Combat/DeferredCombatFeatures.cs`; `C/Application/Combat/BattlePreparationCalculator.cs:167`; tests in `T/DeferredCombatFeatureTests.cs` (or `T/BattleEnvironmentPreparationTests.cs`).

- [ ] Add `DeferredCombatFeatures.IsFlagPickupAvailable` (false) with a Korean comment; it is the only place that states the flag deferral.
- [ ] At `var environment = initialization.Environment;`, when flag pickup is unavailable, replace each `FlagSourceFact` with `WithActive(false)` and keep every other tile, ID, entry ordinal and order unchanged.
- [ ] Leave `FlagMovementTileRule` as the no-op it is; add a one-line Korean comment pointing to `DeferredCombatFeatures.IsFlagPickupAvailable`.
- [ ] Tests: preparation with a flag placement yields an inactive `FlagSourceFact` with the authored ID; source IDs and `EnvironmentProgress` of other tiles are unchanged; a move through the flag cell produces no source or held-environment change. The existing `FlagPickupDoesNotChangeHeldOrSourceState` stays.
- [ ] No View change (the View skips inactive sources).

### T5. R2 — filter enhancement offers with deferred traits

**Files:** `C/Application/Shop/EnhancementOfferPlanner.cs:25-27`; tests in `T/EnhancementShopRuleTests.cs`.

- [ ] Exclude a definition from the pool when any `TraitKey` in `AddedTraits` or `ReplacedTraits` fails `DeferredCombatFeatures.IsTraitAvailable`. Filter before the weighted draw so weights, order and offer limit of the remaining pool are unchanged.
- [ ] Tests: a definition containing ThornField is never offered; an available definition is still offered with the same random sequence as before when no deferred definition is present.

### T6. R3 — delete the Precision Shot enhancement (Q-R3b A: keep trait code)

**Files:** delete `A/Enhancement/Item/Enhancement_PrecisionShot.asset` and `.meta`; edit `A/Enhancement/EnhancementDataTable.asset` (remove only the line `- {fileID: 11400000, guid: 6c79b5755c380a64687079e4ffb99fbb, type: 2}`); `C/Contracts/Enhancements/EnhancementKey.cs`; `C/Domain/Traits/Effects/PrecisionShotEffectRule.cs` (comment only); `TI/ImpactBoltAuthoringTests.cs`.

- [ ] Before deleting, confirm again that the GUID `6c79b5755c380a64687079e4ffb99fbb` is referenced only by `EnhancementDataTable.asset`.
- [ ] Keep `EnhancementKey.PrecisionShot = 8` with a Korean comment: no authored enhancement uses it; do not reuse the value.
- [ ] Keep all Precision Shot trait code. Add a Korean comment at `PrecisionShotEffectRule` that the first-recipient index follows delivery order, so under direct splash delivery it is EntityId order (design R3), to be settled in the enhancement rework.
- [ ] Delete the test case `RealPrecisionShotRecipe_PreservesImpactBoltInOwnedAndCombatProfiles` (both `TestCase`s). Keep the other cases in the file.
- [ ] Search assets for the GUID afterwards; expect no match.

### T7. R6 — category movement tooltips

**Files:** `D/Category/Item/CategoryConfigDataSO.cs` (`MovementInput` struct fields only).

- [ ] Add Korean `[Tooltip]`s: on `movementAllowance`, that 0 disables voluntary movement for every unit of that category; on `moveCost`, that a value above `PlayerTurnRules.MaximumActionCost` (10) can never be paid. Do not change validation, defaults or serialized names.

### T8. R7 — one read path for attack cost

**Files:** `C/Application/Combat/FollowUpAttackExecutor.cs:131`; test in a follow-up attack fixture (for example `T/EnhancementLuckyBulletTests.cs` or the existing follow-up cost test).

- [ ] Replace `actor.Stats.AttackCost` with `CombatActionAdmission.AttackCost(actor)`. Attack prices stay in `CategoryActionRules`; `Stats.AttackCost` remains a published copy for display only.
- [ ] Test: a follow-up attack evaluates its cost with the same per-category price as ordinary admission, even when the stored `Stats.AttackCost` in the fixture differs.

### T9. R8 — enemy falls back to pursuit after a rejected attack

**Files:** `C/Application/Combat/EnemyAi/EnemyAiTurnService.cs:33-44`; test in `T/StageEnemyAiTurnTests.cs`.

- [ ] Restructure per unit: if an aim is selected, calculate the attack and return it when accepted; if no aim was selected or the attack was rejected, try `TrySelectPursuitTarget` + `EnemyMovementPathfinder.TryFind` and return the movement when accepted. Do not try further aims. Keep stable EntityId order and one action per unit.
- [ ] Limitation found while planning: the pursuit pathfinder also decides from enemy-visible facts. `EnemyMovementPathfinder.TryFind` stops at the first cell from which it believes it can attack the pursuit target and returns false when that cell is the origin (`EnemyMovementPathfinder.cs:39-41`, `:60`). So when the rejected attack was aimed at the same nearest target, the fallback finds no movement and the unit still does not act. The fallback produces movement only when the rejected aim targeted a different unit than the nearest hostile (aim ranking prefers damage and target kind, not distance). This matches rule 7 ("otherwise attempts movement") without letting the AI use hidden facts; do not make the pathfinder read hidden facts to change it.
- [ ] Tests: (a) the selected aim targets a non-nearest enemy and is rejected by a fact not known to the enemy (for example a player-only sandstorm on the enemy's cell with that target at exactly the base range); the enemy moves toward the nearest hostile instead, and player AP is unchanged. (b) The same hidden fact with the nearest target: the attack is rejected, pursuit finds no movement, the transition is the normal phase completion, and no state changes for that unit.
- [ ] Existing `Facts_ZeroDamageLegalTargetStillAttacks` and pursuit tests keep passing.

### T10. Cleanup C1–C6

**Files and steps**

- [ ] C1: delete `C/Application/Combat/ThornFieldLifetimeCalculator.cs`, `C/Application/Combat/CombatStatusLifecycle.cs`, `C/Application/Combat/InitialTornadoPlacementCalculator.cs`, `C/Application/Artifact/StageSoulService.cs` and their `.meta`. Confirm no references remain first.
- [ ] C2: delete `DeferredCombatFeatures.HasPriorityComponent`.
- [ ] C3: delete `C/Application/Combat/EnemyAi/BossTurnCalculator.cs` and `C/Application/Combat/RegenerationTurnCalculator.cs` with `.meta`; delete test cases `T/BossTurnTests.cs` `NecromancyOwner_RejectsWithoutCreationOrControllerProgress` and `T/IntimidationStatePreservationTests.cs` `Regeneration_ChangesHealthWithoutErasingDebuff`. Keep the other cases.
- [ ] C4: delete `EnvironmentTerrainRules.EdgeActionCost`; in `C/Application/Combat/EnemyAi/EnemyMovementPathfinder.cs` replace both calls with a step cost of 1 (keep `MovementSearchLabel`); in `T/EnvironmentTerrainRuleTests.cs` delete `Terrain_DoesNotAddActionPrice` and remove only the `EdgeActionCost` assertions from `LocalRules_RejectNegativeInputsWithoutAddingRetiredSurcharge`.
- [ ] C5: remove the unused `countdown` parameter from `CombatCommandCalculator.Finish`, `MovementProjection.Complete` and `SoulEffectCalculator.PlanDeaths`, and update the callers (`CommandDeathSettlement.cs:61`, `T/ArtifactSoulBufferCalculationTests.cs:24`). Remove the unused `fields` parameter from `CombatTurnCalculator.State`. Change the `firstActionDelay` tooltip in `D/Unit/Trait/UnitTraitAuthoring.cs:93-94` to say it is unused while Necromancy is deferred; keep the serialized field.
- [ ] C6: update the `NecromancyTraitSpec` summary comment (`C/Contracts/Commands/Traits/NecromancyTraitSpec.cs:7`) so it no longer refers to `BossTurnCalculator` or "자기 턴".

### P1 verification (user runs)

| Assembly | Targets |
|---|---|
| `ProjectL.Tests.Core` | Whole assembly. Focus: `PlayerTurnMovementTests`, `PullRangeTests` and other `Pull*Tests`, `BattlePreparationRuleTests`, `BossTurnTests`, `DeferredCombatFeatureTests`, `BattleEnvironmentPreparationTests`, `EnhancementShopRuleTests`, `EnhancementPrecisionShotTests`, `EnhancementActionCostTests`, the T8 follow-up fixture, `StageEnemyAiTurnTests`, `EnemyMovementPathTests`, `EnvironmentTerrainRuleTests`, `IntimidationStatePreservationTests`, `ArtifactSoulBufferCalculationTests` |
| `ProjectL.Tests.CoreIntegration` | Whole assembly. Focus: `ImpactBoltAuthoringTests`, the new boss-stage preparation test, `CombatAdvanceServiceTests`, `MovementPublicationTests` |

Source checks before reporting: no reference to deleted types/members; GUID `6c79b575…` absent from assets; each deleted `.cs` has no orphan `.meta` and each new file has one; `git diff --check` clean.

## 3. Phase P2

### T11. Pointer and rule comments

**Files:** `C/Application/Combat/DeferredCombatFeatures.cs`, `C/Application/Combat/KillChainAttackCalculator.cs`, `C/Domain/Traits/Rules/HuntersMarkTraitRule.cs`, `C/Application/Combat/CombatDebuffApplication.cs`.

- [ ] `DeferredCombatFeatures`: Korean comments for R1 (Necromancy cadence is decided with the planner before summons return) and R14 (ThornField reactivation needs field lifetime and defense-reduction expiry in turn units and night-end clearing), each pointing to design section 5.1.
- [ ] `KillChainAttackCalculator` (kill count): R9 comment.
- [ ] `HuntersMarkTraitRule.CanApplyMark`: R10 comment.
- [ ] `CombatDebuffApplication.CompleteAttack`: R11 comment that any accepted ordinary attack, including a damage-free Pull, completes the Intimidation expiry.
- [ ] No behavior change. R13 needs nothing.

**Verification:** `ProjectL.Tests.Core` whole assembly (compile check through Test Runner).

## 4. Phase P3

### T12. R15 — trait availability outside combat

**Files:** `C/Application/Combat/DeferredCombatFeatures.cs`; `C/Application/Combat/Traits/TraitEffectProvider.cs:148-150`; `C/Contracts/Snapshots/OwnedCharacterStatsSnapshot.cs`; a new contract file under `C/Contracts/Snapshots/Traits/` (+ `.meta`) if a value type is needed; `C/Application/Roster/OwnedCharacterStatsCalculator.cs`; tests in `T/OwnedCharacterStatsCalculatorTests.cs`.

- [ ] Move the partial-availability reason (currently `TraitKey.Intimidation ? "DeferredPriorityComponent" : ""` inside `TraitEffectProvider`) into `DeferredCombatFeatures` (for example `GetTraitPartialUnavailableReason`), and make `TraitEffectProvider` call it. Behavior unchanged.
- [ ] Add per-trait availability to `OwnedCharacterStatsSnapshot`, one entry per key in `Stats.TraitKeys` in the same order, carrying the key, unavailable reason and partial reason from `DeferredCombatFeatures`. The change is additive: existing constructor callers keep compiling through a default, and existing description snapshots are unchanged.
- [ ] Fill it in `OwnedCharacterStatsCalculator`.
- [ ] Items: no new read model (design R15).
- [ ] Test: for each `TraitKey` a unit can author, out-of-combat availability equals the in-combat `TraitStatusSnapshot` availability; Sniper's Focus and CactusCannon's ThornField report unavailable; Intimidation reports the partial reason.

**Verification:** `ProjectL.Tests.Core` whole assembly (focus `OwnedCharacterStatsCalculatorTests`, trait status/provider tests); `ProjectL.Tests.CoreIntegration` whole assembly.

### T13. Messages for other owners (chat text, no handoff file)

- [ ] View owner, paste-ready:
  - New `OwnedCharacterStatsSnapshot` trait availability field and how to show it on lobby, deck and shop cards.
  - Rope equip rejection reason `DeferredUnitTurnTiming` (no item availability field).
  - Existing gaps: Impact Bolt has no `TraitVisual` entry; `UnitCardUI.cs:265` does not pass `ImpactBoltDescription`.
  - Deferred flags are now inactive at battle start (no View change needed).
- [ ] Planner, paste-ready:
  - Stage 1 boss fights without summons (up to two Gravekeepers removed), so the boss stage is easier.
  - Enemy movement follows category allowances (Demon 99 → 3, Gravekeeper 5 → 4, UndeadHeavy 2 → 3, UndeadCannonCart 3 → 2; old values were also limited by per-unit AP).
  - The Precision Shot enhancement was removed; PoisonThorn is no longer offered while ThornField is deferred.
  - Open questions recorded in design section 5.1.

## 5. Execution record

Append per Phase: date and HEAD; changed files; Test Runner targets and the user's results (or "not run"); deviations from this plan; next allowed Phase.

### 5.1 P1 — 2026-10-26, HEAD `e413008c` (uncommitted)

Status: code done; compile and Test Runner **not run**. Nothing committed.

**Changed (Core/DB)**
- T1 `Contracts/Results/MovementFailureReason.cs`: explicit values 7–17, 19, 20; 6 and 18 retired.
- T2 `Contracts/Combat/CombatAttackProfile.cs`: new `public static IsSupportedPullMethod` (single definition); the constructor rejects an unsupported Pull method and `AttackRange < 1` with `ArgumentException`. `Domain/Traits/Rules/PullTraitRule.ValidateMethod` calls it (runtime invariant kept).
- T3 `Application/Combat/BattlePreparationCalculator.cs`: `HasAvailableBehavior` (basic attack, voluntary movement or available Roar) replaces the Necromancy guard; message names the unit key.
- T4 `DeferredCombatFeatures.IsFlagPickupAvailable` (false); preparation `DeactivateDeferredFlags` sets each `FlagSourceFact` inactive and keeps every other tile, ID, ordinal and order; pointer comment in `FlagMovementTileRule`.
- T5 `Application/Shop/EnhancementOfferPlanner.cs`: pool excludes definitions whose added or replaced traits are unavailable.
- T6 deleted `A/Enhancement/Item/Enhancement_PrecisionShot.asset` + `.meta`; removed its one line from `EnhancementDataTable.asset` (GUID now absent from `Assets`); `EnhancementKey.PrecisionShot = 8` do-not-reuse comment; known-limitation comment in `PrecisionShotEffectRule`.
- T7 `CategoryConfigDataSO.MovementInput` tooltips.
- T8 `FollowUpAttackExecutor`: base cost from `CombatActionAdmission.AttackCost(actor)`. No Core code reads `Stats.AttackCost` any more.
- T9 `EnemyAiTurnService`: a rejected attack falls through to the pursuit attempt.
- T10 deleted (+ `.meta`) `ThornFieldLifetimeCalculator`, `CombatStatusLifecycle`, `InitialTornadoPlacementCalculator`, `StageSoulService`, `BossTurnCalculator`, `RegenerationTurnCalculator`; removed `HasPriorityComponent`, `EdgeActionCost` (pathfinder uses step cost 1), `countdown` (`Finish`, `MovementProjection.Complete`, `PlanDeaths`) and `fields` (`CombatTurnCalculator.State`); `firstActionDelay` tooltip; `NecromancyTraitSpec` summary.

**Tests**
- Added: `PlayerTurnMovementTests.MovementFailureReason_ValuesAreStable`; `PullRangeTests.PullProfile_RejectsZeroAttackRange`; `BattlePreparationRuleTests.InitialEnemy_RequiresAnAvailableBehavior` (3 cases); `BattleEnvironmentPreparationTests.Preparation_DeferredFlagIsPlacedInactiveWithoutShiftingOtherSources`; `DeferredCombatFeatureTests.FlagPickup_IsDeferred`, `MoveThroughInactiveFlag_ChangesNoSourceOrHeldEnvironment`; `EnhancementShopRuleTests.Offers_ExcludeDeferredTraitsWithoutChangingAvailableSampling`; `EnhancementDoubleShotTests.FollowUpAttackCost_UsesCategoryPriceNotStoredStatsCopy`; `StageEnemyAiTurnTests.RejectedAttackOnFartherTarget_FallsBackToPursuitOfNearestHostile`, `RejectedAttackOnNearestTarget_FindsNoPursuitAndEndsPhaseWithoutChangingUnit`; new CoreIntegration file `BossStagePreparationTests.cs` (+ `.meta`, `ProductionBossStage_PreparesAndPlacesBoss`).
- Changed: `PullRangeTests.UnsupportedTargetingConfiguration_IsAnError` expects `ArgumentException` at profile construction (four existing cases kept; the plan said three). `ArtifactSoulBufferCalculationTests` call without `countdown`.
- Deleted: `ImpactBoltAuthoringTests.RealPrecisionShotRecipe_…` (2 cases), `BossTurnTests.NecromancyOwner_…`, `IntimidationStatePreservationTests.Regeneration_…`, `EnvironmentTerrainRuleTests.Terrain_DoesNotAddActionPrice`, and the `EdgeActionCost` assertions (with their now-unused local) in `LocalRules_RejectNegativeInputsWithoutAddingRetiredSurcharge`.

**Deviations**
- T3 boss-stage CoreIntegration test runs the real production DB through `FrameworkRuntime` (`InitializeRoster(1)`, deck) and reads stage 2101 with `DatabaseBattleInitializationSource`, then calls `BattlePreparationCalculator.Prepare` with the same Buffer facts as `BattleEntryService`. It does not go through `EnterChapter`/`EnterBattle`, which need a registered chapter graph.
- T8 test is in `EnhancementDoubleShotTests`, not the Lucky Bullet fixture the plan suggested: only the same-direction follow-up (Double Shot) reaches the changed line; Kill Chain and Lucky Bullet follow-ups take the target path. It sets the stored `Stats.AttackCost` to 0, which the old path rejected (`AttackCostEffectContext` requires ≥ 1).
- T9 case (b) also exists as `EnemyMovementPathTests.HiddenSandstormRejectingPlannedAttack_EndsTurnWithoutAttacking` (unchanged).

**First Test Runner run (user, 2026-10-27) — fixtures adjusted.**
- `PullAndMarkPresentationTests.OwnedProjection_ExposesPullConfiguration` (3): the synthetic DB gave Cowboy Pull with shared method 1 (RadialSquare), which T2 now rejects at battle entry. The test now adds method 2 (CardinalLine, Single, no splash — same shape as production `TargetingMethod_Shielder`) and points Cowboy to it. No production asset changed.
- `CombatRewardPublicationTests` (7): fixture enemies had no basic attack, no movement and no Roar, which T3 rejects. Per the user, the fixture enemies now allow basic attack (attack power 0, no movement). Results after this change: both fixtures pass; the user reports all P1 targets pass (2026-10-27). Next allowed Phase: P2.

**Framework test assembly compile break (resolved by user decision, 2026-10-26).** `Assets/Tests/EditMode/Framework/ArtifactDataTableTests.cs:91,107,130` called `PlanDeaths` with the removed fourth argument; a compile error in any assembly stops Unity from reloading scripts. Per the user, only the fourth argument was removed at those three call sites (compile fix only). The test's `countdownOnDeath` parameter is now unused; its expectations were not migrated and the Framework assembly is still not run.

### 5.2 P2 — 2026-10-27, HEAD `e413008c` + uncommitted P1

Status: comments only, no behavior change; Test Runner **not run**. Nothing committed.

**Changed (comments only)**
- `Application/Combat/DeferredCombatFeatures.cs`: class-level pointer for R1 (Necromancy cadence decided with the planner before summons return) and R14 (ThornField lifetime and defense-reduction expiry in turn units, night-end clearing), referencing design §5.1.
- `Application/Combat/KillChainAttackCalculator.cs` (kill count): R9 pointer.
- `Domain/Traits/Rules/HuntersMarkTraitRule.cs` (`CanApplyMark`): R10 pointer.
- `Application/Combat/CombatDebuffApplication.cs` (`CompleteAttack` summary): R11 rule — any accepted ordinary attack, including a damage-free Pull, completes the Intimidation expiry.

R13 needs nothing. No deviations.

**Verification (user):** `ProjectL.Tests.Core` whole assembly (compile check). Next allowed Phase after review: P3.

### 5.3 P3 — 2026-10-27, HEAD `e413008c` + uncommitted P1·P2

Status: code done; Test Runner **not run**. Nothing committed. Messages for other owners delivered as chat text (no handoff file).

**Changed**
- New `Contracts/Snapshots/Traits/TraitAvailabilitySnapshot.cs` (+ `.meta`): key, unavailable reason, partial reason, `IsAvailable`.
- `Application/Combat/DeferredCombatFeatures.cs`: `GetTraitPartialUnavailableReason` (the Intimidation `DeferredPriorityComponent` rule moved here from `TraitEffectProvider`) and `GetTraitAvailability`.
- `Application/Combat/Traits/TraitEffectProvider.cs`: calls `GetTraitPartialUnavailableReason`; in-combat output unchanged.
- `Contracts/Snapshots/OwnedCharacterStatsSnapshot.cs`: new `TraitAvailability` and trailing optional constructor parameter `traitAvailability`. When supplied it must have one entry per `Stats.TraitKeys` in the same order; when omitted it is empty (existing callers, including View/Framework test fakes, keep compiling and existing description snapshots are unchanged).
- `Application/Roster/OwnedCharacterStatsCalculator.cs`: always fills it from `DeferredCombatFeatures.GetTraitAvailability`.

**Tests (`OwnedCharacterStatsCalculatorTests`)**: `OwnedTraitAvailability_MatchesBattleTraitStatus` (9 trait keys, each compared with the prepared battle's `TraitStatusSnapshot`; Focus/ThornField/MoveAttackDiscount/Regeneration unavailable, Intimidation partial), `OwnedTraitAvailability_FollowsTraitKeyOrderAndConstructorChecksIt`.

**Decision made in P3 (not in the design):** the default for an omitted `traitAvailability` is an empty list ("not supplied"), not a list of available entries, so a fake or older producer never claims a deferred trait is available.

**Verification (user):** `ProjectL.Tests.Core` whole assembly (focus `OwnedCharacterStatsCalculatorTests`, trait status/provider tests); `ProjectL.Tests.CoreIntegration` whole assembly.
