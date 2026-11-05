# Core Review Remediation

Date: 2026-10-26
Status: All decisions recorded on 2026-10-26 (section 9). Q-R1–Q-R10 were decided item by item by the user; Q-R3b, Q-R11, Q-R13 and Q-R15 follow the recommendation, as the user delegated. Ready for a Task plan. No code or asset has been changed for this document.
Source baseline: `feature/Cheon/Core/NewCombatSytem`, HEAD `e413008c`, clean working tree.
Review range: commits `42d3263a`..`e413008c` (player turns and shared ActionCost, category movement SO, trait rework, Ballista Impact Bolt).

## 1. Intent and scope

This document turns the 2026-10-26 Core code review into concrete remediation items. Each item states the verified source facts, the problem, and either a fix (no gameplay decision) or options with a recommendation (decision required).

In scope:

- Core calculation (`C`), Core-facing system adapters (`CS`), InGame DB data and authoring (`D`) and their assets (`A`).
- Tests in `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` only.

Out of scope:

- The Framework test assembly (`Assets/Tests/EditMode/Framework`). It is treated as abandoned on the Core side; its outdated expectations are not findings and are not migrated.
- View implementation. Required View changes are listed in section 7 as handoff items only.
- Redesigning deferred content (planner-owned per decision D7 of the player-turn design). This document does not reactivate any deferred trait, item or gimmick.
- Trait enhancements as a feature. They will be reworked at large scale (user, 2026-10-26), so this document does not fix enhancement semantics. It only removes the Precision Shot enhancement (R3) and decides how inert enhancement offers are handled until the rework (R2).

Item kinds:

- **Decided**: needed a user decision. The options considered and the recorded decision with its date are given.
- **Fix**: a contract violation or defect with one reasonable correction.
- **Record**: no code change now; the fact is recorded as a precondition for later work.
- **Cleanup**: dead code or stale comments.

## 2. Path aliases

| Alias | Path |
|---|---|
| `C` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator` |
| `CS` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem` |
| `D` | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table` |
| `A` | `Assets/ProjectL/DataBaseSO/InGameGroup` |
| `L` | `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem` |
| `T` | `Assets/Tests/EditMode` (Core tests) |
| `TI` | `Assets/Tests/EditMode/CoreIntegration` |

## 3. Summary

| ID | Title | Kind | Severity | Recommendation |
|---|---|---|---|---|
| R1 | Boss stage preparation throws and stops the game | Decided | High | A: narrow the guard; boss fights with Roar, without summons |
| R2 | Enhancements for deferred traits are still offered | Decided | Medium | A: filter offers in `EnhancementOfferPlanner` |
| R3 | Precision Shot bypass goes to the lowest EntityId under Impact Bolt | Decided | Medium | Delete the Ballista-only Precision Shot enhancement; keep the trait code (Q-R3b) |
| R4 | Pull targeting is validated only at attack/query time | Fix | Medium | Validate in `CombatAttackProfile` construction |
| R5 | Placed flags stay active but do nothing | Decided | Medium | A: create flag sources inactive while pickup is deferred |
| R6 | Category movement values accept 0 and unpayable costs | Decided | Low | B: keep the approved 2026-10-25 rule; add SO tooltips |
| R7 | Attack cost has two read paths; category action values split SO/code | Decided | Low | B: keep the 2026-10-25 split, unify the read path |
| R8 | Enemy AI does not fall back to movement after a rejected attack | Decided | Low | B: fall back to pursuit movement |
| R9 | Kill Chain counts deaths caused during Pull | Decided | Low (unreachable now) | C: record for the enhancement/trait rework |
| R10 | Reaction-derived follow-ups can consume Hunter's Marks | Decided | Low (unreachable now) | C: record for the rework |
| R11 | A damage-free Pull clears the attacker's Intimidation | Decided | Low | B: keep and document |
| R12 | `MovementFailureReason` values were renumbered | Fix | Low | Restore explicit historical values |
| R13 | Very large Hunter's Mark percent throws mid-combat | Decided | Low | B: keep approved checked arithmetic |
| R14 | Night end no longer clears thorn fields; defense reduction has no expiry | Record | Low (latent) | Record as ThornField reactivation precondition |
| R15 | No availability information outside combat | Decided | Low | A: trait availability in `OwnedCharacterStatsSnapshot`; items use the existing rejection reason |
| C1–C6 | Dead code, dead parameters, stale comments | Cleanup | Low | Remove or correct |

## 4. Decision and fix items

### R1. Boss stage preparation throws and stops the game

**Facts**

- `C/Application/Combat/BattlePreparationCalculator.cs:120-122` throws `InvalidOperationException("DeferredUnitTurnTiming: ...")` for any initial enemy with `CanBasicAttack == false` and a Necromancy trait.
- `A/Unit/Item/Enemy/Unit_Stage1Boss.asset` has `canBasicAttack: 0`, `canMoveVoluntarily: 0`, and traits Necromancy, Roar and Knockback. It is placed by `A/Stage/Combat/Combat_Boss.asset`, referenced by `A/Stage/Root/Stage_Boss.asset`.
- The last node of a chapter map must be a Boss combat node (`CS/ChapterProgressionCoordinator.cs:204`, `:247`), so a normal run always reaches this stage.
- The exception is not converted into a rejection. `CS/CombatSystem/BattleEntryService.cs:103` calls `Prepare` without a catch; `L/LifeCycles/Combat/Jobs/CombatPrepareJob.cs:56-60` calls `_entry.Fail()` and rethrows, so the runtime fault boundary stops the game.
- Why the guard exists: the old boss behavior ran in `C/Application/Combat/EnemyAi/BossTurnCalculator.cs` on the boss's own unit turn (pending Roar, otherwise summon, then end turn). Without unit turns, the summon timing has no defined meaning, so Necromancy was deferred (`C/Application/Combat/Summoning/CombatSummonCalculator.cs` always rejects; `C/Application/Combat/DeferredCombatFeatures.cs:13-14`). Plan B5 then required an encounter whose only boss behavior is disabled to be "unavailable", which was implemented as this guard.
- Roar does not depend on unit turns. `C/Application/Combat/BossRoarCalculator.cs:28-36` selects a boss when it is night, combat is active and its hit counter reaches the threshold; `ApplyPendingReactions` runs after every command and automatic advance (`C/Application/Combat/CombatCommandCalculator.cs`, `Calculate` and `Advance`).
- Source trace (not executed): without the guard, the boss is skipped in the enemy phase (attack admission returns `BasicAttackDisabled` from `CombatAttackQueries.UnitReady`; movement admission in `CombatMovementCalculator.Actor` rejects it because voluntary movement is disabled) and still roars as a reaction.

**Problem.** The guard rejects more than intended: the boss is not inert because Roar is active. The rejection is also a fault rather than a rejection, and it blocks every run at its final node.

**Options**

| Option | Change | Effect |
|---|---|---|
| A | Narrow the guard to enemies with no available behavior at all: no basic attack, no voluntary movement and no available reaction trait (Roar). Keep it a configuration error for truly inert enemies. | Stage 1 boss is playable with Roar; summons stay deferred and are shown as unavailable. Smallest change that keeps the B5 intent. |
| B | Remove the guard. | Same result for the current boss, but a future fully inert enemy would silently enter combat. |
| C | Define Necromancy for the shared enemy phase (for example, the boss's ordinary enemy-phase action is a summon attempt when below its cap). | Restores summons. Requires a gameplay rule from the planner (D7) and new tests. |
| D | Keep rejecting, but return a normal `ChapterProgressionResult` rejection. | The game no longer stops, but a run still cannot be completed. |

**Decision (user, 2026-10-26): A.** Necromancy timing in the new turn structure (option C) is left to a later decision with the planner and is not part of this remediation.

Details of A:

- "Available behavior" means `CanBasicAttack`, `CanMoveVoluntarily`, or a Roar trait that `DeferredCombatFeatures.IsTraitAvailable` reports as available. Roar is the only reaction trait counted for now.
- An enemy with none of these remains a configuration error at battle preparation. The Stage 1 boss passes because Roar is available.
- Necromancy stays deferred: no summons, and its status is published as unavailable. Its authored values stay as they are; `firstActionDelay` becomes dead data (C5).
- Content effect to report to the planner: the `Combat_Boss` fight loses up to two summoned Gravekeepers (`maxLivingSummons: 2`), so it becomes easier. The boss itself only roars (threshold 5 night hits, 3-cell knockback); the other five enemies act normally, and defeating the boss remains the victory condition.

**Verification:** Core test that `BattlePreparationCalculator.Prepare` accepts a non-attacking, non-moving enemy with Roar and Necromancy, and still rejects one with neither attack, movement nor Roar. CoreIntegration test that an enemy phase with such a boss completes (`EnemyPhaseEnded`) and that a pending Roar still fires.

### R2. Enhancements for deferred traits are still offered

**Facts**

- `C/Application/Shop/EnhancementOfferPlanner.cs:25-27` builds the offer pool from every enhancement definition for an owned unit without an enhancement. There is no availability filter.
- `A/Enhancement/Item/Enhancement_PoisonThorn.asset` (cost 700) has only a `ThornFieldTraitAuthoring` in `replacedTraits`. ThornField is deferred (`DeferredCombatFeatures.cs:13`); its bindings are skipped in `C/Application/Combat/CombatCommandEffects.cs:38`, and `ThornFieldApplicationCalculator.Apply` is a no-op.
- Buying it consumes gold and the unit's single enhancement slot, with no combat effect.
- Items already follow the opposite rule: `CS/ShopSystem/ShopDefinitionAdapter.cs:37` offers only items with `ItemRuleRegistry.IsAvailable`.

**Options**

| Option | Change |
|---|---|
| A | In `EnhancementOfferPlanner`, exclude a definition when any `TraitKey` in `AddedTraits` or `ReplacedTraits` is unavailable according to `DeferredCombatFeatures.IsTraitAvailable`. |
| B | Offer it but mark it unavailable and non-purchasable. Requires a new offer field and View work. |
| C | Stop offering all enhancements until the enhancement rework. Also removes the working ones (ChainExplosion for DynamiteHammer, DoorBreaching and DoubleShot for ShotGun). |
| D | Delete `Enhancement_PoisonThorn` like Precision Shot (R3). |
| E | Keep it purchasable. |

**Decision (user, 2026-10-26): A.** Recommended because it is a few lines in one owner, matches the item rule, keeps `DeferredCombatFeatures` as the single trait availability source, and does not remove working enhancements before the rework decides their future. Choose C if the shop should not sell any enhancement until the rework; choose D if PoisonThorn is not expected to survive the rework. Purchase order and currency handling stay unchanged. Already-owned enhancements (if any) are not removed; their traits are shown as unavailable through R15.

**Verification:** Core test that the planner never offers an enhancement containing a deferred trait and still offers available ones with unchanged weights and order.

### R3. Precision Shot bypass under Impact Bolt goes to the lowest EntityId

**Facts**

- `A/Enhancement/Item/Enhancement_PrecisionShot.asset` targets Unit_Ballista and describes itself as "첫 피해 대상의 방어력을 무시합니다."
- `C/Domain/Traits/Effects/PrecisionShotEffectRule.cs:26` bypasses defense when `ActualRecipientIndex == 0`.
- Under piercing, delivery order was the order of encounter along the arrow. Impact Bolt uses direct delivery: `C/Application/Combat/CombatAttackDelivery.cs` (`ResolveDirect`) follows `selection.Targets`, which `C/Domain/Combat/AttackTargetResolver.cs:39` sorts by EntityId. `C/Application/Combat/AttackDirectDamageCalculator.cs:51` increments the index in that order.
- Result: a 70% splash neighbour, an obstacle, or any eligible target with a smaller EntityId than the impact centre receives the bypass.
- `T/EnhancementPrecisionShotTests.cs` (`ImpactBolt_PreservesFirstRecipientDefenseBypassAndAuthoredSplash`) uses centre id 2 and neighbour id 3, so it cannot detect the issue.

- The enhancement was designed for piercing. `e413008c` removed its piercing falloff replacement and shortened the description, leaving only the first-recipient bypass.

**Decision (user, 2026-10-26): delete the Ballista-only Precision Shot enhancement.** Trait enhancements will be reworked at large scale, so the first-recipient meaning under splash is not redesigned now. The options considered before the decision (Primary hit gets the bypass; reorder direct delivery; keep EntityId order) are superseded.

**Deletion scope**

- Delete `A/Enhancement/Item/Enhancement_PrecisionShot.asset` and its `.meta`, and remove its entry from `A/Enhancement/EnhancementDataTable.asset`. Preserve every other table entry and reference.
- Delete the CoreIntegration case `TI/ImpactBoltAuthoringTests.cs` `RealPrecisionShotRecipe_PreservesImpactBoltInOwnedAndCombatProfiles`, which reads the real table row. Its other cases use a synthetic `EnhancementDefinition` and stay.
- No View code or ViewGroup asset references Precision Shot (checked), so no View handoff is needed for the enhancement itself.
- The Precision Shot trait has no other authored user: `PrecisionShotTraitAuthoring` appears only in the deleted asset.

**Q-R3b. What happens to the Precision Shot trait code?**

| Option | Change | Effect |
|---|---|---|
| A | Keep the trait code (`TraitKey.PrecisionShot`, spec/definition/rule/effect/state/snapshot, authoring/data types, `TraitSpecAdapter` mapping, registry entry) and `EnhancementKey.PrecisionShot = 8` as a reserved value with a do-not-reuse comment. Mark the first-recipient-under-splash issue as a known limitation in the trait rule comment. | Smallest change; the enhancement rework decides whether the trait survives. Core tests `T/EnhancementPrecisionShotTests.cs` and the Precision Shot cases of `T/EnhancementActionCostTests.cs` use synthetic specs and keep passing. |
| B | Delete the trait code as well, retiring `TraitKey.PrecisionShot` and `EnhancementKey.PrecisionShot` without reuse, and delete the Core tests above. | Removes unused code now, but touches about ten Core/DB files and their tests ahead of a rework that may replace them anyway. |

**Decision (user delegated to the recommendation, 2026-10-26): A.** No serialized reference to `PrecisionShotTraitAuthoring` remains after the asset deletion, so B stays possible later without asset risk.

**Verification:** DB/catalog construction succeeds without the row (CoreIntegration database tests that read the real tables); no remaining `Enhancement_PrecisionShot` GUID reference in assets; Core enhancement tests unchanged.

### R4. Pull targeting is validated only at attack or query time (Fix)

**Facts**

- `C/Contracts/Combat/CombatAttackProfile.cs` documents that a profile accepted at construction raises no combination error during combat. Its constructor checks Impact Bolt, piercing and ThornField combinations, and Pull with piercing, but not the Pull targeting shape.
- `C/Domain/Traits/Rules/PullTraitRule.cs:12-18` (`ValidateMethod`: cardinal line, single choice, no splash, unrestricted angle) is called from `C/Application/Combat/PullAttackCalculator.cs:37` during every selection, range and unit-range query. `PullTraitRule.SelectionRange` (`:20-25`) throws when the attack range is below 1.
- Cowboy uses `A/Targeting/Item/TargetingMethod_Shielder.asset`, shared with Unit_Shielder. Editing that method for Shielder would make Cowboy's first hover or attack throw and stop the game.
- Profiles are constructed at battle entry (`CS/CombatSystem/BattleDefinitionAdapter.cs:140`) and for enhanced units (`C/Application/Enhancements/OwnedUnitSpecResolver.cs:58`).

**Fix.** When the definition has a Pull trait, the `CombatAttackProfile` constructor rejects an unsupported targeting method and `definition.AttackRange < 1`, throwing `ArgumentException` like the other combination checks. Because `ProjectL.Core.Contracts` references no other assembly, the method condition moves from `PullTraitRule.ValidateMethod` into one public static Contracts method that both the constructor and `PullTraitRule` call. The runtime call in `PullAttackCalculator` may remain as an invariant check. Sandstorm cannot reduce a positive range below 1 (`C/Domain/Movement/EnvironmentTerrainRules.cs:62-66`), so no runtime path is affected.

Optional content follow-up (not part of the fix): give Cowboy its own targeting method asset so Shielder edits cannot invalidate it.

**Verification:** Core tests that constructing a profile with Pull and each invalid method property, or with attack range 0, throws `ArgumentException`. Update `T/PullRangeTests.cs` (`UnsupportedTargetingConfiguration_IsAnError`) to expect the failure at profile construction.

### R5. Placed flags stay active but do nothing

**Facts**

- `C/Application/Combat/FlagMovementTileRule.cs:13-17` returns empty effects; the source is never deactivated.
- `C/Contracts/Combat/InitialObjectPlacementSpec.cs:26-29` creates every `FlagSourceFact` with `Active = true`.
- `A/Object/Item/Object_Flag.asset` is placed in `Combat_Boss`, `Combat_Normal_D`, `Combat_Normal_F`, `Combat_Normal_I`, `Combat_Normal_J` and `Combat_Normal_K`.
- The player-turn plan (B5) requires that disabled producers do not appear active.

**Options**

| Option | Change | Effect |
|---|---|---|
| A | While flag pickup is deferred, create placed flag sources with `Active = false`. | Source IDs and entry ordinals stay as authored, so environment allocation does not shift. |
| B | Do not create flag sources while deferred. | Fewer facts, but shifts source IDs and next-ID progression. |
| C | Keep active, non-functional flags. | Violates B5. |

**Decision (user, 2026-10-26): A.** The deferral decision should be expressed once (next to `DeferredCombatFeatures`), not only inside the tile rule. Placed sources are created in `CS/CombatSystem/StageEnvironmentAdapter.cs:68-74` through `InitialObjectPlacementSpec.CreateSource`; the plan chooses whether the inactive state is applied there or in battle preparation. The View already skips inactive object sources (`ObjectViewSet.cs:52` for drawing, `CombatEnvironmentText.cs:35` for hover text), so A needs no View change.

**Verification:** Core test that battle preparation produces inactive flag sources and that entering their cell produces no events.

### R6. Category movement values accept 0 and unpayable costs

**Facts**

- `D/Category/Item/CategoryConfigData.cs:72-77`, `C/Contracts/Commands/Categories/CategorySpec.cs:27-30` and `C/Domain/Entities/CategoryDefinition.cs:25-28` reject only negative values.
- `C/Application/Combat/CombatActionAdmission.cs:42-43` treats allowance 0 as "movement disabled", so an allowance of 0 for Rider silently disables movement for every Rider unit.
- A move cost of 0 is accepted. A move cost above `PlayerTurnRules.MaximumActionCost` (10) can never be paid.
- Boss voluntary movement is forbidden regardless of the config (`C/Contracts/Commands/UnitSpec.cs:134`), so the Boss movement row has no runtime effect.
- **This behavior is an approved earlier design, not an oversight.** [Category movement SO design §3](2026-10-25-category-movement-so-design.md) states: both values are nonnegative; allowance 0 means no voluntary movement; cost 0 is a valid free movement that still consumes the action opportunity; costs above the shared maximum are valid but unaffordable and must never be clamped. Core tests pin it: `T/CharacterCategoryContractTests.cs` (`CategorySpec_PreservesExplicitMovement` with `(0, 0)` and `(5, 12)`) and `T/CharacterCategoryBootstrapTests.cs` (`Registry_UnitDefinitionUsesSuppliedMovement`).
- The review finding therefore restated a deliberate rule as a defect. The only remaining concern is authoring safety: a designer setting a movable category's allowance to 0 silently disables movement for every unit in it.

**Options**

| Option | Change |
|---|---|
| A | Reverse the earlier decision: Main, Rider, Heavy and Cannon require allowance `>= 1` and cost `1..PlayerTurnRules.MaximumActionCost`; Boss allowance must be 0. Update `CategoryConfigData.Validate`, `CategorySpec`, `CategoryDefinition` and the tests above. |
| B | Keep the approved rule. Optionally make the SO Inspector tooltip state that allowance 0 disables voluntary movement for the whole category and that a cost above 10 is unaffordable. |

**Decision (user, 2026-10-26): B with the tooltip.** The rule was decided one day earlier with explicit semantics and tests, and the current asset values are correct. Only `D/Category/Item/CategoryConfigDataSO.cs` changes: the two fields of its `MovementInput` struct (shared by all five categories, matching the existing `[Tooltip]` style of the level-growth fields) get Korean Inspector tooltips stating that allowance 0 disables voluntary movement for every unit of that category, and that a move cost above `PlayerTurnRules.MaximumActionCost` (10) can never be paid. Validation, data types and assets are unchanged.

**Verification:** no runtime change; the existing tests stay as they are. Confirm in the Unity Inspector that the tooltips appear and the asset values are unchanged.

### R7. Category action rules live in two places

**Facts**

- Movement allowance and cost come from `A/Category/Item/CategoryConfig_Default.asset` through `CS/CharacterSystem/UnitSpecAdapter.cs:43-51`.
- Attack cost comes from code: `C/Domain/Categories/CategoryActionRules.cs:9-20` (Cannon 5, others 3), read by `CombatActionAdmission.AttackCost` and `C/Domain/Entities/UnitDefinition.cs:22`.
- Two read paths for attack cost exist: the main path recomputes it (`CombatActionAdmission.cs:46`), while `C/Application/Combat/FollowUpAttackExecutor.cs:131` reads the stored `Stats.AttackCost`. They agree today.
- Hard-coded copies of the movement table: `C/Application/BootstrapModels/CategoryRegistry.cs:83-96` (`CreateDefault`, used by the two-argument `UnitCatalog` constructor and tests) and `Assets/ProjectL/Script/Systems/Factory/Runtime/Development/TemporaryCoreDatabase.cs:55` (development DB). The View-owned `DevBattleInitializationSource` has a third copy.
- Enemies now use category allowances. Compared with the old per-unit `moveRange`: Demon 99 → 3 (Main), Gravekeeper 5 → 4 (Rider), UndeadHeavy 2 → 3 (Heavy), UndeadCannonCart 3 → 2 (Cannon). The old values were upper bounds also limited by per-unit AP and per-step cost, so 99 was not the old effective reach.

- **Earlier decisions.** [Category movement SO design §4](2026-10-25-category-movement-so-design.md) deliberately kept attack prices in `CategoryActionRules` ("keep its attack-price responsibility"), and decided that enemy movement uses the authored category allowance without spending the player price, with no per-unit authored overrides. So the split between SO (movement) and code (attack price) is a scoped decision, and the enemy allowance values above are its intended result. Each value still has exactly one source; the real SSOT defect is the two read paths for attack cost.

**Options (authoring source)**

| Option | Change |
|---|---|
| A | Add attack cost per category to `CategoryConfigData`/SO next to movement; carry it through `CategorySpec`/`CategoryDefinition`; delete `CategoryActionRules`; read attack cost through one path. Reopens the 2026-10-25 scope decision. |
| B | Keep attack cost as a code rule (as decided on 2026-10-25) and fix only the duplicate read path: admission, availability, preview and follow-up attacks all read the same value. |

**Enemy movement.** Already decided on 2026-10-25 (category allowance, no per-unit override); no new decision. The planner should only be told about the resulting values.

**Decision (user, 2026-10-26): authoring B.** Attack prices stay in `CategoryActionRules`; ordinary admission, availability, preview and follow-up attacks read them through one path, so `FollowUpAttackExecutor` no longer reads a separately stored copy. Option A remains available if designers later need to tune attack prices without code changes. `CreateDefault` and `TemporaryCoreDatabase` remain explicit fixture data and should be labelled as such rather than derived.

**Verification (B):** Core test that a follow-up attack's cost evaluation and ordinary attack admission use the same per-category price, including after a category change of the stored stats snapshot.

### R8. Enemy AI does not fall back to movement after a rejected attack

**Facts.** `C/Application/Combat/EnemyAi/EnemyAiTurnService.cs:33-44` uses `if (TrySelectAttackAim) ... else if (pursuit)`. Aim selection uses a public preview (enemy-visible facts); if execution in the authoritative world rejects the attack, the unit neither attacks nor moves this enemy phase and stays `Available` until the phase ends. This can only happen when hidden facts differ from what the enemy sees.

Confirmed rule 7 of the [player-turn design](2026-10-23-player-turn-shared-actioncost-design.md) says each eligible enemy "attacks if it has a legal target from its current position, otherwise attempts movement, at most once in its phase". The current code deviates from that rule in this case; it is not an earlier decision.

**Options**

| Option | Change |
|---|---|
| A | Try the next-best aim, then movement. |
| B | Fall back directly to pursuit movement. |
| C | Keep: the unit idles. |

**Limitation found while planning.** `EnemyMovementPathfinder.TryFind` also uses enemy-visible facts and returns no path when it believes the origin is already an attack position. So when the rejected aim targeted the nearest hostile (the pursuit target), the fallback still produces no movement. It produces movement only when the rejected aim targeted a different unit. Making the pathfinder read hidden facts would leak them to the AI and is not proposed.

**Decision (user, 2026-10-26): B.** It restores confirmed rule 7: the attack the enemy chose from what it can see was not legal in the authoritative world, so it attempts movement instead. It stays deterministic and bounded (at most one rejected attack and one movement calculation per unit) and does not let the AI probe hidden facts by trying aim after aim. A is the more literal reading of "has a legal target" but repeats authoritative attack calculations per aim; prefer it only if the planner wants enemies to search for another target after a hidden-fact failure.

**Verification:** Core test where a hidden fact makes the selected attack fail and the unit moves instead.

### R9. Kill Chain counts deaths caused during Pull

**Facts.** `C/Application/Combat/KillChainAttackCalculator.cs:51-52` counts hostile characters alive in `plan.Input` and dead in the projection. After a pull, `PullAttackCalculator.RebuildDelivery` keeps the pre-pull input, so a unit killed by a hazard during the pull (for example a mine blast hitting another enemy) counts as a kill and grants an extra Kill Chain attack. `FollowUpAttackExecutor.ResolveAttack` ignores those deaths because it reads only events after the attack event index.

**Reachability (checked).** The combination cannot occur with current content. Kill Chain (`KillChainAttackTraitAuthoring`) is authored only on `Unit_Akimbo` and `Unit_MainCharacter`; Pull only on `Unit_Cowboy`; no enhancement adds Kill Chain or Pull (the remaining enhancements are ChainExplosion for DynamiteHammer, DoorBreaching and DoubleShot for ShotGun, and PoisonThorn for CactusCannon). It would only appear through new content, most likely from the planned enhancement rework, which this document excludes.

**Options**

| Option | Change |
|---|---|
| A | Fix now: use the state after the pull resolves as the Kill Chain baseline. |
| B | Keep counting pull-hazard deaths as kills. |
| C | Record only: add the attribution question to the enhancement/trait rework preconditions (next to R10 and R14). |

**Decision (user, 2026-10-26): C.** Recorded in section 5.1. The case is unreachable, and the right attribution (does a hazard death caused by the pull count as "killed by this attack"?) is a gameplay rule that belongs to the rework that would create the combination. If the user prefers to close it now, A is the consistent choice because follow-up attacks already ignore pre-attack deaths.

**Verification (C):** none. (A: Core test with Pull + Kill Chain where a pull hazard kills a second enemy; no extra attack is granted.)

### R10. Reaction-derived follow-ups can consume Hunter's Marks

**Facts.** `C/Application/Combat/AttackDeliveryExecutor.cs:39-45` runs Kill Chain and follow-up attacks for reaction plans too. Their hits use `CombatDamageKind.ExtraAttack`, which `C/Domain/Traits/Rules/HuntersMarkTraitRule.cs:29-32` accepts. The approved G-M rule excludes reaction attacks. Reaction attacks come only from `BossRoarCalculator` (`CombatAttackCalculator.Reaction`), and follow-ups are produced by the reacting unit's own trait effects (`FollowUpAttackExecutor`: `PlanAttackResolved`, `PlanKillChainActivated`, `PlanDisplacementResolved`). The only Roar unit, `Unit_Stage1Boss`, has Necromancy (deferred), Roar and Knockback, none of which produce follow-ups, so the case is unreachable with current content.

**Options**

| Option | Change |
|---|---|
| A | Give derived attacks the damage kind of the initiating attack so reaction-derived hits never touch marks. |
| B | Do not run Kill Chain or follow-ups for reaction plans. Changes existing reaction behavior. |
| C | Record only; resolve when such content exists. |

**Decision (user, 2026-10-26): C.** The case is unreachable with current content; A requires origin plumbing through follow-up execution. Recorded in section 5.1.

### R11. A damage-free Pull clears the attacker's Intimidation

**Facts.** `C/Application/Combat/CombatAttackCalculator.cs:144` calls `CombatDebuffApplication.CompleteAttack`, which clears the attacker's own Intimidation (`C/Application/Combat/CombatDebuffApplication.cs:39-43`) after any accepted ordinary attack, including a Pull on an ally that deals no damage.

**Options**

| Option | Change |
|---|---|
| A | Clear only when the attack delivered at least one direct hit. |
| B | Keep: any accepted ordinary attack completes the debuff. |

**Decision (user delegated to the recommendation, 2026-10-26): B.** The action is an accepted attack that consumed AP and the unit's action; "expires on attack completion" stays a simple rule. This also matches the [player-turn design](2026-10-23-player-turn-shared-actioncost-design.md) lifetime table: Intimidation's attack reduction "clears when its holder completes the relevant attack as before". Add a Korean comment at `CombatDebuffApplication.CompleteAttack` stating that any accepted ordinary attack, including a damage-free Pull, completes the expiry.

### R12. `MovementFailureReason` values were renumbered (Fix)

**Facts.** Removing `NotActiveUnit` (6) and `NoPendingMovement` (18) from `C/Contracts/Results/MovementFailureReason.cs` shifted later implicit values: `UnsupportedMoveMethod` now reuses 6, `StaleContext` became 17 and `VoluntaryMoveDisabled` 18. `AttackFailureReason` in the same change preserved its values with "do not reuse" comments. No serialized consumer of the numeric value was found.

**Fix.** Give explicit historical values (`UnsupportedMoveMethod = 7` through `RepeatedCell = 17`, `StaleContext = 19`, `VoluntaryMoveDisabled = 20`) and add the same "do not reuse 6 and 18" comment.

**Verification:** Core test pinning the numeric values, following the existing enum-value tests.

### R13. Very large Hunter's Mark percent throws mid-combat

**Facts.** `HuntersMarkTraitRule.EvaluateHit` uses `CategoryMatchupDamageCalculator.Apply` with checked arithmetic. A very large percent overflows during combat. `C/Application/BootstrapModels/UnitCatalog.cs` guards Focus against overflow but not Hunter's Mark. The approved G-M rule says checked arithmetic rejects an unrepresentable result.

**Options**

| Option | Change |
|---|---|
| A | Add an authoring upper bound (for example 1000%) validated in the trait data and catalog. |
| B | Keep the approved behavior; no change. |

**Decision (user delegated to the recommendation, 2026-10-26): B, no change.** The behavior is approved and only reachable with absurd authoring values. A remains available if the planner later wants an explicit authoring limit.

### R14. Night end no longer clears thorn fields; defense reduction has no expiry (Record)

**Facts.** The old night end emptied thorn fields and advanced their lifetimes every unit turn. `C/Application/Combat/CombatTurnCalculator.cs:128-129` (`EndEnemyPhase`) now keeps `ThornFields`, and `CombatStatusLifecycle.OnTurnEnded` (the only defense-reduction expiry) has no caller. No production path currently creates thorn fields or defense reduction.

**Record.** Reactivating ThornField requires defining field lifetime and defense-reduction expiry in player/enemy turn units, and clearing fields at night end. Add this as a precondition next to `DeferredCombatFeatures` so it is not lost. No code change now.

### R15. No availability information outside combat

**Facts**

- In combat, `C/Application/Combat/Traits/TraitEffectProvider.cs:148-150` sets `IsAvailable`/`UnavailableReason` on trait statuses.
- Out of combat, `OwnedCharacterStatsSnapshot` (built in `C/Application/Roster/OwnedCharacterStatsCalculator.cs`, served by `CS/CharacterSystem/CharacterStatsQueryService.cs`) exposes `Stats.TraitKeys` and four description snapshots (`ThornFieldDescription`, `HuntersMarkDescription`, `PullDescription`, `ImpactBoltDescription`), none with availability. Lobby, deck and shop cards therefore present deferred traits as active: Focus (`Unit_Sniper`) and ThornField (`Unit_CactusCannon`). (Enemy units are not owned, so their deferred traits only appear in combat, where availability is already published.)
- Items: Core publishes only `ItemInventorySnapshot` (item key → count) through `ItemInventoryBuffer`; there is **no Core item definition read model** (correction to the first draft of this item). Item names and icons come from View data. An equip of Rope is rejected with the reason `DeferredUnitTurnTiming` (`C/Application/Combat/Items/ItemEquipmentCalculator.cs:37`) without consuming stock.

**Options**

| Option | Change |
|---|---|
| A | Traits: publish per-trait availability in `OwnedCharacterStatsSnapshot`, sourced from `DeferredCombatFeatures` (the same source and reason strings as `TraitEffectProvider`). Items: no new read model; the View shows the existing equip rejection reason. |
| A+ | A, plus a new Core item-availability read surface for the pack. Adds a public contract that does not exist today. |
| B | Leave it; the View cannot show deferral outside combat. |

**Decision (user delegated to the recommendation, 2026-10-26): A.** The trait part extends an existing read model; adding an item read surface only to grey out Rope is not justified while items are also expected to change. Scheduled in P3 because it changes a contract the View consumes (section 7).

**Verification:** Core test that, for every trait key, the out-of-combat availability equals the in-combat `TraitStatusSnapshot` availability for the same unit.

## 5. Cleanup

| ID | Target | Action |
|---|---|---|
| C1 | `C/Application/Combat/ThornFieldLifetimeCalculator.cs`, `CombatStatusLifecycle.cs`, `InitialTornadoPlacementCalculator.cs`, `C/Application/Artifact/StageSoulService.cs` (no callers) | Delete with metas, unless R14's record says to keep a file as the reactivation starting point. |
| C2 | `DeferredCombatFeatures.HasPriorityComponent` (always false, no callers) | Delete. |
| C3 | `EnemyAi/BossTurnCalculator.cs`, `RegenerationTurnCalculator.cs` (tests only) | Delete with their test cases, or keep explicitly as deferred-feature code with a comment. Recommendation: delete; R1 option C would be designed fresh. |
| C4 | `EnvironmentTerrainRules.EdgeActionCost` (always returns `baseCost`), still called by `EnemyMovementPathfinder` | Replace calls with the plain step count and delete. |
| C5 | Dead parameters `countdown` (`CombatCommandCalculator.Finish`, `MovementProjection.Complete`, `PlanDeaths`) and `fields` (`CombatTurnCalculator.State`); Necromancy `firstActionDelay` tooltip "행동 타임라인 시간 단위" (`D/Unit/Trait/UnitTraitAuthoring.cs:93-94`) | Remove parameters; mark `firstActionDelay` as unused while Necromancy is deferred. Keep the serialized field to avoid asset churn. |
| C6 | `C/Contracts/Commands/Traits/NecromancyTraitSpec.cs:7` comment refers to `BossTurnCalculator` and "자기 턴" | Update after C3. |

Deferred definitions (trait specs, `RopeItemRule`, `SoulAltarRule`, tornado/railway/flag specs) stay as required by D7.

### 5.1 Recorded preconditions for later rework

These are not implemented here. Whoever reactivates the feature or designs the enhancement/trait rework must decide them first. In implementation, a one-line Korean comment pointing to this section is placed at the named code location so the condition is not lost.

| From | Precondition | Code location for the pointer comment |
|---|---|---|
| R9 | If a unit can have both Pull and Kill Chain, decide whether a hazard death caused by the pull counts as a kill of that attack. Current code counts it; follow-up attacks do not. | `C/Application/Combat/KillChainAttackCalculator.cs` (kill count) |
| R10 | If a reacting unit (Roar or a future reaction) gains Kill Chain or follow-up traits, decide whether reaction-derived hits may consume or apply Hunter's Marks. G-M excludes reaction attacks, but derived hits currently use `ExtraAttack`, which marks accept. | `C/Domain/Traits/Rules/HuntersMarkTraitRule.cs` (`CanApplyMark`) |
| R14 | Before reactivating ThornField, define field lifetime and defense-reduction expiry in player/enemy turn units, and clear fields at night end. | `C/Application/Combat/DeferredCombatFeatures.cs` |
| R1 | Necromancy cadence in the shared enemy phase is decided with the planner before summons return. | `C/Application/Combat/DeferredCombatFeatures.cs` |

## 6. Principles check

- **SSOT.** R2 and R5 make `DeferredCombatFeatures` (traits/objects) and `ItemRuleRegistry` (items) the only availability sources. R7 (either option) leaves one source per category action value and one read path for attack cost.
- **SOLID.** Each fix stays at its feature owner: offer admission in the shop planner, combination checks in the profile, recipient choice in damage indexing, AI fallback in the AI service. No new service, interface or Framework change is needed.
- **FW contracts.** No change to service registration, Factory, ServiceKeys, SystemGroups or asmdefs. LifeCycle Jobs are unchanged; R1 removes the fault instead of adding error handling in Jobs.
- **Change independence.** R4, R12 and C-items can land without any decision. R7B is local to Core attack-cost reads; R7A would touch the DB data, SO, adapter and one asset together. R15 changes View-facing contracts and is isolated in its own phase.

## 7. View handoff (no View edits in this work)

| Item | Needed View change |
|---|---|
| Existing | Impact Bolt has no `TraitVisual` entry, and `UnitCardUI.cs:265` does not pass `ImpactBoltDescription` to the card. |
| R5 A | None. The View already skips inactive object sources (checked read-only). |
| R15 A | Show trait availability from the new `OwnedCharacterStatsSnapshot` field on lobby, deck and shop cards. For Rope, show the existing equip rejection reason (`DeferredUnitTurnTiming`); no new item field is provided. |

## 8. Proposed phases

| Phase | Items | Entry condition |
|---|---|---|
| P1 | R1, R2, R3 (deletion, with Q-R3b), R4, R5, R6 (tooltip), R7, R8, R12, C1–C6 | This document accepted. No further gameplay decision needed. |
| P2 | Comments only: R1, R9, R10 and R14 pointer comments (section 5.1) and the R11 comment. R13 needs no change. | P1 reviewed. |
| P3 | R15 | P2 reviewed; View owner informed of the contract change. |

Each phase stops for review. Verification uses `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` through the Unity Test Runner. Framework and View tests are not run or migrated.

## 9. Decisions

| ID | Question | Decision |
|---|---|---|
| Q-R1 | How should the Stage 1 boss work without summons? | Decided: A (2026-10-26); Necromancy timing later with the planner |
| Q-R2 | How are enhancements with deferred traits handled in the shop? | Decided: A (2026-10-26) |
| Q-R3 | What happens to the Precision Shot enhancement? | Decided: delete the enhancement (2026-10-26) |
| Q-R3b | Keep or delete the Precision Shot trait code? | A, keep until the enhancement rework (recommendation, 2026-10-26) |
| Q-R5 | How are deferred flags placed? | Decided: A (2026-10-26) |
| Q-R6 | What are the valid category movement ranges? | Decided: B with SO tooltips (2026-10-26) |
| Q-R7a | Where is category attack cost authored? | Decided: B (2026-10-26) |
| Q-R7b | Do enemies keep category movement allowances? | Already decided on 2026-10-25 (yes); inform the planner |
| Q-R8 | What does an enemy do after a rejected attack? | Decided: B (2026-10-26) |
| Q-R9 | Do deaths during a pull count for Kill Chain? | Decided: C, record (2026-10-26) |
| Q-R10 | Reaction-derived follow-ups and marks | Decided: C, record (2026-10-26) |
| Q-R11 | Does a damage-free Pull clear Intimidation? | B, keep and comment (recommendation, 2026-10-26) |
| Q-R13 | Hunter's Mark percent upper bound | B, no change (recommendation, 2026-10-26) |
| Q-R15 | Publish availability outside combat? | A, traits only; items use the existing rejection reason (recommendation, 2026-10-26) |
